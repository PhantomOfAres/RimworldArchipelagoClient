using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using RimWorld;
using RimWorldArchipelago;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    internal class APResearchManager
    {
        private const long BASE_LOCATION_ID = 1;
        private const int LOCATION_ID_GAP = 1000;

        private static Dictionary<long, string> apResearch = new Dictionary<long, string>();
        private static HashSet<string> disabledExpansionResearchNames = new HashSet<string>();
        private static Dictionary<long, ScoutedItemInfo> cachedScoutedItems = new Dictionary<long, ScoutedItemInfo>();

        public static void DisableNormalResearch()
        {
            SlotOptions slotOptions = ArchipelagoClient.SlotData.SlotOptions;
            foreach (ResearchProjectDef researchProject in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if ((!slotOptions.RoyaltyEnabled && researchProject.modContentPack.PackageId == ModContentPack.RoyaltyModPackageId) ||
                    (!slotOptions.IdeologyEnabled && researchProject.modContentPack.PackageId == ModContentPack.IdeologyModPackageId) ||
                    (!slotOptions.BiotechEnabled && researchProject.modContentPack.PackageId == ModContentPack.BiotechModPackageId) ||
                    (!slotOptions.AnomalyEnabled && researchProject.modContentPack.PackageId == ModContentPack.AnomalyModPackageId))
                {
                    disabledExpansionResearchNames.Add(researchProject.defName);
                    continue;
                }

                if (IsApResearch(researchProject.defName))
                {
                    continue;
                }

                if (researchProject != null)
                {
                    researchProject.prerequisites = null;
                    researchProject.hiddenPrerequisites = null;
                }
            }
        }

        public static void GenerateArchipelagoResearch(Dictionary<long, ScoutedItemInfo> scoutedItemInfo, bool isNewSeed)
        {
            if (apResearch.Count > 0)
            {
                if (isNewSeed)
                {
                    Messages.Message("Cannot start a new seed during the same session! Please restart the game!", MessageTypeDefOf.NegativeEvent);
                }
                return;
            }
            cachedScoutedItems = scoutedItemInfo;
            System.Random rand = new System.Random(ArchipelagoClient.SlotData.Seed.GetHashCode());
            SlotData slotData = ArchipelagoClient.SlotData;
            ResearchTabDef archipelagoTab = DefDatabase<ResearchTabDef>.GetNamed("Archipelago");
            List<ResearchProjectDef> previousLevel = null;
            List<ResearchProjectDef> currentLevel = new List<ResearchProjectDef>();
            long baseLocationId = BASE_LOCATION_ID;
            int x = 0;
            int y = 0;
            for (int i = 0; i < slotData.SlotOptions.BasicResearchLocationCount; i++)
            {
                ResearchProjectDef research = GenerateResearchDef(i + baseLocationId, x, y, slotData, archipelagoTab, previousLevel, rand);
                currentLevel.Add(research);
                y += 1;
                if (y > 8)
                {
                    previousLevel = currentLevel;
                    currentLevel = new List<ResearchProjectDef>();
                    y = 0;
                    x += 1;
                }
                DefDatabase<ResearchProjectDef>.Add(research);
            }

            baseLocationId += LOCATION_ID_GAP;
            for (int i = 0; i < slotData.SlotOptions.HiTechResearchLocationCount; i++)
            {
                ResearchProjectDef research = GenerateResearchDef(i + baseLocationId, x, y, slotData, archipelagoTab, previousLevel, rand);
                currentLevel.Add(research);
                y += 1;
                if (y > 8)
                {
                    previousLevel = currentLevel;
                    currentLevel = new List<ResearchProjectDef>();
                    y = 0;
                    x += 1;
                }
                DefDatabase<ResearchProjectDef>.Add(research);
            }

            baseLocationId += LOCATION_ID_GAP;
            for (int i = 0; i < slotData.SlotOptions.MultiAnalyzerResearchLocationCount; i++)
            {
                ResearchProjectDef research = GenerateResearchDef(i + baseLocationId, x, y, slotData, archipelagoTab, previousLevel, rand);
                currentLevel.Add(research);
                y += 1;
                if (y > 8)
                {
                    previousLevel = currentLevel;
                    currentLevel = new List<ResearchProjectDef>();
                    y = 0;
                    x += 1;
                }
                DefDatabase<ResearchProjectDef>.Add(research);
            }

            ResearchProjectDef.GenerateNonOverlappingCoordinates();
        }

        // This simply catches up research locations - it's mostly for users starting a new settlement to recover their data.
        public static void CompleteLocations(ReadOnlyCollection<long> checkedLocations)
        {
            foreach (ResearchProjectDef researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (IsApResearch(researchProjectDef.defName) &&
                    checkedLocations.Contains(GetLocationId(researchProjectDef.defName)) &&
                    !researchProjectDef.IsFinished)
                {
                    Find.ResearchManager.FinishProject(researchProjectDef);
                }
            }

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

        private static ResearchProjectDef GenerateResearchDef(long archipelagoId, int xIndex, int yIndex, SlotData slotData, ResearchTabDef archipelagoTab, List<ResearchProjectDef> previousLevel, System.Random rand)
        {
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

            apResearch[archipelagoId] = archipelagoResearch.defName;
            UpdateResearchDescription(archipelagoResearch, archipelagoId, false);
            archipelagoResearch.baseCost = slotData.SlotOptions.ResearchBaseCost;
            archipelagoResearch.tab = archipelagoTab;
            archipelagoResearch.researchViewX = xIndex * 1.0f;
            archipelagoResearch.researchViewY = yIndex * 0.7f;
            archipelagoResearch.generated = true;
            if (previousLevel != null)
            {
                int maxPrereqs = 3;
                if (yIndex == 0 || yIndex == 8)
                {
                    maxPrereqs = 2;
                }
                int prereqsLeft = rand.Next(ArchipelagoClient.SlotData.SlotOptions.ResearchMaxPrerequisites + 1);

                archipelagoResearch.prerequisites = new List<ResearchProjectDef>();
                for (int i = Mathf.Max(0, yIndex - 1); i <= Mathf.Min(yIndex + 1, 8); i++)
                {
                    if (rand.Next(maxPrereqs) < prereqsLeft)
                    {
                        archipelagoResearch.prerequisites.Add(previousLevel[i]);
                        prereqsLeft -= 1;
                    }
                    maxPrereqs -= 1;
                }
            }
            return archipelagoResearch;
        }

        public static void UpdateAllDescriptions(bool playingGame = true)
        {
            foreach ((long id, string name) in apResearch)
            {
                ResearchProjectDef researchProjectDef = DefDatabase<ResearchProjectDef>.GetNamed(name);
                UpdateResearchDescription(researchProjectDef, id, playingGame);
            }
        }

        public static void UpdateResearchDescription(ResearchProjectDef archipelagoResearch, long archipelagoId, bool playingGame)
        {
            ScoutedItemInfo scoutedItem = null;
            if (cachedScoutedItems != null && cachedScoutedItems.ContainsKey(archipelagoId))
            {
                scoutedItem = cachedScoutedItems[archipelagoId];
            }

            bool showNothing = false;
            bool showSummary = false;
            bool showItem = false;

            if (playingGame && archipelagoResearch.IsFinished)
            {
                showItem = true;
            }
            else if (scoutedItem == null ||
                ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.None ||
                ((!playingGame || !archipelagoResearch.CanStartNow) &&
                (ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.SummaryAvailable ||
                ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.FullItemAvailable)))
            {
                showNothing = true;
            }
            else if (ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.SummaryAll ||
                (playingGame && archipelagoResearch.CanStartNow && ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.SummaryAvailable))
            {
                showSummary = true;
            }
            else if (ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.FullItemAll ||
                (playingGame && archipelagoResearch.CanStartNow && ArchipelagoClient.SlotData.SlotOptions.ResearchScoutType == ScoutType.FullItemAvailable))
            {
                showItem = true;
            }

            if (showNothing)
            {
                archipelagoResearch.label = archipelagoResearch.defName;
                archipelagoResearch.description = "This research is a mystery! Completing it will send something to someone.";
            }
            if (showSummary)
            {
                if (scoutedItem.Flags.HasFlag(ItemFlags.Advancement))
                {
                    archipelagoResearch.label = $"{scoutedItem.Player.Name}'s Required Item";
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s required item!";
                }
                else if (scoutedItem.Flags.HasFlag(ItemFlags.NeverExclude))
                {
                    archipelagoResearch.label = $"{scoutedItem.Player.Name}'s Useful Item";
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s important item!";
                }
                else if (scoutedItem.Flags.HasFlag(ItemFlags.Trap))
                {
                    archipelagoResearch.label = $"{scoutedItem.Player.Name}'s Trap Item";
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s trap item!";
                }
                else
                {
                    archipelagoResearch.label = $"{scoutedItem.Player.Name}'s Filler Item";
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s filler item!";
                }
            }
            if (showItem)
            {
                archipelagoResearch.label = $"{scoutedItem.Player.Name}'s {scoutedItem.ItemName}";
                if (scoutedItem.Flags.HasFlag(ItemFlags.Advancement))
                {
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s required item, {scoutedItem.ItemName}!";
                }
                else if (scoutedItem.Flags.HasFlag(ItemFlags.NeverExclude))
                {
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s important item, {scoutedItem.ItemName}!";
                }
                else if (scoutedItem.Flags.HasFlag(ItemFlags.Trap))
                {
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s trap item, {scoutedItem.ItemName}!";
                }
                else
                {
                    archipelagoResearch.description = $"This research has {scoutedItem.Player.Name}'s filler item, {scoutedItem.ItemName}!";
                }
            }

            archipelagoResearch.ClearCachedData();
        }

        public static bool IsApResearch(string researchName)
        {
            return apResearch.ContainsValue(researchName);
        }

        public static bool CanStartResearch(string researchName)
        {
            return IsApResearch(researchName) || disabledExpansionResearchNames.Contains(researchName);
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
