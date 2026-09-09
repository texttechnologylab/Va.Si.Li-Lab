
using Oculus.Interaction.Input;

namespace Ubiq.XR
{
    public interface IUseable
    {
        void Use(Hand controller);

        void UnUse(Hand controller);
    }
}
