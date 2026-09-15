using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Components;

public struct AiTimer : ICountDownTimer
{
    public TimeSpan TimeLeft { get; set; }
}
