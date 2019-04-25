// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************
using System.IO;
using System.Threading;
using Collector.SDK.Collectors;
using Collector.SDK.Configuration;
using Collector.SDK.Samples.Collectors;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Collector.SDK.Tests
{
    [TestClass]
    public class FolderMonitorTest
    {
        [TestInitialize]
        public void Init()
        {
            ComponentRegistration.Reset();
        }

        [TestMethod]
        [Owner("roy.morris@ivanti.com")]
        [TestCategory("Unit")]
        public void FolderMonitorCollector_Success()
        {
            using (StreamReader file = File.OpenText(@"Configuration\collector-config-file-monitor.json"))
            {
                JsonSerializer serializer = new JsonSerializer();

                var collectorConfig = (CollectorConfiguration)serializer.Deserialize(file, typeof(CollectorConfiguration));
                var collector = CollectorFactory.CreateCollector(collectorConfig) as FolderMonitorCollector;
                collector.Run().Wait();

                collector.GetStackCount().Should().Be(0);
            }
        }
    }
}
