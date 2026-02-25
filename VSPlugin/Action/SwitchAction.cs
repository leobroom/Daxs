namespace Daxs
{
    internal class SwitchAction : BaseState, IAction
    {
        public SwitchAction(InputX Input) : base(  Input) {}

        public override string HUD_Text => "Mode: " + next.ToString();

        public override void Execute() 
        {
            next = (LayoutSystem.Instance.Current.Name == Layout.Fly) ? Layout.Walk : Layout.Fly;
            LayoutSystem.Instance.Set(next);
            _hud.SetText(HUD_Emoji, HUD_Text);
        }

        Layout next = Layout.Fly;
    }
}