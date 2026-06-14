using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FreesiaGerberDemo.Controls
{
    public class ImageViewer : ScrollViewer
    {
        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            nameof(ImageSource),
            typeof(ImageSource),
            typeof(ImageViewer),
            new PropertyMetadata(null, OnImageSourceChanged));

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        private readonly Image ImageControl = new Image
        {
            Stretch = Stretch.None
        };

        public ImageViewer()
        {
            Background = Brushes.Transparent;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            Content = ImageControl;
        }

        private bool IsDragging;
        private Point DragStartPoint;
        private double DragHorizontalOffset;
        private double DragVerticalOffset;
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            IsDragging = true;
            DragStartPoint = e.GetPosition(this);
            DragHorizontalOffset = HorizontalOffset;
            DragVerticalOffset = VerticalOffset;
            Cursor = Cursors.Hand;
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            EndDrag();
            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (!IsDragging)
                return;

            Point CurrentPoint = e.GetPosition(this);
            Vector Delta = CurrentPoint - DragStartPoint;
            ScrollToHorizontalOffset(DragHorizontalOffset - Delta.X);
            ScrollToVerticalOffset(DragVerticalOffset - Delta.Y);
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            EndDrag();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void EndDrag()
        {
            if (!IsDragging)
                return;

            IsDragging = false;
            Cursor = null;
            ReleaseMouseCapture();
        }

        private static void OnImageSourceChanged(DependencyObject Sender, DependencyPropertyChangedEventArgs e)
        {
            ImageViewer Viewer = (ImageViewer)Sender;
            Viewer.ImageControl.Source = (ImageSource)e.NewValue;
        }

    }
}
