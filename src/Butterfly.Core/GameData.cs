using System;
using System.IO;

namespace Butterfly.Core
{
    /// <summary>Tuning values plus authored content, loaded once from the data/ directory.</summary>
    public sealed class GameData
    {
        public Tuning Tuning { get; }
        public string DataDirectory { get; }

        private GameData(Tuning tuning, string dataDirectory)
        {
            Tuning = tuning;
            DataDirectory = dataDirectory;
        }

        public static GameData Load(string dataDirectory)
        {
            var tuning = Tuning.Parse(File.ReadAllText(Path.Combine(dataDirectory, "tuning.json")));
            return new GameData(tuning, dataDirectory);
        }

        /// <summary>Loads from the nearest data/ directory above <paramref name="startDirectory"/>.</summary>
        public static GameData LoadDefault(string? startDirectory = null)
        {
            return Load(FindDataDirectory(startDirectory ?? AppContext.BaseDirectory));
        }

        public static string FindDataDirectory(string startDirectory)
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "data", "tuning.json");
                if (File.Exists(candidate)) return Path.Combine(dir.FullName, "data");
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Could not find data/tuning.json above " + startDirectory);
        }
    }
}
