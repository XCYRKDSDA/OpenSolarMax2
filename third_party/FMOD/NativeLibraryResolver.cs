using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FmodStudioSystem = FMOD.Studio.System;

namespace FMOD;

/// <summary>
/// MSBuild 拷贝文件时无法保留符号链接，因此导致产物目录里会有三份一模一样的三个文件，如 libfmod.so libfmod.so.14 libfmod.so.14.14。
/// 于是通过 DllImport("fmod") 加载的 libfmod.so 和通过 DllImport("fmodstudio") 间接加载的 libfmod.so.14 发生了冲突。
/// 因此库中只保留各个动态库的 SONAME 版本，以允许通过系统 ldd 加载时能够互相正确加载到。
/// 但是 Windows 上没有 SONAME 机制，且库名和 Unix/Linux 下不同，因此不能直接写 DllImport("libfmod.so.14")。
/// 而 DllImport 特性的参数又要求是编译期常量，因此只能专门实现该 Resolver 解决动态库查找问题。
/// </summary>
internal static class NativeLibraryResolver
{
    private static readonly string _nativeDirectory = FindNativeDirectory();

    [ModuleInitializer]
    internal static void Initialize() =>
        NativeLibrary.SetDllImportResolver(typeof(FmodStudioSystem).Assembly, Resolve);

    private static IntPtr Resolve(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath
    )
    {
        if (_nativeDirectory is null || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return IntPtr.Zero;

        // 名字中的版本号需与仓库内 FMOD 动态库的 SONAME 一致
        var fileName = libraryName switch
        {
            "fmod" => "libfmod.so.14",
            "fmodL" => "libfmodL.so.14",
            "fmodstudio" => "libfmodstudio.so.14",
            "fmodstudioL" => "libfmodstudioL.so.14",
            _ => null,
        };
        if (fileName is null)
            return IntPtr.Zero;

        var file = Path.Combine(_nativeDirectory, fileName);
        return File.Exists(file) ? NativeLibrary.Load(file) : IntPtr.Zero;
    }

    /// <summary>原生库所在目录：产物里 RID 目录下的 native，或平铺到根目录的场景。</summary>
    private static string FindNativeDirectory()
    {
        var runtimes = Path.Combine(AppContext.BaseDirectory, "runtimes");
        IEnumerable<string> candidates = Directory.Exists(runtimes)
            ? Directory.EnumerateDirectories(runtimes).Select(d => Path.Combine(d, "native"))
            : [];

        return candidates
            .Append(AppContext.BaseDirectory)
            .Where(Directory.Exists)
            .FirstOrDefault(d => Directory.EnumerateFiles(d, "libfmod*.so*").Any());
    }
}
