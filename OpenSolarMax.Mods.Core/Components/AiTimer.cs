namespace OpenSolarMax.Mods.Core.Components;

public struct AiTimer : ICountDownTimer
{
    public TimeSpan TimeLeft { get; set; }
}
