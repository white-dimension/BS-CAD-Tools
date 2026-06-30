using System;
using BS.CAD.Tools.Engine.Context;

namespace BS.CAD.Tools.Engine.Commands
{
    /// <summary>
    /// Engine-level execution pipeline. Current stage only centralizes dispatch;
    /// command bodies still call existing services and UI-safe workflows.
    /// </summary>
    public sealed class CommandPipeline
    {
        private readonly CommandRegistry _registry;

        public CommandPipeline(CommandRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Execute(string commandName, CadCommandContext context)
        {
            if (!_registry.TryGet(commandName, out var handler) || handler == null)
                throw new InvalidOperationException($"Engine command is not registered: {commandName}");

            handler(context ?? throw new ArgumentNullException(nameof(context)));
        }
    }
}
