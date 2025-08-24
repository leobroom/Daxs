using Rhino;

namespace Daxs
{
    internal class RhinoCmdAction : IAction
    {
        private readonly string commandName;
        private readonly bool switchToMenuControll;

        public RhinoCmdAction(string commandName, bool switchToMenuControll)
        {
            this.commandName = commandName;
            this.switchToMenuControll = switchToMenuControll;
        }

        public void Execute()
        {
            if (switchToMenuControll)
                LayoutManager.Instance.SetLayout("Menu");
            RhinoApp.RunScript(commandName, true);
            if (switchToMenuControll)
                LayoutManager.Instance.SetToPreviousLayout();
        }
    }
}