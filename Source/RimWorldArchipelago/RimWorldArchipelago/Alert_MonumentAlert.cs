using System;
using RimWorld;
using RimworldArchipelago;
using Verse;

public class Alert_MonumentAlert : Alert
{
    private AlertReport alwaysOnReport = new AlertReport();

    public override string GetLabel()
    {
        if (ArchipelagoGameComponent.HasAchievedMonumentVictory)
        {
            return "Archipelago Monument Fulfilled!";
        }
        else
        {
            return "Archipelago Monument Unfulfilled";
        }
    }

    public override AlertReport GetReport()
    {
        if (!ArchipelagoClient.Connected)
        {
            return false;
        }

        return ArchipelagoClient.SlotData.SlotOptions.VictoryCondition == VictoryType.Monument;
    }
    
    public override TaggedString GetExplanation()
    {
        return ArchipelagoGameComponent.CachedRequirementString;
    }
}

