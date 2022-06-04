
namespace viva
{


    public abstract class InputController
    {

        public readonly Mechanism vehicle;

        public delegate void InputFunc(Player player);

        public InputController(Mechanism _vehicle)
        {
            vehicle = _vehicle;
        }

        public virtual void OnEnter(Player player) { }
        public virtual void OnExit(Player player) { }

        public virtual void OnFixedUpdateControl(Player player) { }
        public virtual void OnLateUpdateControl(Player player) { }
    }

}