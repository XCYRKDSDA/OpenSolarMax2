namespace OpenSolarMax.Mods.Core.Components;

public struct AttackTimer : ICountDownTimer
{
    public TimeSpan TimeLeft { get; set; }
}
