using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Butterfly.Core;

namespace Butterfly.Presentation
{
    /// <summary>
    /// The graphical client's one handle on the game (P2, 2026-10-05). It owns the single authoritative
    /// <see cref="Simulation"/> and translates: screen models out, the player's clicks in. Every click is an ordinary command
    /// in the console's grammar, run against the same simulation methods, so a game clicked through replays exactly like one
    /// typed into the console. It keeps no game state of its own: only what the client is showing (the departure screen,
    /// the arrival's beats, a feed of what happened).
    /// </summary>
    public sealed class GameSession
    {
        public Simulation Sim { get; }

        /// <summary>The departure screen is open: the console's armed jump. Opened only by the player.</summary>
        public bool DepartureOpen { get; private set; }

        /// <summary>The arrival's beats are still being shown.</summary>
        public bool ShowingArrival { get; private set; }

        private readonly List<FeedLine> _history = new List<FeedLine>();
        public IReadOnlyList<FeedLine> History => _history;

        private readonly List<string> _commands = new List<string>();
        /// <summary>Every command run, in order: with the seed, enough to replay the game (as a console inputs file).</summary>
        public IReadOnlyList<string> Commands => _commands;

        public GameSession(Simulation sim) { Sim = sim; }

        /// <summary>A new game from a data/ directory (the client passes its copy, e.g. Unity's StreamingAssets/data).</summary>
        public static GameSession Start(string dataDirectory, ulong seed) => new GameSession(new Simulation(GameData.Load(dataDirectory), seed));

        private static string F(double v) => ActionCatalog.F(v);

        // ---- where the game is ------------------------------------------------------------------------------------------

        public Phase Phase
        {
            get
            {
                if (Sim.Arrived)
                {
                    if (ShowingArrival) return Phase.Arrival;
                    if (DepartureOpen) return Phase.Departure;
                    return Sim.ReturnPending ? Phase.Return : Phase.AfterReturn;
                }
                if (DepartureOpen) return Phase.Departure;
                return Sim.Turn == 1 && Sim.SeededChoiceOpen ? Phase.Opening : Phase.Rome;
            }
        }

        public HudModel Hud()
        {
            var w = Sim.World;
            var h = new HudModel
            {
                Date = Sim.Now.Display, Year = Sim.Now.Year, Month = Sim.Now.Month + 1, MonthNumber = Sim.Turn,
                AttentionFree = w.Attention, AttentionTotal = Sim.AttentionPerTurn,
                Money = Sim.Money(w.Gold), Aurei = Sim.AureiText(w.Aurei),
                MachineReady = Sim.MachineReady, Away = Sim.Arrived,
            };
            foreach (var (what, n) in Sim.ReservedAttentionParts()) h.Reserved.Add(n + " — " + what);
            h.Machine = Sim.MachineReady ? "ready"
                : !Sim.MachineAssessed ? "not yet assessed"
                : Sim.MachineStepsDone + "/" + Sim.MachineStepsTotal + " repairs · gold " + F(Sim.MachineGoldRestored) + "/" + F(Sim.MachineGoldNeeded) + " aurei";
            return h;
        }

        /// <summary>Everything the player can do now, grouped (the same catalog as the console's menu).</summary>
        public List<ActionGroup> Actions() => ActionCatalog.For(Sim, DepartureOpen);

        /// <summary>The actions that belong to one place on the map.</summary>
        public List<ActionGroup> ActionsAt(string place) =>
            Actions().Where(g => g.Place == place && g.Items.Count > 0).ToList();

        // ---- the opening and the narrative ------------------------------------------------------------------------------

        /// <summary>The locked opening ("The machine stops screaming before you do."), as the console tells it.</summary>
        public OpeningModel Opening()
        {
            var text = Sim.Data.Content;
            var m = new OpeningModel();
            foreach (var para in text.Template("opening.scene").Split(new[] { "\n\n" }, StringSplitOptions.None)) m.Paragraphs.Add(para);
            m.Paragraphs.Add(text.Template("opening.gold", new Dictionary<string, string> { { "aurei", F(Sim.MachineGoldNeeded) } }));
            m.Paragraphs.Add(text.Template("opening.money"));
            return m;
        }

