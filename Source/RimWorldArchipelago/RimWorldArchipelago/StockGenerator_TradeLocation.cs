using RimWorld;
using RimWorld.Planet;
using RimworldArchipelago;
using System.Collections.Generic;
using Verse;

class StockGenerator_TradeLocation : StockGenerator
{
    private ThingDef thingDef;

    public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
    {
        if (ArchipelagoGameComponent.PlayerHasMoreTradeLocations())
        {
            foreach (Thing item in StockGeneratorUtility.TryMakeForStock(thingDef, 1, faction))
            {
                item.def.tradeability = Tradeability.Buyable;
                yield return item;
            }
        }
        else
        {
            yield return null;
        }
    }

    public override bool HandlesThingDef(ThingDef thingDef)
    {
        return thingDef == this.thingDef;
    }

    public override IEnumerable<string> ConfigErrors(TraderKindDef parentDef)
    {
        foreach (string item in base.ConfigErrors(parentDef))
        {
            yield return item;
        }

        if (!thingDef.tradeability.TraderCanSell())
        {
            yield return thingDef?.ToString() + " tradeability doesn't allow traders to sell this thing";
        }
    }
}