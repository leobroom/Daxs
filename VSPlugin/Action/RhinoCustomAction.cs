using Rhino;

namespace Daxs
{
    internal class RhinoCustomAction :BaseState, IAction
    {
        private readonly string commandName;
        private readonly bool switchToMenuControll;

        public RhinoCustomAction(GButton Button, InputX Input, string commandName, bool switchToMenuControll) : base(AProperty.Custom,  Button,  Input)
        {
            this.commandName = commandName;
            this.switchToMenuControll = switchToMenuControll;
        }

        public RhinoCustomAction(GButton Button, InputX Input, object[] args) : this( Button,  Input, (string)args[0], (bool)args[1]) { }

        public string HUD_Name => commandName;

        public AProperty Name => AProperty.Custom;

        public void Execute()
        {
            if (switchToMenuControll)
                LayoutManager.Instance.Set(Layout.Menu);
            RhinoApp.RunScript(commandName, true);
            if (switchToMenuControll)
                LayoutManager.Instance.SetToPreviousLayout();
        }

        public override object[] GetArgs()=>new object[] { commandName, switchToMenuControll };
    }
}