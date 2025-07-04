using System.Collections.Generic;
using RimWorld;
using Verse;

public class ArchipelagoItemIncidentParams : IncidentParms
{
    public string sender;
    public string letterTitle;
    public string letterText;
}

public class IncidentWorker_ArchipelagoItemPod : IncidentWorker
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        string sender = "Someone";
        string title = "Archipelago Item Received";
        string description = $"{sender} sent you a thing!";
        if (parms is ArchipelagoItemIncidentParams apParms)
        {
            sender = apParms.sender;
            title = apParms.letterTitle;
            description = apParms.letterText;
        }
        Map map = (Map)parms.target;
        IntVec3 intVec = DropCellFinder.RandomDropSpot(map);
        DropPodUtility.DropThingsNear(intVec, map, parms.gifts);
        SendStandardLetter(title, description, LetterDefOf.PositiveEvent, parms, new TargetInfo(intVec, map));
        return true;
    }
}

