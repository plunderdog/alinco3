'easy serializeble

Public Class Integerlist
    Inherits CollectionBase

    Public Overridable Function Add(ByVal value As Integer) As Integer
        If MyBase.List.Contains(value) = False Then
            Return MyBase.List.Add(value)
        End If
    End Function

    Public Overridable Function Contains(ByVal value As Integer) As Boolean
        Return MyBase.List.Contains(value)
    End Function

    Public Overridable Sub Remove(ByVal value As Integer)
        If MyBase.List.Contains(value) Then
            MyBase.List.Remove(value)
        End If
    End Sub

    Default Public Overridable Property Item(ByVal index As Integer) As Integer
        Get
            Return DirectCast(MyBase.List.Item(index), Integer)
        End Get
        Set(ByVal value As Integer)
            MyBase.List.Item(index) = value
        End Set
    End Property

End Class