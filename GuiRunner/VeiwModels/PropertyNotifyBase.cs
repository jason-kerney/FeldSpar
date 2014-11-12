using System.ComponentModel;
using System.Runtime.CompilerServices;
using FeldSparGuiCSharp.Annotations;

namespace FeldSparGuiCSharp.VeiwModels
{
    public class PropertyNotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}