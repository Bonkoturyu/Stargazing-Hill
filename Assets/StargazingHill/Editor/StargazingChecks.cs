using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace StargazingHill.Editor
{
    /// <summary>
    /// Project-wide invariants that only Unity can decide, kept apart from the world builder so the local
    /// check runner has one place to call.
    ///
    /// The static Python validation cannot reach any of this: it reads source text and asset hashes without
    /// ever compiling the project or loading UdonSharp, so a behaviour with no compiled program asset looks
    /// perfectly fine to it.
    /// </summary>
    public static class StargazingChecks
    {
        private const string ProjectAssemblyName = "Assembly-CSharp";
        // Third-party UdonSharp behaviours also land in Assembly-CSharp when they ship as plain assets
        // rather than as packages, and they manage their own program assets. Only this project's namespace
        // is ours to guarantee.
        private const string ProjectNamespace = "StargazingHill";

        /// <summary>
        /// Every UdonSharpBehaviour in the project must have a compiled U# program asset. Without one,
        /// UdonSharpUndo.AddComponent throws at scene-build time rather than at compile time, so nothing
        /// short of actually loading UdonSharp catches the omission.
        /// </summary>
        public static void CheckUdonSharpProgramAssetsForBatchMode()
        {
            List<Type> behaviours = GetProjectUdonSharpBehaviours();
            if (behaviours.Count == 0)
                throw new InvalidOperationException(
                    "No project UdonSharpBehaviour types were found; the check would pass vacuously.");

            var missing = new List<string>();
            for (int index = 0; index < behaviours.Count; index++)
            {
                Type behaviour = behaviours[index];
                UdonSharpProgramAsset programAsset =
                    UdonSharpEditorUtility.GetUdonSharpProgramAsset(behaviour);
                if (programAsset == null)
                {
                    missing.Add(behaviour.Name + " (no program asset)");
                    continue;
                }
                if (programAsset.CompiledVersion < UdonSharpProgramVersion.CurrentVersion)
                    missing.Add(behaviour.Name + " (program asset not compiled)");
            }

            if (missing.Count > 0)
            {
                var message = new StringBuilder("UdonSharp program asset check failed for ");
                message.Append(missing.Count).Append(" of ").Append(behaviours.Count).Append(": ");
                message.Append(string.Join(", ", missing));
                throw new InvalidOperationException(message.ToString());
            }

            Debug.Log("[Stargazing Hill] UdonSharp program assets present for " + behaviours.Count +
                      " behaviours: " + DescribeBehaviours(behaviours));
        }

        private static List<Type> GetProjectUdonSharpBehaviours()
        {
            var behaviours = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<UdonSharpBehaviour>())
            {
                if (type.IsAbstract) continue;
                Assembly assembly = type.Assembly;
                if (assembly.GetName().Name != ProjectAssemblyName) continue;
                if (type.Namespace != ProjectNamespace) continue;
                behaviours.Add(type);
            }
            behaviours.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            return behaviours;
        }

        private static string DescribeBehaviours(List<Type> behaviours)
        {
            var names = new string[behaviours.Count];
            for (int index = 0; index < behaviours.Count; index++) names[index] = behaviours[index].Name;
            return string.Join(", ", names);
        }
    }
}
