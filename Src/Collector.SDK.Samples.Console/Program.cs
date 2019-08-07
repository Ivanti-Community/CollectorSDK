// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************
using Collector.SDK.Collectors;
using Collector.SDK.Configuration;
using Newtonsoft.Json;
using System.IO;

namespace Collector.SDK.Samples.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //using (StreamReader file = File.OpenText(@"ad-collector-config.json"))
            using (StreamReader file = File.OpenText(@"kafka-collector-config.json"))
            {
                var serializer = new JsonSerializer();
                // convert from json to the collector configuration object
                var collectorConfig = (CollectorConfiguration)serializer.Deserialize(file, typeof(CollectorConfiguration));
                // instantiate the collector
                var collector = CollectorFactory.CreateCollector(collectorConfig);
                // Run it...
                collector.Run();
                System.Console.WriteLine("Hit any key to exit...");
                System.Console.ReadKey();
            }
        }
    }
}
