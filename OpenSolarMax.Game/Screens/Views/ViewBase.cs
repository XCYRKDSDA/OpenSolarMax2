using Microsoft.Xna.Framework;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Views;

internal abstract class ViewBase(SolarMax game) : IScreen
{
    public SolarMax Game { get; } = game;

    public virtual void OnActivated() { }

    public virtual void OnDeactivated() { }

    public abstract void Update(GameTime gameTime);

    public abstract void Draw(GameTime gameTime);
}
