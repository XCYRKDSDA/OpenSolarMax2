using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Game.Fixtures;

/// <summary>
/// 测试进程内共享的图形设备夹具：创建一个 <see cref="GraphicsDevice"/>，提供给所有测试用例使用，
/// 测试结束时统一释放。对设备的一切操作都必须通过 <see cref="Run(Action)"/> 提交执行。
/// </summary>
/// <remarks>
/// 使用本夹具的测试工程必须把它注册为程序集夹具，并关闭用例并行：
/// <c>[assembly: AssemblyFixture(typeof(GraphicsDeviceFixture))]</c>、
/// <c>[assembly: Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]</c>。
/// </remarks>
public sealed class GraphicsDeviceFixture : IDisposable
{
    // 构造 GraphicsDeviceManager 需要 Game 实例，创建设备所依赖的 SdlGameWindow.Instance
    // 也只在 Game 创建窗口的流程中赋值，因此用一个空的 Game 子类来满足这个要求。
    private sealed class HostGame : XNAGame;

    private readonly XNAGame _game = new HostGame();
    private readonly BlockingCollection<Action> _workQueue = new(new ConcurrentQueue<Action>());
    private readonly ManualResetEventSlim _initialized = new(false);
    private readonly Thread _thread;
    private Exception? _initializationFailure;
    private GraphicsDevice? _graphicsDevice;

    /// <summary>
    /// 共享的图形设备。引用可在任意线程读取，
    /// 但对设备的一切操作必须放在 <see cref="Run(Action)"/> 提交的工作里执行。
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// 提供给需要 Game 引用的组件（如 MusicManager）构造使用的 Game 实例。
    /// 构造组件本身不涉及图形 API，可在任意线程进行。
    /// </summary>
    public XNAGame Game => _game;

    private void RunLoop()
    {
        // OpenGL 上下文只在创建它的线程上有效，跨线程调用渲染 API 是未定义行为，
        // 因此设备在本专用线程上创建，后续设备操作也通过 Run 提交到这里执行。
        try
        {
            // 通过 Game + GraphicsDeviceManager.ApplyChanges 创建设备而不调用 Run：
            // MonoGame 创建 SDL 窗口时带有隐藏标志，只有 Run 才显示窗口，所以全程没有可见窗口。
            // 不能绕过 Game 直接构造 GraphicsDevice：DesktopGL 下 GraphicsAdapter.CurrentDisplayMode
            // 会读取 SdlGameWindow.Instance.Handle，直接构造会抛出空引用异常（MonoGame issue #5089）。
            var gdm = new GraphicsDeviceManager(_game) { GraphicsProfile = GraphicsProfile.HiDef };
            gdm.ApplyChanges();
            _graphicsDevice = gdm.GraphicsDevice;
        }
        catch (Exception ex)
        {
            _initializationFailure = ex;
        }
        finally
        {
            _initialized.Set();
        }

        foreach (var work in _workQueue.GetConsumingEnumerable())
        {
            // 工作项内部（Run）已捕获全部异常，这里不会抛出而中断循环
            work();
        }

        // Game.Dispose 会一并释放 GraphicsDeviceManager，后者随之释放 GraphicsDevice
        _game.Dispose();
    }

    public GraphicsDeviceFixture()
    {
        _thread = new Thread(RunLoop) { Name = "GraphicsDeviceFixture", IsBackground = true };
        _thread.Start();
        _initialized.Wait();

        if (_initializationFailure is not null)
        {
            _workQueue.CompleteAdding();
            throw new InvalidOperationException(
                "GraphicsDevice 创建失败。设备创建需要下列环境之一："
                    + "桌面会话（X11/Wayland）；无显示服务器但 /dev/dri 可访问（SDL 走 KMSDRM/offscreen 后端）；"
                    + "或用 xvfb-run 启动测试进程提供虚拟显示。",
                _initializationFailure
            );
        }

        GraphicsDevice = _graphicsDevice!;
    }

    /// <summary>
    /// 在夹具专用线程上执行 <paramref name="action"/> 并等待完成；
    /// 工作中的异常会在调用线程上按原来的类型和堆栈重新抛出。
    /// </summary>
    public void Run(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        using var done = new ManualResetEventSlim();
        _workQueue.Add(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                done.Set();
            }
        });
        done.Wait();
        failure?.Throw();
    }

    /// <summary>
    /// 在夹具专用线程上执行 <paramref name="func"/> 并返回结果；
    /// 工作中的异常会在调用线程上按原来的类型和堆栈重新抛出。
    /// </summary>
    public T Run<T>(Func<T> func)
    {
        T result = default!;
        // 必须写成语句块 lambda：() => result = func() 是带有返回值的表达式 lambda，
        // 可转换为 Func<T>，重载解析会把它绑定回 Run<T> 自身，造成无限递归。
        Run(() =>
        {
            result = func();
        });
        return result;
    }

    public void Dispose()
    {
        // CompleteAdding 之后 RunLoop 不再取出工作项，循环结束后 RunLoop 释放 Game 与设备；
        // Join 等待该线程完成，再释放其余资源。
        _workQueue.CompleteAdding();
        _thread.Join();
        _workQueue.Dispose();
        _initialized.Dispose();
    }
}