        /// <summary>
        /// The choice waiting for the player, if any: one at a time, in the order the console's "decide" group lists them,
        /// then invitations and commission terms. Null when nothing waits.
        /// </summary>
        public DecisionCard? CurrentDecision()
        {
            if (Sim.Arrived) return null;
            var groups = Actions();
            var w = Sim.World;
            var decide = groups.First(g => g.Key == "decide").Items;
            if (Sim.SeededChoiceOpen)
            {
                var c = Card("seeded", Sim.Data.Content.Template("opening.choice"), "", Sim.Data.Content.Template("walk.intro.start"));
                foreach (var a in decide.Where(a => a.Command.StartsWith("choose ", StringComparison.Ordinal)))
                {
                    string id = a.Command.Substring(7);
                    c.Choices.Add(new GameAction(Cap(a.Label) + " — " + Sim.Data.Content.Project(id)!.Description, a.Command));
                }
                return c;
            }
            if (w.Promise.Status == PromiseStatus.Offered)
                return WithChoices(Card("promise", "Demetria's request", LastText("promise.offer"), "Demetria"), decide, "promise ");
            if (Sim.OutbreakAwaitingResponse)
                return WithChoices(Card("plague", "The pestilence", LastText("plague.outbreak"), ""), decide, "respond ");
            if (Sim.PendingEvent is EventDef ev)
                return WithChoices(Card("event:" + ev.Id, ev.Title, ev.Text, ""), decide, "decide ");
            foreach (var i in w.Institutions.Where(x => x.OfferedRank > 0))
                return WithChoices(Card("office:" + i.Key, Cap(i.Def.ShortName), "You are offered office: " + Sim.OfficeTitle(i, i.OfferedRank) + ".", i.Leader), decide, "office ", i.Key);
            foreach (var p in w.Invitations.Where(p => p.Pending != InvitationOffer.None))
            {
                var d = Sim.InvitationPathDefFor(p.Institution)!;
                var c = Card("invitation:" + d.Institution, d.Inviter + "'s invitation", LastText("invitation.offer", d.Institution), d.Inviter);
                foreach (var a in groups.First(g => g.Key == "invitations").Items.Where(a => a.Command.EndsWith(" " + d.Institution, StringComparison.Ordinal))) c.Choices.Add(a);
                return c;
            }
            foreach (var cm in w.Commissions.Where(c => c.Status == CommissionStatus.TermsOffered))
            {
                var d = Sim.CommissionDefOf(cm);
                var c = Card("terms:" + d.Id, d.Title, d.TermsScene.Text + "\n\n" + Sim.TermsLine(cm), d.Client);
                foreach (var a in groups.First(g => g.Key == "commissions").Items.Where(a => a.Command.EndsWith(" " + d.Id, StringComparison.Ordinal))) c.Choices.Add(a);
                return c;
            }
            return null;
        }

        private DecisionCard Card(string key, string title, string text, string speaker)
        {
            var (name, portrait) = Speaker(speaker);
            return new DecisionCard { Key = key, Title = title, Text = text, Speaker = name, Portrait = portrait };
        }

        private static DecisionCard WithChoices(DecisionCard c, List<GameAction> decide, string prefix, string suffix = "")
        {
            foreach (var a in decide.Where(a => a.Command.StartsWith(prefix, StringComparison.Ordinal) && (suffix.Length == 0 || a.Command.EndsWith(" " + suffix, StringComparison.Ordinal))))
                c.Choices.Add(a);
            return c;
        }

        private string LastText(string type, string target = "") =>
            Sim.Log.Events.LastOrDefault(e => e.Type == type && (target.Length == 0 || e.Target == target))?.Text ?? "";

        /// <summary>A known person's name and portrait slot for an actor id or name; ("", "") for anyone else.</summary>
        private (string Name, string Portrait) Speaker(string actor)
        {
            if (string.IsNullOrEmpty(actor)) return ("", "");
            var d = Sim.Data.Content.People.FirstOrDefault(p => string.Equals(p.Id, actor, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, actor, StringComparison.OrdinalIgnoreCase) ||
                                                                p.Name.StartsWith(actor + " ", StringComparison.OrdinalIgnoreCase));
            return d == null ? (actor, "") : (d.Name, ArtManifest.Portrait(d.Id));
        }

