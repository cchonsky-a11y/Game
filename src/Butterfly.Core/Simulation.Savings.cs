using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Gold across the jump (decided 2026-09-28). The machine carries a small purse; beyond it, gold can be deposited with
    /// the banking house (a little interest in gold, with the risk that the house fails or its head embezzles) or buried
    /// (no interest, with the risk that someone else finds it). Anything else left in hand is lost. What became of it is
    /// revealed on arrival, never before.
    /// </summary>
    public sealed partial class Simulation
    {
        public double CarryAurei => T.Get("savings.carryAurei");

        private double _depositSince;
        private double _depositReturned;
        private bool _depositLost, _hoardLost;

        /// <summary>Deposits gold with the banking house (a trip to the Forum): it pays a little interest, if it survives.</summary>
        public CommandResult Deposit(double aurei) => PutAway(aurei, deposit: true);

        /// <summary>Buries gold somewhere only you know: no interest, and someone may find it.</summary>
        public CommandResult Bury(double aurei) => PutAway(aurei, deposit: false);

        private CommandResult PutAway(double aurei, bool deposit)
        {
            aurei = Math.Floor(aurei);
            if (aurei < 1) return CommandResult.Fail((deposit ? "Deposit" : "Bury") + " how many aurei? You have " + AureiText(World.Aurei) + ".");
            if (aurei > World.Aurei + 1e-9) return CommandResult.Fail("You have only " + AureiText(World.Aurei) + ".");
            // Caps (decided 2026-09-28, P0-35): the bank refuses huge deposits, and a jar holds only so much.
            double cap = T.Get(deposit ? "savings.depositCapAurei" : "savings.hoardCapAurei");
            double held = deposit ? World.DepositAurei : World.HoardAurei;
            if (held + aurei > cap + 1e-9)
                return CommandResult.Fail(deposit
                    ? "The banking house won't take more than " + AureiText(cap) + " from one depositor" + (held > 0 ? " (it holds " + AureiText(held) + " of yours)" : "") + ": too much gold draws attention it doesn't want."
                    : "One jar holds " + AureiText(cap) + " at most" + (held > 0 ? " (it already holds " + AureiText(held) + ")" : "") + "; a bigger hoard can't be hidden.");
            // After an arrival there are no turns, so putting gold away before the next jump costs no Attention.
            if (!Arrived)
            {
                var attention = CheckAttention(T.GetInt("savings.attention"));
                if (attention != null) return attention;
                SpendAttention(T.GetInt("savings.attention"));
            }
            double before = World.Aurei;
            World.Aurei -= aurei;
            string key = deposit ? "savings.deposit" : "savings.hoard";
            double keptBefore = deposit ? World.DepositAurei : World.HoardAurei;
            if (deposit) { World.DepositAurei += aurei; _depositSince = Now.YearFraction; }
            else World.HoardAurei += aurei;
            Record(key, "aurei", null, new[] { "player" },
                new[] { new Effect("aurei", before, World.Aurei), new Effect(key, keptBefore, keptBefore + aurei) },
                deposit ? "You deposit " + AureiText(aurei) + " with the banking house of Octavius; a clerk writes your name in the ledger."
                        : "At night you bury " + AureiText(aurei) + " in a sealed jar, somewhere only you know.");
            return CommandResult.Success(deposit ? "Deposited " + AureiText(aurei) + " (" + AureiText(World.DepositAurei) + " with the bank)."
                                                 : "Buried " + AureiText(aurei) + " (" + AureiText(World.HoardAurei) + " in the jar).");
        }

        /// <summary>Chance a decade that the banking house loses your deposit (failure or embezzlement).</summary>
        public double DepositLossChance()
        {
            var bank = World.Institution("bank");
            double integrity = T.Get("corruption.integrity." + bank.Integrity);
            // A large deposit tempts the banker more (P0-35).
            double chance = T.Get("savings.depositLossPerDecade") * (1 - integrity) * (1 + T.Get("savings.depositRiskPerHundredAurei") * World.DepositAurei / 100);
            if (HasInfluence(bank)) chance *= 0.5;
            return Math.Min(1, chance);
        }

        /// <summary>Chance a decade that someone finds your jar: the bigger the hoard, the likelier (P0-35).</summary>
        public double HoardFoundChance() =>
            Math.Min(1, T.Get("savings.hoardFoundPerDecade") * (1 + T.Get("savings.hoardRiskPerFiftyAurei") * World.HoardAurei / 50));

        public static string RiskBand(double perDecade) => perDecade < 0.05 ? "Low" : perDecade < 0.1 ? "Medium" : "High";

        /// <summary>One decade (or half-decade) of the absence for your deposit and your hoard.</summary>
        private void SavingsDecadeStep()
        {
            if (World.DepositAurei > 0 && !_depositLost && Rng.Chance(1 - Math.Pow(1 - DepositLossChance(), StepFraction)))
            {
                _depositLost = true;
                Record("savings.deposit.lost", "aurei", null, new[] { World.Institution("bank").Leader }, null,
                    "The banking house of Octavius loses its depositors' gold.");
            }
            if (World.HoardAurei > 0 && !_hoardLost && Rng.Chance(1 - Math.Pow(1 - HoardFoundChance(), StepFraction)))
            {
                _hoardLost = true;
                Record("savings.hoard.lost", "aurei", null, new[] { "world" }, null, "Someone digs up a jar of gold.");
            }
        }

        /// <summary>At departure: the machine takes what it can carry; the rest in hand is lost.</summary>
        private void SavingsAtDeparture(Arrival arrival)
        {
            // A new absence: what befell the gold on the last one doesn't carry over to what you put away now.
            _depositLost = _hoardLost = false;
            _depositReturned = 0;
            arrival.AureiCarried = Math.Min(World.Aurei, CarryAurei);
            arrival.AureiLeft = World.Aurei - arrival.AureiCarried;
            arrival.AureiDeposited = World.DepositAurei;
            arrival.AureiBuried = World.HoardAurei;
            World.Aurei = arrival.AureiCarried;
        }

        /// <summary>On arrival: what the bank returns (with simple interest) and whether the hoard is still there.</summary>
        private void SavingsOnArrival(Arrival arrival)
        {
            if (World.DepositAurei > 0 && !_depositLost)
                _depositReturned = Math.Floor(World.DepositAurei * (1 + T.Get("savings.depositInterestPerYear") * (Now.YearFraction - _depositSince)));
            arrival.AureiDepositReturned = _depositReturned;
            arrival.AureiHoardFound = World.HoardAurei > 0 && !_hoardLost ? World.HoardAurei : 0;
            World.Aurei += arrival.AureiDepositReturned + arrival.AureiHoardFound;
            World.DepositAurei = 0;
            World.HoardAurei = 0;
        }

        /// <summary>The Discovery beat's lines about your gold.</summary>
        private IEnumerable<string> SavingsLines(Arrival arrival, Dictionary<string, string> values)
        {
            var text = Data.Content;
            var v = new Dictionary<string, string>(values)
            {
                { "carried", AureiText(arrival.AureiCarried) }, { "returned", AureiText(arrival.AureiDepositReturned) }, { "deposited", AureiText(arrival.AureiDeposited) },
                { "years", F(Math.Round(Now.YearFraction - _depositSince)) }, { "hoard", AureiText(arrival.AureiBuried) }, { "left", AureiText(arrival.AureiLeft) },
                { "lostHow", World.Institution("bank").Integrity == "venal" ? "its head fled with the depositors' gold a generation ago." : "it failed in a bad year, and its depositors were paid nothing." },
            };
            if (arrival.AureiCarried >= 1) yield return text.Template("savings.carried", v);
            if (arrival.AureiDeposited >= 1) yield return text.Template(arrival.AureiDepositReturned > 0 ? "savings.depositKept" : "savings.depositLost", v);
            if (arrival.AureiBuried >= 1) yield return text.Template(arrival.AureiHoardFound > 0 ? "savings.hoardKept" : "savings.hoardLost", v);
            if (arrival.AureiLeft >= 1) yield return text.Template("savings.left", v);
        }

        /// <summary>
        /// The jump briefing's lines about your money, in one place: what the machine carries, what is too much to carry
        /// (deposit or bury it), what the bank and the jar already hold (risk bands, never the outcome), the coin in your hands
        /// (it stays behind), and the money institutions hold (it stays with them, not in your purse).
        /// </summary>
        private IEnumerable<string> SavingsBriefing()
        {
            double carried = Math.Min(World.Aurei, CarryAurei), left = World.Aurei - carried;
            yield return "Your gold: the machine can carry " + AureiText(CarryAurei) + "; you hold " + AureiText(World.Aurei) + " in aurei" +
                         (left >= 1 ? ", so it would carry " + AureiText(carried) + "." : ", and it carries all of it.");
            if (left >= 1)
                yield return "  Too much to carry: " + AureiText(left) + ". Deposit it with the banking house (deposit <n>; up to " + AureiText(T.Get("savings.depositCapAurei")) +
                             ", it earns interest, and the house can fail) or bury it (bury <n>; a jar holds " + AureiText(T.Get("savings.hoardCapAurei")) +
                             ", no interest, and it can be found)" + (Arrived ? "" : ", 1 Attention each") + ". Whatever you don't stays behind and is lost.";
            if (World.DepositAurei >= 1)
                yield return "  With the banking house: " + AureiText(World.DepositAurei) + ", earning " + F(T.Get("savings.depositInterestPerYear") * 100) +
                             "% a year in gold; risk the house fails or embezzles: " + RiskBand(DepositLossChance()) + ".";
            if (World.HoardAurei >= 1)
                yield return "  Buried: " + AureiText(World.HoardAurei) + "; risk someone finds it: " + RiskBand(T.Get("savings.hoardFoundPerDecade")) + " (more the longer you're gone).";
            if (World.Gold >= 1 && !Arrived)
            {
                var endowable = World.Institutions.Where(Controls).Select(i => i.Key).ToList();
                yield return "  Coin in hand: " + Money(World.Gold) + ". Denarii stay behind when you leave: change them into aurei first (exchange <n> denarii) to carry, deposit or bury them, or spend them" +
                             (endowable.Count > 0 ? ", or endow " + string.Join(" or ", endowable) : "") + ".";
            }
            var holders = Arrived ? new List<Institution>() : Influential().Where(i => i.Holdings > 0).ToList();
            if (holders.Count > 0)
                yield return "  Money held by " + string.Join(" and ", holders.Select(i => i.Def.ShortName)) + " stays with " + (holders.Count == 1 ? "it" : "them") +
                             " in Rome and works there while you're away. It isn't part of your purse and won't come back to you as coin.";
        }
    }
}
