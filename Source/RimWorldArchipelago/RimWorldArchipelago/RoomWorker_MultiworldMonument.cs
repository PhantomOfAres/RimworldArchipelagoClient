using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RimworldArchipelago
{
    public class RoomRoleWorker_MultiworldMonument : RoomRoleWorker
    {
        public const string ARCHIPELAGO_STRUCTURE_DEF_NAME = "SculptureArchipelago";

        public override float GetScore(Room room)
        {
            return GetRoomScore(room);
        }

        public static float GetRoomScore(Room room)
        {
            int numStatues = 0;
            int numBuildingRequirements = 0;
            Dictionary<string, int> requirements = ArchipelagoClient.SlotData.MonumentBuildings;
            if (requirements.NullOrEmpty())
            {
                return 0;
            }

            Dictionary<string, int> fulfilled = new Dictionary<string, int>();
            foreach (string thingDefName in requirements.Keys)
            {
                fulfilled[thingDefName] = 0;
            }

            List<Thing> containedAndAdjacentThings = room.ContainedAndAdjacentThings;
            for (int i = 0; i < containedAndAdjacentThings.Count; i++)
            {
                Thing thing = containedAndAdjacentThings[i];
                if (thing.def.defName == ARCHIPELAGO_STRUCTURE_DEF_NAME)
                {
                    numStatues += 1;
                }
                else if (requirements.ContainsKey(thing.def.defName) && fulfilled[thing.def.defName] < requirements[thing.def.defName])
                {
                    fulfilled[thing.def.defName] += 1;
                    numBuildingRequirements += 1;
                }
            }

            if (numStatues == 0)
            {
                return 0;
            }

            return (float)numStatues * 200201f + numBuildingRequirements * 100f + room.GetStat(RoomStatDefOf.Wealth);
        }

        public static string GetRequirementString(Room room)
        {
            if (!ArchipelagoClient.Connected)
            {
                return "Not Connected!";
            }

            StringBuilder stringBuilder = new StringBuilder();
            Dictionary<string, int> requirements = ArchipelagoClient.SlotData.MonumentBuildings;
            Dictionary<string, int> fulfilled = new Dictionary<string, int>();
            foreach (string thingDefName in requirements.Keys)
            {
                fulfilled[thingDefName] = 0;
            }

            int roomWealth = 0;
            if (room != null)
            {
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    if (requirements.ContainsKey(thing.def.defName) && fulfilled[thing.def.defName] < requirements[thing.def.defName])
                    {
                        fulfilled[thing.def.defName] += 1;
                    }
                }

                roomWealth = Mathf.RoundToInt(room.GetStat(RoomStatDefOf.Wealth));
            }

            bool foundError = false;
            stringBuilder.AppendLine("To win the game by building an Archipelago Monument, you must construct a single room with the following requirements:\n");
            foreach (string thingDefName in requirements.Keys)
            {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                if (thingDef == null)
                {
                    foundError = true;
                }
                else
                {
                    stringBuilder.AppendLine($"{thingDef.LabelCap}: {fulfilled[thingDefName]}/{requirements[thingDefName]}");
                }
            }

            if (ArchipelagoClient.SlotData.MonumentWealthRequirement > 0)
            {
                float cappedWealth = Mathf.Min(roomWealth, ArchipelagoClient.SlotData.MonumentWealthRequirement);
                stringBuilder.AppendLine($"Room wealth: {cappedWealth}/{ArchipelagoClient.SlotData.MonumentWealthRequirement}");
            }

            if (foundError)
            {
                stringBuilder.AppendLine("Couldn't find one of the listed buildings - ignoring it for this game!");
            }

            return stringBuilder.ToString();
        }

        public static bool CanWin(Room room)
        {
            if (!ArchipelagoClient.Connected)
            {
                return false;
            }

            Dictionary<string, int> requirements = ArchipelagoClient.SlotData.MonumentBuildings;
            Dictionary<string, int> fulfilled = new Dictionary<string, int>();
            foreach (string thingDefName in requirements.Keys)
            {
                fulfilled[thingDefName] = 0;
            }

            if (room != null)
            {
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    if (requirements.ContainsKey(thing.def.defName) && fulfilled[thing.def.defName] < requirements[thing.def.defName])
                    {
                        fulfilled[thing.def.defName] += 1;
                    }
                }

                if (room.GetStat(RoomStatDefOf.Wealth) < ArchipelagoClient.SlotData.MonumentWealthRequirement)
                {
                    return false;
                }
            }

            foreach (string thingDefName in requirements.Keys)
            {
                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
                if (thingDef != null && requirements[thingDefName] > fulfilled[thingDefName])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
