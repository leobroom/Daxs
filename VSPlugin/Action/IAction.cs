namespace Daxs
{
    interface IAction
    {
        InputX Input { get; }

        string HUD_Name { get; }

        void Execute();
    }
}