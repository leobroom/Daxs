using Rhino;
using Rhino.Collections;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.PlugIns;
using System;

namespace Daxs
{
    public class DaxPlugIn : Rhino.PlugIns.PlugIn
    {
        public DaxPlugIn()
        {
            Instance = this;
        }

        public override PlugInLoadTime LoadTime=> PlugInLoadTime.AtStartup; 

        ///<summary>Gets the only instance of the Dax plug-in.</summary>
        public static DaxPlugIn Instance { get; private set; }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {         
            RhinoApp.Initialized += OnRhinoInitialized;
            RhinoApp.RdkNewDocument += OnRhinoNewDocument;


            return base.OnLoad(ref errorMessage);
        }

        private void OnRhinoNewDocument(object sender, EventArgs e)
        {
            RhinoApp.WriteLine("OnRhinoNewDocument");



            RhinoDoc.ActiveDoc.Strings.SetString("Preferences", "Unit", "SI");
        }




        /// <summary>
        /// Starts Dax after Rhino has fully initialized, when autostart is enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRhinoInitialized(object sender, EventArgs e)
        {
                RhinoApp.Initialized -= OnRhinoInitialized; // Run only once

                bool autostartActive = ((BooleanValue)Daxs.Settings.Instance["AutoStart"]).Value;

                if (autostartActive && ControllerManager.Instance.State == DaxStatus.NotInitialized)
                   ControllerManager.Instance.Toggle();
        }



        //Loading from Document

        protected override bool ShouldCallWriteDocument(FileWriteOptions options)
        {
            return true;
        }


        //Versioning
        private const int Major = 0, Minor = 0;

        /// <summary>
        /// Called when Rhino is saving a .3dm file to allow the plug-in to save document user data.
        /// </summary>
        protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
        {
            RhinoApp.InvokeOnUiThread((Action)(() =>
            {   
                RhinoApp.WriteLine($"WriteDocument...");   
            }));

            archive.Write3dmChunkVersion(Major, Minor);

            var dict = new ArchivableDictionary();
            dict.Set("DaxNavMeshGuid", Daxs.Settings.Instance.NavMeshId);

            archive.WriteDictionary(dict);
        }

        /// <summary>
        /// Called whenever a Rhino document is being loaded and plug-in user data was
        /// encountered written by a plug-in with this plug-in's GUID.
        /// </summary>
        protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
        {
            archive.Read3dmChunkVersion(out int major, out int minor);

            var dict = archive.ReadDictionary();

            try
            {
                Guid navMeshId = dict.GetGuid("DaxNavMeshGuid");
                Mesh mesh = GetMeshById(navMeshId);

                if (mesh == null)
                {
                    LayoutManager.Instance.SetCollisionMesh(null, Guid.Empty);
                    return;
                }

                LayoutManager.Instance.SetCollisionMesh(mesh, navMeshId);

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    RhinoApp.WriteLine($"DAXS Navigationmesh found in file! Loaded: {navMeshId}");
                }));
            }
            catch (Exception)
            {
                LayoutManager.Instance.SetCollisionMesh(null, Guid.Empty);
            }
         




  
        }

        public static Mesh GetMeshById(Guid id)
        {
            var rhObj = RhinoDoc.ActiveDoc.Objects.Find(id);
            if (rhObj == null)
                return null;

            return rhObj.Geometry as Mesh;   // or DuplicateMesh()
        }
    }
}