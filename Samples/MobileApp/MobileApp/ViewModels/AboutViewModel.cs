using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MobileApp.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            Title = "关于";

            OpenWebCommand = new Command(() => Launcher.OpenAsync(new Uri("https://newlifex.com")));
        }

        public ICommand OpenWebCommand { get; }
    }
}