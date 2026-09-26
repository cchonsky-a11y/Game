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
        public int MachineStepsDone => World.MachineDone.Count;
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
                default: return "";
            }
        }

        /// <summary>Gold for a step: its price, or the alternative price if Rome can't give you what it needs.</summary>
        public int MachineStepGold(MachineStepDef step) => MachineRequirementMet(step) ? step.Gold : step.AltGold;

        /// <summary>Starts the next step of a machine system (coil, coolant or chronometer).</summary>
        public CommandResult Repair(string systemText)
        {
            string text = (systemText ?? "").Trim().ToLowerInvariant();
            string? system = MachineSystems.FirstOrDefault(x => text.Length >= 3 && x.StartsWith(text, StringComparison.Ordinal));
            if (system == null) return CommandResult.Fail("Repair what? coil, coolant or chronometer.");
            if (World.ActiveMachineSteps.Any(a => a.Def.System == system)) return CommandResult.Fail("You are already working on the " + system + ".");
            var step = NextMachineStep(system);
            if (step == null) return CommandResult.Fail("The " + system + " is finished.");
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
            World.ActiveMachineSteps.Add(new ActiveMachineStep(step, e.Id));
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
                Record("machine.step", a.Def.Id, new[] { a.StartEventId }, new[] { "player" },
                    new[] { new Effect("machine.steps", MachineStepsDone - 1, MachineStepsDone) },
                    a.Def.Text + " (Machine: " + MachineStepsDone + "/" + MachineStepsTotal + " steps" + (MachineReady ? "; it can carry you now." : ".") + ")");
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
        }
    }
}
