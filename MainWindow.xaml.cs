using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Avalonia.Platform;
using Avalonia;

namespace KioskRun {
    public class MainWindow : Window {
        private TextBlock? _displayText;
        

        public MainWindow() {
            InitializeComponent();
            LoadLogo();
           
            _displayText = this.FindControl<TextBlock>("DisplayText") ?? throw new NullReferenceException("DisplayText control not found.");
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            _displayText = this.FindControl<TextBlock>("DisplayText");
        }

        public void UpdateDisplayText(string text) {
            if (_displayText != null) {
                _displayText.Text = text;
            }
        }
        
        private void LoadLogo() {
        var uri = new Uri("avares://KioskRun/Assets/logo.png");
        using (var stream = AssetLoader.Open(uri)) {
            if (stream != null) {
                var bitmap = new Bitmap(stream);
                var logoImage = this.FindControl<Image>("LogoImage");
                if (logoImage != null) {
                    logoImage.Source = bitmap;
                }
                else {
                    System.Diagnostics.Debug.WriteLine("LogoImage control not found!");
                }
            }
            else {
                System.Diagnostics.Debug.WriteLine("Embedded logo resource not found!");
            }
        }
    }


}
}
