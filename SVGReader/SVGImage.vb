
Imports System.ComponentModel
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Xml

Public Class SvgImage
    Inherits Shape

    Protected Overrides ReadOnly Property DefiningGeometry As Geometry
        Get
            Return GetValue(DataProperty)
        End Get
    End Property

    Private TransformGroup As New TransformGroup
    Private TranslateTransform As New TranslateTransform
    Private ScaleTransform As New ScaleTransform

    Sub New()
        MyBase.New
        TransformGroup.Children.Add(TranslateTransform)
        TransformGroup.Children.Add(ScaleTransform)
        RenderTransform = TransformGroup
    End Sub

    Private _viewbox As Rect
    Private Property ViewBox As Rect
        Get
            Return _viewbox
        End Get
        Set(value As Rect)
            _viewbox = value
        End Set
    End Property

    Public Shared ReadOnly SourceProperty As DependencyProperty = DependencyProperty.Register("Source", GetType(Uri), GetType(SvgImage), New PropertyMetadata(AddressOf SourcePropertyChanged))

    Private Shared Sub SourcePropertyChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        CType(d, SvgImage).UpdateData(e.NewValue)
    End Sub

    Public Property Source As Uri
        Get
            Return GetValue(SourceProperty)
        End Get

        Set(ByVal value As Uri)
            SetValue(SourceProperty, value)
        End Set
    End Property

    Public Shared ReadOnly DataPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("Data", GetType(Geometry), GetType(SvgImage), New FrameworkPropertyMetadata(New GeometryGroup, FrameworkPropertyMetadataOptions.AffectsRender))
    Public Shared ReadOnly DataProperty As DependencyProperty = DataPropertyKey.DependencyProperty

    Public Overloads Property Data As Geometry
        Get
            Return GetValue(DataProperty)
        End Get
        Protected Set(ByVal value As Geometry)
            SetValue(DataPropertyKey, value)
            UpdateLayout()
        End Set
    End Property

    Public Shared ReadOnly WidthPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("Width", GetType(Double), GetType(SvgImage), New PropertyMetadata(Double.NaN))
    Public Shared Shadows ReadOnly WidthProperty As DependencyProperty = WidthPropertyKey.DependencyProperty

    Public Overloads Property Width As Double
        Get
            Return GetValue(WidthProperty)
        End Get
        Protected Set(ByVal value As Double)
            MyBase.Width = value
            SetValue(WidthPropertyKey, value)
        End Set
    End Property

    Public Shared ReadOnly HeightPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("Height", GetType(Double), GetType(SvgImage), New PropertyMetadata(Double.NaN))
    Public Shared Shadows ReadOnly HeightProperty As DependencyProperty = HeightPropertyKey.DependencyProperty

    Public Overloads Property Height As Double
        Get
            Return GetValue(HeightProperty)
        End Get
        Protected Set(ByVal value As Double)
            MyBase.Height = value
            SetValue(HeightPropertyKey, value)
        End Set
    End Property

    Private Sub UpdateData(uri As Uri)
        If uri Is Nothing Then Data = New GeometryGroup : Exit Sub

        Try
            Dim path As String = String.Empty
            Try
                path = uri.LocalPath
            Catch
            End Try

            If Not File.Exists(path) Then
                Using stream = Windows.Application.GetResourceStream(uri).Stream
                    UpdateDataFromStream(stream)
                End Using
            Else
                Using stream = New FileStream(path, FileMode.Open)
                    UpdateDataFromStream(stream)
                End Using
            End If
        Catch ex As Exception
            Data = New GeometryGroup
        End Try
    End Sub

    Private Sub UpdateDataFromStream(stream As Stream)
        Dim readersettings As New XmlReaderSettings With {.DtdProcessing = DtdProcessing.Ignore} ' .XmlResolver = Nothing,

        Using reader = XmlReader.Create(stream, readersettings)

            reader.MoveToContent()

            Dim node = XNode.ReadFrom(reader)
            If node.NodeType = XmlNodeType.Element Then Data = ReadSvgNode(node)

        End Using
    End Sub

    Private Function ReadSvgNode(ByVal node As XElement) As GeometryGroup
        Dim result As New GeometryGroup With {.FillRule = FillRule.Nonzero}

        If node.Name.LocalName = "svg" Then
            Dim gotwidth As Boolean
            Dim newwidth As Double
            Dim gotheight As Boolean
            Dim newheight As Double
            Dim gotviewbox As Boolean
            Dim newviewbox As Rect

            For Each attr In node.Attributes
                If attr.Name.LocalName = "width" Then gotwidth = GetDoubleAttribute(attr.Value, newwidth)
                If attr.Name.LocalName = "height" Then gotheight = GetDoubleAttribute(attr.Value, newheight)
                If attr.Name.LocalName = "viewBox" Then gotviewbox = GetRectAttribute(attr.Value, newviewbox)
            Next

            Clip = If(gotviewbox, New RectangleGeometry(newviewbox), Nothing)
            Width = If(gotviewbox, newviewbox.Right, If(gotwidth, newwidth, Double.NaN))
            Height = If(gotviewbox, newviewbox.Bottom, If(gotheight, newheight, Double.NaN))
            ViewBox = If(gotviewbox, newviewbox, Nothing)
            TranslateTransform.X = If(gotviewbox, -_viewbox.X, 0)
            TranslateTransform.Y = If(gotviewbox, -_viewbox.Y, 0)
            ScaleTransform.ScaleX = If(gotviewbox, _viewbox.Right / _viewbox.Width, 1)
            ScaleTransform.ScaleY = If(gotviewbox, _viewbox.Bottom / _viewbox.Height, 1)
        End If

        If node.Name.LocalName = "path" Then
            For Each attr In node.Attributes
                If attr.Name.LocalName = "d" AndAlso Not String.IsNullOrEmpty(attr.Value) Then
                    Try
                        result.Children.Add(Geometry.Parse(attr.Value))
                    Catch
                    End Try
                End If
            Next
        End If

        If node.Name.LocalName = "line" Then
            Dim x1, y1, x2, y2 As Double
            If GetDoubleAttribute(node, "x1", x1) AndAlso
                GetDoubleAttribute(node, "y1", y1) AndAlso
                GetDoubleAttribute(node, "x2", x2) AndAlso
                GetDoubleAttribute(node, "y2", y2) Then result.Children.Add(New LineGeometry(New Point(x1, y2), New Point(x2, y2)))
        End If

        If node.Name.LocalName = "rect" Then
            Dim x, y, w, h As Double
            If GetDoubleAttribute(node, "x", x) AndAlso
                GetDoubleAttribute(node, "y", y) AndAlso
                GetDoubleAttribute(node, "width", w) AndAlso
                GetDoubleAttribute(node, "height", h) Then result.Children.Add(New RectangleGeometry(New Rect(x, y, w, h)))
        End If

        If node.Name.LocalName = "ellipse" Then
            Dim cx, cy, rx, ry As Double
            If GetDoubleAttribute(node, "cx", cx) AndAlso
                GetDoubleAttribute(node, "cy", cy) AndAlso
                GetDoubleAttribute(node, "rx", rx) AndAlso
                GetDoubleAttribute(node, "ry", ry) Then result.Children.Add(New EllipseGeometry(New Point(cx, cy), rx, ry))
        End If

        If node.Name.LocalName = "circle" Then
            Dim cx, cy, r As Double
            If GetDoubleAttribute(node, "cx", cx) AndAlso
                GetDoubleAttribute(node, "cy", cy) AndAlso
                GetDoubleAttribute(node, "r", r) Then result.Children.Add(New EllipseGeometry(New Point(cx, cy), r, r))
        End If

        If node.Name.LocalName = "polyline" Then
            Try
                Dim polyline As New Polyline
                Dim attr As XAttribute = node.Attribute("points")
                If attr IsNot Nothing AndAlso Not String.IsNullOrEmpty(attr.Value) Then
                    polyline.Points = PointCollection.Parse(attr.Value)
                    polyline.Measure(ViewBox.Size)
                    polyline.Arrange(ViewBox)
                    result.Children.Add(polyline.RenderedGeometry)
                End If
            Catch
            End Try
        End If

        If node.Name.LocalName = "polygon" Then
            Try
                Dim polygon As New Polygon
                Dim attr As XAttribute = node.Attribute("points")
                If attr IsNot Nothing AndAlso Not String.IsNullOrEmpty(attr.Value) Then
                    polygon.Points = PointCollection.Parse(attr.Value)
                    polygon.Measure(ViewBox.Size)
                    polygon.Arrange(ViewBox)
                    result.Children.Add(polygon.RenderedGeometry)
                End If
            Catch
            End Try
        End If

        For Each child As XElement In node.Elements
            result.Children.Add(ReadSvgNode(child))
        Next

        Return result
    End Function


    Private Function GetDoubleAttribute(element As XElement, name As String, ByRef d As Double) As Boolean
        Dim attr As XAttribute = element.Attribute(name)
        If attr Is Nothing Then Return False
        Dim value As String = attr.Value
        If String.IsNullOrEmpty(value) Then Return False
        value = Regex.Replace(value, "[^0-9.-]", "")
        Return Double.TryParse(value, d)
    End Function

    Private Function GetDoubleAttribute(value As String, ByRef d As Double) As Boolean
        If String.IsNullOrEmpty(value) Then Return False
        value = Regex.Replace(value, "[^0-9.-]", "")
        Return Double.TryParse(value, d)
    End Function

    Private Function GetRectAttribute(value As String, ByRef r As Rect) As Boolean
        If String.IsNullOrEmpty(value) Then Return False
        value = Regex.Replace(value, "[^0-9 .-]", "")
        Try
            r = Rect.Parse(value)
            Return True
        Catch
            Return False
        End Try
    End Function

End Class
