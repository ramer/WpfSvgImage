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

    Private transformGroup As New TransformGroup
    Private translateTransform As New TranslateTransform
    Private scaleTransform As New ScaleTransform

    Sub New()
        MyBase.New
        transformGroup.Children.Add(translateTransform)
        transformGroup.Children.Add(scaleTransform)
        RenderTransform = transformGroup
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

    Public Property Data As Geometry
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

    Public Shared ReadOnly ViewBoxWidthPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("ViewBoxWidth", GetType(Double), GetType(SvgImage), New PropertyMetadata(Double.NaN))
    Public Shared ReadOnly ViewBoxWidthProperty As DependencyProperty = ViewBoxWidthPropertyKey.DependencyProperty

    Public Property ViewBoxWidth As Double
        Get
            Return GetValue(ViewBoxWidthProperty)
        End Get
        Protected Set(ByVal value As Double)
            SetValue(ViewBoxWidthPropertyKey, value)
        End Set
    End Property

    Public Shared ReadOnly ViewBoxHeightPropertyKey As DependencyPropertyKey = DependencyProperty.RegisterReadOnly("ViewBoxHeight", GetType(Double), GetType(SvgImage), New PropertyMetadata(Double.NaN))
    Public Shared ReadOnly ViewBoxHeightProperty As DependencyProperty = ViewBoxHeightPropertyKey.DependencyProperty

    Public Property ViewBoxHeight As Double
        Get
            Return GetValue(ViewBoxHeightProperty)
        End Get
        Protected Set(ByVal value As Double)
            SetValue(ViewBoxHeightPropertyKey, value)
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
                Dim sri = Windows.Application.GetResourceStream(uri)
                Using stream = sri.Stream
                    UpdateDataFromStream(stream)
                End Using
            Else
                Using stream = New FileStream(path, FileMode.Open)
                    UpdateDataFromStream(stream)
                End Using
            End If
        Catch
            Data = New GeometryGroup
        End Try
    End Sub

    Private Sub UpdateDataFromStream(stream As Stream)
        Dim readersettings As New XmlReaderSettings With {.DtdProcessing = DtdProcessing.Ignore} ' .XmlResolver = Nothing,

        Using reader = XmlReader.Create(stream, readersettings)

            reader.MoveToContent()

            Dim node = XNode.ReadFrom(reader)
            Data = ReadSvgNode(node)

        End Using
    End Sub

    Private Function ReadSvgNode(ByVal node As XElement) As Geometry
        Dim result As Geometry = Nothing

        Select Case node.Name.LocalName.ToLower
            Case "svg"
                Dim newwidth As Double?
                Dim newheight As Double?
                Dim newviewbox As Rect?

                newwidth = GetDoubleAttribute(node.Attribute("width"))
                newheight = GetDoubleAttribute(node.Attribute("height"))
                newviewbox = GetRectAttribute(node.Attribute("viewBox"))

                Clip = If(newviewbox.HasValue, New RectangleGeometry(newviewbox), Nothing)
                Width = If(newviewbox.HasValue, newviewbox.Value.Right, If(newwidth.HasValue, newwidth.Value, Double.NaN))
                Height = If(newviewbox.HasValue, newviewbox.Value.Bottom, If(newheight.HasValue, newheight.Value, Double.NaN))
                ViewBox = If(newviewbox.HasValue, newviewbox.Value, Rect.Empty)
                ViewBoxWidth = If(newviewbox.HasValue, newviewbox.Value.Width, Double.NaN)
                ViewBoxHeight = If(newviewbox.HasValue, newviewbox.Value.Height, Double.NaN)
                translateTransform.X = If(newviewbox.HasValue, -newviewbox.Value.X, 0)
                translateTransform.Y = If(newviewbox.HasValue, -newviewbox.Value.Y, 0)
                scaleTransform.ScaleX = If(newviewbox.HasValue, newviewbox.Value.Right / newviewbox.Value.Width, 1)
                scaleTransform.ScaleY = If(newviewbox.HasValue, newviewbox.Value.Bottom / newviewbox.Value.Height, 1)

                Dim geometrygroup As New GeometryGroup With {.FillRule = FillRule.Nonzero}
                If node.HasElements Then
                    For Each child As XElement In node.Elements
                        Dim geometry = ReadSvgNode(child)
                        If geometry IsNot Nothing Then geometrygroup.Children.Add(geometry)
                    Next
                End If
                result = geometrygroup

            Case "g"
                Dim geometrygroup As New GeometryGroup With {.FillRule = FillRule.Nonzero}
                If node.HasElements Then
                    For Each child As XElement In node.Elements
                        Dim geometry = ReadSvgNode(child)
                        If geometry IsNot Nothing Then geometrygroup.Children.Add(geometry)
                    Next
                End If
                result = geometrygroup

            Case "path"
                For Each attr In node.Attributes
                    If attr.Name.LocalName = "d" AndAlso Not String.IsNullOrEmpty(attr.Value) Then
                        Try
                            Return Geometry.Parse(attr.Value)
                        Catch
                        End Try
                    End If
                Next

            Case "line"
                Dim x1, y1, x2, y2 As Double
                If GetDoubleAttribute(node.Attribute("x1"), x1) AndAlso
                    GetDoubleAttribute(node.Attribute("y1"), y1) AndAlso
                    GetDoubleAttribute(node.Attribute("x2"), x2) AndAlso
                    GetDoubleAttribute(node.Attribute("y2"), y2) Then result = New LineGeometry(New Point(x1, y2), New Point(x2, y2))

            Case "rect"
                Dim x, y, w, h As Double
                If GetDoubleAttribute(node.Attribute("x"), x) AndAlso
                    GetDoubleAttribute(node.Attribute("y"), y) AndAlso
                    GetDoubleAttribute(node.Attribute("width"), w) AndAlso
                    GetDoubleAttribute(node.Attribute("height"), h) Then result = New RectangleGeometry(New Rect(x, y, w, h))

            Case "ellipse"
                Dim cx, cy, rx, ry As Double
                If GetDoubleAttribute(node.Attribute("cx"), cx) AndAlso
                    GetDoubleAttribute(node.Attribute("cy"), cy) AndAlso
                    GetDoubleAttribute(node.Attribute("rx"), rx) AndAlso
                    GetDoubleAttribute(node.Attribute("ry"), ry) Then result = New EllipseGeometry(New Point(cx, cy), rx, ry)

            Case "circle"
                Dim cx, cy, r As Double
                If GetDoubleAttribute(node.Attribute("cx"), cx) AndAlso
                    GetDoubleAttribute(node.Attribute("cy"), cy) AndAlso
                    GetDoubleAttribute(node.Attribute("r"), r) Then result = New EllipseGeometry(New Point(cx, cy), r, r)

            Case "polyline"
                Try
                    Dim polyline As New Polyline
                    Dim attr As XAttribute = node.Attribute("points")
                    If attr IsNot Nothing AndAlso Not String.IsNullOrEmpty(attr.Value) Then
                        polyline.Points = PointCollection.Parse(attr.Value)
                        polyline.Measure(ViewBox.Size)
                        polyline.Arrange(ViewBox)
                        result = polyline.RenderedGeometry
                    End If
                Catch
                End Try

            Case "polygon"
                Try
                    Dim polygon As New Polygon
                    Dim attr As XAttribute = node.Attribute("points")
                    If attr IsNot Nothing AndAlso Not String.IsNullOrEmpty(attr.Value) Then
                        polygon.Points = PointCollection.Parse(attr.Value)
                        polygon.Measure(ViewBox.Size)
                        polygon.Arrange(ViewBox)
                        result = polygon.RenderedGeometry
                    End If
                Catch
                End Try
        End Select

        If result IsNot Nothing Then result.Transform = GetTransformAttribute(node.Attribute("transform"))

        Return result
    End Function

    Private Function GetDoubleAttribute(attr As XAttribute, ByRef d As Double) As Boolean
        If attr Is Nothing Then Return False
        Dim value As String = attr.Value
        If String.IsNullOrEmpty(value) Then Return False
        value = Regex.Replace(value, "[^0-9.-]", "")
        Return Double.TryParse(value, d)
    End Function

    Private Function GetDoubleAttribute(attr As XAttribute) As Double?
        If attr Is Nothing Then Return Nothing
        Dim value As String = attr.Value
        If String.IsNullOrEmpty(value) Then Return Nothing
        value = Regex.Replace(value, "[^0-9.-]", "")
        Dim d As Double
        Return If(Double.TryParse(value, d), d, Nothing)
    End Function

    Private Function GetRectAttribute(attr As XAttribute) As Rect?
        If attr Is Nothing Then Return Nothing
        Dim value As String = attr.Value
        If String.IsNullOrEmpty(value) Then Return Nothing
        value = Regex.Replace(value, "[^0-9 .-]", "")
        Try
            Return Rect.Parse(value)
        Catch
            Return Nothing
        End Try
    End Function

    Private Function GetTransformAttribute(attr As XAttribute) As TransformGroup
        If attr Is Nothing Then Return Nothing
        Dim value As String = attr.Value
        If String.IsNullOrEmpty(value) Then Return Nothing
        Dim transformgroup As New TransformGroup

        Dim match = Regex.Match(value, "([^)]+?)\(([^)]+?)\)")
        If match.Success Then
            Dim transform As String
            Dim transformdata As String
            If match.Groups.Count = 3 Then
                transform = match.Groups(1).Value
                transformdata = match.Groups(2).Value

                Select Case transform
                    Case "matrix"
                        Try
                            transformgroup.Children.Add(Media.Transform.Parse(transformdata))
                        Catch
                        End Try
                    Case "translate"
                        Dim x, y As Double
                        Dim ddd = transformdata.Split({" ", ","}, StringSplitOptions.RemoveEmptyEntries)
                        If ddd.Count = 1 Then
                            If Double.TryParse(ddd(0), x) Then
                                transformgroup.Children.Add(New TranslateTransform(x, 0))
                            End If
                        ElseIf ddd.Count = 2 Then
                            If Double.TryParse(ddd(0), x) AndAlso Double.TryParse(ddd(1), y) Then
                                transformgroup.Children.Add(New TranslateTransform(x, y))
                            End If
                        End If
                    Case "scale"
                        Dim x, y As Double
                        Dim ddd = transformdata.Split({" ", ","}, StringSplitOptions.RemoveEmptyEntries)
                        If ddd.Count = 1 Then
                            If Double.TryParse(ddd(0), x) Then
                                transformgroup.Children.Add(New ScaleTransform(x, 0))
                            End If
                        ElseIf ddd.Count = 2 Then
                            If Double.TryParse(ddd(0), x) AndAlso Double.TryParse(ddd(1), y) Then
                                transformgroup.Children.Add(New ScaleTransform(x, y))
                            End If
                        End If
                    Case "rotate"
                        Dim a, x, y As Double
                        Dim ddd = transformdata.Split({" ", ","}, StringSplitOptions.RemoveEmptyEntries)
                        If ddd.Count = 1 Then
                            If Double.TryParse(ddd(0), a) Then
                                transformgroup.Children.Add(New RotateTransform(a, 0, 0))
                            End If
                        ElseIf ddd.Count = 3 Then
                            If Double.TryParse(ddd(0), a) AndAlso Double.TryParse(ddd(1), x) AndAlso Double.TryParse(ddd(2), y) Then
                                transformgroup.Children.Add(New RotateTransform(a, x, y))
                            End If
                        End If
                    Case "skewX"
                        Dim x As Double
                        Dim ddd = transformdata.Split({" ", ","}, StringSplitOptions.RemoveEmptyEntries)
                        If ddd.Count = 1 Then
                            If Double.TryParse(ddd(0), x) Then
                                transformgroup.Children.Add(New SkewTransform(x, 0))
                            End If
                        End If
                    Case "skewY"
                        Dim y As Double
                        Dim ddd = transformdata.Split({" ", ","}, StringSplitOptions.RemoveEmptyEntries)
                        If ddd.Count = 1 Then
                            If Double.TryParse(ddd(0), y) Then
                                transformgroup.Children.Add(New SkewTransform(0, y))
                            End If
                        End If
                End Select

            End If
        End If

        Return transformgroup
    End Function


End Class
