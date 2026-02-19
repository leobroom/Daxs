namespace Daxs
{
    internal abstract class BaseState : IAction
    {
        public BaseState(InputX Input)
        {
            this.Input = Input;
        }

        public InputX Input { get; }
        public abstract string HUD_Text { get; }

        public virtual string HUD_Emoji => "🎮";

        public abstract void Execute();
    }
}