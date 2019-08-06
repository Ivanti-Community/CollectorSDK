using System.Collections.Generic;
using Collector.SDK.ActiveDirectory.DataModels;
using Collector.SDK.Converters;
using Collector.SDK.DataModel;

namespace Collector.SDK.ActiveDirectory.Converters
{
    public class LocationConverter : AbstractConverter
    {
        public override Dictionary<string, object> Convert(KeyValuePair<string, object> point, IEntityCollection dataRow)
        {
            var result = new Dictionary<string, object>();
            var location = new Location();
            foreach (var key in dataRow.Entities.Keys)
            {
                if (key.ToLower() == "city")
                {
                    location.City = (string) dataRow.Entities[key];
                }
                else if (key.ToLower() == "state")
                {
                    location.State = (string)dataRow.Entities[key];
                }
            }
            result.Add("location", location);
            return result;
        }
    }
}
