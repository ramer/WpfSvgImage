using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace WpfSvgImageCSharp
{
    class SvgImage : Shape
    {
        protected override Geometry DefiningGeometry
        {
            get { return (Geometry)GetValue(DataProperty); }
        }

        TransformGroup transformGroup = new TransformGroup();
        TranslateTransform translateTransform = new TranslateTransform();
        ScaleTransform scaleTransform = new ScaleTransform();

        public SvgImage() : base()
        {
            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(scaleTransform);
            RenderTransform = transformGroup;
        }

        private Rect _viewbox;
        private Rect ViewBox
        {
            get { return _viewbox; }
            set { _viewbox = value; }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(Uri), typeof(SvgImage), new PropertyMetadata(SourcePropertyChanged));

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SvgImage img = d as SvgImage;
            if (img != null) img.UpdateData((Uri)e.NewValue);
        }

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyPropertyKey DataPropertyKey = DependencyProperty.RegisterReadOnly("Data", typeof(Geometry), typeof(SvgImage), new FrameworkPropertyMetadata(new GeometryGroup(), FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DataProperty = DataPropertyKey.DependencyProperty;

        public Geometry Data
        {
            get { return (Geometry)GetValue(DataProperty); }
            protected set 
            {
                SetValue(DataPropertyKey, value);
                UpdateLayout();
            }
        }

        public static readonly DependencyPropertyKey WidthPropertyKey = DependencyProperty.RegisterReadOnly("Width", typeof(double), typeof(SvgImage), new PropertyMetadata(double.NaN));
        public static new readonly DependencyProperty WidthProperty = WidthPropertyKey.DependencyProperty;

        public new double Width
        {
            get { return (double)GetValue(WidthProperty); }
            protected set
            {
                base.Width = value;
                SetValue(WidthPropertyKey, value);
            }
        }

        public static readonly DependencyPropertyKey HeightPropertyKey = DependencyProperty.RegisterReadOnly("Height", typeof(double), typeof(SvgImage), new PropertyMetadata(double.NaN));
        public static new readonly DependencyProperty HeightProperty = HeightPropertyKey.DependencyProperty;

        public new double Height
        {
            get { return (double)GetValue(HeightProperty); }
            protected set
            {
                base.Height = value;
                SetValue(HeightPropertyKey, value);
            }
        }

        public static readonly DependencyPropertyKey ViewBoxWidthPropertyKey = DependencyProperty.RegisterReadOnly("ViewBoxWidth", typeof(double), typeof(SvgImage), new PropertyMetadata(double.NaN));
        public static readonly DependencyProperty ViewBoxWidthProperty = ViewBoxWidthPropertyKey.DependencyProperty;

        public double ViewBoxWidth
        {
            get { return (double)GetValue(ViewBoxWidthProperty); }
            protected set { SetValue(ViewBoxWidthPropertyKey, value); }
        }

        public static readonly DependencyPropertyKey ViewBoxHeightPropertyKey = DependencyProperty.RegisterReadOnly("ViewBoxHeight", typeof(double), typeof(SvgImage), new PropertyMetadata(double.NaN));
        public static readonly DependencyProperty ViewBoxHeightProperty = ViewBoxHeightPropertyKey.DependencyProperty;

        public double ViewBoxHeight
        {
            get { return (double)GetValue(ViewBoxHeightProperty); }
            protected set { SetValue(ViewBoxHeightPropertyKey, value); }
        }

        public virtual void UpdateData(Uri uri)
        {
            if (uri == null)
            {
                Data = new GeometryGroup();
                return;
            }

            try
            {
                string path = string.Empty;
                try
                {
                    path = uri.LocalPath;
                }
                catch { }

                if (!File.Exists(path))
                {
                    StreamResourceInfo sri = Application.GetResourceStream(uri);
                    using (var stream = sri.Stream)
                    {
                        UpdateDataFromStream(stream);
                    }
                }
                else
                { 
                    using (var stream = new FileStream(path, FileMode.Open))
                    {
                        UpdateDataFromStream(stream);
                    }
                }
            }
            catch
            {
                Data = new GeometryGroup();
            }
        }

        private void UpdateDataFromStream(Stream stream)
        {
            XmlReaderSettings readersettings = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore }; // .XmlResolver = Nothing,

            using (var reader = XmlReader.Create(stream, readersettings))
            {
                reader.MoveToContent();

                var node = XNode.ReadFrom(reader);
                Data = ReadSvgNode((XElement)node);
            }
        }

        private Geometry ReadSvgNode(XElement node)
        {
            Geometry result = null;

            switch (node.Name.LocalName.ToLower())
            {
                case "svg":
                {
                    double? newwidth;
                    double? newheight;
                    Rect? newviewbox;

                    newwidth = GetDoubleAttribute(node.Attribute("width"));
                    newheight = GetDoubleAttribute(node.Attribute("height"));
                    newviewbox = GetRectAttribute(node.Attribute("viewBox"));

                    Clip = newviewbox.HasValue ? new RectangleGeometry(newviewbox.Value) : null;
                    Width = newviewbox.HasValue ? newviewbox.Value.Right : newwidth.HasValue ? newwidth.Value : double.NaN;
                    Height = newviewbox.HasValue ? newviewbox.Value.Bottom : newheight.HasValue ? newheight.Value : double.NaN;
                    ViewBox = newviewbox.HasValue ? newviewbox.Value : Rect.Empty;
                    ViewBoxWidth = newviewbox.HasValue ? newviewbox.Value.Width : double.NaN;
                    ViewBoxHeight = newviewbox.HasValue ? newviewbox.Value.Height : double.NaN;
                    translateTransform.X = newviewbox.HasValue ? -newviewbox.Value.X : 0;
                    translateTransform.Y = newviewbox.HasValue ? -newviewbox.Value.Y : 0;
                    scaleTransform.ScaleX = newviewbox.HasValue ? newviewbox.Value.Right / (double)newviewbox.Value.Width : 1;
                    scaleTransform.ScaleY = newviewbox.HasValue ? newviewbox.Value.Bottom / (double)newviewbox.Value.Height : 1;

                    GeometryGroup geometrygroup = new GeometryGroup() { FillRule = FillRule.Nonzero };
                    if (node.HasElements)
                    {
                        foreach (XElement child in node.Elements())
                        {
                            var geometry = ReadSvgNode(child);
                            if (geometry != null) geometrygroup.Children.Add(geometry);
                        }
                    }
                    result = geometrygroup;
                    break;
                }

                case "g":
                {
                    GeometryGroup geometrygroup = new GeometryGroup() { FillRule = FillRule.Nonzero };
                    if (node.HasElements)
                    {
                        foreach (XElement child in node.Elements())
                        {
                            var geometry = ReadSvgNode(child);
                            if (geometry != null) geometrygroup.Children.Add(geometry);
                        }
                    }
                    result = geometrygroup;
                    break;
                }

                case "path":
                {
                    foreach (var attr in node.Attributes())
                    {
                        if (attr.Name.LocalName == "d" && !string.IsNullOrEmpty(attr.Value))
                        {
                            try
                            {
                                return Geometry.Parse(attr.Value);
                            }
                            catch { }
                        }
                    }
                    break;
                }

                case "line":
                {
                    double x1, y1, x2, y2;
                    if (GetDoubleAttribute(node.Attribute("x1"), out x1) && GetDoubleAttribute(node.Attribute("y1"), out y1) && GetDoubleAttribute(node.Attribute("x2"), out x2) && GetDoubleAttribute(node.Attribute("y2"), out y2))
                        result = new LineGeometry(new Point(x1, y2), new Point(x2, y2));
                    break;
                }

                case "rect":
                    {
                        double x, y, w, h;
                        if (GetDoubleAttribute(node.Attribute("x"), out x) && GetDoubleAttribute(node.Attribute("y"), out y) && GetDoubleAttribute(node.Attribute("width"), out w) && GetDoubleAttribute(node.Attribute("height"), out h))
                            result = new RectangleGeometry(new Rect(x, y, w, h));
                        break;
                    }

                case "ellipse":
                    {
                        double cx, cy, rx, ry;
                        if (GetDoubleAttribute(node.Attribute("cx"), out cx) && GetDoubleAttribute(node.Attribute("cy"), out cy) && GetDoubleAttribute(node.Attribute("rx"), out rx) && GetDoubleAttribute(node.Attribute("ry"), out ry))
                            result = new EllipseGeometry(new Point(cx, cy), rx, ry);
                        break;
                    }

                case "circle":
                    {
                        double cx, cy, r;
                        if (GetDoubleAttribute(node.Attribute("cx"), out cx) && GetDoubleAttribute(node.Attribute("cy"), out cy) && GetDoubleAttribute(node.Attribute("r"), out r))
                            result = new EllipseGeometry(new Point(cx, cy), r, r);
                        break;
                    }

                case "polyline":
                    {
                        try
                        {
                            Polyline polyline = new Polyline();
                            XAttribute attr = node.Attribute("points");
                            if (attr != null && !string.IsNullOrEmpty(attr.Value))
                            {
                                polyline.Points = PointCollection.Parse(attr.Value);
                                polyline.Measure(ViewBox.Size);
                                polyline.Arrange(ViewBox);
                                result = polyline.RenderedGeometry;
                            }
                        }
                        catch { }
                        break;
                    }

                case "polygon":
                    {
                        try
                        {
                            Polygon polygon = new Polygon();
                            XAttribute attr = node.Attribute("points");
                            if (attr != null && !string.IsNullOrEmpty(attr.Value))
                            {
                                polygon.Points = PointCollection.Parse(attr.Value);
                                polygon.Measure(ViewBox.Size);
                                polygon.Arrange(ViewBox);
                                result = polygon.RenderedGeometry;
                            }
                        }
                        catch { }
                        break;
                    }
            }

            if (result != null)
                result.Transform = GetTransformAttribute(node.Attribute("transform"));

            return result;
        }

        private bool GetDoubleAttribute(XAttribute attr, out double d)
        {
            if (attr == null)
            {
                d = 0;
                return false;
            }
            string value = attr.Value;
            if (string.IsNullOrEmpty(value))
            {
                d = 0;
                return false;
            }
            value = Regex.Replace(value, "[^0-9.-]", "");
            return double.TryParse(value, out d);
        }

        private double? GetDoubleAttribute(XAttribute attr)
        {
            if (attr == null) return null;
            string value = attr.Value;
            if (string.IsNullOrEmpty(value)) return null;
            value = Regex.Replace(value, "[^0-9.-]", "");
            double d;
            if (double.TryParse(value, out d))
            {
                return d;
            }
            else
            {
                return null;
            }
        }

        private Rect? GetRectAttribute(XAttribute attr)
        {
            if (attr == null) return null;
            string value = attr.Value;
            if (string.IsNullOrEmpty(value)) return null;
            value = Regex.Replace(value, "[^0-9 .-]", "");
            try
            {
                return Rect.Parse(value);
            }
            catch
            {
                return null;
            }
        }

        private TransformGroup GetTransformAttribute(XAttribute attr)
        {
            if (attr == null) return null;
            string value = attr.Value;
            if (string.IsNullOrEmpty(value)) return null;
            TransformGroup transformgroup = new TransformGroup();

            var match = Regex.Match(value, @"([^)]+?)\(([^)]+?)\)");
            if (match.Success)
            {
                string transform;
                string transformdata;
                if (match.Groups.Count == 3)
                {
                    transform = match.Groups[1].Value;
                    transformdata = match.Groups[2].Value;

                    switch (transform)
                    {
                        case "matrix":
                        {
                            try
                            {
                                transformgroup.Children.Add(Transform.Parse(transformdata));
                            }
                            catch { }
                            break;
                        }

                        case "translate":
                        {
                            double x, y;
                            var ddd = transformdata.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (ddd.Count() == 1)
                            {
                                if (double.TryParse(ddd[0], out x))
                                    transformgroup.Children.Add(new TranslateTransform(x, 0));
                            }
                            else if (ddd.Count() == 2)
                            {
                                if (double.TryParse(ddd[0], out x) && double.TryParse(ddd[1], out y))
                                    transformgroup.Children.Add(new TranslateTransform(x, y));
                            }
                            break;
                        }

                        case "scale":
                        {
                            double x, y;
                            var ddd = transformdata.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (ddd.Count() == 1)
                            {
                                if (double.TryParse(ddd[0], out x))
                                    transformgroup.Children.Add(new ScaleTransform(x, 0));
                            }
                            else if (ddd.Count() == 2)
                            {
                                if (double.TryParse(ddd[0], out x) && double.TryParse(ddd[1], out y))
                                    transformgroup.Children.Add(new ScaleTransform(x, y));
                            }
                            break;
                        }

                        case "rotate":
                        {
                            double a, x, y;
                            var ddd = transformdata.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (ddd.Count() == 1)
                            {
                                if (double.TryParse(ddd[0], out a))
                                    transformgroup.Children.Add(new RotateTransform(a, 0, 0));
                            }
                            else if (ddd.Count() == 3)
                            {
                                if (double.TryParse(ddd[0], out a) && double.TryParse(ddd[1], out x) && double.TryParse(ddd[2], out y))
                                    transformgroup.Children.Add(new RotateTransform(a, x, y));
                            }
                            break;
                        }

                        case "skewX":
                        {
                            double x;
                            var ddd = transformdata.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (ddd.Count() == 1)
                            {
                                if (double.TryParse(ddd[0], out x))
                                    transformgroup.Children.Add(new SkewTransform(x, 0));
                            }
                            break;
                        }

                        case "skewY":
                        {
                            double y;
                            var ddd = transformdata.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (ddd.Count() == 1)
                            {
                                if (double.TryParse(ddd[0], out y))
                                    transformgroup.Children.Add(new SkewTransform(0, y));
                            }
                            break;
                        }
                    }
                }
            }

            return transformgroup;
        }
    }
}
