using RimWorld;
using RimworldArchipelago;
using System;
using System.Collections;
using System.Collections.Generic;
using Verse;
using Verse.AI;

public class JobDriver_SendRaidLocation : JobDriver_UseItem
{
    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
        this.FailOn(() => !base.TargetThingA.TryGetComp<CompUsable>().CanBeUsedBy(pawn));
        yield return Toils_Goto.GotoThing(TargetIndex.A, base.TargetThingA.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
        yield return PrepareToUse();
        Toil useToil = Use();
        useToil.AddFinishAction(() => ArchipelagoGameComponent.SendRaidLocation());
        yield return useToil;
    }
}
