'
'''<summary>serializeble</summary>
Public Class Location
    Private mLandblock As Integer
    Private mNS, mEW As Double
    Private mX, mEi, mZ As Double
    Private Const GOARROWLINK_ID As Integer = 110011

#Region "properties"
    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property inDungeon() As Boolean
        Get
            'Return CBool((landblock And &HFF00&) > &H100)
            Return Not outdoors
        End Get
    End Property

    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property outdoors() As Boolean
        Get
            Return CBool((landblock And &HFF00&) = 0)
        End Get
    End Property

    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property inBuilding() As Boolean
        Get
            Return CBool((landblock And &HFF00&) = &H100)
        End Get
    End Property

    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property onSurface() As Boolean
        Get
            Return CBool((landblock And &HFF00&) < &H100)
        End Get
    End Property

    <Xml.Serialization.XmlIgnore()> _
    Property ew() As Double
        Get
            If mEW = 0 Then mEW = Longitude(landblock, mX)

            Return mEW
        End Get
        Friend Set(ByVal value As Double)
            mEW = value
        End Set
    End Property

    <Xml.Serialization.XmlIgnore()> _
    Property ns() As Double
        Get
            If mNS = 0 Then mNS = Latitude(landblock, mEi)
            Return mNS
        End Get
        Friend Set(ByVal value As Double)
            mNS = value
        End Set
    End Property

    Property z() As Double
        Get
            Return mZ
        End Get
        Set(ByVal value As Double)
            mZ = value
        End Set
    End Property

    Property y() As Double
        Get
            Return mEi
        End Get
        Set(ByVal value As Double)
            mEi = value
        End Set
    End Property
    Property x() As Double
        Get
            Return mX
        End Get
        Set(ByVal value As Double)
            mX = value
        End Set
    End Property
    Property landblock() As Integer
        Get
            Return mLandblock
        End Get
        Set(ByVal value As Integer)
            mLandblock = value
        End Set
    End Property
#End Region

    Public Function DungeonId() As Integer
        If Not inDungeon Then
            Return 0
        Else
            Dim di As Integer = landblock >> &H10
            Return di Xor &HFFFF0000
        End If

    End Function

    Private Function Latitude(ByVal landblock As Integer, ByVal yoff As Double) As Double 'NS
        Dim l As Long
        l = (landblock And &HFF0000) \ &H2000&
        Return (l + yoff / 24 - 1019.5) / 10
    End Function

    Private Function Longitude(ByVal landblock As Integer, ByVal xoff As Double) As Double 'EW
        Dim l As Long
        l = (landblock And &HFF000000) \ &H200000
        If (l < 0) Then l = l + 2048
        Return (l + xoff / 24 - 1019.5) / 10
    End Function

    Friend Sub Update(ByVal Landblock As Integer, ByVal x As Double, ByVal y As Double, ByVal z As Double)
        mLandblock = Landblock : mX = x : mEi = y : mZ = z
        mEW = Longitude(Landblock, mX)
        mNS = Latitude(Landblock, mEi)
    End Sub
    Sub New()

    End Sub
    Sub New(ByVal Landblock As Integer, ByVal x As Double, ByVal y As Double, ByVal z As Double)
        Update(Landblock, x, y, z)
    End Sub
    Sub New(ByVal Landblock As Integer, ByVal coords As Decal.Adapter.Wrappers.Vector3Object)
        Update(Landblock, coords.X, coords.Y, coords.Z)
    End Sub

    Overrides Function ToString() As String
        Dim r As String
        If (ns > 0) Then
            r = ns.ToString("0.00", Util.NumberFormatInfo) & "N, "
        Else

            r = Math.Abs(ns).ToString("0.00", Util.NumberFormatInfo) & "S, "
        End If

        If (ew > 0) Then
            r = r & ew.ToString("0.00", Util.NumberFormatInfo) & "E"
        Else

            r = r & Math.Abs(ew).ToString("0.00", Util.NumberFormatInfo) & "W"
        End If

        Return r
    End Function

    Overloads Function ToString(ByVal flag As Boolean) As String
        Dim r As String = Me.ToString
        If flag Then
            Return "(" & "<Tell:IIDString:" & GOARROWLINK_ID & ":" & r + ">" & r & "<\Tell>" & ") "
        Else
            Return r
        End If
    End Function
End Class
