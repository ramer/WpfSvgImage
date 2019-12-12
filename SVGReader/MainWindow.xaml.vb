
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO

Class MainWindow

    Public files As New ObservableCollection(Of String)

    Sub New()
        InitializeComponent()
        lbFiles.ItemsSource = files
    End Sub

    Private Sub tbPath_TextChanged(sender As Object, e As TextChangedEventArgs) Handles tbPath.TextChanged
        If Directory.Exists(tbPath.Text) Then
            files.Clear()
            For Each f In Directory.GetFiles(tbPath.Text, "*.svg", SearchOption.TopDirectoryOnly)
                files.Add(f)
            Next
        End If
    End Sub

End Class
