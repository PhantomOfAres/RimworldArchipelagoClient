using RimWorld;
using System;

public enum APItemLocationType
{
    None = 0,
    Raid = 1,
    Trade = 2,
}

public class CompProperties_SendAPLocation : CompProperties_UseEffect
{
    public int delayTicks = -1;

    public float orderPriority = -1000f;
    public APItemLocationType locationType = APItemLocationType.None;
    public CompProperties_SendAPLocation()
    {
        compClass = typeof(CompUseEffect_SendAPLocation);
    }
}
