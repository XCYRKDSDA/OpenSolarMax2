using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal abstract class ViewModelBase : ObservableObject, IViewModel
{
    public IUiServices UiServices { get; }

    protected ViewModelBase(IUiServices uiServices)
    {
        UiServices = uiServices;
    }

    public virtual void Update(GameTime gameTime) { }

    public virtual void Dispose() { }
}
