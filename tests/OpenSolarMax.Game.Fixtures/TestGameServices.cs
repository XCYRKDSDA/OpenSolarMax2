using Apos.Shapes;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game;
using OpenSolarMax.Game.Audio;
using FmodStudioSystem = FMOD.Studio.System;

namespace OpenSolarMax.Game.Fixtures;

/// <summary>
/// <see cref="IGameServices"/> 的测试实现：设备来自测试夹具，
/// FMOD 使用 nosound 输出，目录由测试注入。
/// </summary>
public sealed class TestGameServices : IGameServices, IDisposable
{
    private readonly GraphicsDeviceFixture _fixture;

    public GraphicsDevice GraphicsDevice { get; }

    public FmodStudioSystem FmodSystem { get; }

    public IFolders Folders { get; }

    public MusicManager Music { get; }

    public ShapeBatch ShapeBatch { get; }

    private static void Check(FMOD.RESULT result, string step)
    {
        if (result != FMOD.RESULT.OK)
            throw new InvalidOperationException($"FMOD 调用失败（{step}）：{result}");
    }

    public TestGameServices(GraphicsDeviceFixture fixture, IFolders folders)
    {
        _fixture = fixture;
        GraphicsDevice = fixture.GraphicsDevice;
        Folders = folders;

        Check(FmodStudioSystem.create(out var fmodSystem), "create");
        try
        {
            Check(
                fmodSystem.initialize(
                    512,
                    FMOD.Studio.INITFLAGS.NORMAL,
                    FMOD.INITFLAGS.NORMAL,
                    IntPtr.Zero
                ),
                "initialize"
            );
            // 实测：对 Studio 的核心系统在 initialize 之前调用 setOutput 返回 ERR_INVALID_HANDLE，
            // 故先初始化（默认输出），再切换为 nosound
            Check(fmodSystem.getCoreSystem(out var coreSystem), "getCoreSystem");
            Check(coreSystem.setOutput(FMOD.OUTPUTTYPE.NOSOUND), "setOutput(NOSOUND)");
        }
        catch
        {
            fmodSystem.release();
            throw;
        }

        FmodSystem = fmodSystem;

        // MusicManager 构造只记录 Game 引用，可在任意线程创建
        Music = new MusicManager(fixture.Game);
        // ShapeBatch 构造会创建图形资源，必须在夹具的设备线程上执行
        ShapeBatch = fixture.Run(() => new ShapeBatch(fixture.GraphicsDevice));
    }

    public void Dispose()
    {
        _fixture.Run(() => ShapeBatch.Dispose());
        Music.Dispose();
        FmodSystem.release();
    }
}
