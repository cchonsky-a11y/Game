using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P1 player-facing access for the institutions not yet on an invitation path (2026-10-04). The Senate factions take
    /// clients through a patron's introduction, which isn't built yet, so they take no new members in play; the Tiber Island
    /// sanctuary takes gifts and names its givers benefactors; the banking house sells real financial shares. Internally the
    /// P0 stake still records standing for the systems that read it (legacy compatibility).
    /// </summary>
    public sealed partial class Simulation
    {
        public bool PatronageOnly(Institution i) => i.Def.Access == "patronage";

        /// <summary>
        /// Where you stand with a senator's following (P1 polish pass, 2026-10-04): a patron's introduction makes you a client
        /// who attends the morning salutatio, not a member. No place in its following, office or policy comes of it yet.
        /// </summary>
        public string PatronageStanding(Institution i)
        {
            if (i.Stake > 0) return "you are in its following";
            var a = World.AccessTo(i.Key);
            return a.Stage >= InstitutionAccessStage.KnowsMember
                ? "you stand at the senator's salutatio as a client, introduced to his steward " + a.KnownMemberId + "; that is not a place in his following"
                : "it takes clients through a patron's introduction: a merchant or physician the senator's house already trusts";
        }
        public bool TakesGifts(Institution i) => i.Def.Access == "gifts";

        /// <summary>A gift to an institution that takes gifts (1 Attention-priced step of the legacy stake): you become a benefactor.</summary>
        public CommandResult Give(string id)
        {
            var i = FindInstitution(id);
            if (i == null || !TakesGifts(i)) return CommandResult.Fail("They don't take gifts in that way.");
            int stakeBefore = StakePercent(i);
            var r = Buy(i.Key, 1);
            if (!r.Ok) return r;
            return CommandResult.Success(stakeBefore == 0
                ? "You give to " + i.Def.Name + ". " + i.Leader + " names you among its benefactors; your name goes on the board by the door."
                : "You give to " + i.Def.Name + " again. " + i.Leader + " thanks you by name at the next rite.");
        }

        /// <summary>The cost of the next gift (the legacy price of one more step of standing).</summary>
        public double GiftCost(Institution i) => BuyCost(i, 1);
    }
}
