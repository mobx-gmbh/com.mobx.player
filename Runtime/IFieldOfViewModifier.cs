using MobX.Mediator.Generation;

namespace MobX.Player
{
    [GenerateMediator(MediatorTypes.ListAsset)]
    public interface IFieldOfViewModifier
    {
        public void ModifyFieldOfView(ref float fieldOfView, float unmodifiedFieldOfView);
    }
}