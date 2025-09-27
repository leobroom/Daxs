namespace Daxs
{
    interface IAction
    {
        string HUD_Name { get; }

        //Used for serialization
        AProperty Name { get; }

        void Execute();

        object[] GetArgs();

        //void ImportArgs(ArgDto args);
    }
}