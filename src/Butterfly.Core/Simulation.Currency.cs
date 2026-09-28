using System;
using System.Globalization;

namespace Butterfly.Core
{
    /// <summary>
    /// Two kinds of money (decided 2026-09-28): everyday denarii, Rome's silver coin, which the mint debases; and gold aurei,
    /// which hold their value, so their price in denarii rises with the price level. Money is kept internally in units of
    /// AD 155 aurei (so every tuning value keeps its scale) and shown as denarii. Changing money is an action: a trip to the
    /// money changers at the Forum, for a fee.
    /// </summary>
    public sealed partial class Simulation
    {
        public double DenariiPerUnit => T.Get("currency.denariiPerUnit");

        /// <summary>Denarii for an internal amount.</summary>
        public double Denarii(double units) => units * DenariiPerUnit;

        /// <summary>An internal amount shown as denarii, e.g. "1,750 denarii".</summary>
        public string Money(double units) => Math.Round(Denarii(units)).ToString("#,0", CultureInfo.InvariantCulture) + " denarii";

        /// <summary>An amount the player typed in denarii, as an internal amount.</summary>
        public double FromDenarii(double denarii) => denarii / DenariiPerUnit;

        /// <summary>The market price of one aureus (internal units): it rises with the price level, since gold isn't debased.</summary>
        public double AureusPrice => World.PriceLevel;

        public double AureusInDenarii => Denarii(AureusPrice);

        public string AureiText(double aurei) => Aurei(aurei);

        /// <summary>"1 aureus", "2 aurei": every amount of gold shown to the player goes through here.</summary>
        public static string Aurei(double aurei) =>
            aurei.ToString("#,0.#", CultureInfo.InvariantCulture) + (Math.Abs(aurei - 1) < 1e-9 ? " aureus" : " aurei");

        /// <summary>Sells aurei to the money changers for denarii, less their fee.</summary>
        public CommandResult SellAurei(double aurei)
        {
            aurei = Math.Floor(aurei);
            if (aurei < 1) return CommandResult.Fail("Change how many aurei? You have " + AureiText(World.Aurei) + ".");
            if (aurei > World.Aurei + 1e-9) return CommandResult.Fail("You have only " + AureiText(World.Aurei) + ".");
            var attention = CheckAttention(T.GetInt("currency.exchangeAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("currency.exchangeAttention"));
            double units = aurei * AureusPrice * (1 - T.Get("currency.exchangeFee"));
            double goldBefore = World.Gold, aureiBefore = World.Aurei;
            World.Aurei -= aurei;
            World.Gold += units;
            Record("currency.exchange", "aurei", null, new[] { "player" },
                new[] { new Effect("aurei", aureiBefore, World.Aurei), new Effect(GoldKey, goldBefore, World.Gold) },
                "At the money changers' tables you change " + AureiText(aurei) + " for " + Money(units) + " (" + F(Math.Round(AureusInDenarii, 1)) +
                " denarii an aureus, less their " + F(T.Get("currency.exchangeFee") * 100) + "%).");
            return CommandResult.Success("You change " + AureiText(aurei) + " for " + Money(units) + ".");
        }

        /// <summary>Buys aurei from the money changers with denarii, plus their fee.</summary>
        public CommandResult BuyAurei(double aurei)
        {
            aurei = Math.Floor(aurei);
            if (aurei < 1) return CommandResult.Fail("Buy how many aurei? One costs " + Money(AureiCost(1)) + " now.");
            double cost = AureiCost(aurei);
            if (World.Gold < cost - 1e-9) return CommandResult.Fail(AureiText(aurei) + " cost " + Money(cost) + "; you have " + Money(World.Gold) + ".");
            var attention = CheckAttention(T.GetInt("currency.exchangeAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("currency.exchangeAttention"));
            double goldBefore = World.Gold, aureiBefore = World.Aurei;
            World.Gold -= cost;
            World.Aurei += aurei;
            Record("currency.exchange", "aurei", null, new[] { "player" },
                new[] { new Effect("aurei", aureiBefore, World.Aurei), new Effect(GoldKey, goldBefore, World.Gold) },
                "At the money changers' tables you buy " + AureiText(aurei) + " for " + Money(cost) + " (" + F(Math.Round(AureusInDenarii, 1)) +
                " denarii an aureus, plus their " + F(T.Get("currency.exchangeFee") * 100) + "%).");
            return CommandResult.Success("You buy " + AureiText(aurei) + " for " + Money(cost) + ".");
        }

        /// <summary>What buying aurei costs now (internal units), with the changers' fee.</summary>
        public double AureiCost(double aurei) => aurei * AureusPrice * (1 + T.Get("currency.exchangeFee"));

        /// <summary>How many whole aurei the denarii in hand would buy now.</summary>
        public double AffordableAurei() => Math.Floor(World.Gold / AureiCost(1) + 1e-9);
    }
}
