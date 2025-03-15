//using Archipelago.MultiClient.Net;
// using RimWorld;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimWorldArchipelago
{
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
            listingStandard.TextEntryLabeled("Host name:  ", settings.hostname);
            listingStandard.TextEntryLabeled("Slot name:  ", settings.slotName);
            listingStandard.TextEntryLabeled("Password:  ", settings.password);
            if (listingStandard.ButtonText("Connect"))
            {
                Main.Connect(settings.hostname, settings.slotName, settings.password);
            }
            if (Main.Connected)
            {
                foreach ((long id, string name) in Main.GetAllLocations())
                {
                    if (listingStandard.ButtonText(name))
                    {
                        Main.SendLocation(id);
                    }
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

    [StaticConstructorOnStartup]
    public class Main
    {
        private static ArchipelagoSession session = null;

        static Main()
        {
            //WebSocket blep = new WebSocket();
            Log.Message("Hello World!"); //Outputs "Hello World!" to the dev console.

            //string address = "ws://localhost:38281";
            //Connect(address, "RimworldPlayer", "");
            //var test = Newtonsoft.Json.DateParseHandling.DateTime;
            //Log.Message(test.ToString());

            
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
        }

        public static void Connect(string server, string user, string pass)
        {
            LoginResult result;

            session = ArchipelagoSessionFactory.CreateSession(server);
            session.Socket.ErrorReceived += Socket_ErrorReceived;
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

            // Successfully connected, `ArchipelagoSession` (assume statically defined as `session` from now on) can now be used to interact with the server and the returned `LoginSuccessful` contains some useful information about the initial connection (e.g. a copy of the slot data as `loginSuccess.SlotData`)
            var loginSuccess = (LoginSuccessful)result;

            foreach (long id in session.Locations.AllLocations)
            {
                Log.Message($"Location: {session.Locations.GetLocationNameFromId(id)}, {id}");
            }

            //session.Locations.CompleteLocationChecks(5197648001, 5197648003);

            session.Items.ItemReceived += ItemReceived;
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
    }


}
