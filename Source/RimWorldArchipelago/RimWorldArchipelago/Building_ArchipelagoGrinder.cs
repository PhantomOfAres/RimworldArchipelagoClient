using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

public class Building_ArchipelagoGrinder : Building_WorkTable
{
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        Command_Action commandAction = new Command_Action();
        commandAction.defaultLabel = "Send Craft Locations";
        commandAction.defaultDesc = "Set the recipes that this workbench will send to the multiworld";
        commandAction.icon = ContentFinder<Texture2D>.Get("ArchipelagoIcons/ColorIcon");
        commandAction.action = delegate
        {
            Find.WindowStack.Add(new Dialog_Grinder(this));
        };
        yield return commandAction;
    }
}
