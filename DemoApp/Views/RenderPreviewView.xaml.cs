using FreesiaAssembly;
using FreesiaAssembly.Media.Imaging;
using FreesiaGerberLib;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FreesiaGerberDemo.Views
{
    public partial class RenderPreviewView : Grid
    {
        public event Action<string> StatusChanged;
        public event Action<string> StatusDetailChanged;

        public static readonly DependencyProperty SelectedLayerProperty =
            DependencyProperty.Register(nameof(SelectedLayer), typeof(ILayer), typeof(RenderPreviewView), new PropertyMetadata(null, OnSelectedLayerChanged));
        private static void OnSelectedLayerChanged(DependencyObject Sender, DependencyPropertyChangedEventArgs e)
        {
            RenderPreviewView View = (RenderPreviewView)Sender;
            View.RenderImageViewer.ImageSource = null;
            View.StatusDetailChanged?.Invoke("");
            View.RoiLeftTextBox.Text = Bound.Empty.Left.ToString("0.###", CultureInfo.InvariantCulture);
            View.RoiTopTextBox.Text = Bound.Empty.Top.ToString("0.###", CultureInfo.InvariantCulture);
            View.RoiRightTextBox.Text = Bound.Empty.Right.ToString("0.###", CultureInfo.InvariantCulture);
            View.RoiBottomTextBox.Text = Bound.Empty.Bottom.ToString("0.###", CultureInfo.InvariantCulture);
        }
        public ILayer SelectedLayer
        {
            get => (ILayer)GetValue(SelectedLayerProperty);
            set => SetValue(SelectedLayerProperty, value);
        }

        public RenderPreviewView()
        {
            InitializeComponent();
        }

        private readonly Stopwatch Stopwatch = new Stopwatch();
        private void OnRenderButtonClick(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer is null)
            {
                MessageBox.Show("Select a layer before rendering.", "Render", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!TryGetRenderOptions(out Bound ROI, out double PixelPerUnit, out int MaxDegreeOfParallelism, out ARGB Background, out ARGB Foreground))
                return;

            try
            {
                // FreesiaGerberLib separates rasterization inputs into graphic, visual transform, and execution options.
                GraphicOption Graphic = new GraphicOption(ROI, PixelPerUnit, Background, Foreground);
                VisualOption Visual = CreateVisualOption(ROI);
                RenderOption Render = new RenderOption
                {
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                };

                Stopwatch.Restart();
                // BGRA matches WPF PixelFormats.Bgra32, so the rendered buffer can be displayed without channel conversion.
                using (RenderImage<BGRA> Image = SelectedLayer.Render<BGRA>(Graphic, Visual, Render))
                {
                    Stopwatch.Stop();
                    BitmapSource Source = BitmapSource.Create(Image.Width, Image.Height, 96, 96, PixelFormats.Bgra32, null, Image.Scan0, (int)(Image.Stride * Image.Height), (int)Image.Stride);
                    RenderImageViewer.ImageSource = Source;
                }

                StatusChanged?.Invoke("Render completed.");
                StatusDetailChanged?.Invoke($"Render: {Stopwatch.Elapsed.TotalMilliseconds:0.##} ms");
            }
            catch (InvalidOperationException)
            {
                StatusChanged?.Invoke("Render feature is not available.");
                StatusDetailChanged?.Invoke("");
                MessageBox.Show("The current hardware dongle does not allow rendering this layer.", "Render", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke("Render failed.");
                StatusDetailChanged?.Invoke("");
                MessageBox.Show(ex.Message, "Render Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool TryGetRenderOptions(out Bound ROI, out double PixelPerUnit, out int MaxDegreeOfParallelism, out ARGB Background, out ARGB Foreground)
        {
            // GraphicOption.ROI controls the physical area that will be rasterized.
            if (UseFullLayerCheckBox.IsChecked == true)
            {
                ROI = SelectedLayer.Bound;
                RoiLeftTextBox.Text = ROI.Left.ToString("0.###", CultureInfo.InvariantCulture);
                RoiTopTextBox.Text = ROI.Top.ToString("0.###", CultureInfo.InvariantCulture);
                RoiRightTextBox.Text = ROI.Right.ToString("0.###", CultureInfo.InvariantCulture);
                RoiBottomTextBox.Text = ROI.Bottom.ToString("0.###", CultureInfo.InvariantCulture);
            }
            else if (!double.TryParse(RoiLeftTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double Left) ||
                     !double.TryParse(RoiTopTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double Top) ||
                     !double.TryParse(RoiRightTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double Right) ||
                     !double.TryParse(RoiBottomTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double Bottom))
            {
                ROI = Bound.Empty;
                PixelPerUnit = 0d;
                MaxDegreeOfParallelism = 0;
                Background = new ARGB();
                Foreground = new ARGB();
                MessageBox.Show("ROI values must be valid numbers.", "Render Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else if (Right <= Left ||
                     Bottom <= Top)
            {
                ROI = Bound.Empty;
                PixelPerUnit = 0d;
                MaxDegreeOfParallelism = 0;
                Background = new ARGB();
                Foreground = new ARGB();
                MessageBox.Show("ROI requires Right > Left and Bottom > Top.", "Render Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else
            {
                ROI = new Bound(Left, Top, Right, Bottom);
            }

            // PixelPerUnit controls output resolution: larger values generate more pixels for the same Gerber area.
            if (!double.TryParse(PixelPerUnitTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out PixelPerUnit) ||
                PixelPerUnit <= 0d)
            {
                MaxDegreeOfParallelism = 0;
                Background = new ARGB();
                Foreground = new ARGB();
                MessageBox.Show("Pixel / Unit must be a positive number.", "Render Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 0 keeps the library/runtime default scheduling; 1 forces single-thread rendering.
            if (!int.TryParse(MaxThreadsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out MaxDegreeOfParallelism))
            {
                Background = new ARGB();
                Foreground = new ARGB();
                MessageBox.Show("Max Threads must be an integer.", "Render Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TryParseArgb(BackgroundTextBox.Text, out Background))
            {
                Foreground = new ARGB();
                MessageBox.Show("Background must use #AARRGGBB format.", "Render Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TryParseArgb(ForegroundTextBox.Text, out Foreground))
            {
                MessageBox.Show("Foreground must use #AARRGGBB format.", "Render Parameters", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
        private VisualOption CreateVisualOption(Bound ROI)
        {
            // Mirror operations use the ROI center so the selected render area stays in place.
            FlipMode Flip = FlipMode.None;

            if (MirrorHorizontalCheckBox.IsChecked == true)
                Flip |= FlipMode.Horizontal;

            if (MirrorVerticalCheckBox.IsChecked == true)
                Flip |= FlipMode.Vertical;

            return new VisualOption
            {
                Flip = Flip,
                FlipCx = ROI.Cx,
                FlipCy = ROI.Cy,
                RotateAngle = 0d,
                RotateCx = 0d,
                RotateCy = 0d
            };
        }
        private bool TryParseArgb(string Text, out ARGB Color)
        {
            Color = new ARGB();

            if (string.IsNullOrWhiteSpace(Text))
                return false;

            string Value = Text.Trim();
            if (Value.Length != 9 ||
                Value[0] != '#')
            {
                return false;
            }

            if (!byte.TryParse(Value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte A) ||
                !byte.TryParse(Value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte R) ||
                !byte.TryParse(Value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte G) ||
                !byte.TryParse(Value.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte B))
            {
                return false;
            }

            Color = new ARGB(A, R, G, B);
            return true;
        }

    }
}
