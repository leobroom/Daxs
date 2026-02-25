using Rhino;
using System;

namespace Daxs
{
    internal class RhinoCustomAction : BaseState, IAction
    {
        private string _function;
        private string _name;
        private bool _simulateKeys;
        private readonly Settings _settings = Settings.Instance;

        public RhinoCustomAction(InputX Input, GAction cNumber) : base(Input)
        {
            if (cNumber < GAction.C1 || cNumber > GAction.C6)
                throw new ArgumentOutOfRangeException(nameof(cNumber), "Invalid ActionEnum type");

            string c = cNumber.ToString();

            _function = _settings.BindText($"{c}_Function", v => _function = v);
            _name = _settings.BindText($"{c}_Name", v => _name = v);
            _simulateKeys = _settings.BindBoolean($"{c}_SimulateKeys", v => _simulateKeys = v);
        }
     
        public override string HUD_Text => _name;

        public override void Execute()
        {
            if (_simulateKeys)
                LayoutSystem.Instance.Set(Layout.Menu);
            RhinoApp.RunScript(_function, true);
            if (_simulateKeys && LayoutSystem.Instance.Current.Name == Layout.Menu)
                LayoutSystem.Instance.SetToPreviousLayout();

            //Fallback to Fly layout if still in Menu
            if (LayoutSystem.Instance.Current.Name == Layout.Menu)
                LayoutSystem.Instance.Set(Layout.Fly);
        }
    }
}