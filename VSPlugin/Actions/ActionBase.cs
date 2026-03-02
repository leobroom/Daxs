using Daxs.GUI;
using Daxs.Layout;

namespace Daxs.Actions
{
    internal abstract class ActionBase : IAction
    {

        protected OverlayRenderer _hud = OverlayRenderer.Instance;

        public ActionBase(InputX Input)
        {
            this.Input = Input;
        }

        public InputX Input { get; }
        public abstract string HUD_Text { get; }

        public virtual string HUD_Emoji => "🎮";

        public abstract void Execute();

        /// <summary>
        /// Set this to true (or override) if this action should get a callback
        /// once the triggering input is no longer active (e.g. button released).
        /// Default: only hold-actions.
        /// </summary>
        public virtual bool WantsDeactivateCallback => Input == InputX.IsHold;

        /// <summary>
        /// Called exactly once when the action was active and becomes inactive.
        /// Derived actions can override to dispose/close/commit state.
        /// </summary>
        protected virtual void OnDeactivated() { }

        internal void NotifyDeactivated() => OnDeactivated();
    }
}