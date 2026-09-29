using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Tests.Game.Common;

/// <summary>
/// 测试进程内共享的图形设备夹具：创建一个 <see cref="GraphicsDevice"/>，
/// 供所有测试用例使用，测试结束时统一释放。
/// </summary>
/// <remarks>
/// <para>
/// 设备创建方式：构造 <see cref="Game"/> + <see cref="GraphicsDeviceManager"/>，
/// 调用 <see cref="GraphicsDeviceManager.ApplyChanges"/>，不调用 <c>Game.Run</c>。
/// MonoGame 创建底层 SDL 窗口时带隐藏标志，只有 <c>Run</c> 才会把窗口显示出来，
/// 因此全程不会出现可见窗口。
/// </para>
/// <para>
/// 不能绕过 <c>Game</c> 直接构造 <c>GraphicsDevice</c>：DesktopGL 平台上
/// <c>GraphicsAdapter.CurrentDisplayMode</c> 会读取 <c>SdlGameWindow.Instance.Handle</c>，
/// 而该实例只在 <c>GraphicsDeviceManager</c> 创建窗口的流程中产生，
/// 直接构造设备会抛空引用异常（MonoGame issue #5089）。
/// </para>
/// <para>
/// 线程模型：OpenGL 上下文只在创建它的线程上有效，跨线程调用渲染 API 是未定义行为。
/// 因此夹具自起一条专用线程，设备在其上创建，后续所有设备操作也必须在同一线程执行
/// （MonoGame 自己的测试套件同样把设备操作收拢到单线程）。
/// 调用方通过 <see cref="Run(Action)"/> / <see cref="Run{T}(Func{T})"/> 把工作提交到
/// 该线程，并阻塞等待完成。
/// </para>
/// <para>
/// 提交的工作虽然串行执行，但 <c>GraphicsDevice</c> 的状态（如当前 RenderTarget）是全局的：
/// 两个用例交错提交时，会看到彼此修改的状态。因此使用本夹具的测试工程应关闭用例并行。
/// </para>
/// <para>
/// 测试工程注册方式（xUnit v3）：<c>[assembly: AssemblyFixture(typeof(GraphicsDeviceFixture))]</c>，
/// 或让测试类实现 <c>IAssemblyFixture&lt;GraphicsDeviceFixture&gt;</c>。
/// </para>
/// </remarks>
public sealed class GraphicsDeviceFixture : IDisposable
{
    private readonly XNAGame _game = new HostGame();
    private readonly BlockingCollection<Action> _workQueue = new(new ConcurrentQueue<Action>());
    private readonly ManualResetEventSlim _initialized = new(false);
    private readonly Thread _thread;
    private Exception? _initializationFailure;

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
    /// 共享的图形设备。引用可在任意线程读取，
    /// 但对设备的一切操作必须放在 <see cref="Run(Action)"/> 提交的工作里执行。
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    private GraphicsDevice? _graphicsDevice;

    /// <summary>
    /// 在夹具专用线程上执行 <paramref name="action"/> 并等待完成；
    /// 工作中的异常会在调用线程上按原类型、原堆栈重新抛出。
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
    /// 工作中的异常会在调用线程上按原类型、原堆栈重新抛出。
    /// </summary>
    public T Run<T>(Func<T> func)
    {
        T result = default!;
        // 必须写成语句块 lambda：() => result = func() 是带值的表达式 lambda，
        // 可转换为 Func<T>，重载解析会把它绑定回 Run<T> 自身，造成无限递归。
        Run(() =>
        {
            result = func();
        });
        return result;
    }

    private void RunLoop()
    {
        try
        {
            // GraphicsDeviceManager、GraphicsDevice 及底层 GL 上下文必须创建在本线程上
            //（见类注释的线程模型）。Game 实例只充当宿主（见 HostGame），无线程要求。
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
            // 工作项内部（Run）已捕获全部异常，这里不会抛出而中断消费循环
            work();
        }

        // Game.Dispose 会释放注册在它身上的 GraphicsDeviceManager，后者随之释放 GraphicsDevice
        _game.Dispose();
    }

    public void Dispose()
    {
        _workQueue.CompleteAdding();
        _thread.Join();
        _workQueue.Dispose();
        _initialized.Dispose();
    }

    /// <summary>
    /// 空的 Game 子类，只充当 <see cref="GraphicsDeviceManager"/> 的宿主：
    /// 后者构造需要 Game 实例，且设备创建依赖的 <c>SdlGameWindow.Instance</c>
    /// 也只在其创建窗口的流程中赋值。永远不对它调用 <c>Run</c>，游戏循环不会启动。
    /// </summary>
    private sealed class HostGame : XNAGame;
}
