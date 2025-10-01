using System.Reflection;
using System.Runtime.Loader;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal class ModLoadContext(FileEntry file, IReadOnlyDictionary<string, Assembly> sharedAssemblies)
    : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new(file.FileSystem.ConvertPathToInternal(file.Path));

    private readonly IReadOnlyDictionary<string, Assembly> _sharedAssemblies = sharedAssemblies;

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        if (_sharedAssemblies.TryGetValue(assemblyName.FullName, out var assembly))
            return assembly;

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
            return LoadUnmanagedDllFromPath(libraryPath);

        return IntPtr.Zero;
    }
}
