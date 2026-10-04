using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 polish pass (2026-10-04): a relationship-first way toward a senator's house, without seats for sale. After the early
    /// months, a credible intermediary (Cassianus, who sells the house oil, or Serenus, called to its sickroom) introduces the
    /// inventor to Senator Varro's freedman steward, Hermogenes; the inventor stands at the morning salutatio as one client
    /// among forty; and the house soon asks a favor with a cost either way. Access is not power: no place in the following,
    /// no office, no policy.
    /// </summary>
    [Collection("Console")]
    public class P1PoliticalAccessTests
    {
        private static Simulation Trusted(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 5000;
            sim.FindCommission("cellarpump")!.Status = CommissionStatus.Done;
            sim.PersonOf("Cassianus")!.Regard = 2;
            return sim;
        }

        private static void Until(Simulation sim, string scene, int months = 60)
        {
            for (int m = 0; m < months && !sim.World.ScenesSeen.Contains(scene); m++)
            {
                if (sim.PendingEvent != null && sim.PendingEvent.Id != "patronfavor") sim.Decide(sim.PendingEvent.Options[0].Id);
                sim.EndMonth();
            }
        }

        [Fact]
        public void NoSenatorsHouseInTheEarlyMonths()
        {
            var sim = Trusted();
            for (int m = 0; m < 17; m++) sim.EndMonth();
            Assert.DoesNotContain("scene:patron-cassianus", sim.SceneCandidateIds());
            Assert.DoesNotContain("patron-cassianus", sim.World.ScenesSeen);
        }

        [Fact]
        public void ATrustedMerchantIntroducesYouToTheStewardNotTheFollowing()
        {
            var sim = Trusted();
            Until(sim, "patron-cassianus");
            Assert.Contains("patron-cassianus", sim.World.ScenesSeen);
            Assert.True(sim.Knows("Hermogenes"));
            var faction = sim.World.Institution("faction");
            Assert.Equal(InstitutionAccessStage.KnowsMember, sim.World.AccessTo("faction").Stage);
            Assert.False(faction.Backed);                                              // a client, not a member
            Assert.Equal(0, faction.Stake);
            Assert.Contains("salutatio as a client", sim.PatronageStanding(faction));
            Assert.Contains("not a place in his following", sim.PatronageStanding(faction));
            Assert.False(sim.Buy("faction", 1).Ok);                                      // still no seats for sale
        }

        [Fact]
        public void OnlyOneIntroductionHappens()
        {
            var sim = Trusted();
            sim.PersonOf("Serenus")!.Regard = 5;
            sim.World.ScenesSeen.Add("serenus-meet");
            Until(sim, "salutatio-varro", 80);
            Assert.Equal(1, sim.World.ScenesSeen.Count(s => s == "patron-cassianus" || s == "patron-serenus"));
        }

        [Fact]
        public void TheHouseAsksAFavorAndEveryAnswerCostsSomething()
        {
            foreach (var answer in new[] { "oblige", "measure", "decline" })
            {
                var sim = Trusted();
                sim.World.Flags.Add("subura-allotment");                                // you cast honest calices with Gaius
                Until(sim, "salutatio-varro", 80);
                for (int m = 0; m < 3 && sim.PendingEvent?.Id != "patronfavor"; m++) sim.EndMonth();
                Assert.Equal("patronfavor", sim.PendingEvent?.Id);
                double factionBefore = sim.World.Institution("faction").Regard, junianBefore = sim.World.Institution("junian").Regard;
                int gaiusBefore = sim.PersonOf("Gaius")!.Regard, stewardBefore = sim.PersonOf("Hermogenes")!.Regard;
                Assert.True(sim.Decide(answer).Ok);
                var faction = sim.World.Institution("faction");
                if (answer == "oblige")
                {
                    Assert.True(faction.Regard > factionBefore);
                    Assert.True(sim.World.Institution("junian").Regard < junianBefore);   // the other house notices
                    Assert.True(sim.PersonOf("Gaius")!.Regard < gaiusBefore);            // and so does the street you measured for
                    Assert.Contains("varro-client", sim.World.Flags);
                }
                if (answer == "measure") Assert.True(faction.Regard > factionBefore);
                if (answer == "decline") Assert.True(sim.PersonOf("Hermogenes")!.Regard < stewardBefore);
                Assert.False(faction.Backed);                                          // whatever you answer, no seat comes of it
            }
        }

        [Fact]
        public void TheArchiveTakesASenatorsNoteNotMembership()
        {
            // Corey, 2026-10-04: the chronometer's archive step is reached through the senator's household (the salutatio),
            // not faction membership; bribing a clerk stays possible.
            var sim = Trusted();
            var archives = sim.Data.Content.MachineSteps.Single(m => m.Id == "archives");
            int bribe = (int)System.Math.Round(archives.AltGold * sim.World.PriceLevel);
            Assert.False(sim.MachineRequirementMet(archives));                          // a stranger to the house
            Assert.Equal(bribe, sim.MachineStepGold(archives));                          // the clerk's price

            Until(sim, "patron-cassianus");
            Assert.True(sim.Knows("Hermogenes"));
            Assert.False(sim.MachineRequirementMet(archives));                          // knowing the steward isn't a note
            sim.World.Institution("faction").Regard = 10;
            Assert.False(sim.MachineRequirementMet(archives));                          // nor is the house's goodwill

            Until(sim, "salutatio-varro", 80);
            Assert.Contains("salutatio-varro", sim.World.ScenesSeen);
            Assert.True(sim.MachineRequirementMet(archives));                           // the client's note opens the door
            Assert.False(sim.World.Institution("faction").Backed);                      // without making you a member
            Assert.Equal((int)System.Math.Round(archives.Gold * sim.World.PriceLevel), sim.MachineStepGold(archives));
        }
    }
}
