using Rhino;
using System;

namespace Daxs
{
    internal class RhinoCustomAction : BaseState, IAction
    {
        private string _function;
        private string name;
        private bool simulateKeys;
        private readonly Settings settings = Settings.Instance;

        public RhinoCustomAction(InputX Input, GAction cNumber) : base(Input)
        {
            if (cNumber < GAction.C1 || cNumber > GAction.C6)
                throw new ArgumentOutOfRangeException(nameof(cNumber), "Invalid ActionEnum type");

            string c = cNumber.ToString();

            _function = settings.BindText($"{c}_Function", v => _function = v);
            name = settings.BindText($"{c}_Name", v => name = v);
            simulateKeys = settings.BindBoolean($"{c}_SimulateKeys", v => simulateKeys = v);
        }
     
        public override string HUD_Text => name;

        public override void Execute()
        {
            if (simulateKeys)
                LayoutSystem.Instance.Set(Layout.Menu);
            RhinoApp.RunScript(_function, true);
            if (simulateKeys && LayoutSystem.Instance.Current.Name == Layout.Menu)
                LayoutSystem.Instance.SetToPreviousLayout();

            //Fallback to Fly layout if still in Menu
            if (LayoutSystem.Instance.Current.Name == Layout.Menu)
                LayoutSystem.Instance.Set(Layout.Fly);
        }
    }
}