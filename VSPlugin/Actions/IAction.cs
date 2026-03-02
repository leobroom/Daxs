namespace Daxs.Actions
{
    interface IAction
    {
        InputX Input { get; }
        string HUD_Text { get; }
        string HUD_Emoji { get; }
        void Execute();
    }
}