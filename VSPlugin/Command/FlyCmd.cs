using Rhino;
using Rhino.Commands;

namespace Daxs
{
    public class FlyCmd : Command
    {
        public FlyCmd()=> Instance = this;

        public static FlyCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_Fly";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ControllerManager.Instance.SetLayout("Fly");
            return Result.Success;
        }
    }
}