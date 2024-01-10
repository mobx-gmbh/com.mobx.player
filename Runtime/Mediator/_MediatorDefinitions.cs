using Cinemachine;
using MobX.Mediator.Generation;
using MobX.Player;
using UnityEngine;
using static MobX.Player.Mediator.MediatorDefinitions;

[assembly: GenerateMediatorFor(typeof(Camera), MediatorTypes = Basics, NameSpace = NameSpace)]
[assembly: GenerateMediatorFor(typeof(PlayerCharacter), MediatorTypes = Basics, NameSpace = NameSpace)]
[assembly: GenerateMediatorFor(typeof(CinemachineBrain), MediatorTypes = Basics, NameSpace = NameSpace)]

namespace MobX.Player.Mediator
{
    public static class MediatorDefinitions
    {
        public const string NameSpace = "MobX.Player";
        public const MediatorTypes Basics = MediatorTypes.ValueAsset | MediatorTypes.EventAsset;
    }
}