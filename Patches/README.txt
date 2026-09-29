Put Harmony patch classes here. Example skeleton (keep it commented until there is a real target):

// [HarmonyPatch(typeof(SomeGameType), "SomeMethod")]
// internal static class SomeMethod_Patch
// {
//     [HarmonyPostfix]
//     public static void Postfix(...) { }
// }

Rules for this mod: Prefix/Postfix only (no Transpilers), never patch hot paths such as PathFind
or per-frame simulation code without a strong reason, and always keep a fallback if the target is missing.
