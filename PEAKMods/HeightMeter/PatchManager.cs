using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HeightMeterMod
{
    public static class PatchManager
    {
        private static readonly List<Harmony> AllHarmony = new List<Harmony>();
        private static readonly Dictionary<string, Harmony> HarmonyInstances = new Dictionary<string, Harmony>();

        public static void PatchAll()
        {
            // LoadPatch(typeof(Patches.BackpackPatches), "Backpack");

        }

        public static void LoadPatch(Type containerClass, string patchName)
        {
            try
            {
                var harmony = new Harmony(PluginInfo.PLUGIN_GUID + "." + patchName);
                
                var nestedPatchClasses = containerClass.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                
                if (nestedPatchClasses.Length > 0)
                {
                    Utils.LogInfo($"{patchName} => Found {nestedPatchClasses.Length} nested classes");
                    
                    foreach (var nestedClass in nestedPatchClasses)
                    {
                        try
                        {
                            if (nestedClass.GetCustomAttributes(typeof(HarmonyPatch), false).Any())
                            {
                                harmony.PatchAll(nestedClass);
                                Utils.LogInfo($"  ✓ Successfully patched: {nestedClass.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Utils.LogError($"  ✗ Failed to patch {nestedClass.Name}: {ex.Message}");
                        }
                    }
                }
                
                var directPatches = containerClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.GetCustomAttributes(typeof(HarmonyPatch), false).Any() || 
                            m.GetCustomAttributes(typeof(HarmonyPrefix), false).Any() ||
                            m.GetCustomAttributes(typeof(HarmonyPostfix), false).Any());
                
                if (directPatches.Any())
                {
                    harmony.PatchAll(containerClass);
                    Utils.LogInfo($"{patchName} => Patched direct methods");
                }
                
                AllHarmony.Add(harmony);
                HarmonyInstances[patchName] = harmony;
                
                int finalCount = harmony.GetPatchedMethods().Count();
                Utils.LogInfo($"{patchName} => Completed: {finalCount} methods patched");
            }
            catch (Exception ex)
            {
                Utils.LogError($"{patchName} => Critical failure: {ex.Message}");
                Utils.LogError(ex.StackTrace);
            }
        }

        public static void UnpatchAll()
        {
            foreach (var harmony in AllHarmony)
            {
                harmony.UnpatchSelf();
            }
            AllHarmony.Clear();
            HarmonyInstances.Clear();
            Utils.LogInfo("All patches removed");
        }

        public static bool UnpatchByName(string patchName)
        {
            if (HarmonyInstances.TryGetValue(patchName, out var harmony))
            {
                harmony.UnpatchSelf();
                AllHarmony.Remove(harmony);
                HarmonyInstances.Remove(patchName);
                Utils.LogInfo($"Unpatched {patchName}");
                return true;
            }
            return false;
        }
    }
}