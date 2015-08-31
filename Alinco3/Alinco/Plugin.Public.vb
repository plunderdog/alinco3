Imports System.IO

Partial Public Class Plugin
    Private mPaused As Boolean
    Private Shared minstance As Plugin
    Public Shared ReadOnly Property Instance() As Plugin
        Get
            Return minstance
        End Get
    End Property

    Public ReadOnly Property Paused() As Boolean
        Get
            Return mPaused
        End Get
    End Property

    Public ReadOnly Property CurrentContainer() As Integer
        Get
            Return mCurrentContainer
        End Get
    End Property

    Public Function GetLootDecision(ByVal id As Integer, ByVal reserved1 As Integer, ByVal reserved2 As Integer) As Integer
        Try
            If reserved2 = 1 Then
                If mCorpseWithRareId <> 0 AndAlso (reserved1 = mCorpseWithRareId) Then
                    Return 1
                Else
                    Return 0
                End If
            End If
            If mNotifiedItems.ContainsKey(id) Then
                Dim n As notify = CType(mNotifiedItems.Item(id), Global.Alinco.Plugin.notify)

                If n.scantype = eScanresult.salvage Then
                    If mPluginConfig.AutoUst Then
                        Return 1 ' 2 = salvage but let alinco do the salvaging
                    Else
                        Return 2
                    End If

                Else
                    Return 1
                End If
            Else
                Dim wo As Decal.Adapter.Wrappers.WorldObject
                wo = Core.WorldFilter.Item(id)
                If wo IsNot Nothing Then
                    Dim no As New IdentifiedObject(wo)

                    Dim result As eScanresult = CheckObjectForMatch(no, False)
                    If result = eScanresult.salvage Then
                        If mPluginConfig.AutoUst Then
                            Return 1 ' 2 = salvage but let alinco do the salvaging
                        Else
                            Return 2
                        End If
                    ElseIf result <> eScanresult.nomatch Then

                        Return 1
                    End If
                End If
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

    Public Function LootNext(ByVal corpse As Boolean, ByVal landscape As Boolean, ByVal maxrange As Double, ByVal maxz As Integer) As Integer
        Try
            If mwaitonopen Then
                If Not DateDiff(DateInterval.Second, mtryopenstart, Now) > 4 Then
                    Return False
                End If
                mwaitonopen = False
            End If

            If hotkeyloot(landscape, maxrange, maxz) Then
                Return 1
            End If

            If mPluginConfig.AutoUst Then
                If AutoUst() Then
                    Return 1
                End If
            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

End Class
