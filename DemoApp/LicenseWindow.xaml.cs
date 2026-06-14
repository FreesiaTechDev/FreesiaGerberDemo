using FreesiaGerberLib;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace FreesiaGerberDemo
{
    public partial class LicenseWindow : Window
    {
        private static readonly Brush AvailableBrush = new SolidColorBrush(Color.FromRgb(26, 127, 55));

        private static readonly Brush UnavailableBrush = new SolidColorBrush(Color.FromRgb(207, 34, 46));

        public LicenseWindow()
        {
            InitializeComponent();
            UpdateLicenseInfo();
        }

        private void UpdateLicenseInfo()
        {
            GerberLicenseFeature Features = License.Features;

            SetStatusText(IsValidTextBlock, License.IsValid, "Valid", "Invalid");
            FeaturesTextBlock.Text = Features.ToString();
            SetFeatureText(GraphicRS274XTextBlock, GerberLicenseFeature.GraphicRS274X);
            SetFeatureText(GraphicODBTextBlock, GerberLicenseFeature.GraphicODB);
            SetFeatureText(VisualTreeRS274XTextBlock, GerberLicenseFeature.VisualTreeRS274X);
            SetFeatureText(VisualTreeODBTextBlock, GerberLicenseFeature.VisualTreeODB);
        }

        private void SetFeatureText(TextBlock TextBlock, GerberLicenseFeature Feature)
        {
            SetStatusText(TextBlock, License.HasFeature(Feature), "Available", "Unavailable");
        }

        private void SetStatusText(TextBlock TextBlock, bool IsAvailable, string AvailableText, string UnavailableText)
        {
            TextBlock.Text = IsAvailable ? AvailableText : UnavailableText;
            TextBlock.Foreground = IsAvailable ? AvailableBrush : UnavailableBrush;
        }

        private void OnEmailRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to open the default mail client. Please contact: freesiatech0308@gmail.com", "Email", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
