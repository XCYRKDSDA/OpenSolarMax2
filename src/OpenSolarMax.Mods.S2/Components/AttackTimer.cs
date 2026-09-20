namespace OpenSolarMax.Mods.S2.Components;

public struct AttackTimer : ICountDownTimer
{
    public TimeSpan TimeLeft { get; set; }
}
