namespace Daxs
{
    internal class SwitchAction : BaseState, IAction, ICalculate
    {
        public SwitchAction(InputX Input) : base(  Input) {}

        public override string HUD_Text => "Mode: " + next.ToString();

        public override void Execute() 
        {
            LayoutSystem.Instance.Set(next);
        }

        Layout next = Layout.Fly;

        public void Calculate()
        {
            next = (LayoutSystem.Instance.Current.Name == Layout.Fly) ? Layout.Walk : Layout.Fly;
        }
    }
}