namespace Daxs
{
    public readonly struct GamepadState
    {
        public readonly InputX A, B, X, Y, Start, Back, L1, L3, R1, R3, DPadUp, DPadDown, DPadLeft, DPadRight;
        public readonly float L2, R2;
        public readonly float LeftThumbX, LeftThumbY, RightThumbX, RightThumbY;

        public GamepadState(InputX A, InputX B, InputX X, InputX Y, InputX Start, InputX Back, InputX L1, float L2,
        InputX L3, InputX R1, float R2, InputX R3, InputX DPadUp, InputX DPadDown, InputX DPadLeft, InputX DPadRight,
        float LeftThumbX, float LeftThumbY, float RightThumbX, float RightThumbY)
        {
            this.A = A; this.B = B; this.X = X; this.Y = Y;
            this.Start = Start; this.Back = Back;
            this.L1 = L1; this.L2 = L2; this.L3 = L3;
            this.R1 = R1; this.R2 = R2; this.R3 = R3;
            this.DPadUp = DPadUp; this.DPadDown = DPadDown;
            this.DPadLeft = DPadLeft; this.DPadRight = DPadRight;
            this.LeftThumbX = LeftThumbX; this.LeftThumbY = LeftThumbY;
            this.RightThumbX = RightThumbX; this.RightThumbY = RightThumbY;
        }

        public override string ToString()
        {
            return $"Buttons: A={A}, B={B}, X={X}, Y={Y}, Start={Start}, Back={Back}\n" +
                $"L1={L1}, L3={L3}, R1={R1}, R3={R3}\n" +
                $"DPad: Up={DPadUp}, Down={DPadDown}, Left={DPadLeft}, Right={DPadRight}\n" +
                $"L2={L2:0.00}, R2={R2:0.00}\n" +
                $"Left Thumb: X={LeftThumbX:0.00}, Y={LeftThumbY:0.00}\n" +
                $"Right Thumb: X={RightThumbX:0.00}, Y={RightThumbY:0.00}";
        }
    }
}