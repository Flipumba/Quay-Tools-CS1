using System;
using System.Reflection;
using CitiesHarmony.API;
using HarmonyLib;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Applies the [HarmonyPatch] classes of this assembly once Harmony is ready, and removes them again.
    /// Each class is patched on its own so that one failing patch does not disable the others.
    /// </summary>
    internal static class HarmonySetup
    {
        public const string HarmonyId = "com.quaytools.patches";

        public static void Apply()
        {
            try
            {
                HarmonyHelper.DoOnHarmonyReady(delegate ()
                {
                    Harmony harmony = new Harmony(HarmonyId);
                    Assembly assembly = typeof(HarmonySetup).Assembly;
                    int ok = 0, failed = 0;

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;

                        try
                        {
                            new PatchClassProcessor(harmony, type).Patch();
                            ok++;
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            Debug.LogError("[QuayTools] Patch " + type.Name + " failed: " + ex);
                        }
                    }

                    PedestrianPathPatch.Apply(harmony);
                    HidePropsPatch.Apply(harmony);

                    Debug.Log("[QuayTools] Harmony patches applied: " + ok + " ok, " + failed + " failed");
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Harmony setup failed: " + ex);
            }
        }

        /// <summary>Once more when a level has loaded: path-finder classes of other mods (TM:PE) may not have existed when the mod was enabled.</summary>
        public static void ApplyLate()
        {
            try
            {
                HarmonyHelper.DoOnHarmonyReady(delegate ()
                {
                    PedestrianPathPatch.Apply(new Harmony(HarmonyId));
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Late Harmony setup failed: " + ex);
            }
        }

        public static void Revert()
        {
            PedestrianPathPatch.Reset();
            try
            {
                if (!HarmonyHelper.IsHarmonyInstalled) return;

                Harmony harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                Debug.Log("[QuayTools] Harmony patches removed");
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Harmony unpatch failed: " + ex);
            }
        }
    }
}
