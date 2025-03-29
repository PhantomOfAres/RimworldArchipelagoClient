using HarmonyLib;
using RimworldArchipelago;
using System.Collections.Generic;
using Verse;


namespace RimWorldArchipelago
{
    public class ArchipelagoItemDef : Def
    {
        public long Id;
        public string DefType;
        public string RequiredExpansion;
        public List<string> Prerequisites = new List<string>();
    }

    [StaticConstructorOnStartup]
    public class Main
    {
        static Main()
        {
            Harmony harmony = new Harmony("com.phantomofares.rimworld");
            harmony.PatchAll();
        }
    }
}
