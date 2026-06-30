using System;
using System.Collections.Generic;
using BS.CAD.Tools.Engine.Context;

namespace BS.CAD.Tools.Engine.Commands
{
    /// <summary>
    /// Minimal in-process command registry for the engine layer.
    /// It does not replace AutoCAD CommandMethod registration in this phase.
    /// </summary>
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, CadCommandHandler> _commands = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string commandName, CadCommandHandler handler)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentException("Command name is required.", nameof(commandName));

            _commands[commandName] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public bool TryGet(string commandName, out CadCommandHandler? handler)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                handler = null;
                return false;
            }

            return _commands.TryGetValue(commandName, out handler);
        }

        public IReadOnlyCollection<string> Names => _commands.Keys;
    }
}
