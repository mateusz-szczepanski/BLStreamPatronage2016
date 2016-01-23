using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Patronat.Classes
{
    public class Properties : INotifyPropertyChanged
    {
        public ObservableCollection<CustomList> ThumbnailsList { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private string _details;

        public Properties()
        {
            ThumbnailsList = new ObservableCollection<CustomList>();
        }

        public string Details
        {
            get { return _details; }
            set
            {
                if (value != _details)
                {
                    _details = value;
                    OnPropertyChanged("Details");
                }
            }
        }


        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }

        }
    }
}
