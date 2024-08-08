
namespace Ubiq.XR
{
    public interface IDistanceUseable
    {
        void DistanceUse(Hand controller);

        void DistanceLink(Hand controller, IDistanceUseable target);
    }
}
