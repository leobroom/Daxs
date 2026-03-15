using Daxs.GUI;

namespace Daxs.Actions
{
    /// <summary>
    /// Daxs Action after Command Pattern
    /// </summary>
    internal abstract class ActionBase : IAction
    {
        protected HUD _hud = HUD.Instance;

        public ActionBase(InputX Input)
        {
            this.Input = Input;
        }

        /// <summary>
        /// Button Input neccesary to trigger Actiob
        /// </summary>
        public InputX Input { get; }

        /// <summary>
        /// Text to Display in HUD
        /// </summary>
        public abstract string HUD_Text { get; }

        /// <summary>
        /// Emoji to Display in HUD
        /// </summary>
        public virtual string HUD_Emoji => "🎮";

        /// <summary>
        /// Execute Action
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Set this to true (or override) if this action should get a callback
        /// once the triggering input is no longer active (e.g. button released).
        /// Default: only hold-actions.
        /// </summary>
        public virtual bool WantsDeactivateCallback => Input == InputX.IsHold;

        /// <summary>
        /// Called exactly once when the action was active and becomes inactive.
        /// </summary>
        protected virtual void OnDeactivated() { }

        internal void NotifyDeactivated() => OnDeactivated();
    }
}