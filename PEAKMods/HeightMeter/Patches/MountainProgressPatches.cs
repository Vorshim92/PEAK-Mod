// File: MountainProgressPatches.cs

using HarmonyLib;

namespace HeightMeterMod.Patches
{
    public static class MountainProgressPatches
    {
        // NUOVO EVENTO: Notifica quando i punti di progresso sono disponibili.
        public static event System.Action<MountainProgressHandler.ProgressPoint[]> OnProgressPointsAvailable;
        private static bool progressPointsSent = false;

        // Evento esistente per i checkpoint raggiunti
        public static event System.Action<string> OnCheckpointReached;
        
        // --- NUOVA PATCH ---
        [HarmonyPatch(typeof(MountainProgressHandler), "CheckProgress")]
        public static class CheckProgressPatch
        {
            [HarmonyPostfix]
            private static void Postfix(MountainProgressHandler __instance)
            {
                // Invia i dati SOLO UNA VOLTA per run e solo se TUTTI i sistemi sono pronti.
                if (!progressPointsSent && 
                    __instance.progressPoints != null && __instance.progressPoints.Length > 0 &&
                    MapHandler.Instance != null) // <-- CONTROLLO AGGIUNTIVO
                {
                    Plugin.LogDebug("All required game handlers are available. Notifying system.");
                    OnProgressPointsAvailable?.Invoke(__instance.progressPoints);
                    progressPointsSent = true;
                }
            }
        }
        
        // Funzione helper per resettare lo stato all'inizio di una nuova run
        public static void ResetState()
        {
            progressPointsSent = false;
        }

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
        
        // Patch su SetSegmentComplete rimane invariata
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