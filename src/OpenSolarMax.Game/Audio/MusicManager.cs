using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Nine.Assets;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Game.Audio;

/// <summary>
/// 全局背景音乐管理器。挂在 <see cref="SolarMax"/> 的组件列表上，随游戏的每帧更新。
/// 同一时刻只播放一条曲目；换曲时淡入淡出；提供暂停、恢复与整体音量控制。
/// </summary>
public sealed class MusicManager(XNAGame game) : GameComponent(game)
{
    #region 播放状态

    /// <summary>
    /// 当前正在播放的音乐。没有音乐在播时为 null
    /// </summary>
    private MusicChannel? _current;

    /// <summary>
    /// 正在淡出的旧音乐。音量降到 0 并暂停后，由 Update 释放并清空这个引用
    /// </summary>
    private MusicChannel? _fading;

    /// <summary>
    /// 当前播放状态。来自当前音乐的暂停标志，没有音乐在播时为 Stopped
    /// </summary>
    public MediaState State => _current?.State ?? MediaState.Stopped;

    /// <summary>
    /// 当前正在播放或已暂停的曲目；没有音乐时为 null
    /// </summary>
    public SafeFmodEventDescription? CurrentMusic => _current?.Music;

    #endregion

    #region 音量控制

    /// <summary>
    /// 整体音量，0 为静音，1 为满音量
    /// </summary>
    private float _volume = 1f;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = MathHelper.Clamp(value, 0f, 1f);

            if (_current is { })
                _current.GlobalVolume = _volume;
            if (_fading is { })
                _fading.GlobalVolume = _volume;
        }
    }

    #endregion

    #region 播放控制

    /// <summary>
    /// 播放指定曲目。<br/>
    /// - 当前正在播放同一曲目时，不做任何处理；<br/>
    /// - 当前没有播放曲目时，从静音逐渐升高音量进入；<br/>
    /// - 当前正在播放不同曲目时，进行交叉淡化过渡。<br/>
    /// 暂停状态下调用时，丢弃暂停中的音乐，从静音重新进入。
    /// </summary>
    /// <param name="music">目标曲目的事件描述</param>
    /// <param name="transition">过渡时长；为 0 时立即生效，不进行过渡</param>
    public void Play(SafeFmodEventDescription music, TimeSpan transition = default)
    {
        // 同一曲目正在播放，不做任何处理
        if (State == MediaState.Playing && _current is { } playing && playing.Matches(music))
            return;

        // 暂停状态下调用：释放暂停中的音乐
        if (State == MediaState.Paused)
        {
            _current?.Dispose();
            _current = null;
        }

        // 当前音乐转入淡出，为新的曲目让出位置
        if (_current is { } current)
        {
            _fading?.Dispose();
            current.Pause(transition);
            _fading = current;
            _current = null;
        }

        // 配置新曲目
        var channel = MusicChannel.Create(music, _volume);
        channel.FadeTo(1f, transition);
        _current = channel;
    }

    /// <summary>
    /// 暂停播放。过渡时长大于 0 时，先把音量逐渐降低到 0，然后暂停实例的时间轴
    /// </summary>
    public void Pause(TimeSpan transition = default)
    {
        if (State != MediaState.Playing)
            return;

        // 正在淡出的旧音乐不参与暂停，直接释放
        _fading?.Dispose();
        _fading = null;

        _current!.Pause(transition);
    }

    /// <summary>
    /// 恢复播放。过渡时长大于 0 时，从静音逐渐升高回满音量
    /// </summary>
    public void Resume(TimeSpan transition = default)
    {
        if (State != MediaState.Paused)
            return;

        _current!.Resume(transition);
    }

    /// <summary>
    /// 停止播放。过渡时长大于 0 时，先逐渐降低音量，然后停止并释放
    /// </summary>
    public void Stop(TimeSpan transition = default)
    {
        if (_current is { } current)
        {
            _fading?.Dispose();
            current.Pause(transition);
            _fading = current;
            _current = null;
            return;
        }

        // 此时只有正在淡出的音乐：按新的过渡时长重新开始淡出
        _fading?.Pause(transition);
    }

    /// <summary>
    /// 逐帧推进两条音乐的音量变化；正在淡出的音乐暂停后，释放它并清空引用
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        var delta = gameTime.ElapsedGameTime;

        _current?.Tick(delta);
        _fading?.Tick(delta);

        if (_fading is { State: MediaState.Paused })
        {
            _fading.Dispose();
            _fading = null;
        }
    }

    #endregion

    #region 资源释放

    private bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        if (disposing)
        {
            _current?.Dispose();
            _current = null;
            _fading?.Dispose();
            _fading = null;
        }

        base.Dispose(disposing);
    }

    #endregion
}
