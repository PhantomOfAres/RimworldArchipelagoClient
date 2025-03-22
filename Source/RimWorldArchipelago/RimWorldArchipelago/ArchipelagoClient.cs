using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using RimWorld;
using RimWorldArchipelago;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimworldArchipelago
{
    internal class ArchipelagoClient
    {
        private static ArchipelagoSession session = null;

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

        private static void GenerateArchipelagoResearch(Dictionary<long, ScoutedItemInfo> scoutedItemInfo)
        {
            int x = 0;
            int y = 0;
            SlotData slotData = session.DataStorage.GetSlotData<SlotData>();
            ResearchTabDef archipelagoTab = DefDatabase<ResearchTabDef>.GetNamed("Archipelago");
            for (int i = 0; i < slotData.SlotOptions.ResearchLocationCount; i++)
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
                archipelagoResearch.baseCost = slotData.SlotOptions.ResearchBaseCost;
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
    }
}
