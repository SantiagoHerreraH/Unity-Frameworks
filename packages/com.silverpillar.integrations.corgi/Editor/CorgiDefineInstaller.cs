#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;

namespace SilverPillar.Integrations.Corgi.Editor
{
    [InitializeOnLoad]
    public static class CorgiDefineInstaller
    {
        private const string Define = "CORGI_ENGINE";
        private const string CorgiAssemblyName = "MoreMountains.CorgiEngine";

        static CorgiDefineInstaller()
        {
            EditorApplication.delayCall += UpdateDefineForActiveTarget;
        }

        public static void UpdateDefineForActiveTarget()
        {
            bool hasCorgi = HasAssembly(CorgiAssemblyName);

            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup activeGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(activeGroup);

            if (namedTarget == NamedBuildTarget.Unknown)
                return;

            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

            var set = defines
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToHashSet(StringComparer.Ordinal);

            bool changed = false;

            if (hasCorgi)
            {
                if (set.Add(Define))
                    changed = true;
            }
            else
            {
                if (set.Remove(Define))
                    changed = true;
            }

            if (!changed)
                return;

            string newDefines = string.Join(";", set.OrderBy(x => x, StringComparer.Ordinal));
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
        }

        private static bool HasAssembly(string assemblyName)
        {
            return CompilationPipeline.GetAssemblies()
                .Any(a => a.name == assemblyName);
        }
    }

    public sealed class CorgiBuildTargetWatcher : IActiveBuildTargetChanged
    {
        public int callbackOrder => 0;

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            EditorApplication.delayCall += CorgiDefineInstaller.UpdateDefineForActiveTarget;
        }
    }
}
#endif