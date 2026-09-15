namespace OpenSolarMax.Mods.Common.Components;

/// <summary>
/// 倒计时组件接口
/// </summary>
public interface ICountDownTimer
{
    TimeSpan TimeLeft { get; set; }
}

/// <summary>
/// 正计时组件接口
/// </summary>
public interface ICountUpTimer
{
    TimeSpan TimeElapsed { get; set; }
}
