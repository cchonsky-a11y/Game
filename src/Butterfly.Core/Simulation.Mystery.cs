using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The R-17 thread (approved design, restored 2026-10-04). Once the repaired panel shows R-17 ACTIVE, the inventor can
    /// deliberately reopen the reference channel. The answer comes as an interruption a month later: REQUEST RECEIVED,
    /// SOURCE: R-17, DO NOT JUMP, timed like the handshake before the AD 155 lock. Shut it down, keep listening or carry on;
    /// none is correct, and none stops the jump. Nothing here explains what R-17 is.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>True once the panel has shown R-17 ACTIVE and the channel hasn't been opened yet.</summary>
        public bool CanListen => World.ScenesSeen.Contains("r17-active") && !World.Flags.Contains("r17-opened");

        /// <summary>Reopen the reference channel on purpose (1 Attention). Whatever answers, answers next month.</summary>
        public CommandResult Listen()
        {
            if (!World.ScenesSeen.Contains("r17-active")) return CommandResult.Fail("There's nothing on the panel to listen to yet.");
            if (World.Flags.Contains("r17-opened")) return CommandResult.Fail("The channel is already open; whatever answers will answer.");
            var attention = CheckAttention(T.GetInt("machine.listenAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("machine.listenAttention"));
            World.Flags.Add("r17-opened");
            World.ScenePacing.Record(SceneCategory.MachineMystery);
            var e = Record("machine.listen", "machine", null, new[] { "player" }, new[] { new Effect("machine.channel", 0, 1) },
                "You open the reference channel the way you would in the lab, send the machine's own identifier, and wait. Nothing comes back tonight.");
            World.TriggeredEvents.Add("r17-warning");
            return CommandResult.Success(e.Text);
        }
    }
}
