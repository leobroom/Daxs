using Rhino;
using Rhino.Collections;
using Rhino.FileIO;
using Rhino.PlugIns;
using System;

namespace Daxs
{
    public class DaxPlugIn : PlugIn
    {
        public DaxPlugIn(){Instance = this;}

        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

        public static DaxPlugIn Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            RhinoApp.Initialized += OnRhinoInitialized;

            return base.OnLoad(ref errorMessage);
        }

        /// <summary>
        /// Starts Dax after Rhino has fully initialized, when autostart is enabled.
        /// </summary>
        private void OnRhinoInitialized(object sender, EventArgs e)
        {
            RhinoApp.Initialized -= OnRhinoInitialized; // Run only once

            bool autostartActive = ((BooleanValue)Daxs.Settings.Instance["AutoStart"]).Value;
            if (autostartActive && ControllerManager.Instance.State == DaxStatus.NotInitialized)
                ControllerManager.Instance.Toggle();
        }

        protected override bool ShouldCallWriteDocument(FileWriteOptions options) => true;

        //Versioning -> check  archive.Write3dmChunkVersion(Major, Minor);
        private const int Major = 0, Minor = 0;

        /// <summary>
        /// Called when Rhino is saving a .3dm file to allow the plug-in to save document user data.
        /// </summary>
        protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
        {
            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                RhinoApp.WriteLine($"Write Dax data into Document...");
            }));

            archive.Write3dmChunkVersion(Major, Minor);

            var dict = new ArchivableDictionary();
            dict.Set("DaxNavMeshGuid", NavigationManager.Instance.NavMeshId);

            archive.WriteDictionary(dict);
        }

        /// <summary>
        /// Called whenever a Rhino document is being loaded and plug-in user data was
        /// encountered written by a plug-in with this plug-in's GUID.
        /// </summary>
        protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
        {
            archive.Read3dmChunkVersion(out int major, out int minor); //no versioning yet
            var dict = archive.ReadDictionary();

            try
            {
                Guid navMeshId = dict.GetGuid("DaxNavMeshGuid");

                bool isSuccess = NavigationManager.Instance.SetMeshById(navMeshId);
                if (!isSuccess)
                    return;

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    RhinoApp.WriteLine($"DAXS Navigationmesh found in file! Loaded: {navMeshId}");
                }));
            }
            catch (Exception)
            {
                NavigationManager.Instance.SetMeshById(Guid.Empty);
            }
        }
    }
}