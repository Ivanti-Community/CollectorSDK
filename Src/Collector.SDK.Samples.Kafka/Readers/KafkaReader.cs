// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************
using Collector.SDK.Collectors;
using Collector.SDK.Logging;
using Collector.SDK.Readers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Collector.SDK.DataModel;
using Newtonsoft.Json;

namespace Collector.SDK.Kafka.Readers
{
    public class KafkaReader : AbstractReader
    {
        private readonly ILogger _logger;

        public KafkaReader(ICollector collector, ILogger logger) : base(collector)
        {
            _logger = logger;
        }

        public override void Dispose()
        {
            // nada
        }

        public override async Task Read(Dictionary<string, string> properties)
        {
            var groupId = EndPointConfig.Properties["GroupId"];
            var servers = EndPointConfig.Properties["BootstrapServers"];
            var topic = EndPointConfig.Properties["Topic"];
            var topics = new List<string>() { topic };

            var config = new ConsumerConfig
            {
                BootstrapServers = servers,
                GroupId = groupId,
                EnableAutoCommit = false,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true
            };

            const int commitPeriod = 5;

            // Note: If a key or value deserializer is not set (as is the case below), the 
            // deserializer corresponding to the appropriate type from Confluent.Kafka.Deserializers
            // will be used automatically (where available). The default deserializer for string
            // is UTF8. The default deserializer for Ignore returns null for all input data
            // (including non-null data).
            using (var consumer = new ConsumerBuilder<Ignore, string>(config)
                // Note: All handlers are called on the main .Consume thread.
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .SetStatisticsHandler((_, json) => Console.WriteLine($"Statistics: {json}"))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                   _logger.Info($"Assigned partitions: [{0}]", string.Join(", ", partitions));
                // possibly manually specify start offsets or override the partition assignment provided by
                // the consumer group by returning a list of topic/partition/offsets to assign to, e.g.:
                // 
                // return partitions.Select(tp => new TopicPartitionOffset(tp, externalOffsets[tp]));
            })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.Info($"Revoking assignment: [{0}]", string.Join(", ", partitions));
                })
                .Build())
            {
                consumer.Subscribe(topics);

                try
                {
                    while (!Token.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(Token);

                            if (consumeResult.IsPartitionEOF)
                            {
                                _logger.Info($"Reached end of topic {0}, partition {1}, offset {2}.",
                                    consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
                                continue;
                            }

                            _logger.Info($"Received message at {0}: {1}", consumeResult.TopicPartitionOffset, consumeResult.Value);

                            var nameValues = (Dictionary<string, string>)JsonConvert.DeserializeObject(consumeResult.Value, typeof(Dictionary<string, string>));
                            var entityCollection = new EntityCollection();
                            foreach (var key in nameValues.Keys)
                            {
                                entityCollection.Entities.Add(key, nameValues[key]);
                            }
                            Data.Add(entityCollection);
                            await SignalHandler(new Dictionary<string, string>());

                            if (consumeResult.Offset % commitPeriod == 0)
                            {
                                // The Commit method sends a "commit offsets" request to the Kafka
                                // cluster and synchronously waits for the response. This is very
                                // slow compared to the rate at which the consumer is capable of
                                // consuming messages. A high performance application will typically
                                // commit offsets relatively infrequently and be designed handle
                                // duplicate messages in the event of failure.
                                try
                                {
                                    consumer.Commit(consumeResult);
                                }
                                catch (KafkaException e)
                                {
                                    _logger.Error($"Commit error: {0}", e.Error.Reason);
                                }
                            }
                        }
                        catch (ConsumeException e)
                        {
                            _logger.Error($"Consume error: {0}", e.Error.Reason);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.Debug("Closing consumer.");
                    consumer.Close();
                }
            }
        }
    }
}
