using HarmonyLib;
using RimWorld;
using RimworldArchipelago;
using System;
using System.Collections.Generic;
using Verse;

namespace RimWorldArchipelago
{
    // I don't fully understand why we need this. RimWorld does a bunch of reflection stuff
    //   to generate its debug menus, and doing that on the MultiClient dll dies horribly
    //   on System.Numerics.Biginteger. If you understand why and can eliminate this patch,
    //   please do and you'll be my favorite for at least a few days.
    [HarmonyPatch(typeof(GenTypes))]
    [HarmonyPatch(nameof(GenTypes.AllTypes), MethodType.Getter)]
    static class DebugTabMenu_InitActions_Patch
    {
        public static void Postfix(ref List<Type> __result)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < __result.Count; i++)
            {
                if (__result[i].FullName.Contains("MultiClient"))
                {
                    indices.Add(i);
                }
            }

            indices.Reverse();
            foreach (int i in indices)
            {
                __result.RemoveAt(i);
            }
        }
    }

    [HarmonyPatch(typeof(ResearchManager))]
    [HarmonyPatch(nameof(ResearchManager.FinishProject))]
    static class ResearchProgressDef_ResearchManagerFinishProject_Patch
    {
        public static bool Prefix(ResearchManager __instance, ResearchProjectDef proj, bool doCompletionDialog = false, Pawn researcher = null, bool doCompletionLetter = true)
        {
            // Temp check - Eventually, a proper "Is this research an AP location?" function needs to exist.
            if (proj.defName.Contains("Location"))
            {
                string[] splitNames = proj.defName.Split(' ');
                long locationId = long.Parse(splitNames[splitNames.Length - 1]) + 5197648000;
                ArchipelagoClient.SendLocation(locationId);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef))]
    [HarmonyPatch(nameof(ResearchProjectDef.CanStartNow), MethodType.Getter)]
    static class ResearchProgressDef_get_CanStartNow_Patch
    {
        public static bool Prefix(ResearchProjectDef __instance, ref bool __result)
        {
            // Temp check - Eventually, a proper "Is this research an AP location?" function needs to exist.
            if (!__instance.defName.Contains("Location"))
            {
                __result = false;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
