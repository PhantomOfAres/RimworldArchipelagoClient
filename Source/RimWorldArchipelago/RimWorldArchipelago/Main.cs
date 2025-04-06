using HarmonyLib;
using RimWorld;
using RimworldArchipelago;
using System.Collections.Generic;
using Verse;


namespace RimWorldArchipelago
{
    public enum AdjustedTechLevel
    {
        Neolithic,
        Medieval,
        Industrial,
        Spacer,
        HardToMake,
        Anomaly
    }

    public class ArchipelagoItemDef : Def
    {
        public long Id;
        public string DefType;
        public string RequiredExpansion;
        public AdjustedTechLevel TechLevel;
        public List<string> Tags = new List<string>();
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
