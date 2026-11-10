namespace Butterfly.Core.Tests
{
    internal static class TestData
    {
        private static GameData? _data;

        /// <summary>The real data/ directory, so tests check the shipped tuning values.</summary>
        public static GameData Load() => _data ??= GameData.LoadDefault();

        private static GameData? _noIllness;

        /// <summary>
        /// The shipped data with the inventor's illnesses switched off (slice 1a), for tests about something else that play
        /// through the plague years and must not end in a death.
        /// </summary>
        public static GameData NoIllness() => _noIllness ??= Load().WithTuning(new System.Collections.Generic.Dictionary<string, double>
        {
            { "health.plague.illnessPerDeathShare", 0 }, { "health.fever.chance", 0 }, { "health.fever.perYearOver", 0 },
        });
    }
}
