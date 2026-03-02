using Daxs.Layout;

namespace Daxs.Actions
{
    internal class SwitchAction : ActionBase, IAction
    {
        public SwitchAction(InputX Input) : base(  Input) {}

        public override string HUD_Text => "Mode: " + next.ToString();

        public override void Execute() 
        {
            next = (LayoutSystem.Instance.Current.Name == LayoutType.Fly) ? LayoutType.Walk : LayoutType.Fly;
            LayoutSystem.Instance.Set(next);
            _hud.SetText(HUD_Emoji, HUD_Text);
        }

        LayoutType next = LayoutType.Fly;
    }
}