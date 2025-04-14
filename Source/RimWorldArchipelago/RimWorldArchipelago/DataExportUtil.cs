using RimWorld;
using RimWorldArchipelago;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    internal class DataExportUtil
    {
        public static void ExportArchipelagoDefs()
        {
            long nextId = 769100;

            Dictionary<string, ArchipelagoItemDef> allDefs = new Dictionary<string, ArchipelagoItemDef>();
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
                        // Ensure we haven't already included this item type, and ensure we're only targeting items, not buildings or mechs or whatever.
                        if (!alreadyPopulatedItem.Contains(product.thingDef.defName) &&
                            product.thingDef.category == ThingCategory.Item)
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

                            // If the bench(es) used to craft this item require research, mark it as an (Archipelago) prerequisite (If the player has flake production but no drug
                            //   lab, the player can't make flake.)
                            if (recipeDef.AllRecipeUsers != null)
                            {
                                foreach (ThingDef benchType in recipeDef.AllRecipeUsers)
                                {
                                    if (benchType.researchPrerequisites != null)
                                    {
                                        // Note: This logic is slightly off - we require ALL benches be craftable, rather than ANY of them. So clothing will require
                                        //   electricity and complex clothing to be considered in logic, since you can craft it at either a hand or electric tailoring
                                        //   table. That's going to skew electricity early generally, which is somewhere between fine and ideal.
                                        foreach (ResearchProjectDef benchResearch in benchType.researchPrerequisites)
                                        {
                                            ArchipelagoItemDef prereqArchipelagoItem = allDefs[$"{benchResearch.defName}Research"];
                                            if (!item.Prerequisites.Contains(prereqArchipelagoItem.label))
                                            {
                                                item.Prerequisites.Add(prereqArchipelagoItem.label);
                                            }
                                        }
                                    }
                                }
                            }
                            allDefs[item.defName] = item;
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
                if ((badThreatIncidentCategories.Contains(incidentDef.category) ||
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
                    if (goodIncidentCategories.Contains(incidentDef.category))
                    {
                        item.Tags.Add("Positive");
                    }
                    item.Tags.Add(incidentDef.category.defName);
                    allDefs[item.defName] = item;
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
