using FreesiaGerberLib;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace FreesiaGerberDemo.Converters
{
    public sealed class MiscConverter : IValueConverter
    {
        public static MiscConverter Instance { get; } = new MiscConverter();

        private MiscConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (parameter as string)
            {
                case "GerberHeader":
                    {
                        if (value is IGerber Gerber)
                            return GetDisplayPathName(Gerber.Path, Gerber.Type.ToString());

                        return "Gerber";
                    }
                case "GerberPath":
                    {
                        if (value is IGerber Gerber)
                            return Gerber.Path;

                        return null;
                    }
                case "GerberSteps":
                    {
                        if (value is IGerber Gerber)
                            return Gerber.Steps;

                        return null;
                    }
                case "StepHeader":
                    {
                        if (value is IStep Step &&
                            !string.IsNullOrWhiteSpace(Step.Name))
                        {
                            return Step.Name;
                        }

                        return "Step";
                    }
                case "StepLayers":
                    {
                        if (value is IStep Step)
                            return Step.Layers;

                        return null;
                    }
                case "LayerHeader":
                    {
                        if (value is ILayer Layer &&
                            !string.IsNullOrWhiteSpace(Layer.Name))
                        {
                            return Layer.Name;
                        }

                        return "Layer";
                    }
                case "LayerName":
                    {
                        if (value is ILayer Layer &&
                            !string.IsNullOrWhiteSpace(Layer.Name))
                        {
                            return Layer.Name;
                        }

                        return "-";
                    }
                case "LayerBoundText":
                    {
                        if (value is ILayer Layer)
                        {
                            var Bound = Layer.Bound;
                            return $"L {Bound.Left.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                                   $"T {Bound.Top.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                                   $"R {Bound.Right.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                                   $"B {Bound.Bottom.ToString("0.###", CultureInfo.InvariantCulture)}";
                        }

                        return "-";
                    }
                default:
                    {
                        return value;
                    }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        private static string GetDisplayPathName(string PathText, string Fallback)
        {
            if (string.IsNullOrWhiteSpace(PathText))
                return string.IsNullOrWhiteSpace(Fallback) ? "Gerber" : Fallback;

            string TrimmedPath = PathText.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string Name = Path.GetFileName(TrimmedPath);
            return string.IsNullOrWhiteSpace(Name) ? PathText : Name;
        }

    }
}
