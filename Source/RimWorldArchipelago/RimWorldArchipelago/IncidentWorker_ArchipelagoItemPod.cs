using System.Collections.Generic;
using RimWorld;
using Verse;

public class ArchipelagoItemIncidentParams : IncidentParms
{
    public string sender;
}

public class IncidentWorker_ArchipelagoItemPod : IncidentWorker
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        string sender = "Someone";
        if (parms is ArchipelagoItemIncidentParams apParms)
        {
            sender = apParms.sender;
        }
        Map map = (Map)parms.target;
        IntVec3 intVec = DropCellFinder.RandomDropSpot(map);
        DropPodUtility.DropThingsNear(intVec, map, parms.gifts, 110, canInstaDropDuringInit: false, leaveSlag: true);
        SendStandardLetter("Archipelago Sculpture", $"{sender} sent you an Archipelago Structure, which you need for your monument victory!", LetterDefOf.PositiveEvent, parms, new TargetInfo(intVec, map));
        return true;
    }
}

