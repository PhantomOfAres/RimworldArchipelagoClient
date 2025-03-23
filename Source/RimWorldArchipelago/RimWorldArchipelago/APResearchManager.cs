﻿using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimworldArchipelago
{
    internal class APResearchManager
    {
        private const long BASE_LOCATION_ID = 5197648000;
        private const int LOCATION_ID_GAP = 1000;

        private static HashSet<string> apResearchNames = new HashSet<string>();

        public static void DisableNormalResearch()
        {
            foreach (ResearchProjectDef researchProject in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (researchProject != null)
                {
                    researchProject.prerequisites = null;
                    researchProject.hiddenPrerequisites = null;
                }
            }
        }

        public static void GenerateArchipelagoResearch(Dictionary<long, ScoutedItemInfo> scoutedItemInfo)
        {
            SlotData slotData = ArchipelagoClient.SlotData;
            ResearchTabDef archipelagoTab = DefDatabase<ResearchTabDef>.GetNamed("Archipelago");
            long baseLocationId = BASE_LOCATION_ID;
            int x = 0;
            int y = 0;
            for (int i = 0; i < slotData.SlotOptions.BasicResearchLocationCount; i++)
            {
                ResearchProjectDef research = GenerateResearchDef(scoutedItemInfo, i + baseLocationId, x, y, slotData, archipelagoTab);
                y += 1;
                if (y > 8)
                {
                    y = 0;
                    x += 1;
                }
                DefDatabase<ResearchProjectDef>.Add(research);
            }

            baseLocationId += LOCATION_ID_GAP;
            for (int i = 0; i < slotData.SlotOptions.HiTechResearchLocationCount; i++)
            {
                ResearchProjectDef research = GenerateResearchDef(scoutedItemInfo, i + baseLocationId, x, y, slotData, archipelagoTab);
                y += 1;
                if (y > 8)
                {
                    y = 0;
                    x += 1;
                }
                DefDatabase<ResearchProjectDef>.Add(research);
            }

            baseLocationId += LOCATION_ID_GAP;
            for (int i = 0; i < slotData.SlotOptions.MultiAnalyzerResearchLocationCount; i++)
            {
                ResearchProjectDef research = GenerateResearchDef(scoutedItemInfo, i + baseLocationId, x, y, slotData, archipelagoTab);
                y += 1;
                if (y > 8)
                {
                    y = 0;
                    x += 1;
                }
                DefDatabase<ResearchProjectDef>.Add(research);
            }

            ResearchProjectDef.GenerateNonOverlappingCoordinates();
        }

        private static ThingDef _hiTechResearchBench = null;
        private static ThingDef HiTechResearchBench
        {
            get
            {
                if (_hiTechResearchBench == null)
                {
                    _hiTechResearchBench = DefDatabase<ThingDef>.GetNamed("HiTechResearchBench");
                }

                return _hiTechResearchBench;
            }
        }

        private static ThingDef _multiAnalyzer = null;
        private static ThingDef MultiAnalyzer
        {
            get
            {
                if (_multiAnalyzer == null)
                {
                    _multiAnalyzer = DefDatabase<ThingDef>.GetNamed("MultiAnalyzer");
                }

                return _multiAnalyzer;
            }
        }

        private static ResearchProjectDef GenerateResearchDef(Dictionary<long, ScoutedItemInfo> scoutedItemInfo, long archipelagoId, int xIndex, int yIndex, SlotData slotData, ResearchTabDef archipelagoTab)
        {
            ScoutedItemInfo scoutedItem = null;
            if (scoutedItemInfo != null && scoutedItemInfo.ContainsKey(archipelagoId))
            {
                scoutedItem = scoutedItemInfo[archipelagoId];
            }
            ResearchProjectDef archipelagoResearch = new ResearchProjectDef();
            int labelIndex = (int) (archipelagoId - BASE_LOCATION_ID);
            int upgradesRequired = labelIndex / LOCATION_ID_GAP;
            labelIndex = labelIndex % LOCATION_ID_GAP;
            if (upgradesRequired == 0)
            {
                archipelagoResearch.defName = $"Basic Research Location {labelIndex}";
                archipelagoResearch.techLevel = TechLevel.Neolithic;
            }
            else if (upgradesRequired == 1)
            {
                archipelagoResearch.defName = $"Hi-Tech Research Location {labelIndex}";
                archipelagoResearch.requiredResearchBuilding = HiTechResearchBench;
                archipelagoResearch.techLevel = TechLevel.Industrial;
            }
            else if (upgradesRequired == 2)
            {
                archipelagoResearch.defName = $"Multi-Analyzer Research Location {labelIndex}";
                archipelagoResearch.requiredResearchBuilding = HiTechResearchBench;
                archipelagoResearch.requiredResearchFacilities = new List<ThingDef>
                {
                    MultiAnalyzer
                };
                archipelagoResearch.techLevel = TechLevel.Spacer;
            }
            else
            {
                archipelagoResearch.defName = $"Unknown Research Location {labelIndex}";
                Log.Error($"Couldn't find the appropriate requirements for ID {archipelagoId}!");
            }

            apResearchNames.Add(archipelagoResearch.defName);
            archipelagoResearch.label = scoutedItem == null ? archipelagoResearch.defName : $"{scoutedItem.Player.Name}'s {scoutedItem.ItemName}";
            if (scoutedItem == null)
            {
                archipelagoResearch.description = "This research will unlock somebody's something!";
            }
            else if (scoutedItem.Flags.HasFlag(ItemFlags.Advancement))
            {
                archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s required item, {scoutedItem.ItemName}.";
            }
            else if (scoutedItem.Flags.HasFlag(ItemFlags.NeverExclude))
            {
                archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s important item, {scoutedItem.ItemName}.";
            }
            else
            {
                archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s garbage filler item, {scoutedItem.ItemName}.";
            }
            archipelagoResearch.baseCost = slotData.SlotOptions.ResearchBaseCost;
            archipelagoResearch.tab = archipelagoTab;
            archipelagoResearch.researchViewX = xIndex * 1.0f;
            archipelagoResearch.researchViewY = yIndex * 0.7f;
            archipelagoResearch.generated = true;
            return archipelagoResearch;
        }

        public static bool IsApResearch(string researchName)
        {
            return apResearchNames.Contains(researchName);
        }

        public static long GetLocationId(string researchName)
        {
            // I don't want to do it this way, but... I'll fix it later.
            string[] splitNames = researchName.Split(' ');
            switch (splitNames[0])
            {
                case "Basic":
                    return long.Parse(splitNames[splitNames.Length - 1]) + BASE_LOCATION_ID;
                case "Hi-Tech":
                    return long.Parse(splitNames[splitNames.Length - 1]) + BASE_LOCATION_ID + LOCATION_ID_GAP;
                case "Multi-Analyzer":
                    return long.Parse(splitNames[splitNames.Length - 1]) + BASE_LOCATION_ID + LOCATION_ID_GAP + LOCATION_ID_GAP;
                default:
                    Log.Error($"Could not send Location ID {researchName}!");
                    return -1;
            }
        }
    }
}
