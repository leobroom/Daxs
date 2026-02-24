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
            LayoutSystem.Instance.Set(Layout.Fly);
            return Result.Success;
        }
    }
}