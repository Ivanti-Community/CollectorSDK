using Collector.SDK.Collectors;
using Collector.SDK.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collector.SDK
{
    public class ActiveDirectoryCollector : AbstractCollector
    {
        private readonly ILogger _logger;

        public ActiveDirectoryCollector(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Task HandleEvent(StateEvent state)
        {
            return Task.Run(() =>
            {
                _logger.Info("Handling event {0}", state.State);
            });
        }

        public override async Task Run()
        {
            _logger.Info("Running collector");
            var stack = CreateStack("AD-Reader");
            await stack.Run(new Dictionary<string, string>());
        }
    }
}
