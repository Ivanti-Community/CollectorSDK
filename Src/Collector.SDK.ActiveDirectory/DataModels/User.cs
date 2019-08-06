using System;

namespace Collector.SDK.ActiveDirectory.DataModels
{
    public class User
    {
        public string CommonName { get; set; }
        public string DistinguishedName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime Changed { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ChangedUtc { get; set; }
    }
}
