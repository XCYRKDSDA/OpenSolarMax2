using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Nine.Assets;
using OpenSolarMax.Game.Utils;

namespace OpenSolarMax.Game.Audio;

/// <summary>
/// 一条正在播放的音乐。
/// 持有对应的 FMOD 事件实例，负责它自身的音量、淡入淡出、暂停、恢复与释放
/// </summary>
internal sealed class MusicChannel : IDisposable
{
    private bool _disposed;

    #region Fmod 身份与实例

    private readonly EventInstance _instance;

    private readonly SafeFmodEventDescription _music;

    /// <summary>
    /// 这条音乐是否就是给定的曲目，按事件描述的句柄比对；已释放时返回 false
    /// </summary>
    public bool Matches(SafeFmodEventDescription music)
    {
        if (_disposed)
            return false;

        FmodException.Check(_instance.getDescription(out var description));
        return description.handle == music.Native.handle;
    }

    /// <summary>
    /// 这条音乐对应的曲目
    /// </summary>
    public SafeFmodEventDescription Music => _music;

    #endregion

    #region 音量

    /// <summary>
    /// 整体音量，0 为静音，1 为满音量；与当前时刻的音量相乘后写入实例
    /// </summary>
    private float _globalVolume;

    /// <summary>
    /// 当前音量；过渡中每帧由过渡同步，静止时保持不变
    /// </summary>
    private float _gain;

    /// <summary>
    /// 把整体音量与当前时刻的音量相乘后写入 FMOD 实例
    /// </summary>
    private void ApplyGain() => FmodException.Check(_instance.setVolume(_gain * _globalVolume));

    /// <summary>
    /// 当前时刻的音量；过渡中取过渡的插值，静止时取保持的音量<br/>
    /// 写入会把值夹到 0 到 1 之间、终止进行中的过渡并立即写入实例，
    /// 过渡携带的收尾动作（如淡出后暂停）随之中止
    /// </summary>
    public float Gain
    {
        get => _gain;
        set
        {
            if (_disposed)
                return;
            _gain = MathHelper.Clamp(value, 0f, 1f);
            _transition = null;
            ApplyGain();
        }
    }

    public float GlobalVolume
    {
        get => _globalVolume;
        set
        {
            if (_disposed)
                return;
            _globalVolume = MathHelper.Clamp(value, 0f, 1f);
            ApplyGain();
        }
    }

    #endregion

    #region 过渡

    /// <summary>
    /// 正在进行的音量过渡；为 null 表示当前不在过渡中
    /// </summary>
    private MusicTransition? _transition;

    /// <summary>
    /// 从给定起始音量开始一段过渡，并立即把新的音量写入实例
    /// </summary>
    private void StartTransition(
        float fromGain,
        float toGain,
        TimeSpan duration,
        bool pauseAfterFade
    )
    {
        _transition = new MusicTransition
        {
            FromGain = fromGain,
            ToGain = toGain,
            Duration = duration,
            PauseAfterFade = pauseAfterFade,
        };
        _gain = _transition.Gain;
        ApplyGain();
    }

    /// <summary>
    /// 结束当前过渡：把音量停在目标值，必要时暂停实例
    /// </summary>
    private void FinishTransition(MusicTransition transition)
    {
        _gain = transition.ToGain;
        _transition = null;
        ApplyGain();

        if (transition.PauseAfterFade)
            FmodException.Check(_instance.setPaused(true));
    }

    #endregion

    #region 播放控制

    /// <summary>
    /// 当前播放状态；已释放时返回 Stopped
    /// </summary>
    public MediaState State
    {
        get
        {
            if (_disposed)
                return MediaState.Stopped;
            FmodException.Check(_instance.getPaused(out var paused));
            return paused ? MediaState.Paused : MediaState.Playing;
        }
    }

    /// <summary>
    /// 让音量在给定时长内变为目标音量；结束时不暂停
    /// </summary>
    /// <param name="targetGain">目标音量，0 为静音，1 为满音量</param>
    /// <param name="duration">过渡时长；为 0 时立即变为目标音量</param>
    public void FadeTo(float targetGain, TimeSpan duration)
    {
        if (_disposed)
            return;

        StartTransition(_gain, targetGain, duration, pauseAfterFade: false);
    }

    /// <summary>
    /// 暂停播放。过渡时长为 0 时立即暂停；大于 0 时先把音量逐渐降低到 0，再暂停实例
    /// </summary>
    public void Pause(TimeSpan duration)
    {
        if (_disposed)
            return;

        if (duration <= TimeSpan.Zero)
        {
            FmodException.Check(_instance.setPaused(true));
            return;
        }

        StartTransition(_gain, 0f, duration, pauseAfterFade: true);
    }

    /// <summary>
    /// 恢复播放。过渡时长为 0 时立即回到满音量；大于 0 时从静音逐渐升高到满音量
    /// </summary>
    public void Resume(TimeSpan duration)
    {
        if (_disposed)
            return;

        FmodException.Check(_instance.setPaused(false));

        if (duration <= TimeSpan.Zero)
        {
            _gain = 1f;
            _transition = null;
            ApplyGain();
            return;
        }

        StartTransition(0f, 1f, duration, pauseAfterFade: false);
    }

    /// <summary>
    /// 每帧推进音量变化并更新实例音量；变化结束后，若标记了暂停则暂停实例
    /// </summary>
    public void Tick(TimeSpan delta)
    {
        if (_disposed || _transition is not { } transition)
            return;

        if (transition.Completed)
        {
            FinishTransition(transition);
            return;
        }

        transition.Elapsed += delta;
        _gain = transition.Gain;
        ApplyGain();

        if (transition.Completed)
            FinishTransition(transition);
    }

    #endregion

    #region 创建与释放

    private MusicChannel(EventInstance instance, SafeFmodEventDescription music, float globalVolume)
    {
        _instance = instance;
        _music = music;
        _globalVolume = globalVolume;
    }

    /// <summary>
    /// 按事件描述创建一条音乐，并以 0 音量开始播放
    /// </summary>
    /// <param name="music">要播放的曲目</param>
    /// <param name="globalVolume">整体音量</param>
    public static MusicChannel Create(SafeFmodEventDescription music, float globalVolume)
    {
        FmodException.Check(music.Native.createInstance(out var instance));
        FmodException.Check(instance.setVolume(0f));
        FmodException.Check(instance.start());
        return new MusicChannel(instance, music, globalVolume);
    }

    /// <summary>
    /// 停止并释放实例；来自终结器时忽略停止与释放的错误
    /// </summary>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        var stopResult = _instance.stop(STOP_MODE.IMMEDIATE);
        var releaseResult = _instance.release();
        if (disposing)
        {
            FmodException.Check(stopResult);
            FmodException.Check(releaseResult);
        }
    }

    /// <summary>
    /// 停止并释放实例
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 没人调用 Dispose 时兜底释放；终结器线程上不能抛异常
    /// </summary>
    ~MusicChannel()
    {
        Dispose(false);
    }

    #endregion
}
