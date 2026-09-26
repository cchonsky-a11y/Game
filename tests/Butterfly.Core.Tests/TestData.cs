namespace Butterfly.Core.Tests
{
    internal static class TestData
    {
        private static GameData? _data;

        /// <summary>The real data/ directory, so tests check the shipped tuning values.</summary>
        public static GameData Load() => _data ??= GameData.LoadDefault();
    }
}
