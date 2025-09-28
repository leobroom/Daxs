namespace Daxs
{
    interface IAction : IBase
    {
        string HUD_Name { get; }

        void Execute();
    }

    interface IState : IBase
    {
        //Used for serialization


        object Value { get; }
    }

    interface IBase
    {
        object[] GetArgs();

        AProperty Name { get; }

        GButton Button { get; }
        InputX Input { get; }
    }
}