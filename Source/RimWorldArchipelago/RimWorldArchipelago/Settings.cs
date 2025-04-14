using HarmonyLib;
using Newtonsoft.Json;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    public class ArchipelagoSettings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public string hostname = "ws://localhost:38281";
        public string slotName = "RimworldPlayer";
        public string password = "";
        public string seed = "";

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref hostname, "hostName", "ws://localhost:38281");
            Scribe_Values.Look(ref slotName, "slotName", "RimworldPlayer");
            Scribe_Values.Look(ref password, "password", "");
            Scribe_Values.Look(ref seed, "seed", "");
            base.ExposeData();
        }
    }

    public class ArchipelagoMod : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        ArchipelagoSettings settings;

        private static ArchipelagoMod _instance;
        public static ArchipelagoMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    Log.Error($"ArchipelagoMod is uninitialized! I don't know why!");
                }

                return _instance;
            }
        }

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public ArchipelagoMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<ArchipelagoSettings>();
            _instance = this;
            Harmony harmony = new Harmony("com.phantomofares.rimworld");
            harmony.PatchAll();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Only allow the export if the client is not connected - connecting will add a bunch of new data that we don't want to export.
            if (ArchipelagoClient.Connected)
            {
                listingStandard.Label("Connected! If you're looking to export AP data, restart the client first. This can only be done while disconnected.");
            }
            else
            {
                if (listingStandard.ButtonText("Export AP Data"))
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
