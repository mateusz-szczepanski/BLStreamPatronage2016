using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patronat.Classes
{
    public class Properties : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _details;

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
