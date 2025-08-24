namespace Daxs
{
    internal class SwitchAction : IAction
    {
        public SwitchAction() {}
        public void Execute() 
        {
            var current = LayoutManager.Instance.CurrentLayout;
            
            string name = current.Name;
            if (name == "Fly")
                name = "Walk";
            else
                name = "Fly";

            LayoutManager.Instance.SetLayout(name);
        }
    }
}