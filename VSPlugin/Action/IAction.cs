namespace Daxs
{
    interface IAction
    {
        string HUD_Name { get; }

        void Execute();
    }
}