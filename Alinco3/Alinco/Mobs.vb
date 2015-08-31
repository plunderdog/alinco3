Option Strict On

Public Class Mobdata
    Public Class vulninfo
        Public icon As Integer
        Public timecasted As DateTime
        Public duration As Integer = 8 * 60
        Public spellid As Integer
        Public shortname As String
        Public Function getseconds() As Integer

            Dim nSecsLeft As Integer

            If timecasted.Ticks > 0 Then
                Dim diff As TimeSpan
                diff = Now.Subtract(timecasted)
                nSecsLeft = CInt(duration - diff.TotalSeconds)
                If nSecsLeft < 0 Then nSecsLeft = 0
            Else
                nSecsLeft = 0
            End If

            Return nSecsLeft
        End Function
       
        Sub New(ByVal spell As Integer)
            spellid = spell
            timecasted = Now
            duration = 8 * 60
            shortname = String.Empty
            Select Case spellid
                Case 2074, 1156, 1155, 1154 ' imp
                    icon = &H6001385
                    shortname = "Imp"
                Case 2174, &H484 ' piercing
                    icon = &H60013BB
                    shortname = "Pierce"
                Case 2164, 1132, 1131, 1130 ' slash
                    icon = &H60013BC
                    shortname = "Slash"
                Case 2170, 1108, 1107, 1107 ' fire
                    icon = &H6001383
                    shortname = "Fire"
                Case 2168, 1065, 1064, 1063 ' cold
                    icon = &H6001384
                    shortname = "Cold"
                Case 2162, 526, 525, 524 ' acid
                    icon = &H60013B8
                    shortname = "Acid"
                Case 2166, 1053, 1052, 1051 ' bludge
                    icon = &H60013B9
                    shortname = "Bludge"
                Case 2172, 1089, 1088, 1087 ' lighting
                    icon = &H60013BA
                    shortname = "Light"
                Case 2178, &HB0
                    icon = &H6001377
                    shortname = "Fester"
                Case 2282, &H11D
                    icon = &H60013AA
                    shortname = "Yield"
                Case 2320, &H28C
                    icon = &H6001370
                    shortname = "War"
                Case 2103
                    icon = &H60029B8
                    shortname = "Flure"
                Case 834
                    icon = &H60029BD
                    shortname = "Ponas"
                Case 2318, &HEA
                    icon = &H60013AB
                    shortname = "Melee"
                Case &H8B4
                    icon = &H60016C6
                    shortname = "Missle"
                Case &H828, &H53F
                    icon = &H600138C
                    shortname = "Str"
                Case &H808, &H574
                    icon = &H600136C
                    shortname = "Coord"
                Case &H898
                    icon = &H6001369
                    shortname = "Axe"
                Case &H90A
                    icon = &H6001372
                    shortname = "Ua"
                Case &H902
                    icon = &H600138E
                    shortname = "Sword"
                Case &H89C
                    icon = &H600136A
                    shortname = "Bow"
            End Select
        End Sub
    End Class

    Public vulns As Dictionary(Of Integer, vulninfo)
    Public Function hasSpellids(ByVal ids As Integer()) As Boolean

        For Each o As KeyValuePair(Of Integer, vulninfo) In vulns
            Dim nSecsLeft As Integer
            With o.Value
                If .timecasted.Ticks > 0 Then
                    Dim diff As TimeSpan
                    diff = Now.Subtract(.timecasted)
                    nSecsLeft = CInt(.duration - diff.TotalSeconds)
                    If nSecsLeft < 0 Then nSecsLeft = 0
                Else
                    nSecsLeft = 0
                End If

                If nSecsLeft > 0 Then
                    For i As Integer = 0 To UBound(ids)
                        If .spellid = ids(i) Then
                            Return True
                        End If
                    Next
                End If
            End With

        Next

    End Function
    Public Sub UpdateEffect(ByVal effect As Integer, ByVal spellword As String)
        Dim spellid As Integer = 0
        Select Case effect

            Case 23
                If spellword = "Equin Ozael" Then 'Melee
                    spellid = 2318
                ElseIf spellword = "Equin Ofeth" Then 'Missile
                    spellid = &H8B4
                Else
                    ' If spellword = "Equin Opaj" Then 'Yield
                    spellid = 2282
                End If
            Case 38 'fester
                spellid = 2178
            Case 44 'fire
                spellid = 2170
            Case 46 'piercing
                spellid = 2174
            Case 48 'slashing
                spellid = 2164
            Case 50 'acid
                spellid = 2162
            Case 54 'lightning
                spellid = 2172
            Case 52 'cold
                spellid = 2168
            Case 56
                If spellword = "Cruath Quareth" Then 'bludge
                    spellid = 2166
                Else 'imp Cruath Quasith
                    spellid = 2074
                End If
        End Select

        If spellid > 0 Then
            If vulns Is Nothing Then
                vulns = New Dictionary(Of Integer, vulninfo)
            End If
            If Not vulns.ContainsKey(spellid) Then
                vulns.Add(spellid, New vulninfo(spellid))
            Else
                vulns.Item(spellid) = New vulninfo(spellid)
            End If
        End If

    End Sub

End Class
