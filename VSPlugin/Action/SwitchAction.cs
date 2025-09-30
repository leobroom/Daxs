namespace Daxs
{
    internal class SwitchAction : BaseState, IAction
    {
        public SwitchAction( GButton Button, InputX Input) : base(AProperty.Switch,  Button,  Input) {}

        public SwitchAction(ActionBindingDto dto) : this(dto.Button, dto.Input) { }

        public string HUD_Name => LayoutManager.Instance.CurrentLayout.Name.ToString();

        public void Execute() 
        {
            var current = LayoutManager.Instance.CurrentLayout;

            Layout name = current.Name;
            if (name == Layout.Fly)
                name = Layout.Walk;
            else
                name = Layout.Fly;

            LayoutManager.Instance.Set(name);
        }

        public override object[] GetArgs()=> System.Array.Empty<object>();
    }
}