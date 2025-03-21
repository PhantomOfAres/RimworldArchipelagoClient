﻿using HarmonyLib;
using RimworldArchipelago;
using System.Collections.Generic;
using Verse;


namespace RimWorldArchipelago
{
    public class ArchipelagoItemDef : Def
    {
        public long Id;
        public string DefType;
        public List<long> Prerequisites = new List<long>();
    }

    [StaticConstructorOnStartup]
    public class Main
    {
        static Main()
        {
            Harmony harmony = new Harmony("com.phantomofares.rimworld");
            harmony.PatchAll();

            APResearchManager.DisableNormalResearch();
        }
    }
}
