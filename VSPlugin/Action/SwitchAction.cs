namespace Daxs
{
    internal class SwitchAction : BaseState, IAction
    {
        public SwitchAction(InputX Input) : base(  Input) {}

        public override string HUD_Name => LayoutManager.Instance.Current.Name.ToString();

        public override void Execute() 
        {
            var current = LayoutManager.Instance.Current;

            Layout name = current.Name;
            if (name == Layout.Fly)
                name = Layout.Walk;
            else
                name = Layout.Fly;

            LayoutManager.Instance.Set(name);
        }
    }
}