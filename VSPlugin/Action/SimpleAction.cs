namespace Daxs
{
    internal class SimpleAction : IAction
    {
        private readonly string hudname;
        public SimpleAction(string hudname)
        {
            this.hudname = hudname;
        }

        public string HUD_Name => hudname;

        public void Execute()
        {
        }
    }
}