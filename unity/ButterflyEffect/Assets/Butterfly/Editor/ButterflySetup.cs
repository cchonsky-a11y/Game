#nullable enable
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Butterfly.Unity.Editor
{
    /// <summary>
    /// Editor helpers for the P2 slice: copy the simulation in (tools/sync-unity.sh) and make the one scene a build needs.
    /// The game itself starts in any scene (ButterflyApp boots itself), so neither step creates gameplay.
    /// </summary>
    public static class ButterflySetup
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

        [MenuItem("Butterfly/Sync Core and data")]
        public static void Sync()
        {
            var psi = new ProcessStartInfo("/bin/bash", "tools/sync-unity.sh")
            {
                WorkingDirectory = RepoRoot, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            };
            using (var p = Process.Start(psi)!)
            {
                string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode == 0) UnityEngine.Debug.Log(output);
                else UnityEngine.Debug.LogError("tools/sync-unity.sh failed (is the .NET SDK installed?):\n" + output);
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Butterfly/Create main scene")]
        public static void CreateScene()
        {
            const string path = "Assets/Scenes/Main.unity";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            UnityEngine.Debug.Log("Created " + path + " and made it the build's scene. Press Play: the game starts itself.");
        }
    }
}
