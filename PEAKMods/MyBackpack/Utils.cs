using BepInEx.Logging;

namespace BackpackViewerMod
{
    public static class Utils
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void LogInfo(object message) => _logger?.LogInfo(message);
        public static void LogError(object message) => _logger?.LogError(message);
        public static void LogDebug(object message) => _logger?.LogDebug(message);
        public static void LogWarning(object message) => _logger?.LogWarning(message);
    }
}