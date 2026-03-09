using Daxs.Layout;

namespace Daxs.Actions
{
    internal class ChangeSpeedAction : ActionBase, IAction
    {
        private bool _entered;

        public ChangeSpeedAction(InputX Input) : base(Input) { }

        public override string HUD_Text => "Change speed";
        public override string HUD_Emoji => "⚡";

        public override void Execute()
        {
            if (_entered) return;
            _entered = true;

            ActionGate.Instance.EnterModal(this);

            // Switch layout + notify it
            var lm = LayoutSystem.Instance;
            lm.Set(LayoutType.Custom);

            if (lm.Current is CustomLayout cl)
                cl.EnterSpeedAdjustMode(this);
        }

        protected override void OnDeactivated()
        {
            // Layout cleanup
            var lm = LayoutSystem.Instance;

            if (lm.Current is CustomLayout cl)
                cl.ExitSpeedAdjustMode(this);

            // Exit modal suppression
            ActionGate.Instance.ExitModal(this);

            _entered = false;

            // Restore prior layout
            lm.SetToPreviousLayout();
        }
    }
}