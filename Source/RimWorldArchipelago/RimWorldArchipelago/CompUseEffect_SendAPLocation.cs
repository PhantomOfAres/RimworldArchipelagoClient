using RimWorld;
using RimworldArchipelago;
using System;
using Verse;

public class CompUseEffect_SendAPLocation : CompUseEffect
{
    private int delayTicks = -1;

    private CompProperties_SendAPLocation Props => (CompProperties_SendAPLocation)props;

    public override float OrderPriority => Props.orderPriority;

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);
        if (Props.delayTicks <= 0)
        {
            SendLocation();
        }
        else
        {
            delayTicks = Props.delayTicks;
        }
    }

    private void SendLocation()
    {
        if (Props.locationType == APItemLocationType.Raid)
        {
            ArchipelagoGameComponent.SendRaidLocation();
        }
        if (Props.locationType == APItemLocationType.Trade)
        {
            ArchipelagoGameComponent.SendTradeLocation();
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref delayTicks, "delayTicks", -1);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (delayTicks > 0)
        {
            delayTicks--;
        }
        if (delayTicks == 0)
        {
            SendLocation();
            delayTicks = -1;
        }
    }
}
