
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
                    UpdateSvgData(stream)
                End Using
            Else
                Using stream = New FileStream(path, FileMode.Open)
                    UpdateSvgData(stream)
                End Using
            End If
        Catch ex As Exception
            Data = New GeometryGroup
        End Try
    End Sub

    Private Sub UpdateSvgData(stream As Stream)
        Dim readersettings As New XmlReaderSettings With {.DtdProcessing = DtdProcessing.Ignore} ' .XmlResolver = Nothing,
        Dim geometrygroup As New GeometryGroup With {.FillRule = FillRule.Nonzero}

        Dim gotwidth As Boolean
        Dim newwidth As Double
        Dim gotheight As Boolean
        Dim newheight As Double
        Dim gotviewbox As Boolean
        Dim newviewbox As Rect

        Using reader = XmlReader.Create(stream, readersettings)

            While reader.Read
                If Not reader.NodeType = XmlNodeType.Element Then Continue While

                If reader.Name.ToLower = "svg" Then
                    gotwidth = GetDoubleAttribute(reader, "width", newwidth)
                    gotheight = GetDoubleAttribute(reader, "height", newheight)
                    gotviewbox = GetRectAttribute(reader, "viewBox", newviewbox)
                End If

                If reader.Name.ToLower = "path" Then
                    Try
                        Dim attr = reader.GetAttribute("d")
                        If Not String.IsNullOrEmpty(attr) Then geometrygroup.Children.Add(Geometry.Parse(attr))
                    Catch
                    End Try
                End If

                If reader.Name.ToLower = "line" Then
                    Dim x1, y1, x2, y2 As Double
                    If GetDoubleAttribute(reader, "x1", x1) AndAlso
                        GetDoubleAttribute(reader, "y1", y1) AndAlso
                        GetDoubleAttribute(reader, "x2", x2) AndAlso
                        GetDoubleAttribute(reader, "y2", y2) Then geometrygroup.Children.Add(New LineGeometry(New Point(x1, y2), New Point(x2, y2)))
                End If

                If reader.Name.ToLower = "rect" Then
                    Dim x, y, w, h As Double
                    If GetDoubleAttribute(reader, "x", x) AndAlso
                        GetDoubleAttribute(reader, "y", y) AndAlso
                        GetDoubleAttribute(reader, "width", w) AndAlso
                        GetDoubleAttribute(reader, "height", h) Then geometrygroup.Children.Add(New RectangleGeometry(New Rect(x, y, w, h)))
                End If

                If reader.Name.ToLower = "ellipse" Then
                    Dim cx, cy, rx, ry As Double
                    If GetDoubleAttribute(reader, "cx", cx) AndAlso
                        GetDoubleAttribute(reader, "cy", cy) AndAlso
                        GetDoubleAttribute(reader, "rx", rx) AndAlso
                        GetDoubleAttribute(reader, "ry", ry) Then geometrygroup.Children.Add(New EllipseGeometry(New Point(cx, cy), rx, ry))
                End If

                If reader.Name.ToLower = "circle" Then
                    Dim cx, cy, r As Double
                    If GetDoubleAttribute(reader, "cx", cx) AndAlso
                        GetDoubleAttribute(reader, "cy", cy) AndAlso
                        GetDoubleAttribute(reader, "r", r) Then geometrygroup.Children.Add(New EllipseGeometry(New Point(cx, cy), r, r))
                End If

                If reader.Name.ToLower = "polyline" Then
                    Try
                        Dim polyline As New Polyline
                        polyline.Points = PointCollection.Parse(reader.GetAttribute("points"))
                        polyline.Measure(ViewBox.Size)
                        polyline.Arrange(ViewBox)
                        geometrygroup.Children.Add(polyline.RenderedGeometry)
                    Catch
                    End Try
                End If

                If reader.Name.ToLower = "polygon" Then
                    Try
                        Dim polygon As New Polygon
                        polygon.Points = PointCollection.Parse(reader.GetAttribute("points"))
                        polygon.Measure(ViewBox.Size)
                        polygon.Arrange(ViewBox)
                        geometrygroup.Children.Add(polygon.RenderedGeometry)
                    Catch
                    End Try
                End If
            End While
        End Using

        Data = geometrygroup
        Clip = If(gotviewbox, New RectangleGeometry(newviewbox), Nothing)
        Width = If(gotviewbox, newviewbox.Right, If(gotwidth, newwidth, Double.NaN))
        Height = If(gotviewbox, newviewbox.Bottom, If(gotheight, newheight, Double.NaN))
        ViewBox = If(gotviewbox, newviewbox, Nothing)
        TranslateTransform.X = If(gotviewbox, -_viewbox.X, 0)
        TranslateTransform.Y = If(gotviewbox, -_viewbox.Y, 0)
        ScaleTransform.ScaleX = If(gotviewbox, _viewbox.Right / _viewbox.Width, 1)
        ScaleTransform.ScaleY = If(gotviewbox, _viewbox.Bottom / _viewbox.Height, 1)
    End Sub

    Private Function GetDoubleAttribute(reader As XmlReader, attr As String, ByRef d As Double) As Boolean
        Dim strvalue As String = reader.GetAttribute(attr)
        If String.IsNullOrEmpty(strvalue) Then Return False
        strvalue = Regex.Replace(strvalue, "[^0-9.-]", "")
        Return Double.TryParse(strvalue, d)
    End Function

    Private Function GetRectAttribute(reader As XmlReader, attr As String, ByRef r As Rect) As Boolean
        Dim strvalue As String = reader.GetAttribute(attr)
        If String.IsNullOrEmpty(strvalue) Then Return False
        strvalue = Regex.Replace(strvalue, "[^0-9 .-]", "")
        Try
            r = Rect.Parse(strvalue)
            Return True
        Catch
            Return False
        End Try
    End Function

End Class
