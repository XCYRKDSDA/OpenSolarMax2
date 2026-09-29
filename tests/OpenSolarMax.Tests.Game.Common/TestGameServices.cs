using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game;
using FmodStudioSystem = FMOD.Studio.System;

namespace OpenSolarMax.Tests.Game.Common;

/// <summary>
/// <see cref="IGameServices"/> 的测试实现：设备来自测试夹具，
/// FMOD 使用 nosound 输出（不占用音频设备），目录由测试注入。
/// </summary>
public sealed class TestGameServices : IGameServices, IDisposable
{
    public GraphicsDevice GraphicsDevice { get; }

    public FmodStudioSystem FmodSystem { get; }

    public IFolders Folders { get; }

    public TestGameServices(GraphicsDevice graphicsDevice, IFolders folders)
    {
        GraphicsDevice = graphicsDevice;
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
    }

    private static void Check(FMOD.RESULT result, string step)
    {
        if (result != FMOD.RESULT.OK)
            throw new InvalidOperationException($"FMOD 调用失败（{step}）：{result}");
    }

    public void Dispose() => FmodSystem.release();
}
