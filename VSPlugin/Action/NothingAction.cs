namespace Daxs
{
    internal class NothingAction : IAction
    {
        public NothingAction() { }

        public string HUD_Name => "Nothing";

        public void Execute()
        {
        }
    }
}