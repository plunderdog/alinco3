Imports Decal.Adapter.Wrappers
Imports System.Drawing

Partial Public Class Plugin
    Private mRenderService As RenderServiceWrapper
    Private mHud As Hud
    Private mHudPos As System.Drawing.Point
    Private mBaseFontColor As Color
    Private mBaseFontName As String = "Times New Roman"
    Private mBaseFontSize As Integer = 14
    Private mBaseFontweight As FontWeight
    Private mHudCanvasHeight As Integer = 40
    Private mHudCanvasWidth As Integer = 200

    Private Sub setFontColor(ByVal argb As Integer)
        argb = &HFF000000 Or argb
        mBaseFontColor = System.Drawing.Color.FromArgb(argb)
    End Sub

    Protected Sub Drawborder(ByVal r3 As Rectangle)
        mHud.Fill(r3, Color.DimGray)
        Try
            'top left
            Dim c1 As New Rectangle(0, 0, 1, 4)
            mHud.Fill(c1, Color.Gold)
            Dim c2 As New Rectangle(0, 0, 4, 1)
            mHud.Fill(c2, Color.Gold)

            'top rigty
            Dim c3 As New Rectangle(mHudCanvasWidth - 4, 0, 4, 1)
            mHud.Fill(c3, Color.Gold)
            Dim c4 As New Rectangle(mHudCanvasWidth - 1, 0, 1, 4)
            mHud.Fill(c4, Color.Gold)

            'bottom left
            Dim c5 As New Rectangle(0, mHudCanvasHeight - 4, 1, 4)
            mHud.Fill(c5, Color.Gold)
            Dim c6 As New Rectangle(0, mHudCanvasHeight - 1, 4, 1)
            mHud.Fill(c6, Color.Gold)

            'bottom right
            Dim c7 As New Rectangle(mHudCanvasWidth - 4, mHudCanvasHeight - 1, 4, 1)
            mHud.Fill(c7, Color.Gold)
            Dim c8 As New Rectangle(mHudCanvasWidth - 1, mHudCanvasHeight - 4, 1, 4)
            mHud.Fill(c8, Color.Gold)

        Catch ex As Exception

        End Try
    End Sub

    Private Function secondstoTimeString(ByVal seconds As Integer) As String
        Dim d As String = String.Empty
        Dim t As TimeSpan = TimeSpan.FromSeconds(seconds)
        If t.Hours > 9 Then
            d = CStr(t.Hours) & ":"
        ElseIf t.Hours > 0 Then
            d = "0" & CStr(t.Hours) & ":"
        End If

        If t.Minutes > 9 Then
            d &= CStr(t.Minutes)
        Else
            d &= "0" & CStr(t.Minutes)
        End If

        If t.Seconds > 9 Then
            d &= ":" & CStr(t.Seconds)
        Else
            d &= ":" & "0" & CStr(t.Seconds)
        End If

        Return d
    End Function
    Private Sub Renderhud()

        Try
            If mRenderService IsNot Nothing Then
                mHudPos.X = 10
                mHudPos.Y = 10

                If mHud Is Nothing Or (mHud IsNot Nothing AndAlso mHud.Lost) Then
                    mHud = mRenderService.CreateHud(New Rectangle(mHudPos.X, mHudPos.Y, mHudCanvasWidth, mHudCanvasHeight))
                    mHud.Alpha = 255

                End If
                If mcharconfig.simplehud Then
                    mHud.Enabled = True
                Else
                    mHud.Enabled = False
                    Return
                End If

                Try
                    If True Then
                        Dim r3 As New Rectangle(0, 0, mHudCanvasWidth, mHudCanvasHeight)
                        r3 = New Rectangle(1, 1, r3.Width - 2, r3.Height - 2)
                        Drawborder(r3)
                        mHud.Clear(r3)
                    Else
                        mHud.Clear()
                    End If
                    
                    mHud.BeginRender()
                    mHud.BeginText(mBaseFontName, mBaseFontSize, mBaseFontweight, False)
                    Dim strInfo As String = String.Empty

                    If mBuffsPending > 0 Then
                        strInfo = "Pending " & mBuffsPending & "  "
                    End If

                    Dim secs1 As Integer = BuffTimeRemaining(eMagicSchool.Creature Or eMagicSchool.Life)
                    Dim secs2 As Integer = BuffTimeRemaining(eMagicSchool.Item)

                    strInfo &= secondstoTimeString(secs1)

                    If Math.Abs(secs1 - secs2) > 180 Then
                        strInfo &= " / " & secondstoTimeString(secs2)
                    End If

                    Dim r As New Rectangle(4, 4, mHudCanvasWidth - 4, mHudCanvasHeight - 4)
                    mHud.WriteText(strInfo, mBaseFontColor, WriteTextFormats.None, r)

                Catch ex As Exception
                    Util.ErrorLogger(ex)
                Finally
                    mHud.EndText()
                    mHud.EndRender()
                End Try
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

End Class