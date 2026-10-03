using Butterfly.Core;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class P1IntegrationTests
    {
        [Fact]
        public void SceneRouterPenalizesThirdEngineeringScene()
        {
            var pacing = ScenePacingState.FromTuning(TestData.Load().Tuning);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.Engineering);
            var router = SceneRouter.FromTuning(new Rng(1), TestData.Load().Tuning);
            var engineering = new SceneCandidate("engineering", SceneCategory.Engineering, 1);
            var personal = new SceneCandidate("personal", SceneCategory.Personal, 1);

            Assert.Equal(0.15, router.EffectiveWeight(engineering, pacing), 6);
            Assert.Equal(1.0, router.EffectiveWeight(personal, pacing), 6);
        }

        [Fact]
        public void ExplicitFocusRemovesRepeatedCategoryPenalty()
        {
            var pacing = ScenePacingState.FromTuning(TestData.Load().Tuning);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.Engineering);
            var router = SceneRouter.FromTuning(new Rng(1), TestData.Load().Tuning);
            var engineering = new SceneCandidate("engineering", SceneCategory.Engineering, 1);

            Assert.Equal(1.0, router.EffectiveWeight(engineering, pacing, SceneCategory.Engineering), 6);
        }

        [Fact]
        public void WorldInterruptIsNotPenalizedByRepeatedCategory()
        {
            var pacing = ScenePacingState.FromTuning(TestData.Load().Tuning);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.Engineering);
            var router = SceneRouter.FromTuning(new Rng(1), TestData.Load().Tuning);
            var accident = new SceneCandidate("accident", SceneCategory.Engineering, 1, isInterrupt: true);

            Assert.Equal(1.0, router.EffectiveWeight(accident, pacing), 6);
        }

        [Fact]
        public void ProjectAccountingMakesPaymentAndMaterialsVisible()
        {
            var ledger = new EconomyLedger();
            var terms = new ProjectTerms(
                ProjectFundingModel.ClientPaid,
                payer: "Publius",
                materialsPayer: "Inventor",
                upfrontGold: 12,
                completionGold: 20,
                playerMaterialCost: 5);
            var project = new ProjectState("pump", "Pump", "Repair pump.", "Publius", terms, 2, ProjectStage.Agreed);

            ProjectAccounting.RecordAgreement(ledger, project);
            project.Complete();
            ProjectAccounting.RecordCompletion(ledger, project);

            Assert.Equal(27, ledger.Net);
            Assert.Equal(32, ledger.Income);
            Assert.Equal(5, ledger.Expenses);
            Assert.Equal(3, ledger.ForProject("pump").Count);
        }

        [Fact]
        public void InstitutionCannotSkipRelationshipAndGuestHistory()
        {
            var access = new InstitutionAccessState("ostia-craftsmen");
            var invite = new InstitutionInvitationContext("ostia-craftsmen", "felix", true, true, true, true);

            Assert.False(access.TryBecomeSponsoredCandidate(invite));
            Assert.False(access.TryAcceptGuestInvitation(invite));

            access.RecordMemberRelationship("felix");
            Assert.True(access.TryAcceptGuestInvitation(invite));
            Assert.Equal(InstitutionAccessStage.Guest, access.Stage);
            Assert.True(access.TryAcceptGuestInvitation(invite));
            Assert.Equal(InstitutionAccessStage.InvitedBack, access.Stage);
            Assert.True(access.TryBecomeSponsoredCandidate(invite));
            Assert.Equal("felix", access.SponsorId);
            Assert.True(access.AdmitMember("felix"));
            Assert.Equal(InstitutionAccessStage.Member, access.Stage);
        }

        [Fact]
        public void WrongInstitutionInvitationCannotAdvanceAccess()
        {
            var access = new InstitutionAccessState("ostia-craftsmen");
            access.RecordMemberRelationship("felix");
            var medicalInvite = new InstitutionInvitationContext("medical-circle", "serenus", true, true, true, true);

            Assert.False(access.TryAcceptGuestInvitation(medicalInvite));
            Assert.Equal(InstitutionAccessStage.KnowsMember, access.Stage);
        }
    }
}
