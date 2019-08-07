// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************
using Collector.SDK.Collectors;
using Collector.SDK.DataModel;
using Collector.SDK.Logging;
using Collector.SDK.Publishers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Collector.SDK.ActiveDirectory.Publishers
{
    public class LogPublisher : AbstractPublisher
    {
        private ILogger _logger;
        private ICollector _collector;

        public LogPublisher(ILogger logger, ICollector collector) : base(collector)
        {
            _logger = logger;
            _collector = collector;
        }

        public override Task Publish(string senderId, List<object> data, Dictionary<string, string> context)
        {
            return Task.Run(() => {
                foreach (var point in data)
                {
                    var entity = point as IEntity;
                    var payload = JsonConvert.SerializeObject(entity);
                    var logEntry = string.Format("Entity : {0}", payload);
                    var fullPath = string.Format("{0}\\publisher-log.txt", EndPointConfig.Properties["Path"]);
                    using (StreamWriter file = new StreamWriter(fullPath, File.Exists(fullPath)))
                    {
                        file.WriteLine(logEntry);
                    }
                }
            });
        }
    }
}
