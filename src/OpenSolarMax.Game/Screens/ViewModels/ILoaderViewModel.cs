using System.ComponentModel;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal interface ILoaderViewModel
{
    float Progress { get; }

    bool LoadCompleted { get; }
}
