using System.ComponentModel;
using System.Runtime.CompilerServices;
using ViewModel.Annotations;

namespace ViewModel
{
    public class PropertyNotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}