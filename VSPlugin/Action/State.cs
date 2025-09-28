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

        public State(AProperty name, GButton button, InputX input, string hud_Name, object val)
        {
            this.name = name;
            this.hud_Name = hud_Name;
            this.val = val;
            this.button = button;
            Input = input;
        }

        public object[] GetArgs()
        {
           return new object[] { val , hud_Name };
        }
    }
}