        // ---- views ------------------------------------------------------------------------------------------------------

        /// <summary>A place's free look-around: what is there now (Simulation.Visit). Changes nothing.</summary>
        public string LookAround(string place)
        {
            if (place == RomeMap.Lodging) return string.Join("\n", Sim.MachineStatus());
            return RomeMap.IsWalkPlace(place) ? Sim.Visit(place) : "";
        }

        /// <summary>The talk of the Forum and the market (free; changes nothing).</summary>
        public List<string> News() => Sim.News();

        /// <summary>The machine's readiness in the simulation's words ("The machine is ready. You can leave now, or remain…"), or "".</summary>
        public string ReadyLine() =>
            Sim.Arrived ? "" : Sim.ViewOf(MenuSection.Machine).AvailableNow.FirstOrDefault(i => i.Command == "jump")?.Label ?? "";

        public List<PersonCard> People() =>
            Sim.KnownPeople().Select(d => new PersonCard
            {
                Id = d.Id, Name = d.Name, Role = d.Role, Status = Cap(Sim.PersonOf(d.Id)?.Status ?? d.Status), Away = Sim.IsPersonAway(d.Id),
                Household = d.Household, CaresAbout = d.CaresAbout, Wants = d.Goal,
            }).ToList();

        public JournalModel Journal()
        {
            var j = new JournalModel();
            j.Entries.AddRange(Sim.JournalLines());
            j.Recent.AddRange(_history.Skip(Math.Max(0, _history.Count - 40)));
            var l = Sim.World.Ledger;
            foreach (var e in l.Entries.Skip(Math.Max(0, l.Entries.Count - 12)))
                j.Ledger.Add((e.Amount >= 0 ? "+" : "−") + Sim.Money(Math.Abs(e.Amount)) + "  " + (e.Counterparty.Length > 0 ? e.Counterparty + ": " : "") + e.Reason);
            return j;
        }

        public MachineModel Machine()
        {
            var w = Sim.World;
            var m = new MachineModel
            {
                Assessed = Sim.MachineAssessed, StepsDone = Sim.MachineStepsDone, StepsTotal = Sim.MachineStepsTotal,
                GoldRestored = Sim.MachineGoldRestored, GoldNeeded = Sim.MachineGoldNeeded, Ready = Sim.MachineReady,
            };
            m.Panel.AddRange(Sim.MachineStatus());
            foreach (var system in Simulation.MachineSystems)
            {
                var steps = Sim.Data.Content.MachineSteps.Where(s => s.System == system).ToList();
                var active = w.ActiveMachineSteps.FirstOrDefault(a => a.Def.System == system);
                m.Systems.Add(new MachineSystemModel
                {
                    Id = system, Total = steps.Count, Done = steps.Count(s => w.MachineDone.Contains(s.Id)),
                    Next = Sim.NextMachineStep(system)?.Name ?? "",
                    UnderWay = active == null ? "" : active.Def.Name + " (" + active.TurnsRemaining + " month(s) left)",
                });
            }
            m.UpgradesDone.AddRange(Sim.Data.Content.MachineUpgrades.Where(u => w.MachineDone.Contains(u.Id)).Select(u => u.Name));
            foreach (var g in Actions().Where(g => g.Key == "machine")) m.Actions.AddRange(g.Items);
            return m;
        }

