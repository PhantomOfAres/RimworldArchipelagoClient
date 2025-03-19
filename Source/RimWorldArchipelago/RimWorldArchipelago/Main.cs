using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;


namespace RimWorldArchipelago
{
    public class SlotData
    {
        [JsonProperty("options")]
        public SlotOptions SlotOptions { get; set; }
    }

    public class SlotOptions
    {
        public int ResearchLocationCount { get; set; }
    }

    public class ExampleSettings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public string hostname = "ws://localhost:38281";
        public string slotName = "RimworldPlayer";
        public string password = "";

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref hostname, "hostName", "ws://localhost:38281");
            Scribe_Values.Look(ref slotName, "slotName", "RimworldPlayer");
            Scribe_Values.Look(ref password, "password", "");
            base.ExposeData();
        }
    }

    public class ExampleMod : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        ExampleSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public ExampleMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<ExampleSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            settings.hostname = listingStandard.TextEntryLabeled("Host name:  ", settings.hostname);
            settings.slotName = listingStandard.TextEntryLabeled("Slot name:  ", settings.slotName);
            settings.password = listingStandard.TextEntryLabeled("Password:  ", settings.password);

            if (listingStandard.ButtonText("Connect"))
            {
                Main.Connect(settings.hostname, settings.slotName, settings.password);
            }
            /*if (Main.Connected)
            {
                foreach ((long id, string name) in Main.GetAllLocations())
                {
                    Log.Message($"Name: {name}");
                    if (listingStandard.ButtonText(name))
                    {
                        Main.SendLocation(id);
                    }
                }
            }*/

            if (listingStandard.ButtonText("Dump Data"))
            {
                Main.ExportArchipelagoDefs();
            }
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            return "Archipelago";
        }
    }

    public class ArchipelagoItemDef : Def
    {
        public long Id;
        public string DefType;
        public List<long> Prerequisites = new List<long>();
    }

    [StaticConstructorOnStartup]
    public class Main
    {
        private static ArchipelagoSession session = null;

        static Main()
        {
            Harmony harmony = new Harmony("com.phantomofares.rimworld");
            harmony.PatchAll();

            DisableNormalResearch();
        }
        static void Socket_ErrorReceived(Exception e, string message)
        {
            Log.Message($"Socket Error: {message}");
            Log.Message($"Socket Exception: {e.Message}");

            if (e.StackTrace != null)
                foreach (var line in e.StackTrace.Split('\n'))
                    Log.Message($"    {line}");
            else
                Log.Message($"    No stacktrace provided");
        }

        public static void ItemReceived(ReceivedItemsHelper helper)
        {
            ItemInfo itemInfo = helper.DequeueItem();
            Log.Message($"Item received: {itemInfo.ItemName}");

            if (Current.ProgramState == ProgramState.Playing)
            {
                // Haha don't do this
                try
                {
                    ArchipelagoItemDef archipelagoItem = null;
                    foreach (ArchipelagoItemDef itemDef in DefDatabase<ArchipelagoItemDef>.AllDefs)
                    {
                        if (itemDef.Id == itemInfo.ItemId)
                        {
                            archipelagoItem = itemDef;
                        }
                    }
                    if (archipelagoItem == null)
                    {
                        Log.Error($"Could not find item with ID {itemInfo.ItemId} and name {itemInfo.ItemName}");
                    }
                    if (archipelagoItem.DefType == "ResearchProjectDef")
                    {
                        string researchDefName = archipelagoItem.defName.Replace("Research", "");
                        ResearchProjectDef research = DefDatabase<ResearchProjectDef>.GetNamed(researchDefName);
                        Find.ResearchManager.FinishProject(research, doCompletionDialog: true);
                    }
                }
                catch (Exception ex) { }
            }
            else
            {
                Log.Message("lol queue it or something nerd get wrecked.");
            }
        }

        public async static void Connect(string server, string user, string pass)
        {
            LoginResult result;

            if (session?.Socket != null && session.Socket.Connected)
            {
                session.Socket.DisconnectAsync().Wait();
            }

            session = ArchipelagoSessionFactory.CreateSession(server);
            session.Socket.ErrorReceived += Socket_ErrorReceived;
            session.Items.ItemReceived += ItemReceived;
            try
            {
                // handle TryConnectAndLogin attempt here and save the returned object to `result`
                result = session.TryConnectAndLogin("Rimworld", user, ItemsHandlingFlags.AllItems);
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
            }

            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to Connect to {server} as {user}:";
                foreach (string error in failure.Errors)
                {
                    errorMessage += $"\n    {error}";
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    errorMessage += $"\n    {error}";
                }
                Log.Message(errorMessage);

                return; // Did not connect, show the user the contents of `errorMessage`
            }

            var loginSuccess = (LoginSuccessful)result;

            // TODO: Pare this back, but for now, it demonstrates the system using correctly.
            Dictionary<long, ScoutedItemInfo> scoutedItemInfo = await session.Locations.ScoutLocationsAsync(session.Locations.AllLocations.ToArray());

            GenerateArchipelagoResearch(scoutedItemInfo);
        }

        private static void DisableNormalResearch()
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

        private static void GenerateArchipelagoResearch(Dictionary<long, ScoutedItemInfo> scoutedItemInfo)
        {
            int x = 0;
            int y = 0;
            SlotData slotData = session.DataStorage.GetSlotData<SlotData>();
            ResearchTabDef archipelagoTab = DefDatabase<ResearchTabDef>.GetNamed("Archipelago");
            for (int i = 0;  i < slotData.SlotOptions.ResearchLocationCount; i++)
            {
                ScoutedItemInfo scoutedItem = null;
                if (scoutedItemInfo != null && scoutedItemInfo.ContainsKey(i + 5197648000))
                {
                    scoutedItem = scoutedItemInfo[i + 5197648000];
                    // Log.Message($"ITEM: {scoutedItem.LocationName}, {scoutedItem.LocationDisplayName}, {scoutedItem.ItemName}");
                }
                ResearchProjectDef archipelagoResearch = new ResearchProjectDef();
                archipelagoResearch.defName = $"AP Research Location {i}";
                archipelagoResearch.label = scoutedItem == null ? $"AP Research Location {i}" : $"{scoutedItem.Player.Name}'s {scoutedItem.ItemName}";
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
                archipelagoResearch.baseCost = 25;
                archipelagoResearch.tab = archipelagoTab;
                archipelagoResearch.researchViewX = x * 1.0f;
                archipelagoResearch.researchViewY = y * 0.7f;
                y += 1;
                if (y > 8)
                {
                    y = 0;
                    x += 1;
                }
                archipelagoResearch.techLevel = TechLevel.Neolithic;
                archipelagoResearch.generated = true;
                DefDatabase<ResearchProjectDef>.Add(archipelagoResearch); 
            }

            ResearchProjectDef.GenerateNonOverlappingCoordinates();
        }

        public static bool Connected
        {
            get
            {
                return session != null;
            }
        }

        public static List<(long, string)> GetAllLocations()
        {
            List<(long, string)> ret = new List<(long, string)>();
            foreach (long id in session.Locations.AllLocations)
            {
                ret.Add((id, session.Locations.GetLocationNameFromId(id)));
            }

            return ret;
        }

        public static void SendLocation(long id)
        {
            session.Locations.CompleteLocationChecks(id);
        }

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
                Main.SendLocation(locationId);
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
