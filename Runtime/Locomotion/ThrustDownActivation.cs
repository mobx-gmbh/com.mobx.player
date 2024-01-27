using System;

namespace MobX.Player.Locomotion
{
    [Flags]
    public enum ThrustDownActivation
    {
        None = 0,
        Input = 1,
        LastManeuver = 2
    }
}