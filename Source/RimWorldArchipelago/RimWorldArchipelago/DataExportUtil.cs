using RimWorld;
using RimWorldArchipelago;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    internal class DataExportUtil
    {
        public const string AnyElectricityRequirement = "AnyElectricity";
        public const string FabricationRequirement = "Fabrication";
        public const string AdvancedFabricationRequirement = "Advanced Fabrication";
        public const string ComponentDefName = "ComponentIndustrial";
        public const string AdvancedComponentDefName = "ComponentSpacer";

        public static void ExportArchipelagoDefs()
        {
            // Some items in the DLCs require multiple DLC. Since by the time we reach export, those items have lost that context, and in the interest of not requiring
            //  everyone to generate for all combinations of DLC, we just exclude those few problem items here.
            List<string> problematicDefs = new List<string> { "Building_KidOutfitStand", "Gun_HellcatRifle_Unique", "Apparel_VacsuitChildren", "Gun_BeamGraser", "WatermillGenerator" };
            long nextId = 0;

            Dictionary<string, ArchipelagoItemDef> allDefs = new Dictionary<string, ArchipelagoItemDef>();
            Dictionary<string, ArchipelagoItemDef> allDefsByLabel = new Dictionary<string, ArchipelagoItemDef>();
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            foreach (ResearchProjectDef research in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                nextId += 1;
                ArchipelagoItemDef item = new ArchipelagoItemDef();
                item.Id = nextId;
                item.DefType = "ResearchProjectDef";
                item.defName = $"{research.defName}Research";
                if (research.modContentPack != null)
                {
                    item.RequiredExpansion = research.modContentPack.PackageIdPlayerFacing;
                }
                switch (research.techLevel)
                {
                    case TechLevel.Animal:
                    case TechLevel.Neolithic:
                        item.TechLevel = AdjustedTechLevel.Neolithic;
                        break;
                    case TechLevel.Medieval:
                        item.TechLevel = AdjustedTechLevel.Medieval;
                        break;
                    case TechLevel.Industrial:
                        item.TechLevel = AdjustedTechLevel.Industrial;
                        break;
                    case TechLevel.Spacer:
                        item.TechLevel = AdjustedTechLevel.Spacer;
                        break;
                    case TechLevel.Ultra:
                    case TechLevel.Archotech:
                        item.TechLevel = AdjustedTechLevel.HardToMake;
                        break;
                    case TechLevel.Undefined:
                        if (item.RequiredExpansion == ModContentPack.AnomalyModPackageId)
                        {
                            item.TechLevel = AdjustedTechLevel.Anomaly;
                        }
                        else
                        {
                            item.TechLevel = AdjustedTechLevel.Neolithic;
                        }
                        break;
                }
                if (research.tags != null)
                {
                    foreach (ResearchProjectTagDef researchTag in research.tags)
                    {
                        item.Tags.Add(researchTag.defName);
                    }
                }
                item.label = textInfo.ToTitleCase(research.label);
                allDefs[item.defName] = item;
                allDefsByLabel[item.label] = item;
            }

            // Hold on to each item uniquely. If you can make 1 or 4 fine meals, count it once.
            HashSet<string> alreadyPopulatedItem = new HashSet<string>();
            foreach (RecipeDef recipeDef in DefDatabase<RecipeDef>.AllDefs)
            {
                // Only include things that require research. That should give us a good variety but force the thing in question to be craftable
                if (//recipeDef.researchPrerequisite != null &&
                    recipeDef.products != null &&
                    recipeDef.products.Count > 0 &&
                    (recipeDef.factionPrerequisiteTags == null || recipeDef.factionPrerequisiteTags.Count == 0))
                {
                    string requiredResearchExpansion = "";
                    if (recipeDef.researchPrerequisite != null && recipeDef.researchPrerequisite.modContentPack != null && recipeDef.researchPrerequisite.modContentPack.PackageId != ModContentPack.CoreModPackageId)
                    {
                        requiredResearchExpansion = recipeDef.researchPrerequisite.modContentPack.PackageIdPlayerFacing;
                    }

                    foreach (ThingDefCountClass product in recipeDef.products)
                    {
                        if (problematicDefs.Contains(product.thingDef.defName))
                        {
                            continue;
                        }
                        // Ensure we haven't already included this item type, and ensure we're only targeting items, not buildings or mechs or whatever.
                        if (!alreadyPopulatedItem.Contains(product.thingDef.defName) &&
                            product.thingDef.category == ThingCategory.Item &&
                            recipeDef.AllRecipeUsers != null &&
                            recipeDef.AllRecipeUsers.Count() > 0)
                        {
                            nextId += 1;
                            alreadyPopulatedItem.Add(product.thingDef.defName);
                            ArchipelagoItemDef item = new ArchipelagoItemDef();
                            item.Id = nextId;
                            item.DefType = "ThingDef";
                            item.defName = $"{product.thingDef.defName}Thing";
                            if (product.thingDef.modContentPack != null)
                            {
                                item.RequiredExpansion = product.thingDef.modContentPack.PackageIdPlayerFacing;
                            }
                            // Some items can be found in the base game, but crafted in expansions. Treat those items is in-logic only if they can be crafted.
                            if (requiredResearchExpansion != "")
                            {
                                item.RequiredExpansion = requiredResearchExpansion;
                            }

                            // Look. This isn't ideal, but it seems to be the only remaining exception in core, and figuring out whether MayRequire is excluding items is annoying.
                            if (product.thingDef.defName.Contains("Apparel_Cape"))
                            {
                                item.RequiredExpansion = "Ludeon.RimWorld.Royalty";
                            }
                            TechLevel maxTechLevel = (TechLevel)Mathf.Max((int)item.TechLevel, (int)(recipeDef.researchPrerequisite == null ? 0 : recipeDef.researchPrerequisite.techLevel));
                            switch (maxTechLevel)
                            {
                                case TechLevel.Animal:
                                case TechLevel.Neolithic:
                                    item.TechLevel = AdjustedTechLevel.Neolithic;
                                    break;
                                case TechLevel.Medieval:
                                    item.TechLevel = AdjustedTechLevel.Medieval;
                                    break;
                                case TechLevel.Industrial:
                                    item.TechLevel = AdjustedTechLevel.Industrial;
                                    break;
                                case TechLevel.Spacer:
                                    item.TechLevel = AdjustedTechLevel.Spacer;
                                    break;
                                case TechLevel.Ultra:
                                case TechLevel.Archotech:
                                    item.TechLevel = AdjustedTechLevel.HardToMake;
                                    break;
                                case TechLevel.Undefined:
                                    if (item.RequiredExpansion == "Ludeon.RimWorld.Anomaly")
                                    {
                                        item.TechLevel = AdjustedTechLevel.Anomaly;
                                    }
                                    else
                                    {
                                        item.TechLevel = AdjustedTechLevel.Neolithic;
                                    }
                                    break;
                            }
                            item.label = textInfo.ToTitleCase(product.thingDef.label);
                            // If the recipe requires a specific research, mark it as an (Archipelago) prerequisite
                            if (recipeDef.researchPrerequisite != null)
                            {
                                ArchipelagoItemDef prereqArchipelagoItem = allDefs[$"{recipeDef.researchPrerequisite.defName}Research"];
                                item.Prerequisites.Add(prereqArchipelagoItem.label);
                            }

                            // Assume a craft does require power - if any of its benches do NOT, set it to false.
                            bool craftRequiresPower = true;
                            // If the bench(es) used to craft this item require research, mark it as an (Archipelago) prerequisite (If the player has flake production but no drug
                            //   lab, the player can't make flake.)
                            if (recipeDef.AllRecipeUsers != null)
                            {
                                bool foundPrereqs = false;
                                List<ResearchProjectDef> shortestPrereqList = null;
                                foreach (ThingDef benchType in recipeDef.AllRecipeUsers)
                                {
                                    if (!benchType.ConnectToPower)
                                    {
                                        craftRequiresPower = false;
                                    }

                                    if (!foundPrereqs || (benchType.researchPrerequisites != null && benchType.researchPrerequisites.Count < shortestPrereqList.Count))
                                    {
                                        shortestPrereqList = benchType.researchPrerequisites;
                                    }
                                }

                                if (shortestPrereqList != null)
                                {
                                    foreach (ResearchProjectDef benchResearch in shortestPrereqList)
                                    {
                                        ArchipelagoItemDef prereqArchipelagoItem = allDefs[$"{benchResearch.defName}Research"];
                                        if (!item.Prerequisites.Contains(prereqArchipelagoItem.label))
                                        {
                                            item.Prerequisites.Add(prereqArchipelagoItem.label);
                                        }
                                    }
                                }

                                if (craftRequiresPower && !item.Prerequisites.Contains(AnyElectricityRequirement))
                                {
                                    item.Prerequisites.Add(AnyElectricityRequirement);
                                }
                            }

                            foreach (IngredientCount ingredientCount in recipeDef.ingredients)
                            {
                                if (ingredientCount.IsFixedIngredient && ingredientCount.FixedIngredient.defName == ComponentDefName && !item.Prerequisites.Contains(FabricationRequirement))
                                {
                                    item.Prerequisites.Add(FabricationRequirement);
                                }
                                if (ingredientCount.IsFixedIngredient && ingredientCount.FixedIngredient.defName == AdvancedComponentDefName && !item.Prerequisites.Contains(FabricationRequirement))
                                {
                                    item.Prerequisites.Add(AdvancedFabricationRequirement);
                                }
                            }
                            allDefs[item.defName] = item;
                            allDefsByLabel[item.label] = item;
                        }
                    }
                }
            }

            IncidentCategoryDef bigThreatCategory = DefDatabase<IncidentCategoryDef>.GetNamed("ThreatSmall");
            IncidentCategoryDef smallThreatCategory = DefDatabase<IncidentCategoryDef>.GetNamed("ThreatBig");
            IncidentCategoryDef diseaseThreatCategory = DefDatabase<IncidentCategoryDef>.GetNamed("DiseaseHuman");
            List<IncidentCategoryDef> badThreatIncidentCategories = new List<IncidentCategoryDef>
                { bigThreatCategory, smallThreatCategory, diseaseThreatCategory };
            IncidentCategoryDef factionArrivalThreatCategory = DefDatabase<IncidentCategoryDef>.GetNamed("FactionArrival");
            IncidentCategoryDef orbitalVisitorThreatCategory = DefDatabase<IncidentCategoryDef>.GetNamed("OrbitalVisitor");
            IncidentCategoryDef shipChunkDropThreatCategory = DefDatabase<IncidentCategoryDef>.GetNamed("ShipChunkDrop");
            List<IncidentCategoryDef> goodIncidentCategories = new List<IncidentCategoryDef>
                { factionArrivalThreatCategory, orbitalVisitorThreatCategory, shipChunkDropThreatCategory };
            IncidentTargetTagDef playerHomeTag = DefDatabase<IncidentTargetTagDef>.GetNamed("Map_PlayerHome");
            foreach (IncidentDef incidentDef in DefDatabase<IncidentDef>.AllDefs)
            {
                if (problematicDefs.Contains(incidentDef.defName))
                {
                    continue;
                }
                if ((incidentDef.defName == "ArchipelagoSculpturePod" ||
                    incidentDef.defName == "ArchipelagoColonistPod" ||
                    badThreatIncidentCategories.Contains(incidentDef.category) ||
                    goodIncidentCategories.Contains(incidentDef.category)) &&
                    incidentDef.targetTags.Contains(playerHomeTag))
                {
                    nextId += 1;
                    ArchipelagoItemDef item = new ArchipelagoItemDef();
                    item.Id = nextId;
                    item.DefType = "IncidentDef";
                    item.defName = $"{incidentDef.defName}Incident";
                    item.label = textInfo.ToTitleCase(incidentDef.label);
                    if (incidentDef.modContentPack != null)
                    {
                        item.RequiredExpansion = incidentDef.modContentPack.PackageIdPlayerFacing;
                    }
                    if (badThreatIncidentCategories.Contains(incidentDef.category))
                    {
                        item.Tags.Add("Negative");
                    }
                    if (goodIncidentCategories.Contains(incidentDef.category) || incidentDef.defName == "ArchipelagoSculpturePod")
                    {
                        item.Tags.Add("Positive");
                    }
                    item.Tags.Add(incidentDef.category.defName);
                    allDefs[item.defName] = item;
                    allDefsByLabel[item.label] = item;
                }
            }

            List<string> basicResources = new List<string>() { "Bioferrite", "Shard", "HemogenPack", "Plasteel", "Gold", "WoodLog", "Uranium", "Cloth" };
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (problematicDefs.Contains(thingDef.defName))
                {
                    continue;
                }
                // Excluding a bunch of things that aren't properly counted by rooms, created by everyone, or are otherwise problematic
                if ((thingDef.thingClass == typeof(Building) || thingDef.thingClass.IsSubclassOf(typeof(Building))) &&
                    thingDef.IsEdifice() &&
                    !thingDef.building.isAttachment &&
                    !thingDef.defName.Contains("Bed") &&
                    !thingDef.defName.Contains("Door") &&
                    thingDef.canGenerateDefaultDesignator)
                {
                    string requiredResearchExpansion = "";
                    if (thingDef.researchPrerequisites != null && thingDef.researchPrerequisites.Count > 0)
                    {
                        bool canCraftComponents = true;
                        List<string> componentPrereqs = new List<string>();
                        if (thingDef.costList != null)
                        {
                            foreach (ThingDefCount component in thingDef.costList)
                            {
                                if (!allDefs.ContainsKey($"{component.ThingDef.defName}Thing"))
                                {
                                    // If the resource is "basic" (can be obtained relatively easily, just skip it. Otherwise, disallow this building altogether.
                                    if (basicResources.Contains(component.ThingDef.defName))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        if (component.ThingDef.modContentPack.PackageId != ModContentPack.CoreModPackageId)
                                        {
                                            requiredResearchExpansion = component.ThingDef.modContentPack.PackageIdPlayerFacing;
                                        }
                                        canCraftComponents = false;
                                        break;
                                    }
                                }

                                ArchipelagoItemDef archipelagoItem = allDefs[$"{component.ThingDef.defName}Thing"];
                                foreach (string prereq in archipelagoItem.Prerequisites)
                                {
                                    if (!componentPrereqs.Contains(prereq))
                                    {
                                        componentPrereqs.Add(prereq);
                                    }
                                }
                            }
                        }

                        if (!canCraftComponents)
                        {
                            continue;
                        }

                        nextId += 1;
                        ArchipelagoItemDef item = new ArchipelagoItemDef();
                        item.Id = nextId;
                        item.DefType = "BuildingThingDef";
                        item.defName = $"{thingDef.defName}Building";
                        item.label = textInfo.ToTitleCase(thingDef.label);
                        ArchipelagoItemDef prereqArchipelagoItem = allDefs[$"{thingDef.researchPrerequisites[0].defName}Research"];
                        HashSet<string> uniquePrereqs = new HashSet<string>();
                        uniquePrereqs.Add(prereqArchipelagoItem.label);
                        uniquePrereqs.AddRange(componentPrereqs);
                        item.Prerequisites.AddRange(uniquePrereqs);
                        foreach (string prereqLabel in item.Prerequisites)
                        {
                            if (allDefsByLabel.ContainsKey(prereqLabel))
                            {
                                ArchipelagoItemDef prereqItem = allDefsByLabel[prereqLabel];
                                if (requiredResearchExpansion == "" || prereqItem.RequiredExpansion != "Ludeon.RimWorld")
                                {
                                    requiredResearchExpansion = prereqItem.RequiredExpansion;
                                }
                            }
                        }
                        item.RequiredExpansion = requiredResearchExpansion;
                        allDefs[item.defName] = item;
                        allDefsByLabel[item.label] = item;
                    }
                }
            }

            // Now that we have items for everything, add prereq archipelago names.
            foreach (ResearchProjectDef research in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                string researchId = $"{research.defName}Research";
                ArchipelagoItemDef item = allDefs[researchId];
                if (research.prerequisites != null)
                {
                    foreach (ResearchProjectDef prereq in research.prerequisites)
                    {
                        string prereqId = $"{prereq.defName}Research";
                        ArchipelagoItemDef prereqArchipelagoItem = allDefs[prereqId];
                        item.Prerequisites.Add(prereqArchipelagoItem.label);
                    }
                }
            }

            var sts = new XmlWriterSettings()
            {
                Indent = true,

            };

            string path = Environment.ExpandEnvironmentVariables("%userprofile%\\Documents\\ArchipelagoItemDefs.xml");
            XmlWriter writer = XmlWriter.Create(path, sts);
            writer.WriteStartDocument();
            writer.WriteStartElement("Defs");
            foreach ((string _, ArchipelagoItemDef def) in allDefs)
            {
                writer.WriteStartElement("RimWorldArchipelago.ArchipelagoItemDef");

                writer.WriteStartElement("Id");
                writer.WriteString(def.Id.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("DefType");
                writer.WriteString(def.DefType);
                writer.WriteEndElement();

                writer.WriteStartElement("defName");
                writer.WriteString(def.defName);
                writer.WriteEndElement();

                if (def.DefType == "ResearchProjectDef" || def.DefType == "ThingDef")
                {
                    writer.WriteStartElement("TechLevel");
                    writer.WriteString(def.TechLevel.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteStartElement("label");
                writer.WriteString(def.label);
                writer.WriteEndElement();

                writer.WriteStartElement("RequiredExpansion");
                writer.WriteString(def.RequiredExpansion);
                writer.WriteEndElement();

                if (def.Tags.Count > 0)
                {
                    writer.WriteStartElement("Tags");

                    foreach (string name in def.Tags)
                    {
                        writer.WriteStartElement("li");
                        writer.WriteString(name);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                if (def.Prerequisites.Count > 0)
                {
                    writer.WriteStartElement("Prerequisites");

                    foreach (string name in def.Prerequisites)
                    {
                        writer.WriteStartElement("li");
                        writer.WriteString(name);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.Flush();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }
    }
}
