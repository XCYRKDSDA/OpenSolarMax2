namespace OpenSolarMax.Game;

public static partial class Content
{
    public static partial class Fonts
    {
        private static readonly string _base = "Fonts";

        public static string Default => $"{_base}/Downlink-gav1.ttf";
    }

    public static partial class UIs
    {
        private static readonly string _base = "UIs";

        public static class Icons
        {
            private static readonly string _base = $"{UIs._base}/IconsAtlas.json";

            public static string ExitBtn_Idle => $"{_base}:ExitBtn_Idle";

            public static string ExitBtn_Pressed => $"{_base}:ExitBtn_Pressed";

            public static string PauseBtn_Idle => $"{_base}:PauseBtn_Idle";

            public static string PauseBtn_Pressed => $"{_base}:PauseBtn_Pressed";

            public static string SlowSpeedBtn_Idle => $"{_base}:SlowSpeedBtn_Idle";

            public static string SlowSpeedBtn_Pressed => $"{_base}:SlowSpeedBtn_Pressed";

            public static string NormalSpeedBtn_Idle => $"{_base}:NormalSpeedBtn_Idle";

            public static string NormalSpeedBtn_Pressed => $"{_base}:NormalSpeedBtn_Pressed";

            public static string FastSpeedBtn_Idle => $"{_base}:FastSpeedBtn_Idle";

            public static string FastSpeedBtn_Pressed => $"{_base}:FastSpeedBtn_Pressed";

            public static string SliderBar => $"{_base}:SliderBar";

            public static string SliderBtn => $"{_base}:SliderBtn";
        }
    }
}
