using FreesiaGerberLib;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace FreesiaGerberDemo
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch Stopwatch = new Stopwatch();

        private IGerber GerberInfo;

        private ILayer SelectedLayer;

        public MainWindow()
        {
            InitializeComponent();

            RenderPreviewView.StatusChanged += OnViewStatusChanged;
            RenderPreviewView.StatusDetailChanged += OnViewStatusDetailChanged;
            VisualTreePreviewView.StatusChanged += OnViewStatusChanged;
            VisualTreePreviewView.StatusDetailChanged += OnViewStatusDetailChanged;
        }
        private void OnViewStatusChanged(string Status)
        {
            StatusTextBlock.Text = Status;
        }
        private void OnViewStatusDetailChanged(string Status)
        {
            StatusDetailTextBlock.Text = Status;
        }

        private void OnLoadFileMenuItemClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            if (Dialog.ShowDialog() == true)
                LoadGerber(Dialog.FileName);
        }
        private void OnLoadFolderMenuItemClick(object sender, RoutedEventArgs e)
        {
            Forms.FolderBrowserDialog Dialog = new Forms.FolderBrowserDialog();
            if (Dialog.ShowDialog() == Forms.DialogResult.OK)
                LoadGerber(Dialog.SelectedPath);
        }
        private void OnLicenseMenuItemClick(object sender, RoutedEventArgs e)
        {
            LicenseWindow Window = new LicenseWindow
            {
                Owner = this
            };

            Window.ShowDialog();
        }

        private void LoadGerber(string DataPath)
        {
            Stopwatch.Restart();
            FreesiaCode Code = Gerber.TryParse(DataPath, out GerberInfo);
            Stopwatch.Stop();

            Trace.WriteLine($"Load: {Stopwatch.Elapsed.TotalMilliseconds} ms");
            ResetCurrentView();

            switch (Code)
            {
                case FreesiaCode.Success:
                    {
                        DataTreeView.ItemsSource = new[] { GerberInfo };
                        GerberTitleTextBlock.Text = GerberInfo.Type.ToString();
                        StatusTextBlock.Text = $"Loaded: {DataPath}";
                        return;
                    }
                case FreesiaCode.Failure:
                    {
                        StatusTextBlock.Text = "Load failed.";
                        MessageBox.Show("The selected path is not supported Gerber data.", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
                case FreesiaCode.PermissionDenied:
                    {
                        StatusTextBlock.Text = "Permission denied.";
                        MessageBox.Show("The hardware dongle does not allow this feature.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
                case FreesiaCode.Unauthorized:
                    {
                        StatusTextBlock.Text = "Unauthorized.";
                        MessageBox.Show("A valid hardware dongle is required.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
                default:
                    {
                        StatusTextBlock.Text = "Load failed.";
                        MessageBox.Show("Unknown load result.", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
            }

            GerberInfo = null;
        }

        private void OnDataTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(DataTreeView.SelectedItem is ILayer Layer))
            {
                SelectedLayer = null;
                RenderPreviewView.SelectedLayer = null;
                UpdateVisualTreeIfActive();

                return;
            }

            SelectedLayer = Layer;
            RenderPreviewView.SelectedLayer = SelectedLayer;
            UpdateVisualTreeIfActive();
        }
        private void OnMainTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != MainTabControl ||
                MainTabControl.SelectedItem != VisualTreeTabItem)
            {
                return;
            }

            UpdateVisualTreeIfActive();
        }

        private void UpdateVisualTreeIfActive()
        {
            if (MainTabControl.SelectedItem != VisualTreeTabItem)
                return;

            if (GerberInfo is null ||
                SelectedLayer is null)
            {
                VisualTreePreviewView.SelectedLayer = null;
                return;
            }

            VisualTreePreviewView.SelectedLayer = SelectedLayer;
            VisualTreePreviewView.LoadVisualTree();
        }
        private void ResetCurrentView()
        {
            SelectedLayer = null;
            RenderPreviewView.SelectedLayer = null;
            UpdateVisualTreeIfActive();

            DataTreeView.ItemsSource = null;
            GerberTitleTextBlock.Text = "Gerber";
        }

    }
}