using Microsoft.Xna.Framework;
using Nine.Screens;
using OpenSolarMax.Game.Screens.ViewModels;

namespace OpenSolarMax.Game.Screens.Views;

internal abstract class ViewBase<T>(T viewModel, SolarMax game) : IScreen
    where T : IViewModel
{
    public SolarMax Game => game;

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
