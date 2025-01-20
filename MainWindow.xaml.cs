using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;

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
            // Ensure the logo file exists
            string logoPath = "./logo.png"; // Path relative to the executable
            if (System.IO.File.Exists(logoPath)) {
                var bitmap = new Bitmap(logoPath);
                var logoImage = this.FindControl<Image>("LogoImage");
                if (logoImage != null) {
                    logoImage.Source = bitmap;
                }
            }
            else {
                // Handle case when the logo file is not found
                System.Diagnostics.Debug.WriteLine("Logo file not found!");
            }
        }
    }
}
