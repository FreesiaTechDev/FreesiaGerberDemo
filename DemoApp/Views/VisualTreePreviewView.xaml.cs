using FreesiaAssembly;
using FreesiaAssembly.Media.Imaging;
using FreesiaGerberDemo.Models;
using FreesiaGerberLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FreesiaGerberDemo.Views
{
    public partial class VisualTreePreviewView : Grid
    {
        public event Action<string> StatusChanged;
        public event Action<string> StatusDetailChanged;

        public static readonly DependencyProperty SelectedLayerProperty =
            DependencyProperty.Register(nameof(SelectedLayer), typeof(ILayer), typeof(VisualTreePreviewView), new PropertyMetadata(null, OnSelectedLayerChanged));
        private static void OnSelectedLayerChanged(DependencyObject Sender, DependencyPropertyChangedEventArgs e)
        {
            VisualTreePreviewView View = (VisualTreePreviewView)Sender;
            View.LoadedLayer = null;
            View.VisualTreeNodes.Clear();
            View.VisualTreeDetailTextBox.Clear();
            View.StatusDetailChanged?.Invoke("");
        }
        public ILayer SelectedLayer
        {
            get => (ILayer)GetValue(SelectedLayerProperty);
            set => SetValue(SelectedLayerProperty, value);
        }

        private readonly ObservableCollection<VisualTreeNode> VisualTreeNodes = new ObservableCollection<VisualTreeNode>();

        public VisualTreePreviewView()
        {
            InitializeComponent();
            VisualTreeView.ItemsSource = VisualTreeNodes;
        }

        private void OnVisualTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (VisualTreeView.SelectedItem is VisualTreeNode Node)
                VisualTreeDetailTextBox.Text = Node.Detail;
        }
        private void OnVisualTreeViewItemExpanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem Item &&
                Item.DataContext is VisualTreeNode Node)
            {
                Node.EnsureChildrenLoaded();
            }
        }

        private ILayer LoadedLayer;
        private readonly Stopwatch Stopwatch = new Stopwatch();
        public void LoadVisualTree()
        {
            if (LoadedLayer == SelectedLayer)
                return;

            if (SelectedLayer is null)
            {
                StatusChanged?.Invoke("Select a layer before loading visual tree.");
                StatusDetailChanged?.Invoke("");
                return;
            }

            try
            {
                // Keep the demo focused on reading Visual Tree data.
                // Transform options are still passed explicitly so users can see the normal API call shape,
                // but this preview does not apply rotation or mirror transforms.
                VisualOption Visual = new VisualOption
                {
                    Flip = FlipMode.None,
                    FlipCx = 0d,
                    FlipCy = 0d,
                    RotateAngle = 0d,
                    RotateCx = 0d,
                    RotateCy = 0d
                };

                Stopwatch.Restart();
                VisualRoot Root = SelectedLayer.GetVisualTree(Visual);
                Stopwatch.Stop();

                string RootName = Root.Name;
                if (!string.IsNullOrWhiteSpace(RootName))
                {
                    string TrimmedPath = RootName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string Name = Path.GetFileName(TrimmedPath);
                    RootName = string.IsNullOrWhiteSpace(Name) ? RootName : Name;
                }
                else
                {
                    RootName = "Data";
                }

                VisualTreeNodes.Clear();
                VisualTreeDetailTextBox.Clear();
                VisualTreeNodes.Add(new VisualTreeNode($"Root: {RootName}", () => BuildRootDetail(Root), () => CreateRootChildren(Root)));

                LoadedLayer = SelectedLayer;
                StatusChanged?.Invoke("Visual tree loaded.");
                StatusDetailChanged?.Invoke($"Visual Tree: {Stopwatch.Elapsed.TotalMilliseconds:0.##} ms");
            }
            catch (InvalidOperationException)
            {
                LoadedLayer = null;
                VisualTreeNodes.Clear();
                VisualTreeDetailTextBox.Text = "Visual Tree feature is not available for the current hardware dongle.";
                StatusChanged?.Invoke("Visual Tree feature is not available.");
                StatusDetailChanged?.Invoke("");
            }
            catch (Exception ex)
            {
                LoadedLayer = null;
                StatusChanged?.Invoke("Visual tree load failed.");
                StatusDetailChanged?.Invoke("");
                MessageBox.Show(ex.Message, "Visual Tree", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IEnumerable<VisualTreeNode> CreateRootChildren(VisualRoot Root)
        {
            VisualLayer Layer = Root.Content;
            yield return new VisualTreeNode($"Layer: {Layer.Name}", () => BuildLayerDetail(Layer), () => CreateLayerChildren(Layer));
        }
        private IEnumerable<VisualTreeNode> CreateLayerChildren(VisualLayer Layer)
        {
            foreach (VisualLayer ChildLayer in Layer.Layers)
                yield return new VisualTreeNode($"Layer: {ChildLayer.Name}", () => BuildLayerDetail(ChildLayer), () => CreateLayerChildren(ChildLayer));

            foreach (VisualObject Object in Layer.Children)
                yield return new VisualTreeNode($"Object: {Object.Type}", () => BuildObjectDetail(Object), () => CreateObjectChildren(Object));
        }
        private IEnumerable<VisualTreeNode> CreateObjectChildren(VisualObject Object)
        {
            for (int i = 0; i < Object.Datas.Count; i++)
            {
                VisualObjectData Data = Object.Datas[i];
                yield return new VisualTreeNode($"Data {i}", () => BuildDataDetail(Object.Type, Data), () => CreateDataChildren(Data));
            }
        }
        private IEnumerable<VisualTreeNode> CreateDataChildren(VisualObjectData Data)
        {
            if (Data.RegionContext is null)
                yield break;

            int Index = 0;
            foreach (VisualRegionContext Context in Data.RegionContext)
            {
                yield return new VisualTreeNode($"Region Context {Index}", () => BuildRegionContextDetail(Context), () => CreateRegionContextChildren(Context));
                Index++;
            }
        }
        private IEnumerable<VisualTreeNode> CreateRegionContextChildren(VisualRegionContext Context)
        {
            int Index = 0;
            foreach (VisualRegionDescription Description in Context.Descriptions)
            {
                yield return new VisualTreeNode($"Description {Index}", () => BuildRegionDescriptionDetail(Description), null);
                Index++;
            }
        }

        private string BuildRootDetail(VisualRoot Root)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Type");
            Builder.AppendLine("  VisualRoot");
            Builder.AppendLine();
            Builder.AppendLine("Identity");
            Builder.AppendLine($"  Name: {Root.Name}");
            Builder.AppendLine();
            AppendBound(Builder, "Bound", Root.Bound);
            Builder.AppendLine();
            Builder.AppendLine("Drawing");
            Builder.AppendLine($"  Angle: {Root.Angle}");
            Builder.AppendLine($"  Flip: {Root.Flip}");
            return Builder.ToString();
        }
        private string BuildLayerDetail(VisualLayer Layer)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Type");
            Builder.AppendLine("  VisualLayer");
            Builder.AppendLine();
            Builder.AppendLine("Identity");
            Builder.AppendLine($"  Name: {Layer.Name}");
            Builder.AppendLine($"  Step: {Layer.Step?.Name ?? "None"}");
            Builder.AppendLine();
            AppendBound(Builder, "Bound", Layer.Bound);
            return Builder.ToString();
        }
        private string BuildObjectDetail(VisualObject Object)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Type");
            Builder.AppendLine("  VisualObject");
            Builder.AppendLine();
            Builder.AppendLine("Drawing");
            Builder.AppendLine($"  Object Type: {Object.Type}");
            Builder.AppendLine($"  Polarity: {Object.Polarity}");

            // Region geometry is stored in VisualObjectData.RegionContext, so a missing pen is not shown as "None".
            if (Object.Pen != null)
            {
                Builder.AppendLine();
                AppendPen(Builder, Object.Pen);
            }
            Builder.AppendLine();
            Builder.AppendLine("Attributes");
            if (Object.Attributes is null || Object.Attributes.Count == 0)
            {
                Builder.AppendLine("  None");
            }
            else
            {
                foreach (KeyValuePair<string, object> Attribute in Object.Attributes)
                    Builder.AppendLine($"  {Attribute.Key}: {Attribute.Value}");
            }
            Builder.AppendLine();
            Builder.AppendLine("Children");
            Builder.AppendLine($"  Data Count: {Object.Datas.Count}");
            return Builder.ToString();
        }
        private string BuildDataDetail(VisualObjectType Type, VisualObjectData Data)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Type");
            Builder.AppendLine("  VisualObjectData");
            Builder.AppendLine();
            AppendBound(Builder, "Bound", Data.Bound);
            Builder.AppendLine();
            AppendBound(Builder, "Parent Bound", Data.ParentBound);
            Builder.AppendLine();
            Builder.AppendLine("Drawing");
            Builder.AppendLine($"  Object Type: {Type}");
            Builder.AppendLine($"  Pen Theta: {Data.PenTheta}");
            Builder.AppendLine($"  Pen Flip: {Data.PenFlip}");
            Builder.AppendLine();
            Builder.AppendLine("Geometry");
            switch (Type)
            {
                case VisualObjectType.Aperture:
                    // This is the flash/aperture placement point; the rendered extent is determined by the pen and Bound.
                    if (Data.Points != null && Data.Points.Length >= 2)
                        AppendPoint(Builder, "Aperture Position", "Cx", Data.Points[0], "Cy", Data.Points[1]);
                    else
                        Builder.AppendLine($"  Points: {FormatPoints(Data.Points)}");
                    break;

                case VisualObjectType.Line:
                    if (Data.Points != null && Data.Points.Length >= 4)
                    {
                        AppendPoint(Builder, "Start", "Sx", Data.Points[0], "Sy", Data.Points[1]);
                        AppendPoint(Builder, "End", "Ex", Data.Points[2], "Ey", Data.Points[3]);
                    }
                    else
                    {
                        Builder.AppendLine($"  Points: {FormatPoints(Data.Points)}");
                    }
                    break;

                case VisualObjectType.Arc:
                    if (Data.Points != null && Data.Points.Length >= 6)
                    {
                        AppendPoint(Builder, "Start", "Sx", Data.Points[0], "Sy", Data.Points[1]);
                        AppendPoint(Builder, "End", "Ex", Data.Points[2], "Ey", Data.Points[3]);
                        AppendPoint(Builder, "Center", "Cx", Data.Points[4], "Cy", Data.Points[5]);
                    }
                    else
                    {
                        Builder.AppendLine($"  Points: {FormatPoints(Data.Points)}");
                    }

                    Builder.AppendLine("  Arc");
                    Builder.AppendLine($"    Radius: {FormatNumber(Data.ArcRadius)}");
                    Builder.AppendLine($"    Is Clockwise: {Data.ArcIsClockwise}");
                    break;

                case VisualObjectType.Region:
                    // Region data is represented by contour contexts instead of a flat point list.
                    Builder.AppendLine("  Region");
                    Builder.AppendLine("    Geometry Source: RegionContext");
                    Builder.AppendLine($"    Region Context Count: {FormatArrayCount(Data.RegionContext)}");
                    break;

                default:
                    Builder.AppendLine($"  Points: {FormatPoints(Data.Points)}");
                    break;
            }
            return Builder.ToString();
        }
        private string BuildRegionContextDetail(VisualRegionContext Context)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Type");
            Builder.AppendLine("  VisualRegionContext");
            Builder.AppendLine();
            Builder.AppendLine("Drawing");
            Builder.AppendLine($"  Polarity: {Context.Polarity}");
            Builder.AppendLine();
            Builder.AppendLine("Children");
            Builder.AppendLine($"  Descriptions: {Context.Descriptions.Count}");
            return Builder.ToString();
        }
        private string BuildRegionDescriptionDetail(VisualRegionDescription Description)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Type");
            Builder.AppendLine("  VisualRegionDescription");
            Builder.AppendLine();
            Builder.AppendLine("Geometry");
            if (Description.Points != null && Description.Points.Length >= 6)
            {
                AppendPoint(Builder, "Start", "Sx", Description.Points[0], "Sy", Description.Points[1]);
                AppendPoint(Builder, "End", "Ex", Description.Points[2], "Ey", Description.Points[3]);
                AppendPoint(Builder, "Center", "Cx", Description.Points[4], "Cy", Description.Points[5]);
                Builder.AppendLine("  Arc");
                Builder.AppendLine($"    Radius: {FormatNumber(Description.ArcRadius)}");
                Builder.AppendLine($"    Is Clockwise: {Description.ArcIsClockwise}");
            }
            else if (Description.Points != null && Description.Points.Length >= 4)
            {
                AppendPoint(Builder, "Start", "Sx", Description.Points[0], "Sy", Description.Points[1]);
                AppendPoint(Builder, "End", "Ex", Description.Points[2], "Ey", Description.Points[3]);
            }
            else
            {
                Builder.AppendLine($"  Points: {FormatPoints(Description.Points)}");
            }
            return Builder.ToString();
        }

        private void AppendBound(StringBuilder Builder, string Title, Bound Bound)
        {
            Builder.AppendLine(Title);
            Builder.AppendLine($"  Left: {FormatNumber(Bound.Left)}");
            Builder.AppendLine($"  Top: {FormatNumber(Bound.Top)}");
            Builder.AppendLine($"  Right: {FormatNumber(Bound.Right)}");
            Builder.AppendLine($"  Bottom: {FormatNumber(Bound.Bottom)}");
        }
        private void AppendPoint(StringBuilder Builder, string Title, string XName, double X, string YName, double Y)
        {
            Builder.AppendLine($"  {Title}");
            Builder.AppendLine($"    {XName}: {FormatNumber(X)}");
            Builder.AppendLine($"    {YName}: {FormatNumber(Y)}");
        }
        private void AppendPen(StringBuilder Builder, VisualPen Pen)
        {
            if (Pen is null)
            {
                Builder.AppendLine("Pen");
                Builder.AppendLine("  None");
                return;
            }

            Builder.AppendLine("Pen");
            switch (Pen.Type)
            {
                case VisualPenType.Rectangle:
                    Builder.AppendLine("  Type: Rectangle");
                    AppendPenSize(Builder, Pen.Context);
                    break;

                case VisualPenType.Ellipse:
                    if (Pen.Context != null && Pen.Context.Length == 1)
                    {
                        // The public type is Ellipse, but one context value means a circular aperture radius.
                        Builder.AppendLine("  Type: Circle");
                        Builder.AppendLine($"  Radius: {FormatNumber(Pen.Context[0])}");
                    }
                    else
                    {
                        Builder.AppendLine("  Type: Ellipse");
                        AppendPenSize(Builder, Pen.Context);
                    }
                    break;

                case VisualPenType.Oval:
                    Builder.AppendLine("  Type: Oval");
                    AppendPenSize(Builder, Pen.Context);
                    break;

                case VisualPenType.Symbol:
                    // Symbols can contain many visual objects; keep detail compact and avoid expanding them here.
                    Builder.AppendLine("  Type: Symbol");
                    Builder.AppendLine($"  Symbol Count: {FormatArrayCount(Pen.Symbols)}");
                    break;

                case VisualPenType.Special:
                    Builder.AppendLine("  Type: Special");
                    Builder.AppendLine($"  Context: {FormatPoints(Pen.Context)}");
                    break;

                default:
                    Builder.AppendLine($"  Type: {Pen.Type}");
                    Builder.AppendLine($"  Context: {FormatPoints(Pen.Context)}");
                    break;
            }
        }
        private void AppendPenSize(StringBuilder Builder, double[] Context)
        {
            if (Context != null && Context.Length >= 2)
            {
                Builder.AppendLine($"  Width: {FormatNumber(Context[0])}");
                Builder.AppendLine($"  Height: {FormatNumber(Context[1])}");
                return;
            }

            Builder.AppendLine($"  Context: {FormatPoints(Context)}");
        }

        private string FormatPoints(double[] Points)
        {
            if (Points is null)
                return "None";

            return string.Join(", ", Points.Select(i => i.ToString("0.###")));
        }
        private string FormatNumber(double Value)
        {
            if (double.IsNaN(Value))
                return "N/A";

            return Value.ToString("0.###");
        }
        private string FormatArrayCount<T>(T[] Values)
        {
            if (Values is null)
                return "0";

            return Values.Length.ToString();
        }

    }
}
