using System.Collections.Generic;

namespace Daxs
{
    public sealed class ActionBindingDto
    {
        public GButton Button { get; set; }
        public AProperty Property { get; set; }
        public InputX Input { get; set; }
        public List<ArgDto> Args { get; set; } = new();
    }

    // tiny tagged-union for args
    public sealed class ArgDto
    {
        public string Kind { get; set; } = ""; // "double", "bool", "string", "enum:InputX", "enum:InputY"
        public string Value { get; set; } = ""; // raw string storage (we'll parse by Kind)
    }
}