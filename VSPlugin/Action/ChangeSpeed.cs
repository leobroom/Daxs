using Rhino;
using System;

namespace Daxs
{
    internal class ChangeSpeedModal : BaseState, IAction
    {
        private bool _entered;

        public ChangeSpeedModal(InputX Input) : base(Input) { }

        public override string HUD_Text => "Change speed";
        public override string HUD_Emoji => "⚡";

        public override void Execute()
        {
            if (_entered) return;
            _entered = true;

            RhinoApp.WriteLine("Change speed");


            InputGate.Instance.EnterModal(this);

            // Switch layout + notify it
            var lm = LayoutSystem.Instance;
            lm.Set(Layout.Custom);

            if (lm.Current is CustomLayout cl)
                cl.EnterSpeedAdjustMode(this);
        }

        protected override void OnDeactivated()
        {
            RhinoApp.WriteLine("Change speed STOP");

            // Layout cleanup
            var lm = LayoutSystem.Instance;

            if (lm.Current is CustomLayout cl)
                cl.ExitSpeedAdjustMode(this);

            // Exit modal suppression
            InputGate.Instance.ExitModal(this);

            _entered = false;

            // Restore prior layout
            lm.SetToPreviousLayout();
        }
    }
}