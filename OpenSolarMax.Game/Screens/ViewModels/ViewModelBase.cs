using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Screens.ViewModels;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    public SolarMax Game { get; }

    protected ViewModelBase(SolarMax game)
    {
        Game = game;
    }

    public virtual void Update(GameTime gameTime) { }
}
