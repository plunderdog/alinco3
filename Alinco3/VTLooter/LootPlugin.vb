Imports uTank2.LootPlugins
Imports System.IO

Public Class LootPlugin
    Inherits LootPluginBase

    Private mAlincoBase As Alinco.Plugin
    Private mAlincoLoaded As Boolean

    Private Sub TryGetAlincoInstance()
        mAlincoLoaded = False

        Try
            mAlincoBase = Alinco.Plugin.Instance

            If mAlincoBase IsNot Nothing Then
                mAlincoLoaded = True
            End If

        Catch ex As Exception
            'LogToFile("log.txt", ex.Message)
        End Try
    End Sub

    Private Function AlincoRules(ByVal id As Integer, ByVal r1 As Integer, ByVal r2 As Integer) As uTank2.LootPlugins.LootAction
        Try
            '      LogToFile("log.txt", "AlincoRules " & Hex(id))
            Dim i As Integer = mAlincoBase.GetLootDecision(id, r1, r2)
            '       LogToFile("log.txt", name & " AlincoRules=>GetLootDecision " & Hex(id) & " returned " & i)
            Select Case i
                Case 0
                    Return uTank2.LootPlugins.LootAction.NoLoot
                Case 1
                    Return uTank2.LootPlugins.LootAction.Keep
                Case 2
                    Return uTank2.LootPlugins.LootAction.Salvage
            End Select
        Catch ex As Exception
            '        LogToFile("log.txt", "AlincoRules")
        End Try
        Return uTank2.LootPlugins.LootAction.NoLoot
    End Function


    Public Overrides Sub CloseEditorForProfile()

    End Sub

    Public Overrides Function DoesPotentialItemNeedID(ByVal item As uTank2.LootPlugins.GameItemInfo) As Boolean
        Try

            ' HACK check item.container = corpswithrareid
            ' TODO add a public function DoesPotentialItemNeedID to alinco
            Dim result As Integer = mAlincoBase.GetLootDecision(item.Id, item.GetValueInt(IntValueKey.Container, 0), 1)
            If result = 1 Then
                Return True
            End If

            Select Case item.ObjectClass
                Case ObjectClass.MissileWeapon
                    Dim t As Integer = item.GetValueInt(IntValueKey.MissileType, 0)
                    If t = 1 OrElse t = 2 OrElse t = 4 Then
                        Return True
                    End If
                Case uTank2.LootPlugins.ObjectClass.Armor, uTank2.LootPlugins.ObjectClass.Clothing, uTank2.LootPlugins.ObjectClass.Jewelry, uTank2.LootPlugins.ObjectClass.MeleeWeapon, uTank2.LootPlugins.ObjectClass.MissileWeapon, uTank2.LootPlugins.ObjectClass.WandStaffOrb
                    Return True
            End Select
        Catch ex As Exception

        End Try

        Return False
    End Function

    Public Overrides Function GetLootDecision(ByVal item As uTank2.LootPlugins.GameItemInfo) As uTank2.LootPlugins.LootAction

        Try

            If mAlincoLoaded Then
                ' LogToFile("log.txt", "GetLootDecision1")
                
                Return AlincoRules(item.Id, 0, 0)


            End If
        Catch ex As Exception
            ' LogToFile("log.txt", "Error GetLootDecision")
        End Try

        Return uTank2.LootPlugins.LootAction.NoLoot
    End Function

    Public Overrides Sub LoadProfile(ByVal filename As String, ByVal newprofile As Boolean)
        ' LogToFile("log.txt", "LoadProfile " & vbNewLine & filename)
        Try
            If Not mAlincoLoaded Then
                Host.AddChatText("Error: Alinco plugin not loaded.", 14, 1)
                Return
            End If

            If newprofile Then
                Host.AddChatText("Profiles not implemented: Only need one dummy profile for Alinco", 14, 1)

                Try
                    Dim fs As New IO.FileStream(filename, FileMode.Append, FileAccess.Write)
                    Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes("Dummy Profile" & Environment.NewLine)
                    fs.Write(info, 0, info.Length)

                    fs.Close()

                Catch exo As Exception
                    ' empty catch
                End Try
            End If
        Catch ex As Exception
            'LogToFile("log.txt", ex.Message)
        End Try

    End Sub

    Public Overrides Sub OpenEditorForProfile()
        Try
            Host.AddChatText("No editor.", 14, 1)
        Catch ex As Exception

        End Try

    End Sub

    Public Overrides Sub Shutdown()

    End Sub

    Public Overrides Function Startup() As uTank2.LootPlugins.LootPluginInfo
        Try
            mAlincoLoaded = False
            TryGetAlincoInstance()

        Catch ex As Exception

        End Try

        If mAlincoLoaded Then
            Return New LootPluginInfo("los")
        End If

        Return Nothing
    End Function

    Public Overrides Sub UnloadProfile()

    End Sub
End Class
