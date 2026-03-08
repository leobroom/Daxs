namespace Daxs.Settings
{
    /// <summary>
    /// Helper Class for different gamepads
    /// </summary>
    internal readonly struct GpResource
    {
        public GpResource(string imageName, params string[] filter)
        {
            Name = "Daxs.Shared." + imageName;
            Filters = filter;
        }

        public string Name { get; }
        public string[] Filters { get; }
    }
}