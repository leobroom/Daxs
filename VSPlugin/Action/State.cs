using System;

namespace Daxs
{
    internal class State : IState
    {
		private object val;

		public object Value
		{
			get { return val; }
            set { val = value; }
        }

        private string hud_Name;
        public string HUD_Name => hud_Name;

        public AProperty Name => name;

        private GButton button;
        public GButton Button => button;

        private AProperty name;

        public InputX Input { get; }

        public State(GButton button, InputX input, AProperty name, string hud_Name, object val)
        {
            this.name = name;
            this.hud_Name = hud_Name;
            this.button = button;
            this.Input = input;
            this.val =  val;
        }

        public State(ActionBindingDto dto, object[] args) : this(dto.Button, dto.Input, dto.Property, (string)args[1], args[0]) { }

        public object[] GetArgs() => new object[] { val, hud_Name };
    }
}