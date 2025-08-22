using Rhino;
using Rhino.Commands;

namespace Daxs
{
    public class DaxsStartStopCmd : Command
    {
        public DaxsStartStopCmd() { Instance = this; }

        public static DaxsStartStopCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_StartStop";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ControllerManager.Instance.Toggle();
            return Result.Success;
        }
    }
}