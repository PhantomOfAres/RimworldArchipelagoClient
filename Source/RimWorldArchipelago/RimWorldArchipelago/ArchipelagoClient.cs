using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;
using RimWorldArchipelago;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimworldArchipelago
{
    public enum VictoryType
    {
        Any = 0,
        ShipLaunch = 1,
        Royalty = 2,
        Archonexus = 3,
        Anomaly = 4
    }

    public enum StartingResearchLevel
    {
        None = 0,
        Tribal = 1,
        Crashlanded = 2
    }

    public class SlotData
    {
        [JsonProperty("seed")]
        public string Seed { get; set; }
        [JsonProperty("options")]
        public SlotOptions SlotOptions { get; set; }
        [JsonProperty("craft_recipes")]
        public Dictionary<long, List<string>> CraftRecipes { get; set; }
    }

    public class SlotOptions
    {
        public int BasicResearchLocationCount { get; set; }
        public int HiTechResearchLocationCount { get; set; }
        public int MultiAnalyzerResearchLocationCount { get; set; }
        public int ResearchBaseCost { get; set; }
        public int ResearchMaxPrerequisites { get; set; }
        public VictoryType VictoryCondition { get; set; }
        public bool RoyaltyEnabled { get; set; }
        public bool IdeologyEnabled { get; set; }
        public bool BiotechEnabled { get; set; }
        public bool AnomalyEnabled { get; set; }
        public StartingResearchLevel StartingResearchLevel { get; set; }

    }

    public class ArchipelagoGameComponent: GameComponent
    {
        private int handledIndexCount = 0;

        public ArchipelagoGameComponent(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref handledIndexCount, "handledIndexCount");
            base.ExposeData();
        }

        public override void GameComponentUpdate()
        {
            // We may not want to do this every frame, but for now it works fine.
            if (ArchipelagoClient.Connected && Current.ProgramState == ProgramState.Playing)
            {
                ArchipelagoClient.HandleNextReceivedItemIfNeeded(ref handledIndexCount);
            }
        }
    }

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

        public static void ItemReceived(ItemInfo itemInfo)
        {
            Log.Message($"Item received: {itemInfo.ItemName}");
            if (Current.ProgramState == ProgramState.Playing)
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
                    if (research != null)
                    {
                        Find.ResearchManager.FinishProject(research, doCompletionDialog: true);
                    }
                }
            }
            else
            {
                Log.Message("lol queue it or something nerd get wrecked.");
            }
        }

        public static void HandleNextReceivedItemIfNeeded(ref int handledIndexCount)
        {
            if (handledIndexCount < session.Items.AllItemsReceived.Count)
            {
                ItemInfo item = session.Items.AllItemsReceived[handledIndexCount];
                ItemReceived(item);
                handledIndexCount += 1;
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

            APResearchManager.DisableNormalResearch();
            APResearchManager.GenerateArchipelagoResearch(scoutedItemInfo);
            APCraftManager.GenerateArchipelagoCrafts();
        }

        public static bool Connected
        {
            get
            {
                return session != null && session.Socket.Connected;
            }
        }

        public static SlotData SlotData
        {
            get
            {
                return session?.DataStorage?.GetSlotData<SlotData>();
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

        public static bool IsLocationComplete(long locationId)
        {
            return session.Locations.AllLocationsChecked.Contains(locationId);
        }

        public static void SendResearchLocation(string projectName)
        {
            long id = APResearchManager.GetLocationId(projectName);
            session.Locations.CompleteLocationChecks(id);
        }

        public static void SendCraftLocation(string craftRecipeName)
        {
            long id = APCraftManager.GetLocationId(craftRecipeName);
            session.Locations.CompleteLocationChecks(id);
        }

        public static void VictoryAchieved(VictoryType type)
        {
            VictoryType winCondition = SlotData.SlotOptions.VictoryCondition;
            if (type == winCondition || winCondition == VictoryType.Any)
            {
                var statusUpdatePacket = new StatusUpdatePacket();
                statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(statusUpdatePacket);
            }
        }
    }
}
