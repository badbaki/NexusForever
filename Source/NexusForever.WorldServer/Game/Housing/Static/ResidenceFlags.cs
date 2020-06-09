using System;

namespace NexusForever.WorldServer.Game.Housing.Static
{
    [Flags]
    public enum ResidenceFlags
    {
        None,
        groundClutterOff = 1, //Mow ur own damn lawn - BAKI
        HideNeighborSkyplots
    }
}
