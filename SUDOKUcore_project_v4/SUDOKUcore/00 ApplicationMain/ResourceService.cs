using GNPXcore.Properties;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace GNPXcore{
    public class ResourceService: INotifyPropertyChanged{

        private static readonly ResourceService _current = new ResourceService();
        public static ResourceService Current{ get=>_current; }

        private readonly Resources _resources=new Resources();
        public Resources Resources => this._resources;
 
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName=null){
            this.PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(propertyName));
          //  var handler = this.PropertyChanged;
          //  if(handler!=null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ChangeCulture(string name){
            Resources.Culture = CultureInfo.GetCultureInfo(name);
            this.RaisePropertyChanged("Resources");
        }

        public string GetStringCul( string name ){
            return CultureInfo.GetCultureInfo(name).ToString();
        }
    }
}