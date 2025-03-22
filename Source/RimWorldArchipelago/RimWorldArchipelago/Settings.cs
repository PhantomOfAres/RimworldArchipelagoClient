using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    public class SlotData
    {
        [JsonProperty("options")]
        public SlotOptions SlotOptions { get; set; }
    }

    public class SlotOptions
    {
        public int ResearchLocationCount { get; set; }
        public int ResearchBaseCost { get; set; }
    }

    public class ArchipelagoSettings : ModSettings
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

    public class ArchipelagoMod : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        ArchipelagoSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public ArchipelagoMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<ArchipelagoSettings>();
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
                ArchipelagoClient.Connect(settings.hostname, settings.slotName, settings.password);
            }

            // Only allow the export if the client is not connected - connecting will add a bunch of new data that we don't want to export.
            if (!ArchipelagoClient.Connected)
            {
                if (listingStandard.ButtonText("Export Data"))
                {
                    DataExportUtil.ExportArchipelagoDefs();
                }
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
}
