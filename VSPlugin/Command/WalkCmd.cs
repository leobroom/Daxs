using Rhino;
using Rhino.Commands;

namespace Daxs
{
    public class WalkCmd : Command
    {
        public WalkCmd()=> Instance = this;
        
        public static WalkCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_Walk";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            LayoutManager.Instance.Set("Walk");
            return Result.Success;
        }
    }
}