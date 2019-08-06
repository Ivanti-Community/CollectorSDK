using Collector.SDK.Collectors;
using Collector.SDK.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Collector.SDK.Samples.ActiveDirectory
{
    class Program
    {
        static void Main(string[] args)
        {
            using (StreamReader file = File.OpenText(@"ad-collector-config-2.json"))
            {
                var serializer = new JsonSerializer();
                // convert from json to the collector configuration object
                var collectorConfig = (CollectorConfiguration)serializer.Deserialize(file, typeof(CollectorConfiguration));
                // instantiate the collector
                var collector = CollectorFactory.CreateCollector(collectorConfig);
                // Run it...
                collector.Run();
            }
            Console.ReadKey();
        }
    }
}
