using Collector.SDK.ActiveDirectory.DataModels;
using Collector.SDK.Converters;
using Collector.SDK.DataModel;
using System.Collections.Generic;
using System.Reflection;

namespace Collector.SDK.ActiveDirectory.Converters
{
    public class UserConverter : AbstractConverter
    {
        public override Dictionary<string, object> Convert(KeyValuePair<string, object> point, IEntityCollection dataRow)
        {
            var result = new Dictionary<string, object>();
            var user = new User();

            foreach (var data in dataRow.Entities)
            {
                var names = ConvertLeftSide(data.Key);
                foreach (var name in names)
                {
                    var info = GetSetterMethod(user, name);
                    if (info != null)
                    {
                        info.Invoke(user, new object[] { data.Value });
                    }
                }
            }
            result.Add(user.DistinguishedName, user);
            return result;
        }

        private MethodInfo GetSetterMethod(object obj, string key)
        {
            var type = obj.GetType();
            var fields = type.GetMethods();
            foreach (var info in fields)
            {
                if (info.Name.StartsWith("set_"))
                {
                    var fieldName = info.Name.Substring("set_".Length);
                    if (fieldName.ToLower() == key.ToLower())
                    {
                        return info;
                    }
                }
            }
            return null;
        }
    }
}
