using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The workshop (decided 2026-09-28, P0-34): once you own a share of the smith's workshop it becomes a place with
    /// decisions. Each season a few orders come in and you can take only one or two; the smith is a person with ambitions
    /// (his requests come as decision events, data/content/events.json); and you can hire free, paid apprentices (never
    /// enslaved, SYSTEMS §14) who raise the workshop's output and carry your techniques after you leave. What they carry
    /// decides what you find at the forges on arrival.
    /// </summary>
    public sealed partial class Simulation
    {
        private readonly List<string> _orderBoard = new List<string>();
        private int _orderSlot = -1;
        private int _ordersTakenThisSeason;
        private int _firstDepartureYear;

        /// <summary>Whether you own a share of the smith's workshop (the hour-one choice or buying in later).</summary>
        public bool OwnsWorkshop => World.CompletedProjects.Contains("workshop");

        /// <summary>The orders on offer this season (none you've already taken).</summary>
        public IEnumerable<OrderDef> OrderBoard() => _orderBoard.Select(id => Data.Content.Orders.First(o => o.Id == id));

        /// <summary>How many orders the workshop can take this season: one, two once it has enough apprentices.</summary>
        public int OrdersPerSeason() =>
            T.GetInt("workshop.orders.perSeason") + (World.Apprentices >= T.GetInt("workshop.orders.extraAtApprentices") ? 1 : 0);

        public int OrdersLeftThisSeason => Math.Max(0, OrdersPerSeason() - _ordersTakenThisSeason);

        /// <summary>The workshop's output multiplier: its inventions and its apprentices.</summary>
        public double WorkshopOutput() => 1 + World.WorkshopBonus + World.Apprentices * T.Get("workshop.apprentices.outputEach");

        /// <summary>What an order pays before tax, at today's prices.</summary>
        public double OrderPay(OrderDef o) => o.Pay * WorkshopOutput() * World.PriceLevel;

        /// <summary>An apprentice's yearly wage at today's prices.</summary>
        public double ApprenticeWage() => T.Get("workshop.apprentices.wagePerYear") * WageLevel();

        /// <summary>At the start of each turn: a new season's orders come in (seeded draw, only once you own the workshop).</summary>
        private void AdvanceWorkshop()
        {
            if (!OwnsWorkshop) return;
            int every = T.GetInt("workshop.orders.everyMonths");
            int slot = (Now.TotalMonths - SimTime.FromYear(T.GetInt("time.startYear"), T.GetInt("time.startMonth")).TotalMonths) / every;
            if (slot <= _orderSlot) return;
            _orderSlot = slot;
            _ordersTakenThisSeason = 0;
            _orderBoard.Clear();
            var pool = Data.Content.Orders.Select(o => o.Id).ToList();
            int offered = Math.Min(pool.Count, T.GetInt("workshop.orders.offered"));
            for (int k = 0; k < offered; k++)
            {
                int pick = Rng.NextInt(0, pool.Count);
                _orderBoard.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Record("workshop.orders", "workshop", null, new[] { "world" }, null,
                "Orders come in at the workshop: " + string.Join("; ", OrderBoard().Select(o => o.Name + " (order " + o.Id + ")")) + ". " +
                Data.Content.Smith + " can take " + (OrdersPerSeason() == 1 ? "one" : "two") + ".");
        }

        public CommandResult TakeOrder(string id)
        {
            if (!OwnsWorkshop) return CommandResult.Fail("You have no workshop to take orders.");
            var o = OrderBoard().FirstOrDefault(x => x.Id.StartsWith((id ?? "").Trim().ToLowerInvariant(), StringComparison.Ordinal) && (id ?? "").Trim().Length > 0);
            if (o == null)
                return CommandResult.Fail(_orderBoard.Count == 0 ? "No orders are waiting this season." : "Take one of: " + string.Join(", ", OrderBoard().Select(x => x.Id)) + ".");
            if (OrdersLeftThisSeason <= 0) return CommandResult.Fail("The workshop has taken all it can this season; more apprentices would let it take two.");
            var attention = CheckAttention(o.Attention);
            if (attention != null) return attention;
            SpendAttention(o.Attention);
            _orderBoard.Remove(o.Id);
            _ordersTakenThisSeason++;
            World.OrdersTaken++;
            double before = World.Gold, pay = OrderPay(o), tax = pay * WorkTaxRate();
            World.Gold += pay - tax;
            var ev = Record("workshop.order", o.Id, null, new[] { "player", Data.Content.Smith }, new[] { new Effect(GoldKey, before, World.Gold) },
                "The workshop takes an order: " + o.Name + ". " + o.Text + " It pays " + Money(pay) + "; tax takes " + Money(tax) + ".");
            ApplyEffects(Cap(o.Name), o.Effects, new[] { ev.Id });
            ChangeSmithRegard(T.Get("workshop.smith.regardPerOrder"), new[] { ev.Id }, null);
            return CommandResult.Success("You earn " + Money(pay - tax) + " after tax. " + o.Text);
        }

        public CommandResult HireApprentice()
        {
            if (!OwnsWorkshop) return CommandResult.Fail("You have no workshop to take apprentices.");
            if (World.Apprentices >= T.GetInt("workshop.apprentices.max")) return CommandResult.Fail("The workshop has all the apprentices it can use.");
            int cost = T.GetInt("workshop.apprentices.hireAttention");
            var attention = CheckAttention(cost);
            if (attention != null) return attention;
            SpendAttention(cost);
            int before = World.Apprentices;
            World.Apprentices++;
            var ev = Record("workshop.apprentice", "workshop", null, new[] { "player" }, new[] { new Effect("workshop.apprentices", before, World.Apprentices) },
                "A free apprentice joins the workshop at " + Money(ApprenticeWage()) + " a year; you teach them what you know. Apprentices: " + World.Apprentices + ".");
            ChangeSmithRegard(T.Get("workshop.smith.regardPerApprentice"), new[] { ev.Id }, null);
            return CommandResult.Success("A free apprentice joins the workshop at " + Money(ApprenticeWage()) + " a year. The workshop now has " + World.Apprentices + ".");
        }

        public CommandResult DismissApprentice()
        {
            if (World.Apprentices <= 0) return CommandResult.Fail("The workshop has no apprentices.");
            int before = World.Apprentices;
            World.Apprentices--;
            var ev = Record("workshop.apprentice", "workshop", null, new[] { "player" }, new[] { new Effect("workshop.apprentices", before, World.Apprentices) },
                "You let an apprentice go. Apprentices: " + World.Apprentices + ".");
            ChangeSmithRegard(-T.Get("workshop.smith.regardPerApprentice"), new[] { ev.Id }, Data.Content.Smith + " is sorry to lose the pair of hands.");
            return CommandResult.Success("You let an apprentice go. The workshop now has " + World.Apprentices + ".");
        }

        internal void ChangeSmithRegard(double delta, IEnumerable<int>? causes, string? text)
        {
            if (delta == 0) return;
            double before = World.SmithRegard;
            World.SmithRegard = Math.Max(0, Math.Min(100, World.SmithRegard + delta));
            Record("workshop.smith", "workshop", causes, new[] { Data.Content.Smith }, new[] { new Effect("workshop.smith", before, World.SmithRegard) },
                text ?? Data.Content.Smith + "'s regard for you: " + F(World.SmithRegard) + ".");
        }

        /// <summary>Each new year: the apprentices' wages. One you can't pay leaves.</summary>
        private void WorkshopYearTick()
        {
            if (World.Apprentices <= 0) return;
            double wages = World.Apprentices * ApprenticeWage();
            double before = World.Gold;
            if (World.Gold >= wages)
            {
                World.Gold -= wages;
                Record("workshop.wages", GoldKey, null, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                    "You pay the workshop's " + World.Apprentices + " apprentice" + (World.Apprentices == 1 ? "" : "s") + " " + Money(wages) + " for the year.");
                return;
            }
            int apprentices = World.Apprentices;
            int kept = (int)Math.Floor(World.Gold / ApprenticeWage());
            World.Gold -= kept * ApprenticeWage();
            World.Apprentices = kept;
            var ev = Record("workshop.apprentice", "workshop", null, new[] { "world" }, new[] { new Effect("workshop.apprentices", apprentices, kept), new Effect(GoldKey, before, World.Gold) },
                "You can't pay all the apprentices' wages: " + (apprentices - kept) + " leave" + (apprentices - kept == 1 ? "s" : "") + " for other forges.");
            ChangeSmithRegard(-T.Get("workshop.smith.regardPerApprentice") * (apprentices - kept), new[] { ev.Id }, Data.Content.Smith + " is angry about the unpaid apprentices.");
        }

        /// <summary>The techniques the workshop's people know from you: workshop and mechanics inventions.</summary>
        public int WorkshopTechniques() =>
            Data.Content.Inventions.Count(i => (i.Branch == "workshop" || i.Branch == "mechanics") && World.Invented.Contains(i.Id));

        /// <summary>
        /// What the apprentices carry after you leave (P0-34): Economy target points during an absence, for
        /// workshop.carry.years after your first departure: apprentices × (1 + techniques × weight) × per-apprentice.
        /// </summary>
        public double WorkshopCarry(int year)
        {
            if (!OwnsWorkshop || World.Apprentices <= 0 || _firstDepartureYear == 0) return 0;
            if (year - _firstDepartureYear >= T.GetInt("workshop.carry.years")) return 0;
            return World.Apprentices * (1 + WorkshopTechniques() * T.Get("workshop.carry.techniqueWeight")) * T.Get("workshop.carry.perApprentice");
        }

        /// <summary>
        /// The workshop's fate on arrival: "street" (a street of forges), "working" (still working), or "gone".
        /// Score = apprentices + techniques × weight + (smith's regard − 50) ÷ regardPer ± 1 for the Economy against history.
        /// </summary>
        public string WorkshopFate()
        {
            double score = World.Apprentices + WorkshopTechniques() * T.Get("workshop.fate.techniqueWeight")
                         + (World.SmithRegard - T.Get("workshop.smith.startRegard")) / T.Get("workshop.fate.regardPer")
                         + (SubScore(Domain.Economy) >= 100 ? 1 : -1) * T.Get("workshop.fate.economy");
            return score >= T.Get("workshop.fate.streetAt") ? "street" : score >= T.Get("workshop.fate.workingAt") ? "working" : "gone";
        }

        private void NoteFirstDeparture()
        {
            if (_firstDepartureYear == 0) _firstDepartureYear = Now.Year;
        }

        /// <summary>The workshop, for the console: orders, apprentices, the smith.</summary>
        public List<string> WorkshopLines()
        {
            var lines = new List<string>();
            if (!OwnsWorkshop)
            {
                lines.Add("You have no share in a workshop. (The hour-one choice, or the project to buy into the smith's workshop.)");
                return lines;
            }
            lines.Add("The smith's workshop by the Porta Trigemina, with " + Data.Content.Smith + ". Output ×" + R(WorkshopOutput(), "0.00") +
                      " (inventions +" + R(World.WorkshopBonus * 100, "0") + "%, apprentices +" + R(World.Apprentices * T.Get("workshop.apprentices.outputEach") * 100, "0") + "%).");
            lines.Add("Orders this season (" + OrdersLeftThisSeason + " more can be taken):" + (_orderBoard.Count == 0 ? " none waiting." : ""));
            foreach (var o in OrderBoard())
                lines.Add("  take " + o.Id + ": " + o.Name + " — " + Money(OrderPay(o)) + " before tax, " + o.Attention + " Attention" + OrderEffectsText(o));
            lines.Add("Apprentices: " + World.Apprentices + " of " + T.GetInt("workshop.apprentices.max") + ", free and paid, " + Money(ApprenticeWage()) + " a year each " +
                      "(apprentice hire | apprentice dismiss). Each raises output " + R(T.Get("workshop.apprentices.outputEach") * 100, "0") + "%; with " +
                      T.GetInt("workshop.orders.extraAtApprentices") + " the workshop can take two orders a season. They carry your techniques after you leave.");
            lines.Add(Data.Content.Smith + "'s regard for you: " + R(World.SmithRegard, "0") + ". Orders taken: " + World.OrdersTaken + ". Techniques the workshop knows from you: " + WorkshopTechniques() + ".");
            return lines;
        }

        private string OrderEffectsText(OrderDef o)
        {
            var parts = new List<string>();
            foreach (var fx in o.Effects)
            {
                if (fx.Type == "level") parts.Add(fx.Domain + " " + Signed(fx.Value));
                else if (fx.Type == "loyalty") parts.Add(World.Institution(fx.Institution ?? "").Def.ShortName + " " + Signed(fx.Value));
            }
            return parts.Count == 0 ? "" : "; " + string.Join(", ", parts);
        }
    }
}
