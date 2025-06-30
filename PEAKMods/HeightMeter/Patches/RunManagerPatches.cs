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
        
        [HarmonyPatch(typeof(RunManager), "EndRun")]
        public static class EndRunPatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                Plugin.LogDebug("RunManager.EndRun called");
                Plugin.Instance?.OnRunEnd();
            }
        }
    }
}