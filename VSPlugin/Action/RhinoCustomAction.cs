using Rhino;

namespace Daxs
{
    internal class RhinoCustomAction : IAction
    {
        private readonly string commandName;
        private readonly bool switchToMenuControll;

        public RhinoCustomAction(string commandName, bool switchToMenuControll)
        {
            this.commandName = commandName;
            this.switchToMenuControll = switchToMenuControll;
        }

        public RhinoCustomAction(object[] args) : this((string)args[0], (bool)args[1]) { }

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

        public object[] GetArgs()
        {
            return new object[] { commandName, switchToMenuControll };
        }
    }
}