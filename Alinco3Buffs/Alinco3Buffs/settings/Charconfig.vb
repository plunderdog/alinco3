'settings per character
Public Enum eConsumableType
    Food = 0
    ManaStone
    Gem
    HealingKit
End Enum

Public Class consumable
    Public icon As Integer
    Public Constype As eConsumableType
    Public vitalId As Integer
    Public Amt As Integer
    <Xml.Serialization.XmlIgnore()> _
    Friend expired As Boolean
End Class

Public Class CharConfig
    Public PendingbuffsTimeout As Integer = 120
    Public ArchmageEnduranceAugmentation As Integer = 5
    Public buffingwandid As Integer
    Public fallback As Boolean = True
    Public lifemagiclevel As Integer
    Public itemmagiclevel As Integer
    Public creaturemagiclevel As Integer
    Public RegenManapct As Integer
    Public RegenStaminapct As Integer
    Public minmanaForCasting As Integer
    ' Public usemanapots As Boolean
    Public usehealthtomana As Boolean
    Public filtercastself As Boolean
    Public Fastcasting As Boolean = True
    Public simplehud As Boolean = False
    Public profile As Integer
    Public consumables As SDictionary(Of String, consumable)
    Public buffs() As Buffprofile
    Public magiclevel8 As Integer
    Public magiclevel7 As Integer
    Public magiclevel6 As Integer
    Public magiclevel5 As Integer
    Public magiclevel4 As Integer

    Public ItemTimers() As itemBuffInfo
    Public Botoptions As Integerlist
    Public buffbotmaxrange As Integer = 15
#Region "Properties"

    <Xml.Serialization.XmlIgnore()> _
  Public ReadOnly Property selfbufflife() As Integerlist
        Get
            Return buffs(profile).selfbufflife
        End Get
    End Property
    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property selfbuffcreature() As Integerlist
        Get
            Return buffs(profile).selfbuffcreature
        End Get
    End Property
    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property selfbuffarmor() As Integerlist
        Get
            Return buffs(profile).selfbuffarmor
        End Get
    End Property
    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property selfbuffbanes() As Integerlist
        Get
            Return buffs(profile).selfbuffbanes
        End Get
    End Property
    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property selfbuffweapons() As Integerlist
        Get
            Return buffs(profile).selfbuffweapons
        End Get
    End Property
    <Xml.Serialization.XmlIgnore()> _
    Public ReadOnly Property selfbuffweaponbuffs() As SDictionary(Of Integer, Integerlist)
        Get
            Return buffs(profile).selfbuffweaponbuffs
        End Get
    End Property

#End Region

    Public Sub New()
        'default values only, this sub isn't called at deserialize

        ReDim buffs(9)
        For i As Integer = 0 To 9
            buffs(i) = New Buffprofile
            buffs(i).Profilename = "#" & i
        Next

        RegenManapct = 10
        RegenStaminapct = 70
        minmanaForCasting = 10
        'usemanapots = True
        usehealthtomana = True
        lifemagiclevel = 0
        itemmagiclevel = 0
        creaturemagiclevel = 0
        magiclevel8 = 400
        magiclevel7 = 300
        magiclevel6 = 250
        magiclevel5 = 225
        magiclevel4 = 200

    End Sub

    Public Sub validateProfiles()

        Dim i As Integer = 1

        If buffs Is Nothing Then
            ReDim buffs(9)
        Else
            If UBound(buffs) < 9 Then
                ReDim Preserve buffs(9)
            End If
        End If

        For i = 0 To 9
            If buffs(i) Is Nothing Then buffs(i) = New Buffprofile
            buffs(i).Profilename = "#" & i
        Next

        If profile > buffs.Length Then
            profile = 0
        End If

    End Sub

    Public Class Buffprofile
        Public Profilename As String
        Public selfbufflife As Integerlist
        Public selfbuffcreature As Integerlist
        Public selfbuffarmor As Integerlist
        Public selfbuffbanes As Integerlist
        Public selfbuffweapons As Integerlist


        Public selfbuffweaponbuffs As SDictionary(Of Integer, Integerlist)

        Public Sub New()
            selfbufflife = New Integerlist
            selfbuffcreature = New Integerlist
            selfbuffarmor = New Integerlist
            selfbuffweapons = New Integerlist
            selfbuffbanes = New Integerlist

            selfbuffweaponbuffs = New SDictionary(Of Integer, Integerlist)
        End Sub
    End Class

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

    Public Structure itemBuffInfo
        Public spellId As Integer
        Public targetId As Integer
        Public secondsremaining As Integer
        Public playerAgeCasted As Integer
    End Structure
End Class



