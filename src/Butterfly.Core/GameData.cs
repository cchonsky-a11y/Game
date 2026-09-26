using System;
using System.IO;

namespace Butterfly.Core
{
    /// <summary>Tuning values plus authored content, loaded once from the data/ directory.</summary>
    public sealed class GameData
    {
        public Tuning Tuning { get; }
        public Content Content { get; }
        public string DataDirectory { get; }

        private GameData(Tuning tuning, Content content, string dataDirectory)
        {
            Tuning = tuning;
            Content = content;
            DataDirectory = dataDirectory;
        }

        public static GameData Load(string dataDirectory)
        {
            var tuning = Tuning.Parse(File.ReadAllText(Path.Combine(dataDirectory, "tuning.json")));
            var content = Content.Load(Path.Combine(dataDirectory, "content"));
            return new GameData(tuning, content, dataDirectory);
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