        public WorkModel Work()
        {
            var w = Sim.World;
            var m = new WorkModel();
            foreach (var c in Sim.OpenCommissions())
            {
                var d = Sim.CommissionDefOf(c);
                var (_, portrait) = Speaker(d.Client);
                m.Commissions.Add(new WorkItem
                {
                    Title = d.Title, Who = d.Client + ", " + d.ClientRole, Portrait = portrait,
                    State = c.Status == CommissionStatus.Offered ? "A problem to look at (" + d.Diagnosis.Attention + " Attention · pay: none initially · may lead to paid work)"
                          : c.Status == CommissionStatus.TermsOffered ? "Terms on the table: " + Sim.TermsLine(c)
                          : Simulation.StageLabel(d.Work[c.WorkIndex].Stage) + ", " + c.MonthsLeftInStage + " month(s) left in this stage",
                });
            }
            foreach (var c in w.Challenges.Where(c => c.Status != ChallengeStatus.NotYet))
            {
                var d = Sim.ChallengeDefOf(c);
                m.Challenges.Add(new WorkItem
                {
                    Title = d.Name, Who = d.Question,
                    State = "Stage " + c.StageIndex + " of " + d.Stages.Count + ". " +
                            (c.Status == ChallengeStatus.Working ? "Under way: " + Sim.NextStage(c)!.Name + ", " + c.MonthsLeft + " month(s) left."
                             : c.Status == ChallengeStatus.Open ? "Next: " + Sim.StageLine(Sim.NextStage(c)!)
                             : c.Status + "."),
                });
            }
            foreach (var p in w.ActiveProjects) m.UnderWay.Add(new WorkItem { Title = p.Def.Name, State = p.TurnsRemaining + " month(s) left" });
            foreach (var a in w.ActiveInventions) m.UnderWay.Add(new WorkItem { Title = a.Def.Name, State = a.TurnsRemaining + " month(s) left" });
            if (w.WorkshopBuildTurns > 0) m.UnderWay.Add(new WorkItem { Title = "Enlarging the workshop", State = w.WorkshopBuildTurns + " month(s) left" });
            m.Actions.AddRange(Actions().Where(g => g.Section == MenuSection.Projects || g.Section == MenuSection.Knowledge).Where(g => g.Items.Count > 0));
            return m;
        }

        /// <summary>The departure screen. Facts about what you leave, never what will come of it.</summary>
        public DepartureModel Departure()
        {
            var m = new DepartureModel { CanLeave = Sim.Arrived ? Sim.CanJumpAgain : Sim.MachineReady };
            if (!m.CanLeave) return m;
            var (lo, hi) = Sim.JumpRange();
            m.Range = "You will leave AD " + Sim.Now.Year + " and arrive somewhere between AD " + (Sim.Now.Year + lo) + " and AD " + (Sim.Now.Year + hi) +
                      ": the machine's range is " + Sim.JumpRangeText() + ". You can't come back.";
            m.InHand = Sim.Money(Sim.World.Gold) + " and " + Sim.AureiText(Sim.World.Aurei) + " in hand";
            m.Briefing.AddRange(Sim.DepartureBriefing());
            foreach (var g in ActionCatalog.For(Sim, true).Where(g => g.Key == "leave" || g.Key == "then"))
                m.Prepare.AddRange(g.Items.Where(a => a.Command.StartsWith("deposit ", StringComparison.Ordinal) || a.Command.StartsWith("bury ", StringComparison.Ordinal)));
            return m;
        }

        public ArrivalModel? Arrival()
        {
            var a = Sim.Arrival;
            if (a == null) return null;
            var m = new ArrivalModel { JumpYears = a.JumpYears, DepartureYear = a.DepartureYear, ArrivalYear = a.ArrivalYear };
            foreach (var b in a.Beats) m.Beats.Add(new ArrivalBeatModel { Name = b.Name, Text = b.Text });
            return m;
        }

        /// <summary>After an arrival: the Index and what became of your institutions (free; changes nothing).</summary>
        public string LearnMore() => Sim.Arrival?.LearnMore() ?? "";

        /// <summary>The player has read the arrival; the city is next.</summary>
        public void FinishArrival() => ShowingArrival = false;

