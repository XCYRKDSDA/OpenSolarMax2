using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Screens.ViewModels;

public interface IViewModel : INotifyPropertyChanged, INotifyPropertyChanging
{
    void Update(GameTime gameTime);
}
