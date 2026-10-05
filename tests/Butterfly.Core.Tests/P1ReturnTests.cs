using System;
using System.Collections.Generic;
using System.Linq;
using Butterfly.Batch;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The first return (Part II prototype, 2026-10-04): after the first jump the player explores a few places chosen from what
    /// actually happened; recognition, contradiction, a lead to look closer. The second jump waits until enough has been seen.
    /// When you leave matters: staying to finish a thread changes what the return holds.
    /// </summary>
    [Collection("Console")]
    public class P1ReturnTests
    {
        /// <summary>A workshop player who is a guild member, knows Gaius, has the shared foot reproducible (standards stages 1–2).</summary>
        private static Simulation MidStandards(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 20000;
            var access = sim.World.AccessTo("guild");
            access.RecordMemberRelationship("Felix");
            var ok = new InstitutionInvitationContext("guild", "Felix", true, true, true, true);
            Assert.True(access.TryAcceptGuestInvitation(ok));
            Assert.True(access.TryAcceptGuestInvitation(ok));
            Assert.True(access.TryBecomeSponsoredCandidate(ok));
            Assert.True(access.AdmitMember("Felix"));
            sim.World.ScenesSeen.Add("gaius-meet");
            sim.FindChallenge("standards")!.Status = ChallengeStatus.Open;
            Stage(sim, "standards");
            Stage(sim, "standards");
            Assert.Equal(CapabilityLevel.Reproducible, sim.CapabilityLevelOf("metrology"));
            Assert.True(sim.CapabilityLevelOf("gauges") < CapabilityLevel.Reproducible);
            return sim;
        }

        private static void Stage(Simulation sim, string id)
        {
            for (int m = 0; m < 24 && (sim.World.Attention < 2 || sim.ReservedInMonth(1) > 1); m++) sim.EndMonth();
            var r = sim.StartChallengeStage(id);
            Assert.True(r.Ok, r.Message);
            while (sim.FindChallenge(id)!.Status == ChallengeStatus.Working) sim.EndMonth();
        }

        private static void MachineReady(Simulation sim)
        {
            sim.World.MachineDone.AddRange(sim.Data.Content.MachineSteps.Select(s => s.Id));
            if (sim.Data.Content.MachineAssessment != null) sim.World.MachineDone.Add(sim.Data.Content.MachineAssessment.Id);
            sim.World.MachineGoldRestored = sim.MachineGoldNeeded;
            Assert.True(sim.MachineReady);
        }

        /// <summary>A plain first life to the first arrival, through the real machine gate.</summary>
        private static Simulation Arrived(ulong seed = 42)
        {
            var sim = MidStandards(seed);
            MachineReady(sim);
            sim.Jump();
            return sim;
        }

        // ---- initialization and selection --------------------------------------------------------------------------------

        [Fact]
        public void TheFirstJumpOpensAReturnAndTheSecondDoesNot()
        {
            var sim = Arrived();
            var r = sim.World.Return;
            Assert.NotNull(r);
            Assert.True(sim.ReturnPending);
            Assert.Equal(sim.Now.Year, r!.ArrivalYear);
            Assert.Contains(sim.Log.Events, e => e.Type == "return.begin");
            P1Campaign.FollowReturnProtocol(sim);
            sim.Jump();
            Assert.Same(r, sim.World.Return);                                               // no second chapter
            Assert.False(sim.ReturnPending);
            Assert.Equal(1, sim.Log.Events.Count(e => e.Type == "return.begin"));
        }

        [Fact]
        public void TheReturnOffersAFewUniqueSitesWithinTheTunedRange()
        {
            var sim = Arrived();
            var sites = sim.World.Return!.Sites;
            Assert.InRange(sites.Count, sim.T.GetInt("return.sitesMin"), sim.T.GetInt("return.sitesMax"));
            Assert.Equal(sites.Count, sites.Select(s => s.Id).Distinct().Count());
            foreach (var g in sites.GroupBy(s => s.Category)) Assert.True(g.Count() <= sim.T.GetInt("return.maxPerCategory"), g.Key + " over its cap");
            Assert.True(sites.Select(s => s.Category).Distinct().Count() >= 3);
            Assert.All(sites, s => { Assert.False(string.IsNullOrWhiteSpace(s.Recognition)); Assert.False(string.IsNullOrWhiteSpace(s.Contradiction)); Assert.DoesNotContain("{", s.Recognition + s.Contradiction + s.Investigation); });
        }

        [Fact]
        public void TheSameStateGivesTheSameReturn()
        {
            string Describe(Simulation s) => string.Join("\n", s.World.Return!.Sites.Select(x => x.Id + "/" + x.Variant + "/" + x.Recognition + x.Contradiction));
            Assert.Equal(Describe(Arrived()), Describe(Arrived()));
        }

        [Fact]
        public void EverySiteMeetsItsConditionsAndIsGroundedInWhatHappened()
        {
            var sim = Arrived();
            int arrive = sim.Log.Events.Last(e => e.Type == "jump.arrive").Id;
            foreach (var s in sim.World.Return!.Sites)
            {
                if (s.Category == ReturnCategory.Journal)
                    Assert.Contains(sim.World.Journal, j => "journal-" + j.Id == s.Id);
                else
                {
                    var def = sim.Data.Content.ReturnSites.Single(d => d.Id == s.Id);
                    Assert.All(def.Requires, q => Assert.True(sim.Holds(q), s.Id + ": " + q));
                    var variant = def.Variants.Single(v => v.Id == s.Variant);
                    Assert.All(variant.Requires, q => Assert.True(sim.Holds(q), s.Id + "/" + s.Variant + ": " + q));
                    Assert.Same(variant, def.Variants.First(v => v.Requires.All(sim.Holds)));     // the first variant that holds
                }
                Assert.NotEmpty(s.Grounds);
                Assert.All(s.Grounds, id => Assert.True(id <= arrive && sim.Log.Events.Any(e => e.Id == id), s.Id + " grounded in event " + id));
            }
            // The standards thread is grounded in the work that took the shared foot there.
            var fittings = sim.World.Return!.Sites.Single(s => s.Id == "fittings");
            Assert.Contains(fittings.Grounds, id => sim.Log.Events.First(e => e.Id == id).Target.StartsWith("challenge:standards"));
        }

        [Fact]
        public void ScriptedRunsFindSeveralKindsOfEvidence()
        {
            foreach (ulong seed in new ulong[] { 1, 2, 3, 4 })
            {
                var r = P1Campaign.Play(TestData.Load(), seed);
                Assert.True(r.ReturnStarted, "seed " + seed);
                Assert.True(r.ReturnCompleted, "seed " + seed);
                Assert.True(r.ReturnCategories.Distinct().Count() >= 3, "seed " + seed + ": " + string.Join(",", r.ReturnCategories));
                Assert.Equal(0, r.Bugs);
            }
        }

        // ---- exploring and the gate --------------------------------------------------------------------------------------

        [Fact]
        public void VisitingShowsRecognitionAndContradictionAndLookingCloserFollowsTheLead()
        {
            var sim = Arrived();
            var s = sim.World.Return!.Sites[0];
            Assert.False(sim.InvestigateReturnSite("1").Ok);                                // go there first
            var visit = sim.VisitReturnSite("1");
            Assert.True(visit.Ok);
            Assert.Contains(s.Recognition, visit.Message);
            Assert.Contains(s.Contradiction, visit.Message);
            Assert.Contains(s.Lead, visit.Message);
            var closer = sim.InvestigateReturnSite(s.Id);                                   // by id as well as by number
            Assert.True(closer.Ok);
            Assert.Contains(s.Investigation, closer.Message);
            var ev = sim.Log.Events.Last(e => e.Type == "return.visit");
            Assert.Equal(s.Grounds, ev.ImmediateCauses);                                    // the log keeps why the site exists
            sim.VisitReturnSite("1");
            Assert.Equal(1, sim.Log.Events.Count(e => e.Type == "return.visit"));            // a second look isn't a second event
            Assert.Contains("(looked closer)", sim.ReturnLeads().First());
        }

        [Fact]
        public void TheSecondJumpWaitsForTheReturnButNotForEverySite()
        {
            var sim = Arrived();
            var r = sim.World.Return!;
            int need = sim.ReturnVisitsRequired;
            Assert.True(r.Sites.Count > need);                                               // never all of them
            Assert.False(sim.CanJumpAgain);
            Assert.Throws<InvalidOperationException>(() => sim.Jump());
            for (int k = 1; k < need; k++) sim.VisitReturnSite(k.ToString());
            Assert.False(sim.CompleteReturn().Ok);
            Assert.False(sim.CanJumpAgain);
            sim.VisitReturnSite(need.ToString());
            Assert.False(sim.CanJumpAgain);                                                  // finishing is a choice
            Assert.True(sim.CompleteReturn().Ok);
            Assert.True(sim.CanJumpAgain);
            Assert.True(r.Visited.Count < r.Sites.Count);
            Assert.False(sim.CompleteReturn().Ok);                                           // once
            sim.Jump();
            Assert.Equal(2, sim.JumpsMade);
        }

        [Fact]
        public void LookingAroundDoesNotTouchTheGeneratorTheLedgerOrTheSecondJump()
        {
            var thorough = Arrived();
            var quick = Arrived();
            ulong next = thorough.Rng.Clone().NextULong();
            int ledger = thorough.World.Ledger.Entries.Count;
            double gold = thorough.World.Gold;
            for (int k = 0; k < 5; k++) { thorough.ReturnLeads().ToList(); thorough.FindReturnSite("1"); }
            for (int k = 1; k <= thorough.World.Return!.Sites.Count; k++) { thorough.VisitReturnSite(k.ToString()); thorough.InvestigateReturnSite(k.ToString()); }
            Assert.Equal(next, thorough.Rng.Clone().NextULong());
            Assert.Equal(ledger, thorough.World.Ledger.Entries.Count);
            Assert.Equal(gold, thorough.World.Gold);
            thorough.CompleteReturn();
            P1Campaign.FollowReturnProtocol(quick);
            var a = thorough.Jump();
            var b = quick.Jump();
            Assert.Equal(b.JumpYears, a.JumpYears);
            Assert.Equal(b.IndexAfter, a.IndexAfter);
        }

        [Fact]
        public void TheConsoleWalksThroughTheReturn()
        {
            var sim = MidStandards();
            MachineReady(sim);
            string output = ConsoleTests.Play(sim, "jump", "jump", "jump", "visit 1", "look closer 1", "visit 2", "visit 3", "journal", "done", "jump");
            Assert.Contains("Places to look:", output);
            Assert.Contains("Not yet.", output);                                            // 'jump' before the return is seen
            Assert.Contains(sim.World.Return!.Sites[0].Investigation, output.Replace("\n", " "));
            Assert.Contains("You have seen enough of this Rome.", output);
            Assert.Contains("Prepare: deposit <aurei> · bury <aurei>", output);            // the next jump's briefing
        }

        // ---- elapsed time ------------------------------------------------------------------------------------------------

        [Fact]
        public void TwentyFiveYearsAndSixtyYearsFindDifferentPeople()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var gaius = sim.PersonDefOf("Gaius")!;
            var marcus = sim.PersonDefOf("Marcus")!;
            var felix = sim.PersonDefOf("Felix")!;
            Assert.Equal(HumanBand.Elder, sim.HumanBandAt(gaius, 163 + 25)!.Value.Band);   // old, at his shop
            Assert.Equal(HumanBand.Memory, sim.HumanBandAt(gaius, 163 + 60)!.Value.Band);  // a name on a shop
            Assert.Equal(HumanBand.Self, sim.HumanBandAt(marcus, 163 + 25)!.Value.Band);
            Assert.Equal(HumanBand.Heirs, sim.HumanBandAt(marcus, 163 + 60)!.Value.Band);
            Assert.Equal(HumanBand.Heirs, sim.HumanBandAt(felix, 163 + 25)!.Value.Band);
            Assert.Equal(HumanBand.Memory, sim.HumanBandAt(felix, 163 + 60)!.Value.Band);
            Assert.Null(sim.HumanBandAt(sim.PersonDefOf("Livia")!, 190));                     // outside the core cast
        }

        [Fact]
        public void NobodyIsStillYoungAfterTheirTime()
        {
            var sim = new Simulation(TestData.Load(), 42);
            int elder = sim.T.GetInt("return.elderAge");
            foreach (var p in sim.Data.Content.People.Where(p => p.Age > 0))
                for (int year = 180; year <= 240; year++)
                {
                    var (band, age, since) = sim.HumanBandAt(p, year)!.Value;
                    if (band == HumanBand.Self) Assert.True(age < elder && age < p.LivesTo, p.Id + " " + year);
                    if (band == HumanBand.Elder) Assert.True(age >= elder && age < p.LivesTo, p.Id + " " + year);
                    if (band >= HumanBand.Heirs) Assert.True(age >= p.LivesTo && since == age - p.LivesTo, p.Id + " " + year);
                    Assert.Equal(band, sim.HumanBandAt(p, year)!.Value.Band);                // deterministic
                }
        }

        [Fact]
        public void TheSameLifeReadsDifferentlyAfterAShortAndALongAbsence()
        {
            var sim = MidStandards();
            sim.World.ScenesSeen.Add("marcus-meet");
            var shortReturn = sim.ReturnSitesAt(163 + 25).Where(s => s.Category == ReturnCategory.Human).ToList();
            var longReturn = sim.ReturnSitesAt(163 + 60).Where(s => s.Category == ReturnCategory.Human).ToList();
            var gShort = shortReturn.Concat(sim.ReturnCandidates(new ReturnChapter { ArrivalYear = 163 + 25 })).First(s => s.Person == "Gaius");
            var gLong = longReturn.Concat(sim.ReturnCandidates(new ReturnChapter { ArrivalYear = 163 + 60 })).First(s => s.Person == "Gaius");
            Assert.Equal(HumanBand.Elder, gShort.Band);
            Assert.Equal(HumanBand.Memory, gLong.Band);
            Assert.NotEqual(gShort.Recognition, gLong.Recognition);
            Assert.Contains("Gaius is at the shop", gShort.Recognition);
            Assert.DoesNotContain("Gaius is at the shop", gLong.Recognition);
        }

        // ---- the journal -------------------------------------------------------------------------------------------------

        [Fact]
        public void TheJournalKeepsWhatYouWroteAndTheCityKeepsSomethingElse()
        {
            var sim = MidStandards();
            var foot = Assert.Single(sim.World.Journal, j => j.Id == "foot");
            var def = sim.Data.Content.JournalAnchors.Single(a => a.Id == "foot");
            Assert.Equal(def.Then, foot.Text);
            Assert.Equal("journal.note", sim.Log.Events.First(e => e.Id == foot.EventId).Type);
            string written = foot.Text;
            for (int m = 0; m < 6; m++) sim.EndMonth();
            Assert.Single(sim.World.Journal, j => j.Id == "foot");                           // written once
            MachineReady(sim);
            sim.Jump();
            Assert.Equal(written, sim.World.Journal.Single(j => j.Id == "foot").Text);       // the record never changes
            var site = sim.ReturnCandidates(sim.World.Return!).Single(s => s.Id == "journal-foot");
            Assert.Contains(written, site.Recognition);
            Assert.DoesNotContain(written, site.Contradiction);
            Assert.Contains(site.Grounds, id => id == foot.EventId);
            Assert.Contains("AD " + foot.Year, string.Join(" ", sim.JournalLines()));
        }

        // ---- sharpening pass (2026-10-04): threads first, unwarned consequences, institutions in their own words ----------

        private static List<ReturnSite> Humans(Simulation sim) => sim.ReturnSitesAt(163 + 25).Where(s => s.Category == ReturnCategory.Human).ToList();

        [Fact]
        public void APersonWhoseStoryTurnedOnYourDepartureIsFoundFirstEvenIfLessClose()
        {
            var sim = MidStandards();
            sim.World.ScenesSeen.Add("marcus-meet");
            sim.PersonOf("Gaius")!.Regard = 9;                                            // far closer than Marcus
            sim.PersonOf("Marcus")!.Regard = 0;
            var humans = Humans(sim);
            Assert.Equal("Marcus", humans[0].Person);                                      // undecided when you left: a thread
            Assert.Equal("marcus", humans[0].Thread);
            Assert.Equal("Gaius", humans[1].Person);                                       // the rest by closeness, as before
            Assert.Equal(humans.Count, humans.Select(h => h.Id).Distinct().Count());
        }

        [Fact]
        public void TheThreadedPersonIsWhoeverTheThreadBelongsTo()
        {
            var sim = MidStandards();
            sim.World.ScenesSeen.Add("aulus-meet");
            sim.PersonOf("Gaius")!.Regard = 9;
            var power = sim.FindChallenge("power")!;
            power.Status = ChallengeStatus.Open;
            power.StageIndex = 2;                                                          // the shaft part-built
            var humans = Humans(sim);
            Assert.Equal("Aulus", humans[0].Person);
            Assert.Equal("shaft", humans[0].Thread);
        }

        [Fact]
        public void WithoutAThreadedPersonTheClosestComeFirstAsBefore()
        {
            var sim = MidStandards();
            sim.PersonOf("Gaius")!.Regard = 9;
            var all = sim.ReturnCandidates(new ReturnChapter { ArrivalYear = 163 + 25 }).Where(s => s.Category == ReturnCategory.Human).ToList();
            Assert.DoesNotContain(all, s => s.Thread.Length > 0);
            var expected = all.OrderByDescending(s => sim.PersonOf(s.Person)!.Regard).Take(sim.T.GetInt("return.maxPerCategory")).Select(s => s.Person).ToList();
            Assert.Equal(expected, Humans(sim).Select(s => s.Person).ToList());
            var sites = sim.ReturnSitesAt(163 + 25);
            Assert.True(sites.Count <= sim.T.GetInt("return.sitesMax"));
            Assert.All(sites.GroupBy(s => s.Category), g => Assert.True(g.Count() <= sim.T.GetInt("return.maxPerCategory")));
        }

        [Fact]
        public void ThreadPriorityIsDeterministic()
        {
            string Pick() { var s = MidStandards(); s.World.ScenesSeen.Add("marcus-meet"); s.World.ScenesSeen.Add("aulus-meet"); return string.Join(",", s.ReturnSitesAt(190).Select(x => x.Id + "/" + x.Variant)); }
            Assert.Equal(Pick(), Pick());
        }

        [Fact]
        public void TheBriefingWarnsOfWhatYouLeaveNotOfEverythingItLeadsTo()
        {
            // Staying to finish the gauges, nothing about the foot is left unresolved, so the briefing names nothing about it.
            // The return still holds a grounded consequence of that work nobody warned of: the guild owns the foot and sells the stamp.
            var sim = MidStandards();
            MachineReady(sim);
            Stage(sim, "standards");
            Stage(sim, "standards");
            var briefing = sim.DepartureBriefing().ToList();
            sim.Jump();
            var r = sim.World.Return!;
            Assert.DoesNotContain("foot", r.WarnedThreads);
            var fittings = r.Sites.Single(s => s.Id == "fittings");
            Assert.Equal("guildfoot", fittings.Variant);
            Assert.False(fittings.Warned);
            Assert.DoesNotContain(briefing, l => l.Contains("seal") || l.Contains("stamp") || l.Contains("foot"));
            int depart = sim.Log.Events.First(e => e.Type == "jump.depart").Id;
            Assert.Contains(fittings.Grounds, id => id < depart && sim.Log.Events.First(e => e.Id == id).Type == "capability.advance");
        }

        [Fact]
        public void AThreadTheBriefingNamedIsMarkedWarned()
        {
            var sim = MidStandards();
            MachineReady(sim);
            Assert.Contains(sim.DepartureBriefing(), l => l.Contains("The shared foot isn't finished"));
            sim.Jump();
            Assert.Contains("foot", sim.World.Return!.WarnedThreads);
            Assert.True(sim.World.Return.Sites.Single(s => s.Id == "fittings").Warned);
            Assert.Contains(sim.World.Return.Sites, s => !s.Warned && s.Grounds.Count > 0);   // and something it didn't name
        }

        [Fact]
        public void TheGuildSpeaksThroughItsOwnRecord()
        {
            var sim = Arrived();
            var hall = sim.World.Return!.Sites.Single(s => s.Id == "guildhall");
            Assert.Equal("rule", hall.Variant);
            Assert.Contains("Cut into the marble", hall.Recognition);
            Assert.Contains("“Every member's rule shall be tried", hall.Recognition);      // the article itself, in its words
            Assert.DoesNotContain("isn't written anywhere", hall.Recognition + hall.Contradiction);
        }

        // ---- when you leave matters ----------------------------------------------------------------------------------------

        [Fact]
        public void StayingToFinishTheSharedFootChangesWhatTheReturnHolds()
        {
            // Path A: the machine is ready with the shared foot half-made (gauges not yet reproducible); you leave.
            var leave = MidStandards();
            MachineReady(leave);
            var briefingA = leave.DepartureBriefing().ToList();
            Assert.Contains(briefingA, l => l.Contains("The shared foot isn't finished"));
            leave.Jump();

            // Path B: the same life to the same moment; you stay and finish the gauges, then leave.
            var stay = MidStandards();
            MachineReady(stay);
            Assert.Equal(briefingA, stay.DepartureBriefing().ToList());                     // the same moment, the same briefing
            Stage(stay, "standards");
            Stage(stay, "standards");
            Assert.Equal(CapabilityLevel.Reproducible, stay.CapabilityLevelOf("gauges"));
            Assert.DoesNotContain(stay.DepartureBriefing(), l => l.Contains("The shared foot isn't finished"));
            stay.Jump();

            var a = leave.World.Return!.Sites.Single(s => s.Id == "fittings");
            var b = stay.World.Return!.Sites.Single(s => s.Id == "fittings");
            Assert.Equal("foot", a.Thread);
            Assert.Equal("drift", a.Variant);                                               // every shop its own foot again
            Assert.Equal("guildfoot", b.Variant);                                           // one foot, under the guild's seal
            Assert.NotEqual(a.Recognition + a.Contradiction, b.Recognition + b.Contradiction);
            Assert.False(a.Misattributed);
            Assert.True(b.Misattributed);                                                   // it survived, under another name
            Assert.Contains(b.Grounds, id => stay.Log.Events.First(e => e.Id == id).Type == "capability.advance");
            // The journal line you wrote then survives differently: as luck when you left, as the guild's order when you stayed.
            var ja = leave.ReturnCandidates(leave.World.Return!).Single(s => s.Id == "journal-foot");
            var jb = stay.ReturnCandidates(stay.World.Return!).Single(s => s.Id == "journal-foot");
            Assert.Equal("luck", ja.Variant);
            Assert.Equal("guild", jb.Variant);
        }
    }
}
