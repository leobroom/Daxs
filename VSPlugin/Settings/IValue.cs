namespace Daxs.Settings
{
    public interface IValue
    {
        string Name { get; }

        string ToolTip { get; }

        object ObjectValue { get;}
        void Reset();
    }
}