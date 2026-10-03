using Butterfly.Core;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class P1ArchitectureTests
    {
        [Fact]
        public void MenuArchitectureExposesEightPrimarySections()
        {
            Assert.Equal(8, System.Enum.GetValues<MenuSection>().Length);
        }

        [Fact]
        public void SceneRouterDeprioritizesThirdRepeatedCategory()
        {
            var pacing = ScenePacingState.FromTuning(TestData.Load().Tuning);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.Engineering);

            Assert.True(pacing.ShouldDeprioritize(SceneCategory.Engineering));
            Assert.False(pacing.ShouldDeprioritize(SceneCategory.Personal));
        }

        [Fact]
        public void ExplicitPlayerFocusCanContinueSameSceneCategory()
        {
            var pacing = ScenePacingState.FromTuning(TestData.Load().Tuning);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.Engineering);

            Assert.False(pacing.ShouldDeprioritize(SceneCategory.Engineering, playerExplicitlyFocused: true));
        }

        [Fact]
        public void ChangingSceneCategoryResetsConsecutiveCount()
        {
            var pacing = ScenePacingState.FromTuning(TestData.Load().Tuning);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.Engineering);
            pacing.Record(SceneCategory.RomanLife);

            Assert.Equal(SceneCategory.RomanLife, pacing.LastCategory);
            Assert.Equal(1, pacing.ConsecutiveCount);
        }

        [Fact]
        public void ProjectHasExplicitCommercialTermsBeforeWorkBegins()
        {
            var terms = new ProjectTerms(
                ProjectFundingModel.ClientPaid,
                payer: "Bath complex owner",
                materialsPayer: "Bath complex owner",
                upfrontGold: 10,
                completionGold: 20);

            var project = new ProjectState(
                "pump-repair",
                "Pump repair",
                "Stop repeated cylinder failures.",
                "Bath complex owner",
                terms,
                durationMonths: 2,
                stage: ProjectStage.Agreed);

            Assert.Equal(ProjectFundingModel.ClientPaid, project.Terms.FundingModel);
            Assert.Equal("Bath complex owner", project.Terms.Payer);
            Assert.Equal(2, project.MonthsRemaining);
        }

        [Fact]
        public void ProjectProgressUsesMonths()
        {
            var terms = new ProjectTerms(ProjectFundingModel.SelfFundedResearch, "Inventor", "Inventor");
            var project = new ProjectState("metallurgy", "Metallurgy", "Improve reproducibility.", "Inventor", terms, 2, ProjectStage.Observation);

            project.AdvanceMonth();

            Assert.Equal(1, project.MonthsRemaining);
        }

        [Theory]
        [InlineData("felix", true, true, true, true, true)]
        [InlineData("", true, true, true, true, false)]
        [InlineData("felix", false, true, true, true, false)]
        [InlineData("felix", true, false, true, true, false)]
        [InlineData("felix", true, true, false, true, false)]
        [InlineData("felix", true, true, true, false, false)]
        public void InstitutionInvitationRequiresFullCausalChain(
            string inviter,
            bool relationship,
            bool relevantWork,
            bool useful,
            bool sponsorRisk,
            bool expected)
        {
            var gate = new InstitutionInvitationContext(
                "ostia-craftsmen",
                inviter,
                relationship,
                relevantWork,
                useful,
                sponsorRisk);

            Assert.Equal(expected, gate.IsWarranted);
        }
    }
}
