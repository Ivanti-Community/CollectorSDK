using Collector.SDK.Collectors;
using Collector.SDK.DataModel;
using Collector.SDK.Logging;
using Collector.SDK.Readers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collector.SDK.ActiveDirectory.Readers
{
    public class ActiveDirectoryReader : AbstractReader
    {
        private readonly ILogger _logger;

        public ActiveDirectoryReader(ICollector collector, ILogger logger) : base(collector)
        {
            _logger = logger;
        }
        public override void Dispose()
        {
            // nada
        }

        public override async Task Read(Dictionary<string, string> properties)
        {
            SearchResultCollection sResults = null;

            try
            {
                //modify this line to include your domain name
                var uri = EndPointConfig.Properties["URI"];
                //init a directory entry
                var dEntry = new DirectoryEntry(uri, EndPointConfig.User, EndPointConfig.Password, AuthenticationTypes.ServerBind);
                if (dEntry.NativeObject == null)
                {
                    _logger.Error("Bind with admin failed!");
                    return;
                }
                //init a directory searcher
                var dSearcher = new DirectorySearcher(dEntry);

                //This line applies a filter to the search specifying a username to search for
                //modify this line to specify a user name. if you want to search for all
                //users who start with k - set SearchString to "k"
                dSearcher.Filter = "(uid=riemann)"; // EndPointConfig.Properties["Filter"];
                dSearcher.PropertiesToLoad.Add("*");

                //perform search on active directory
                sResults = dSearcher.FindAll();

                //loop through results of search
                foreach (SearchResult searchResult in sResults)
                {
                    var entity = new EntityCollection();
                    var entry = searchResult.GetDirectoryEntry();
                    foreach (string name in entry.Properties.PropertyNames)
                    {
                        entity.Entities.Add(name, entry.Properties[name]);
                        _logger.Info("{0} - {1}", name, entry.Properties[name].Value);
                    }
                    Data.Add(entity);
                    // break;  // For Testing...
                    _logger.Info("***** Reading Next Entity ******");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {

                // dispose of objects used
                if (sResults != null)
                    sResults.Dispose();
            }
            _logger.Info("***** Signalling Handler ******");
            await SignalHandler(new Dictionary<string, string>());
        }
    }
}
