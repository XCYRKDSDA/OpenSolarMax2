using System.ComponentModel;

namespace OpenSolarMax.Game.Screens.ViewModels;

public interface ILoaderViewModel
{
    float Progress { get; }

    bool LoadCompleted { get; }
}
