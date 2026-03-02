using Daxs.Layout;
using Rhino;
using Rhino.Commands;

namespace Daxs.Commands
{
    public class WalkCmd : Command
    {
        public WalkCmd()=> Instance = this;
        
        public static WalkCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_Walk";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            LayoutSystem.Instance.Set(LayoutType.Walk);
            return Result.Success;
        }
    }
}