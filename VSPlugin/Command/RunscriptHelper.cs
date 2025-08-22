using Rhino;
using Rhino.Commands;
using System;

namespace Daxs
{
    /// <summary>
    /// RunScriptHelper helper class
    /// </summary>
    internal class RunScriptHelper : IDisposable
    {
        private readonly uint m_documentSerialNumber = 0;
        private EventHandler<CommandEventArgs> m_endCommand;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="documentSerialNumber">The document runtime serial number.</param>
        public RunScriptHelper(uint documentSerialNumber)
        {
            m_documentSerialNumber = documentSerialNumber;
            Command.EndCommand += m_endCommand = OnEndCommand;
        }

        /// <summary>
        /// Runs a Rhino command script
        /// </summary>
        /// <param name="script">Script to run.</param>
        /// <param name="echo">Echo script command prompts.</param>
        /// <returns></returns>
        public bool RunScript(string script, bool echo)
        {
            var rc = RhinoApp.RunScript(m_documentSerialNumber, script, echo);
            return rc;
        }

        /// <summary>
        /// The results of the last exectued RunScript.
        /// </summary>
        public Result CommandResult { get; private set; }

        /// <summary>
        /// Command.EndCommand event handler
        /// </summary>
        private void OnEndCommand(object sender, CommandEventArgs args)
        {
            CommandResult = args.CommandResult;
        }

        /// <summary>
        /// Actively disposes.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Passively disposes.
        /// </summary>
        ~RunScriptHelper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose of whatever here.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (null != m_endCommand)
            {
                Command.EndCommand -= OnEndCommand;
                m_endCommand = null;
            }
        }
    }
}
