namespace Daxs
{
    interface IAction
    {
        InputX Input { get; }

        string HUD_Text { get; }

        void Execute();
    }

    /// <summary>
    /// Does the Action has to be calculated
    /// </summary>
    interface ICalculate : IAction
    {
        void Calculate();
    }
}