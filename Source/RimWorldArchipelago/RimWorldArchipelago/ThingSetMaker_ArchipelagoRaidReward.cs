using RimWorld;
using RimworldArchipelago;
using System;
using System.Collections.Generic;
using Verse;

public class ThingSetMaker_ArchipelagoRaidReward : ThingSetMaker_MarketValue
{
    protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
    {
        base.Generate(parms, outThings);
        if (!ArchipelagoGameComponent.PlayerHasMoreRaidLocations())
        {
            return;
        }

        ThingDef locationItemDef = DefDatabase<ThingDef>.GetNamed("RaidLocationItem");
        Thing newThing = ThingMaker.MakeThing(locationItemDef);
        outThings.Add(newThing);
    }
}
