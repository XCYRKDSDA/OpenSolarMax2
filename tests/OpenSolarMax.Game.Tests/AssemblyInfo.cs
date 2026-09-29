using OpenSolarMax.Tests.Game.Common;
using Xunit;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(GraphicsDeviceFixture))]
// 共享图形设备的状态不隔离，且加载管道使用进程级资源，测试一律串行
[assembly: Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
