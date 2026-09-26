using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The time machine repair track (decided 2026-09-28; P0 version, to be built out later): three systems of three
    /// small steps each. Every step needs something from Rome (a membership, a finished project) or, for a system's
    /// first step, more gold instead. All 9 are required before the machine can jump.
    /// </summary>
    public sealed partial class Simulation
    {
        public static readonly string[] MachineSystems = { "coil", "coolant", "chronometer" };

        public int MachineStepsTotal => Data.Content.MachineSteps.Count;
        public int MachineStepsDone => World.MachineDone.Count(id => Data.Content.MachineSteps.Any(s => s.Id == id));
        public int MachineUpgradesDone => World.MachineDone.Count(id => Data.Content.MachineUpgrades.Any(s => s.Id == id));

        /// <summary>
        /// How far the machine can carry you (decided 2026-09-28): from the required repairs (25–40 years), longer with
        /// every optional upgrade (+5) and with time spent in the era (+5 per full 5 years beyond the first 5, up to +10),
        /// capped at 60. The distance is drawn at departure, in 5-year steps.
        /// </summary>
        public (int Min, int Max) JumpRange()
        {
            double yearsInEra = Now.YearFraction - T.GetInt("time.startYear");
            int time = (int)Math.Min(T.Get("jump.range.timeBonusMax"),
                T.Get("jump.range.perFiveYears") * Math.Max(0, Math.Floor((yearsInEra - T.Get("jump.range.freeYears")) / 5)));
            int bonus = (int)(T.Get("jump.range.perUpgrade") * MachineUpgradesDone) + time;
            int cap = T.GetInt("jump.range.maxYears");
            int max = Math.Min(cap, T.GetInt("jump.range.baseMax") + bonus);
            int min = Math.Min(max, T.GetInt("jump.range.baseMin") + bonus);
            return (min, max);
        }

        /// <summary>Draws the jump's distance from the seeded generator, in 5-year steps within the range.</summary>
        internal int DrawJumpYears()
        {
            var (min, max) = JumpRange();
            int step = T.GetInt("jump.range.stepYears");
            int options = (max - min) / step + 1;
            return min + step * Rng.NextInt(0, options);
        }

        public string JumpRangeText()
        {
            var (min, max) = JumpRange();
            return (min == max ? min + " years" : min + "–" + max + " years") + " (repairs, " + MachineUpgradesDone + " of " +
                   Data.Content.MachineUpgrades.Count + " upgrades, and your time in this era)";
        }

        /// <summary>Starts an optional machine upgrade (lens, contacts, flywheel): a longer jump.</summary>
        public CommandResult Upgrade(string id)
        {
            var step = Data.Content.MachineUpgrades.FirstOrDefault(u => u.Id == (id ?? "").Trim().ToLowerInvariant());
            if (step == null) return CommandResult.Fail("Upgrade what? " + string.Join(", ", Data.Content.MachineUpgrades.Select(u => u.Id)) + ".");
            if (World.MachineDone.Contains(step.Id)) return CommandResult.Fail(step.Name + " is done.");
            if (World.ActiveMachineSteps.Any(a => a.Def.Id == step.Id)) return CommandResult.Fail(step.Name + " is already under way.");
            return BeginMachineStep(step);
        }
        public bool MachineReady => MachineStepsDone >= MachineStepsTotal;

        /// <summary>The next step of a system that isn't done or under way, or null if the system is finished or busy.</summary>
        public MachineStepDef? NextMachineStep(string system)
        {
            if (World.ActiveMachineSteps.Any(a => a.Def.System == system)) return null;
            return Data.Content.MachineSteps.FirstOrDefault(s => s.System == system && !World.MachineDone.Contains(s.Id));
        }

        public bool MachineRequirementMet(MachineStepDef step)
        {
            bool backed(params string[] ids) => ids.Any(id => World.Institution(id).Backed);
            switch (step.Requirement)
            {
                case null: return true;
                case "tradeMember": return backed("guild", "bank");
                case "medicineWork":
                    return new[] { "fountain", "physician", "quarantine", "midwives" }.Any(World.CompletedProjects.Contains) || backed("circle", "sanctuary");
                case "factionMember": return backed("faction", "junian");
                case "workshop": return World.CompletedProjects.Contains("workshop");
                default: throw new InvalidOperationException("Unknown machine requirement: " + step.Requirement);
            }
        }

        public string MachineRequirementText(MachineStepDef step)
        {
            switch (step.Requirement)
            {
                case "tradeMember": return "membership in the guild or the bank";
                case "medicineWork": return "a finished Medicine project (fountain, physician, quarantine or midwives) or membership in the Circle or the sanctuary";
                case "factionMember": return "membership in a senate faction";
                case "workshop": return "the smith's workshop";
                default: return "";
            }
        }

        /// <summary>Gold for a step: its price, or the alternative price if Rome can't give you what it needs.</summary>
        public int MachineStepGold(MachineStepDef step) => (int)Math.Round((MachineRequirementMet(step) ? step.Gold : step.AltGold) * World.PriceLevel);

        /// <summary>Starts the next step of a machine system (coil, coolant or chronometer).</summary>
        public CommandResult Repair(string systemText)
        {
            string text = (systemText ?? "").Trim().ToLowerInvariant();
            string? system = MachineSystems.FirstOrDefault(x => text.Length >= 3 && x.StartsWith(text, StringComparison.Ordinal));
            if (system == null) return CommandResult.Fail("Repair what? coil, coolant or chronometer.");
            if (World.ActiveMachineSteps.Any(a => a.Def.System == system)) return CommandResult.Fail("You are already working on the " + system + ".");
            var step = NextMachineStep(system);
            if (step == null) return CommandResult.Fail("The " + system + " is finished.");
            return BeginMachineStep(step);
        }

        private CommandResult BeginMachineStep(MachineStepDef step)
        {
            int gold = MachineStepGold(step);
            if (World.Gold < gold) return CommandResult.Fail(step.Name + " costs " + gold + " gold; you have " + F(World.Gold) + ".");
            var attention = CheckAttention(step.AttentionPerTurn);
            if (attention != null) return attention;
            SpendAttention(step.AttentionPerTurn);
            double before = World.Gold;
            SpendGold(gold);
            bool viaRome = MachineRequirementMet(step);
            var e = Record("machine.start", step.Id, null, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                "You begin: " + step.Name + (step.Requirement != null && !viaRome ? " (without help from Rome, you " + step.AltText + ")" : "") +
                " (" + gold + " gold, " + step.Turns + " turn" + (step.Turns == 1 ? "" : "s") + ").");
            World.ActiveMachineSteps.Add(new ActiveMachineStep(step, e.Id) { WithoutRome = !viaRome });
            return CommandResult.Success("Started: " + step.Name + ".");
        }

        private void ProgressMachine()
        {
            foreach (var a in World.ActiveMachineSteps.ToList())
            {
                a.TurnsRemaining--;
                if (a.TurnsRemaining > 0) continue;
                World.ActiveMachineSteps.Remove(a);
                World.MachineDone.Add(a.Def.Id);
                bool upgrade = Data.Content.MachineUpgrades.Any(u => u.Id == a.Def.Id);
                Record("machine.step", a.Def.Id, new[] { a.StartEventId }, new[] { "player" },
                    new[] { upgrade ? new Effect("machine.upgrades", MachineUpgradesDone - 1, MachineUpgradesDone)
                                    : new Effect("machine.steps", MachineStepsDone - 1, MachineStepsDone) },
                    (a.WithoutRome ? a.Def.AltDoneText : a.Def.Text) + (upgrade
                        ? " (Upgrade: the machine's range is now " + JumpRangeText() + ".)"
                        : " (Machine: " + MachineStepsDone + "/" + MachineStepsTotal + " steps" + (MachineReady ? "; it can carry you now." : ".") + ")"));
            }
        }

        /// <summary>Attention pledged to machine steps already under way.</summary>
        private int ReservedMachineAttention() =>
            World.ActiveMachineSteps.Where(a => a.TurnsRemaining < a.Def.Turns).Sum(a => a.Def.AttentionPerTurn);

        /// <summary>What still stands between you and the jump.</summary>
        public IEnumerable<string> MachineStatus()
        {
            foreach (var system in MachineSystems)
            {
                var steps = Data.Content.MachineSteps.Where(s => s.System == system).ToList();
                int done = steps.Count(s => World.MachineDone.Contains(s.Id));
                var active = World.ActiveMachineSteps.FirstOrDefault(a => a.Def.System == system);
                var next = NextMachineStep(system);
                string line = Cap(system) + ": " + done + "/" + steps.Count;
                if (active != null) line += " — under way: " + active.Def.Name + " (" + active.TurnsRemaining + " turn(s) left)";
                else if (next != null)
                    line += " — next: " + next.Name + " (" + MachineStepGold(next) + " gold, " + next.AttentionPerTurn + " Attention" +
                            (next.Turns > 1 ? " a turn for " + next.Turns + " turns" : "") + ")" +
                            (next.Requirement == null ? "" : MachineRequirementMet(next) ? "; Rome helps: you have " + MachineRequirementText(next)
                                : "; with " + MachineRequirementText(next) + " it would cost " + next.Gold + " gold");
                else line += " — done";
                yield return line;
            }
            foreach (var u in Data.Content.MachineUpgrades)
            {
                var active = World.ActiveMachineSteps.FirstOrDefault(a => a.Def.Id == u.Id);
                yield return "Upgrade " + u.Id + ": " + u.Name + (World.MachineDone.Contains(u.Id) ? " — done"
                    : active != null ? " — under way (" + active.TurnsRemaining + " turn(s) left)"
                    : " — " + MachineStepGold(u) + " gold, " + u.AttentionPerTurn + " Attention a turn for " + u.Turns + " turns" +
                      (MachineRequirementMet(u) ? "" : "; with " + MachineRequirementText(u) + " it would cost " + u.Gold + " gold"));
            }
            yield return "Jump range: " + JumpRangeText() + ".";
        }
    }
}
