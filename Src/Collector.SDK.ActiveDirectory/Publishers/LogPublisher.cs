using Collector.SDK.Collectors;
using Collector.SDK.DataModel;
using Collector.SDK.Logging;
using Collector.SDK.Publishers;
using Newtonsoft.Json;
using System.Collections.Generic;
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
                    var list = point as IEntityCollection;
                    foreach (var key in list.Entities.Keys)
                    {
                        var payload = JsonConvert.SerializeObject(list.Entities[key]);
                        _logger.Info("Entity - {0} {1}", key, payload);
                    }
                }
            });
        }
    }
}
