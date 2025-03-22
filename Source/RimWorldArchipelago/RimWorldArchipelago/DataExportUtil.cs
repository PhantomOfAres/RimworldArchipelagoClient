using RimWorldArchipelago;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
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
                item.label = textInfo.ToTitleCase(research.label);
                allDefs[item.defName] = item;
            }

            // Now that we have items for everything, add prereq archipelago ids.
            foreach (ResearchProjectDef research in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                ArchipelagoItemDef item = allDefs[$"{research.defName}Research"];
                if (research.prerequisites != null)
                {
                    foreach (ResearchProjectDef prereq in research.prerequisites)
                    {
                        ArchipelagoItemDef prereqArchipelagoItem = allDefs[prereq.defName];
                        item.Prerequisites.Add(prereqArchipelagoItem.Id);
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

                writer.WriteStartElement("label");
                writer.WriteString(def.label);
                writer.WriteEndElement();

                if (def.Prerequisites.Count > 0)
                {
                    writer.WriteStartElement("Prerequisites");

                    foreach (long id in def.Prerequisites)
                    {
                        writer.WriteStartElement("li");
                        writer.WriteString(id.ToString());
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
