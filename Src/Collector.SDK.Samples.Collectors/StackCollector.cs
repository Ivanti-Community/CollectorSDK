using Collector.SDK.Collectors;
using Collector.SDK.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collector.SDK.Samples.Collectors
{
    public class StackCollector : AbstractCollector
    {
        private readonly ILogger _logger;

        public StackCollector(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Task HandleEvent(StateEvent state)
        {
            return Task.Run(() =>
            {
                _logger.Info("Handling event {0} {1} {2}", state.SenderId, state.State, state.ExtraInfo.ToString());
                if (state.State.Equals(CollectorConstants.STATE_PUBLISHER_DONE))
                {
                    // may want to do something here to finish up the operation.
                }
            });
        }

        public override async Task Run()
        {
            _logger.Info("Running collector {0}", Properties["ReaderId"]);
            var stack = CreateStack(Properties["ReaderId"]);
            await stack.Run(new Dictionary<string, string>());
        }
    }
}
