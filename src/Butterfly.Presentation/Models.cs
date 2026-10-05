using System.Collections.Generic;

namespace Butterfly.Presentation
{
    /// <summary>Where the game is, for choosing what the client shows. The simulation decides it; the client follows it.</summary>
    public enum Phase
    {
        /// <summary>The first month, before the hour-one choice: the locked opening.</summary>
        Opening,
        /// <summary>Living in Rome, AD 155 onward.</summary>
        Rome,
        /// <summary>The departure screen is open (only when the player opened it; never pushed).</summary>
        Departure,
        /// <summary>Just arrived: the arrival's beats.</summary>
        Arrival,
        /// <summary>The first return: places to look.</summary>
        Return,
        /// <summary>After the return is finished: walk, learn more, or stop. Nothing after this is built.</summary>
        AfterReturn,
    }

    public sealed class HudModel
    {
        public string Date { get; set; } = "";
        public int Year { get; set; }
        /// <summary>1–12.</summary>
        public int Month { get; set; }
        public int MonthNumber { get; set; }
        public int AttentionFree { get; set; }
        public int AttentionTotal { get; set; }
        /// <summary>Attention already pledged this month, and to what ("1 — Machine assessment").</summary>
        public List<string> Reserved { get; } = new List<string>();
        public string Money { get; set; } = "";
        public string Aurei { get; set; } = "";
        /// <summary>"not yet assessed", "3/9 repairs · gold 12/40 aurei" or "ready".</summary>
        public string Machine { get; set; } = "";
        public bool MachineReady { get; set; }
        public bool Away { get; set; }
    }

    /// <summary>One line of what happened, with who it was (if anyone) so the client can show a portrait.</summary>
    public sealed class FeedLine
    {
        public string Text { get; }
        /// <summary>A person's name, or "" for narration.</summary>
        public string Speaker { get; }
        /// <summary>The portrait slot to show ("portrait.felix"), or "".</summary>
        public string Portrait { get; }
        public string Stamp { get; }

        public FeedLine(string text, string speaker, string portrait, string stamp)
        {
            Text = text;
            Speaker = speaker;
            Portrait = portrait;
            Stamp = stamp;
        }
    }

    /// <summary>What one action did.</summary>
    public sealed class Outcome
    {
        public bool Ok { get; set; }
        /// <summary>The simulation's own answer, as it said it.</summary>
        public string Message { get; set; } = "";
        /// <summary>What happened in the world because of it (scenes, news, completed work), in order.</summary>
        public List<FeedLine> Feed { get; } = new List<FeedLine>();
        /// <summary>A screen the client should show for it ("people", "journal", …), or "".</summary>
        public string Navigate { get; set; } = "";
        public bool Quit { get; set; }
    }

