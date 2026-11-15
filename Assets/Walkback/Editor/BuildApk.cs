// Written with Claude Code (AI), directed and tested by team MARSXR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Walkback.EditorTools
{
    /// <summary>
    /// The Quest APK from the command line, the same as File > Build with the project's own settings:
    /// the scenes of the build list, Android, a Development build (so logcat shows the app's lines).
    ///   Unity.exe -batchmode -force-d3d11 -projectPath . -buildTarget Android -executeMethod Walkback.EditorTools.BuildApk.Run -logFile TestOutput/build.log -quit
    /// The APK lands in Builds/walkback.apk (APKs are never committed: .gitignore).
    /// </summary>
    public static class BuildApk
    {
        public const string OutputPath = "Builds/walkback.apk";

        [MenuItem("Walkback/Build APK")]
        public static void Run()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("Walkback APK: " + summary.result + ", " + (summary.totalSize / 1048576f).ToString("F1") + " MB, " +
                summary.totalTime.TotalSeconds.ToString("F0") + " s, " + Path.GetFullPath(OutputPath));
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
