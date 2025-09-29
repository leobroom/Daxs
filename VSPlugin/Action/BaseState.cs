namespace Daxs
{
    internal abstract class BaseState : IBase
    {
        public BaseState(AProperty Name, GButton Button, InputX Input)
        {
            this.Name = Name;
            this.Button = Button;
            this.Input = Input;
        }

        public AProperty Name { get; }

        public GButton Button { get; }

        public InputX Input { get; }

        public abstract object[] GetArgs();
    }
}