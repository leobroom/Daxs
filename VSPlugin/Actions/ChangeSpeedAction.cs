using Daxs.Layout;
using Rhino;

namespace Daxs.Actions
{
    internal class ChangeSpeedAction : ActionBase, IAction
    {
        private bool _entered;

        public ChangeSpeedAction(InputX Input) : base(Input) { }

        public override string HUD_Text => "Change speed";
        public override string HUD_Emoji => "⚡";

        private LayoutType _current = LayoutType.Fly;

        public override void Execute()
        {
            if (_entered) return;
            _entered = true;

            ActionGate.Instance.EnterModal(this);

            // Switch layout + notify it
            var lm = LayoutSystem.Instance;
            _current = lm.Current.Name;
            lm.Set(LayoutType.Custom);

            if (lm.Current is ChangeSpeedLayout cl)
                cl.EnterSpeedAdjustMode(this, _current);
        }

        protected override void OnDeactivated()
        {
            RhinoApp.WriteLine("OnDeactivated");
            // Layout cleanup
            var lm = LayoutSystem.Instance;

            if (lm.Current is ChangeSpeedLayout cl)
                cl.ExitSpeedAdjustMode(this);

            // Exit modal suppression
            ActionGate.Instance.ExitModal(this);

            _entered = false;

            // Restore prior layout
            lm.Set(_current);
        }
    }
}