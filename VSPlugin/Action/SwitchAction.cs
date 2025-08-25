namespace Daxs
{
    internal class SwitchAction : IAction
    {
        public SwitchAction() {}
        public void Execute() 
        {
            var current = LayoutManager.Instance.CurrentLayout;

            Layout name = current.Name;
            if (name == Layout.Fly)
                name = Layout.Walk;
            else
                name = Layout.Fly;

            LayoutManager.Instance.Set(name);
        }
    }
}