using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal interface IViewModel : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
{
    void Update(GameTime gameTime);
}
