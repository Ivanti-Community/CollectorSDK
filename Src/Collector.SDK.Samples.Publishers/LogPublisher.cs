// ***************************************************************
// Copyright 2018 Ivanti Inc. All rights reserved.
// ***************************************************************
using Collector.SDK.Publishers;
using Collector.SDK.Configuration;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using Collector.SDK.Collectors;
using Collector.SDK.Logging;
using Collector.SDK.Transformers;
using System.Threading.Tasks;

namespace Collector.SDK.Samples.Publishers
{
    public class LogPublisher : AbstractPublisher
    {
        private readonly ILogger _logger;
        private ICollector _collector;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        public LogPublisher(ILogger logger, ICollector collector) : base(collector)
        {
            _logger = logger;
            _collector = collector;
        }

        public override async Task Publish(string senderId, List<object> data, Dictionary<string, string> context)
        {
            if (data.Count > 0)
            {
                var logFileName = context[CollectorConstants.KEY_FILENAME];

                var payload = JsonConvert.SerializeObject(data);
                var message = string.Format(CultureInfo.InvariantCulture,
                    "\"SenderId\":{0}, \"Payload\":{1}, \"Log\":{2}", Id, payload, logFileName);

                // _logger.Info(message);
                var index = logFileName.LastIndexOf("\\");
                if (index > 0)
                {
                    logFileName = logFileName.Substring(index);
                }
                var folder = EndPointConfig.Properties[CollectorConstants.KEY_FOLDER];
                var fileName = string.Format(CultureInfo.InvariantCulture, "{0}{1}.txt", folder, logFileName);
                using (StreamWriter outputFile = new StreamWriter(fileName))
                {
                    await outputFile.WriteLineAsync(message);
                    outputFile.Close();
                }
            }
        }
    }
}
