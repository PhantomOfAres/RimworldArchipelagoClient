using HarmonyLib;
using RimWorld;
using RimWorldArchipelago;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    public static class MainMenuMarker
    {
        public static bool drawing;

        static void Prefix() => drawing = true;
        static void Postfix() => drawing = false;
    }

    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    public static class MainMenu_AddHeight
    {
        static void Prefix(ref Rect rect) => rect.height += 45f;
    }

    [HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
    public static class MainMenuPatch
    {
        public static bool canLoadGame = true;

        static void Prefix(Rect rect, List<ListableOption> optList)
        {
            if (!MainMenuMarker.drawing) return;

            if (Current.ProgramState == ProgramState.Entry)
            {
                int tutorialIndex = optList.FindIndex(opt => opt.label == "Tutorial".Translate());
                int newColonyIndex = optList.FindIndex(opt => opt.label == "NewColony".Translate());
                int loadColonyIndex = optList.FindIndex(opt => opt.label == "LoadGame".Translate());
                if (newColonyIndex != -1)
                {
                    if (!ArchipelagoClient.Connected)
                    {
                        if (loadColonyIndex != -1)
                        {
                            optList.RemoveAt(loadColonyIndex);
                        }
                        optList.Insert(newColonyIndex + 1, new ListableOption("Connect to Archipelago", () =>
                        {
                            Find.WindowStack.Add(new ArchipelagoOptionsMenu());
                        }));
                        optList.RemoveAt(newColonyIndex);
                        optList.RemoveAt(tutorialIndex);
                    }
                    else
                    {
                        if (!canLoadGame && loadColonyIndex != -1)
                        {
                            optList.RemoveAt(loadColonyIndex);
                        }
                        optList.Insert(newColonyIndex + 1, new ListableOption("Connected!", () =>
                        {
                            Messages.Message("RimWorld can't unload Archipelago's changes.\nRestart RimWorld to connect to a different Archipelago server or player slot.",
                                MessageTypeDefOf.SilentInput, false);
                        }));
                    }
                }
            }
        }
    }


    public class ArchipelagoOptionsMenu : Window
    {
        ArchipelagoSettings settings;
        private bool newConnection = false;
        public ArchipelagoOptionsMenu()
        {
            settings = ArchipelagoMod.Instance.GetSettings<ArchipelagoSettings>();

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnAccept = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Gap();
            if (!ArchipelagoClient.Connected)
            {
                settings.hostname = listingStandard.TextEntryLabeled("Host name:  ", settings.hostname);
                settings.slotName = listingStandard.TextEntryLabeled("Slot name:  ", settings.slotName);
                settings.password = listingStandard.TextEntryLabeled("Password:  ", settings.password);

                if (listingStandard.ButtonText("Connect"))
                {
                    settings.Write();
                    ArchipelagoClient.Connect(settings.hostname, settings.slotName, settings.password, settings.seed);
                }
                
                if (ArchipelagoClient.ConnectionFailed)
                {
                    listingStandard.Label("Connection failed!");
                    listingStandard.Label(ArchipelagoClient.ConnectionErrorReason);
                }
            }
            // Only allow the export if the client is not connected - connecting will add a bunch of new data that we don't want to export.
            else
            {
                listingStandard.Label("Connected! Your client may be hanging! That is normal.");
                listingStandard.GapLine();

                if (settings.seed != ArchipelagoClient.SlotData.Seed)
                {
                    if (settings.seed != "")
                    {
                        newConnection = true;
                        MainMenuPatch.canLoadGame = false;
                    }
                    settings.seed = ArchipelagoClient.SlotData.Seed;
                    settings.Write();
                }

                if (newConnection)
                {
                    listingStandard.Label("Connected to a new server! Disabling loading for this session, you should start a new game.");
                    listingStandard.GapLine();
                }

                listingStandard.Label("If you want to connect to a different server, you must restart the client.");
            }

            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Gap();
            listingStandard.Gap();
            if (listingStandard.ButtonText("Close"))
            {
                Close();
            }
            listingStandard.End();
        }
    }
}
