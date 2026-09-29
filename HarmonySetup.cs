using System;
using CitiesHarmony.API;
using HarmonyLib;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Applies all [HarmonyPatch] classes of this assembly once Harmony is ready, and removes them again.
    /// Right now the assembly contains no patch classes, so PatchAll does nothing.
    /// Add patches as separate classes under Patches/ and they get picked up automatically.
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
                    try
                    {
                        Harmony harmony = new Harmony(HarmonyId);
                        harmony.PatchAll(typeof(HarmonySetup).Assembly);
                        Debug.Log("[QuayTools] Harmony patches applied");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[QuayTools] Harmony patching failed: " + ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Harmony setup failed: " + ex);
            }
        }

        public static void Revert()
        {
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