        public ReturnModel? Return()
        {
            var r = Sim.World.Return;
            if (r == null) return null;
            var m = new ReturnModel
            {
                ArrivalYear = r.ArrivalYear, YearsAway = r.JumpYears, Seen = r.Visited.Count, VisitsRequired = Sim.ReturnVisitsRequired,
                CanFinish = Sim.ReturnCanComplete, Finished = r.Completed, HasJournal = Sim.World.Journal.Count > 0,
            };
            for (int k = 0; k < r.Sites.Count; k++)
            {
                var s = r.Sites[k];
                bool seen = r.Visited.Contains(s.Id), closer = r.Investigated.Contains(s.Id);
                m.Sites.Add(new ReturnSiteModel
                {
                    Number = k + 1, Id = s.Id, Place = s.Place,
                    State = closer ? SiteState.LookedCloser : seen ? SiteState.Seen : SiteState.Unvisited,
                    Recognition = seen ? s.Recognition : "", Contradiction = seen ? s.Contradiction : "", Lead = seen ? Cap(s.Lead) : "",
                    Finding = closer ? s.Investigation : "",
                    Portrait = s.Person.Length > 0 ? ArtManifest.Portrait(s.Person) : "",
                });
            }
            return m;
        }

        // ---- doing things -----------------------------------------------------------------------------------------------

        /// <summary>Opens the departure screen (only ever on the player's request). Nothing happens until they choose to leave.</summary>
        public bool OpenDeparture()
        {
            if (!(Sim.Arrived ? Sim.CanJumpAgain : Sim.MachineReady)) return false;
            if (!DepartureOpen) _commands.Add("jump");        // the console's first 'jump' shows the briefing
            DepartureOpen = true;
            return true;
        }

        /// <summary>Closes the departure screen: the player stays.</summary>
        public void CloseDeparture() => DepartureOpen = false;

        private static readonly string[] PrepCommands = { "paydown", "endow", "audit", "orders", "status", "s", "why", "help", "?", "exchange", "deposit", "bury", "restore", "visit", "walk" };

        private static readonly Dictionary<string, string> LookScreens = new Dictionary<string, string>
        {
            { "status", "now" }, { "news", "now" }, { "people", "people" }, { "ledger", "journal" }, { "view", "now" }, { "institutions", "institutions" },
            { "machine", "machine" }, { "workshop", "work" }, { "inventions", "work" }, { "projects", "work" }, { "help", "help" }, { "journal", "journal" },
        };

        /// <summary>The event types the console shows after a month (Program.EndTurn), plus every scene.</summary>
        private static readonly HashSet<string> Shown = new HashSet<string>
        {
            "project.complete", "debt.tier", "plague.warning", "plague.outbreak", "plague.toll", "plague.opening", "plague.passed",
            "seeded.payoff", "promise.offer", "promise.kept", "commitment.complete", "income.bonus", "seeded.choice", "institution.unpaid", "year.start",
            "machine.step", "machine.assessed", "invention.complete", "institution.stake", "institution.seniority", "rivalry.strike", "institution.collapse", "bust.warning", "bust.toll",
            "workshop.apprentice", "workshop.size",
            "commission.encounter", "commission.stage", "commission.complete", "commission.referral", "institution.access",
            "invitation.offer", "institution.join", "invitation.wait",
            "person.life", "person.return", "challenge.open", "challenge.stage", "challenge.complete",
        };

