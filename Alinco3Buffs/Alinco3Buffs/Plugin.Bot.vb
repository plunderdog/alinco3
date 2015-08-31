Partial Public Class Plugin

    'Private mTellsqueue As New Queue(Of String)
    'Private mChatqueue As New Queue(Of String)
    Private mBotBuffs As New Dictionary(Of String, BuffInfo)

    'Private Function ChatTime() As Boolean
    '    Dim strMsg As String
    '    Try
    '        If mChatqueue.Count > 0 Then

    '            strMsg = mChatqueue.Dequeue

    '            Host.Actions.InvokeChatParser(strMsg)

    '            Return True

    '        End If

    '    Catch ex As Exception
    '        Util.ErrorLogger(ex)
    '    End Try

    '    Return False
    'End Function

    'Private Sub Tell(ByVal strPlayerName As String, ByVal strText As String)
    '    Try
    '        Dim strMsg As String
    '        If Not strText Is Nothing Then
    '            Util.Log("metell: " & strPlayerName & vbTab & " :" & strText)
    '            If strText.Length > 1 And Not strPlayerName Is Nothing AndAlso Len(strText.Trim) > 0 Then

    '                strMsg = "@tell " & strPlayerName & ", " & strText
    '                mChatqueue.Enqueue(strMsg)
    '            End If

    '        End If
    '    Catch ex As Exception
    '        Util.ErrorLogger(ex)
    '    End Try
    'End Sub

    'Private Sub handletells()
    '    Try
    '        Dim msg As String
    '        Dim pos As Integer
    '        Dim cmd As String = String.Empty
    '        Dim player As String = String.Empty

    '        If mTellsqueue.Count > 0 Then

    '            msg = mTellsqueue.Dequeue

    '            If Not String.IsNullOrEmpty(msg) Then

    '                pos = msg.IndexOf(" tells you, ")
    '                If pos > 1 Then
    '                    cmd = msg.Substring(pos + 12)
    '                    cmd = Replace(cmd, """", "")

    '                    pos = msg.IndexOf(">")
    '                    If pos > 0 Then
    '                        Dim strTemp As String = msg.Substring(pos + 1)

    '                        pos = strTemp.IndexOf("<")
    '                        If pos > 0 Then
    '                            strTemp = strTemp.Substring(0, pos)
    '                            player = strTemp
    '                        End If
    '                    End If
    '                End If

    '                If Not String.IsNullOrEmpty(cmd) AndAlso Not String.IsNullOrEmpty(player) Then
    '                    'Util.Log("someone: " & player & vbTab & "tells you:" & cmd)

    '                    Dim firstword As String
    '                    pos = cmd.IndexOf(" ")
    '                    If pos > 0 Then
    '                        firstword = cmd.Substring(0, pos)
    '                    ElseIf cmd.IndexOf(",") > 0 Then
    '                        pos = cmd.IndexOf(",")
    '                        firstword = cmd.Substring(0, pos)
    '                    Else
    '                        firstword = cmd
    '                    End If

    '                    Select Case firstword.ToLower
    '                        Case "xp"

    '                    End Select
    '                End If

    '            End If

    '        End If

    '    Catch ex As Exception
    '        Util.ErrorLogger(ex)
    '    End Try
    'End Sub
End Class
