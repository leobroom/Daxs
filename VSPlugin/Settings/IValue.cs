namespace Daxs.Settings
{
    public interface IValue
    {
        string Name { get; }
        void Reset();
    }
}