using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;
using RimWorld;
using RimWorldArchipelago;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        private const int MIN_TICKS_IN_A_DAYISH = 45000;
        private const int MAX_TICKS_IN_A_DAYISH = 75000;

        private int handledIndexCount = 0;
        private int ticksLeftInTimer = 0;
        private static List<string> IncidentsToRunOnTimer = new List<string>();

        public ArchipelagoGameComponent(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref handledIndexCount, "handledIndexCount");
            Scribe_Values.Look(ref ticksLeftInTimer, "ticksLeftInTimer");
            Scribe_Collections.Look(ref IncidentsToRunOnTimer, "incidentsToRunOnTimer", LookMode.Value);
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

        public static void IncidentReceived(ArchipelagoItemDef itemDef)
        {
            string incidentDefName = itemDef.defName.Replace("Incident", "");
            if (itemDef.Tags.Contains("Negative"))
            {
                IncidentsToRunOnTimer.Add(incidentDefName);
            }
            else
            {
                IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(incidentDefName);
                IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(incidentDef.category, Find.CurrentMap);
                incidentDef.Worker.TryExecute(incidentParams);
            }
        }

        public override void GameComponentTick()
        {
            if (ticksLeftInTimer > 0)
            {
                ticksLeftInTimer -= 1;
            }
            // Only trigger (and reset) the timer if something's on the list.
            if (ticksLeftInTimer <= 0 && IncidentsToRunOnTimer.Count > 0)
            {
                // Shuffle the list so one is picked randomly.
                for (int i = IncidentsToRunOnTimer.Count - 1; i > 0; i--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, i + 1);
                    string iContents = IncidentsToRunOnTimer[i];
                    IncidentsToRunOnTimer[i] = IncidentsToRunOnTimer[swapIndex];
                    IncidentsToRunOnTimer[swapIndex] = iContents;
                }

                string incidentToRemove = "";
                foreach (string incidentDefName in IncidentsToRunOnTimer)
                {
                    IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(incidentDefName);
                    IncidentParms incidentParams = StorytellerUtility.DefaultParmsNow(incidentDef.category, Find.CurrentMap);

                    // Removing this section for now. Basic raids work fine. I'll figure out how I want to sprinkle in variety later.
                    /*
                    // We do a mini CanStartNow check here. Don't spawn an incident with too-low threat points, because it will kill the player...
                    if (incidentParams.points >= 0f && incidentParams.points < incidentDef.minThreatPoints)
                    {
                        continue;
                    }

                    // And don't spawn an anomaly incident before the player has advanced that far.
                    if (ModsConfig.AnomalyActive && incidentDef.IsAnomalyIncident && Find.Anomaly.LevelDef.anomalyThreatTier < incidentDef.minAnomalyThreatLevel && Find.Anomaly.GenerateMonolith)
                    {
                        continue;
                    }*/

                    // Otherwise, ignore all other checks. Hope you don't die good luck!
                    incidentDef.Worker.TryExecute(incidentParams);
                    incidentToRemove = incidentDefName;
                    break;
                }

                if (incidentToRemove != "")
                {
                    IncidentsToRunOnTimer.Remove(incidentToRemove);
                }

                ticksLeftInTimer = UnityEngine.Random.Range(MIN_TICKS_IN_A_DAYISH, MAX_TICKS_IN_A_DAYISH);
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
                else if (archipelagoItem.DefType == "IncidentDef")
                {
                    ArchipelagoGameComponent.IncidentReceived(archipelagoItem);
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
