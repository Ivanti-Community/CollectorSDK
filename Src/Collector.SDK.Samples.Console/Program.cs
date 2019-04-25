// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************

using Collector.SDK.Configuration;
using Newtonsoft.Json;
using System.IO;
using Collector.SDK.Collectors;

namespace Collector.SDK.Samples.Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (StreamReader file = File.OpenText(@"collector-config-file-monitor.json"))
            {
                JsonSerializer serializer = new JsonSerializer();

                var collectorConfig = (CollectorConfiguration)serializer.Deserialize(file, typeof(CollectorConfiguration));
				var collector = CollectorFactory.CreateCollector(collectorConfig);
	            collector.Run().Wait();
            }
	        System.Console.WriteLine("Hit any key to exit...");
	        System.Console.ReadKey();
		}
	}
}
