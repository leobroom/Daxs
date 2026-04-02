namespace Daxs.GUI
{
    internal sealed class SetGamepadOverlayCommand : OverlayCommand<GamepadOverlayElement>
    {
        private readonly GamepadOverlayState _state;

        public SetGamepadOverlayCommand(GamepadOverlayState state)
            : base(OverlayIds.GamepadOverlay)
        {
            _state = state?.Clone() ?? new GamepadOverlayState();
        }

        protected override void ApplyTo(HUD hud, GamepadOverlayElement element)
        {
            element.SetState(_state);
            element.Show();
            hud.EnsureEnabledUiThread();
        }
    }

    internal sealed class HideGamepadOverlayCommand : OverlayCommand<GamepadOverlayElement>
    {
        public HideGamepadOverlayCommand()
            : base(OverlayIds.GamepadOverlay)
        {
        }

        protected override void ApplyTo(HUD hud, GamepadOverlayElement element)
        {
            element.Hide();
        }
    }
}