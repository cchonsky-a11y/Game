using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>All mutable state of the single P0 region (Rome) and the inventor.</summary>
    public sealed class World
    {
        public List<DomainState> Domains { get; } = new List<DomainState>();

        public DomainState this[Domain d] => Domains.First(x => x.Domain == d);
    }

    /// <summary>Outcome of a player command: success flag and a message for the player.</summary>
    public sealed class CommandResult
    {
        public bool Ok { get; }
        public string Message { get; }

        private CommandResult(bool ok, string message)
        {
            Ok = ok;
            Message = message;
        }

        public static CommandResult Success(string message) => new CommandResult(true, message);
        public static CommandResult Fail(string message) => new CommandResult(false, message);
    }
}
