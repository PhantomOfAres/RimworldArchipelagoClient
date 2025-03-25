﻿using HarmonyLib;
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
            if (APResearchManager.IsApResearch(proj.defName))
            {
                ArchipelagoClient.SendResearchLocation(proj.defName);
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
            if (!APResearchManager.IsApResearch(__instance.defName))
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

    // I don't like this one, but there's no more generic way to provide a failure reason (without overwriting the whole DrawButton button.)
    [HarmonyPatch(typeof(ResearchProjectDef))]
    [HarmonyPatch(nameof(ResearchProjectDef.PrerequisitesCompleted), MethodType.Getter)]
    static class ResearchProgressDef_get_PrerequisitesCompleted_Patch
    {
        public static bool Prefix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!APResearchManager.IsApResearch(__instance.defName))
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

    [HarmonyPatch(typeof(RecipeWorker))]
    [HarmonyPatch(nameof(RecipeWorker.Notify_IterationCompleted))]
    static class RecipeWorker_Patch
    {
        public static bool Prefix(RecipeWorker __instance)
        {
            if (APCraftManager.IsApCraft(__instance.recipe.defName))
            {
                ArchipelagoClient.SendCraftLocation(__instance.recipe.defName);
            }

            return true;
        }
    }
}
