// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************
using Collector.SDK.Publishers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Collector.SDK.Collectors;
using Collector.SDK.Logging;
using System.Threading.Tasks;
using Collector.SDK.DataModel;

namespace Collector.SDK.Samples.Publishers
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
                var path = EndPointConfig.Properties["Path"];
                if (string.IsNullOrEmpty(path))
                {
                    _logger.Error("Property 'Path' is missing from the end point config properties");
                    var stateEvent = new StateEvent()
                    {
                        SenderId = Id,
                        State = CollectorConstants.STATE_PUBLISHER_ERROR,
                        ExtraInfo = "Path property is missing."
                    };
                    _collector.SignalEvent(stateEvent);
                    return;
                }

                CreateDirectory(EndPointConfig.Properties["Path"]);
                foreach (var point in data)
                {
                    var entity = point as IEntity;
                    var payload = JsonConvert.SerializeObject(entity);
                    var logEntry = string.Format("Entity : {0}", payload);
                    var fullPath = string.Format("{0}\\publisher-log.txt", path);
                    using (StreamWriter file = new StreamWriter(fullPath, File.Exists(fullPath)))
                    {
                        file.WriteLine(logEntry);
                    }
                }
            });
        }

        private void CreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            Directory.CreateDirectory(path);
            var index = path.LastIndexOf('/');
            if (index <= 0)
                return;
            path = path.Substring(0, path.Length - index);
            CreateDirectory(path);
        }
    }
}
