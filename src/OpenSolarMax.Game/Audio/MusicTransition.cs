namespace OpenSolarMax.Game.Audio;

/// <summary>
/// 一次音量过渡的状态：起止音量、时长、已经过的时间，以及结束后是否暂停实例
/// </summary>
internal sealed class MusicTransition
{
    /// <summary>
    /// 过渡的起始音量
    /// </summary>
    public float FromGain { get; set; }

    /// <summary>
    /// 过渡的目标音量
    /// </summary>
    public float ToGain { get; set; }

    /// <summary>
    /// 过渡的总时长；不大于 0 时表示立即到达目标音量
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 已经过的时间
    /// </summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// 过渡结束后是否暂停实例
    /// </summary>
    public bool PauseAfterFade { get; set; }

    /// <summary>
    /// 已进行的时间占总时长的比例，夹在 0 至 1 之间；总时长不大于 0 时，比例视为 1
    /// </summary>
    public float Progress =>
        Duration <= TimeSpan.Zero ? 1f : Math.Clamp((float)(Elapsed / Duration), 0f, 1f);

    /// <summary>
    /// 过渡是否已经完成
    /// </summary>
    public bool Completed => Progress >= 1f;

    /// <summary>
    /// 当前时刻的音量，按等功率曲线从起始音量走向目标音量
    /// </summary>
    public float Gain
    {
        get
        {
            var progress = Progress;
            var shaped = ToGain >= FromGain ? MathF.Sqrt(progress) : 1f - MathF.Sqrt(1f - progress);
            return FromGain + (ToGain - FromGain) * shaped;
        }
    }
}
