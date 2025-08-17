// #! csharp
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using System.Linq;
using Daxs;

RhinoDoc doc = __rhino_doc__;

Mesh mesh = null;

// Try preselection
var selected = doc.Objects.GetSelectedObjects(false, false).Where(o => o.Geometry is Mesh).ToList();

if (selected.Count == 1)
{
    mesh = (selected[0].Geometry as Mesh)?.DuplicateMesh();
    RhinoApp.WriteLine("Preselected mesh used.");
}
else
{
    var gm = new GetObject();
    gm.SetCommandPrompt("Select a mesh");
    gm.GeometryFilter = ObjectType.Mesh;
    gm.DisablePreSelect();
    gm.SubObjectSelect = false;
    gm.Get();

    if (gm.CommandResult() != Result.Success)
        return;

    mesh = gm.Object(0).Mesh()?.DuplicateMesh();
    RhinoApp.WriteLine("Mesh was selected.");
}

//Set Mesh to
ControllerManager.Instance.SetCollisionMesh(mesh);