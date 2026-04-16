namespace Daxs.Settings
{
    public interface IValue
    {
        string Name { get; }

        string ToolTip { get; }
        void Reset();
    }
}