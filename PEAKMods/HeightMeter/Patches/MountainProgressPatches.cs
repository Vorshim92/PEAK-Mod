using HarmonyLib;

namespace HeightMeterMod.Patches
{
    public static class MountainProgressPatches
    {
        // Event fired when a checkpoint is reached
        public static event System.Action<string> OnCheckpointReached;
        
        [HarmonyPatch(typeof(MountainProgressHandler), "TriggerReached")]
        public static class TriggerReachedPatch
        {
            [HarmonyPostfix]
            private static void Postfix(MountainProgressHandler.ProgressPoint progressPoint)
            {
                Plugin.LogDebug($"Checkpoint reached: {progressPoint.title}");
                OnCheckpointReached?.Invoke(progressPoint.title);
            }
        }
        
        [HarmonyPatch(typeof(MountainProgressHandler), "SetSegmentComplete")]
        public static class SetSegmentCompletePatch
        {
            [HarmonyPostfix]
            private static void Postfix(int segment)
            {
                Plugin.LogDebug($"Segment {segment} completed");
            }
        }
    }
}