        /// <summary>
        /// Runs one command (a <see cref="GameAction.Command"/>, or the same grammar typed). The simulation decides what
        /// happens; this returns its answer and what happened in the world because of it.
        /// </summary>
        public Outcome Do(string command)
        {
            var o = new Outcome();
            var parts = (command ?? "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return o;
            string cmd = parts[0].ToLowerInvariant(), arg = parts.Length > 1 ? parts[1] : "";
            if (cmd == "quit" || cmd == "exit") { o.Ok = true; o.Quit = true; return o; }
            _commands.Add(string.Join(" ", parts));
            if (cmd != "jump" && !(DepartureOpen && PrepCommands.Contains(cmd))) DepartureOpen = false;
            int from = Sim.Log.Events.Count;
            CommandResult r = Sim.Arrived ? AfterArrival(cmd, arg, parts, o) : InEra(cmd, arg, parts, o);
            o.Ok = r.Ok;
            if (o.Message.Length == 0) o.Message = r.Message;
            foreach (var e in Sim.Log.Events.Skip(from).Where(e => Shown.Contains(e.Type) || e.Type.StartsWith("scene.", StringComparison.Ordinal)))
            {
                var (name, portrait) = Speaker(e.Actors.FirstOrDefault(a => a != "player" && Sim.Data.Content.People.Any(p => p.Id == a)) ?? "");
                o.Feed.Add(new FeedLine(e.Text, name, portrait, e.Time.Stamp));
            }
            var settle = Sim.Log.Events.Skip(from).LastOrDefault(e => e.Type == "gold.settle");
            if (settle != null && settle.Text.Contains("could pay only")) o.Feed.Add(new FeedLine(settle.Text, "", "", settle.Time.Stamp));
            _history.AddRange(o.Feed);
            return o;
        }

        private static double Num(string s, out bool ok)
        {
            ok = double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
            return v;
        }

        private CommandResult InEra(string cmd, string arg, string[] parts, Outcome o)
        {
            string third = parts.Length > 2 ? parts[2] : "";
            if (LookScreens.TryGetValue(cmd, out var screen)) { o.Navigate = screen; return CommandResult.Success(cmd == "news" ? string.Join("\n", Sim.News()) : ""); }
            switch (cmd)
            {
                case "end": Sim.EndMonth(); return CommandResult.Success("");
                case "wait":
                {
                    int months = Sim.AdvanceUntilDecision();
                    return CommandResult.Success(months > 1 ? months + " months pass; nothing needed you until now." : "");
                }
                case "jump":
                    if (!Sim.MachineReady) return CommandResult.Fail("The machine isn't ready: " + Sim.MachineStepsDone + " of " + Sim.MachineStepsTotal + " repair steps done.");
                    if (!DepartureOpen) { DepartureOpen = true; o.Navigate = "departure"; return CommandResult.Success(""); }
                    return Leave(o);
                case "choose": return Sim.ChooseSeeded(arg.ToLowerInvariant());
                case "promise": return Sim.AnswerPromise(arg.StartsWith("y", StringComparison.OrdinalIgnoreCase));
                case "respond": return Sim.RespondToPlague(arg.ToLowerInvariant());
                case "decide": return Sim.Decide(arg);
                case "office": return arg == "accept" || arg == "decline" ? Sim.AnswerOffice(third, arg == "accept") : CommandResult.Fail("office accept|decline <institution>");
                case "work": return Sim.Work(parts.Length > 1 ? arg.ToLowerInvariant() : "odd");
                case "take": return Sim.TakeOrder(arg);
                case "expand": return Sim.Expand();
                case "apprentice": return arg.StartsWith("h", StringComparison.OrdinalIgnoreCase) ? Sim.HireApprentice() : Sim.DismissApprentice();
                case "assess": return Sim.Assess();
                case "repair": return Sim.Repair(arg);
                case "upgrade": return Sim.Upgrade(arg);
                case "listen": return Sim.Listen();
                case "restore": { double n = Num(arg, out bool ok); return ok ? Sim.RestoreGold(n) : CommandResult.Fail("restore <aurei>"); }
                case "exchange":
                {
                    double n = Num(arg, out bool ok);
                    string unit = third.ToLowerInvariant();
                    if (!ok || unit.Length == 0) return CommandResult.Fail("exchange <n> aurei|denarii");
                    return unit.StartsWith("den", StringComparison.Ordinal) ? Sim.BuyAurei(Math.Floor(Sim.FromDenarii(n) / Sim.AureiCost(1) + 1e-9)) : Sim.SellAurei(n);
                }
                case "start": return Sim.StartProject(arg.ToLowerInvariant());
                case "invent": return Sim.Invent(arg);
                case "commission":
                {
                    string verb = arg.ToLowerInvariant();
                    return verb == "look" ? Sim.LookAtCommission(third) : verb == "accept" ? Sim.AcceptCommission(third)
                         : verb == "counter" ? Sim.CounterCommission(third) : verb == "decline" ? Sim.DeclineCommission(third)
                         : CommandResult.Fail("commission look|accept|counter|decline <id>");
                }
                case "challenge": return arg.ToLowerInvariant() == "begin" ? Sim.StartChallengeStage(third) : CommandResult.Fail("challenge begin <id>");
                case "invitation":
                    return arg.ToLowerInvariant() == "accept" ? Sim.AcceptInvitation(third) : arg.ToLowerInvariant() == "decline" ? Sim.DeclineInvitation(third) : CommandResult.Fail("invitation accept|decline <institution>");
                case "give": return Sim.Give(arg);
                case "attend": return Sim.Attend(arg, parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : null);
                case "buy":
                {
                    if (!int.TryParse(third.TrimEnd('%'), out int pct)) pct = 1;
                    return Sim.Buy(arg, pct);
                }
                case "found": return Sim.Found(arg);
                case "invest": { double n = Num(third, out bool ok); return ok ? Sim.Invest(arg, Sim.FromDenarii(n)) : CommandResult.Fail("invest <institution> <denarii>"); }
                case "oversee": return Sim.Oversee(arg);
                case "charter": return Sim.Charter(arg);
                case "policy": return Simulation.TryParsePolicy(arg, third, out var issue, out var stance) ? Sim.SetPolicy(issue, stance) : CommandResult.Fail("policy <issue> <stance>");
                case "advocate": return Simulation.TryParsePolicy(arg, third, out var aIssue, out var aStance) ? Sim.Advocate(aIssue, aStance) : CommandResult.Fail("advocate <issue> <stance>");
                case "priority":
                    return DomainInfo.TryParseDomain(arg, out var d) && DomainInfo.TryParsePriority(third, out var p) ? Sim.SetPriority(d, p) : CommandResult.Fail("priority <domain> <protect|maintain|accept>");
                case "deposit": { double n = Num(arg, out bool ok); return ok ? Sim.Deposit(n) : CommandResult.Fail("deposit <aurei>"); }
                case "bury": { double n = Num(arg, out bool ok); return ok ? Sim.Bury(n) : CommandResult.Fail("bury <aurei>"); }
                case "visit": case "walk": return CommandResult.Success(Sim.Visit(arg));
                default: return CommandResult.Fail("That isn't something you can do here.");
            }
        }

        private CommandResult Leave(Outcome o)
        {
            Sim.Jump();
            DepartureOpen = false;
            ShowingArrival = true;
            o.Navigate = "arrival";
            return CommandResult.Success("The machine shudders. Decades pass in the dark... It carries you " + Sim.Arrival!.JumpYears + " years.");
        }

        private CommandResult AfterArrival(string cmd, string arg, string[] parts, Outcome o)
        {
            string target = cmd == "look" && arg == "closer" ? (parts.Length > 2 ? parts[2] : "") : arg;
            if (Sim.World.Return != null)
            {
                if (cmd == "visit" && Sim.FindReturnSite(arg) != null) { ShowingArrival = false; return Sim.VisitReturnSite(arg); }
                if (cmd == "look" && arg == "closer" || cmd == "investigate" || cmd == "closer") { ShowingArrival = false; return Sim.InvestigateReturnSite(target); }
                if (cmd == "done" || cmd == "finish") return Sim.CompleteReturn();
            }
            if (cmd == "journal") { o.Navigate = "journal"; return CommandResult.Success(""); }
            if (cmd == "jump" && Sim.ReturnPending)
                return CommandResult.Fail("Not yet. You have seen " + Sim.World.Return!.Visited.Count + " of the places; see " + Sim.ReturnVisitsRequired + " first.");
            if (cmd == "learn" || cmd == "more") return CommandResult.Success(Sim.Arrival!.LearnMore());
            if (cmd == "jump" && Sim.CanJumpAgain)
            {
                if (!DepartureOpen) { DepartureOpen = true; o.Navigate = "departure"; return CommandResult.Success(""); }
                return Leave(o);
            }
            if ((cmd == "deposit" || cmd == "bury") && Sim.CanJumpAgain)
            {
                double n = Num(arg, out bool ok);
                if (!ok) return CommandResult.Fail(cmd + " <aurei>");
                return cmd == "deposit" ? Sim.Deposit(n) : Sim.Bury(n);
            }
            if (cmd == "visit" || cmd == "walk") return CommandResult.Success(Sim.Visit(arg));
            return CommandResult.Fail("That isn't something you can do here.");
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
