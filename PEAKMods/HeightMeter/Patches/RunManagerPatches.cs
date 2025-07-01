using HarmonyLib;

namespace HeightMeterMod.Patches
{
    public static class RunManagerPatches
    {
        [HarmonyPatch(typeof(RunManager), "StartRun")]
        public static class StartRunPatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                Plugin.LogDebug("RunManager.StartRun called");
                Plugin.Instance?.OnRunStart();
            }
        }
    }
}