    /// <summary>The narrative view: a scene that asks for a choice. One at a time.</summary>
    public sealed class DecisionCard
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "";
        public string Speaker { get; set; } = "";
        public string Portrait { get; set; } = "";
        public string Text { get; set; } = "";
        public List<GameAction> Choices { get; } = new List<GameAction>();
    }

    public sealed class OpeningModel
    {
        public string Title { get; set; } = "THE BUTTERFLY EFFECT";
        public List<string> Paragraphs { get; } = new List<string>();
    }

    public sealed class PersonCard
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        /// <summary>How their life stands now (present conditions only).</summary>
        public string Status { get; set; } = "";
        public bool Away { get; set; }
        public string Household { get; set; } = "";
        public string CaresAbout { get; set; } = "";
        public string Wants { get; set; } = "";
        public string Portrait => "portrait." + Id;
    }

    public sealed class JournalModel
    {
        /// <summary>The inventor's own sentences, as written ("AD 158: …").</summary>
        public List<string> Entries { get; } = new List<string>();
        /// <summary>What has happened lately (this session's feed), newest last.</summary>
        public List<FeedLine> Recent { get; } = new List<FeedLine>();
        /// <summary>The money ledger's latest lines.</summary>
        public List<string> Ledger { get; } = new List<string>();
    }

    public sealed class MachineSystemModel
    {
        public string Id { get; set; } = "";
        public int Done { get; set; }
        public int Total { get; set; }
        /// <summary>The next step's name, or "" when the system is done.</summary>
        public string Next { get; set; } = "";
        /// <summary>The step under way, with months left, or "".</summary>
        public string UnderWay { get; set; } = "";
    }

    public sealed class MachineModel
    {
        public bool Assessed { get; set; }
        public int StepsDone { get; set; }
        public int StepsTotal { get; set; }
        public double GoldRestored { get; set; }
        public double GoldNeeded { get; set; }
        public bool Ready { get; set; }
        /// <summary>The machine's panel, as the simulation words it.</summary>
        public List<string> Panel { get; } = new List<string>();
        public List<MachineSystemModel> Systems { get; } = new List<MachineSystemModel>();
        public List<string> UpgradesDone { get; } = new List<string>();
        public List<GameAction> Actions { get; } = new List<GameAction>();
    }

    public sealed class WorkItem
    {
        public string Title { get; set; } = "";
        /// <summary>Who it is for or with.</summary>
        public string Who { get; set; } = "";
        public string Portrait { get; set; } = "";
        public string State { get; set; } = "";
    }

    public sealed class WorkModel
    {
        public List<WorkItem> Commissions { get; } = new List<WorkItem>();
        public List<WorkItem> Challenges { get; } = new List<WorkItem>();
        public List<WorkItem> UnderWay { get; } = new List<WorkItem>();
        public List<ActionGroup> Actions { get; } = new List<ActionGroup>();
    }

    public sealed class DepartureModel
    {
        /// <summary>"You will leave AD 163 and arrive somewhere between AD 188 and AD 208…"</summary>
        public string Range { get; set; } = "";
        /// <summary>What you leave behind: facts, never predictions (Simulation.DepartureBriefing).</summary>
        public List<string> Briefing { get; } = new List<string>();
        public string InHand { get; set; } = "";
        /// <summary>Things to do with gold before going (deposit, bury).</summary>
        public List<GameAction> Prepare { get; } = new List<GameAction>();
        public bool CanLeave { get; set; }
    }

    public sealed class ArrivalBeatModel
    {
        public string Name { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public sealed class ArrivalModel
    {
        public int JumpYears { get; set; }
        public int DepartureYear { get; set; }
        public int ArrivalYear { get; set; }
        public List<ArrivalBeatModel> Beats { get; } = new List<ArrivalBeatModel>();
    }

    /// <summary>What the player has done at a return site. Never whether it is true, false, important or their doing.</summary>
    public enum SiteState { Unvisited, Seen, LookedCloser }

    public sealed class ReturnSiteModel
    {
        public int Number { get; set; }
        public string Id { get; set; } = "";
        public string Place { get; set; } = "";
        public SiteState State { get; set; }
        /// <summary>After a visit: what you recognize there.</summary>
        public string Recognition { get; set; } = "";
        /// <summary>After a visit: what doesn't fit.</summary>
        public string Contradiction { get; set; } = "";
        /// <summary>After a visit: what you could look into.</summary>
        public string Lead { get; set; } = "";
        /// <summary>After looking closer: what you found.</summary>
        public string Finding { get; set; } = "";
        /// <summary>A person's portrait slot when the place follows someone's life, else "".</summary>
        public string Portrait { get; set; } = "";
        public string Art => "site." + Id;
        public string VisitCommand => "visit " + Number;
        public string LookCloserCommand => "look closer " + Number;
    }

    public sealed class ReturnModel
    {
        public int ArrivalYear { get; set; }
        public int YearsAway { get; set; }
        public List<ReturnSiteModel> Sites { get; } = new List<ReturnSiteModel>();
        public int Seen { get; set; }
        public int VisitsRequired { get; set; }
        public bool CanFinish { get; set; }
        public bool Finished { get; set; }
        public bool HasJournal { get; set; }
    }
}
