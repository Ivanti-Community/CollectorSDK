using Collector.SDK.ActiveDirectory.DataModels;
using Collector.SDK.Converters;
using Collector.SDK.DataModel;
using System.Collections.Generic;

namespace Collector.SDK.ActiveDirectory.Converters
{
    public class DeviceConverter : AbstractConverter
    {
        public override Dictionary<string, object> Convert(KeyValuePair<string, object> point, IEntityCollection data)
        {
            var result = new Dictionary<string, object>();
            var device = new Device();
            if (point.Key.ToLower() == "location")
            {
                device.Location = (Location)point.Value;
            }
            foreach (var key in data.Entities.Keys)
            {
                if (key.ToLower() == "guid")
                {
                    device.Guid = (string) data.Entities[key];
                }
            }
            result.Add(device.Guid, device);
            return result;
        }
    }
}
