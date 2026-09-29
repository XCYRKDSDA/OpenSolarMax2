using Microsoft.Xna.Framework;
using Nine.Screens;
using OpenSolarMax.Game.Screens.ViewModels;

namespace OpenSolarMax.Game.Screens.Views;

internal abstract class ViewBase<T>(T viewModel, IUiServices uiServices) : IScreen
    where T : IViewModel
{
    public IUiServices UiServices => uiServices;

    public T ViewModel => viewModel;

    public virtual void OnActivated() { }

    public virtual void OnDeactivated() { }

    public virtual void Update(GameTime gameTime)
    {
        viewModel.Update(gameTime);
    }

    public abstract void Draw(GameTime gameTime);

    public virtual void Dispose()
    {
        viewModel.Dispose();
    }
}
