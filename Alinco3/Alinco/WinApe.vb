Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Reflection
Imports System.Threading

Public Class WinApe
    <StructLayout(LayoutKind.Sequential)> _
      Public Structure RECTAPI
        Public left As Integer
        Public top As Integer
        Public right As Integer
        Public bottom As Integer
    End Structure

    <DllImport("user32.dll", ExactSpelling:=True, CharSet:=System.Runtime.InteropServices.CharSet.Auto)> _
    Private Shared Function MoveWindow(ByVal hWnd As IntPtr, ByVal x As Integer, ByVal y As Integer, ByVal cx As Integer, ByVal cy As Integer, _
        ByVal repaint As Integer) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)> _
     Friend Shared Function GetClientRect(ByVal hwnd As IntPtr, ByRef lpRect As RECTAPI) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)> _
     Friend Shared Function GetWindowRect(ByVal hwnd As IntPtr, ByRef lpRect As RECTAPI) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="GetWindowLong")> _
     Friend Shared Function GetWindowLong(ByVal HWND As IntPtr, ByVal Index As Integer) As Integer
    End Function
    <DllImport("user32.dll", EntryPoint:="SetWindowLong")> _
         Friend Shared Function SetWindowLong(ByVal HWND As IntPtr, ByVal Index As Integer, ByVal newlong As Integer) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="GetActiveWindow")> _
    Friend Shared Function GetActiveWindow() As IntPtr
    End Function

    Friend Shared Function WindowedFullscreen(ByVal hwndAC As IntPtr) As Integer
        Const GWL_STYLE As Integer = -16
        Const WS_CAPTION As Integer = &HC00000 '/* WS_BORDER | WS_DLGFRAME */

        Dim rectWindow, rectClient As RECTAPI
        Dim cwidth, cheight As Integer
        Dim wwidth, wheight As Integer
        Dim WorkingAreaX As Integer

        If Not hwndAC.Equals(IntPtr.Zero) Then
            If GetClientRect(hwndAC, rectClient) <> 0 Then
                cwidth = rectClient.right - rectClient.left
                cheight = rectClient.bottom - rectClient.top
                If GetWindowRect(hwndAC, rectWindow) <> 0 Then
                    wwidth = rectWindow.right - rectWindow.left
                    wheight = rectWindow.bottom - rectWindow.top
                End If
                If UBound(Windows.Forms.Screen.AllScreens) >= 0 Then
                    For Each ts As Windows.Forms.Screen In Windows.Forms.Screen.AllScreens
                        If rectClient.left >= ts.WorkingArea.Left And rectClient.left <= ts.WorkingArea.Right Then
                            WorkingAreaX = ts.WorkingArea.X
                            'Util.Log("Bounds: width " & ts.Bounds.Width & " Height " & ts.Bounds.Height)
                            'Util.Log("WorkingArea: width " & ts.WorkingArea.Width & " Height " & ts.WorkingArea.Height)
                            'Util.Log("rectClient: width " & cwidth & " Height " & cheight)
                            'Util.Log("rectWindow: width " & wwidth & " Height " & wheight)

                            Dim wstyler As Integer = GetWindowLong(hwndAC, GWL_STYLE)
                            If (wstyler And WS_CAPTION) = WS_CAPTION Then
                                If (ts.Bounds.Width = cwidth Or ts.Bounds.Width <= wwidth) And (ts.Bounds.Height <= wheight) Then
                                    Util.Log("Matched screen RemoveTitlebar ")
                                    wstyler = wstyler Xor WS_CAPTION
                                    SetWindowLong(hwndAC, GWL_STYLE, wstyler)
                                    If GetWindowRect(hwndAC, rectWindow) <> 0 Then
                                        wwidth = rectWindow.right - rectWindow.left
                                        wheight = rectWindow.bottom - rectWindow.top
                                        If wwidth > 0 And wheight > 0 Then 'not minimized ?                           
                                            MoveWindow(hwndAC, WorkingAreaX, 0, wwidth, wheight, 1)
                                        End If
                                    End If
                                End If
                            Else
                                Util.Log("windowstyle does not have WS_CAPTION")
                                If wwidth = cwidth And wheight = cheight And (ts.Bounds.Height > wheight) Then

                                    wstyler = wstyler Or WS_CAPTION
                                    SetWindowLong(hwndAC, GWL_STYLE, wstyler)

                                End If
                            End If

                            Exit For
                        End If
                    Next
                End If
            End If
        End If
        Util.Log("*")
    End Function

    <StructLayout(LayoutKind.Sequential)> _
  Friend Structure POINTAPI
        Public x As Integer
        Public y As Integer
    End Structure
    Private Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
    Private Const MOUSEEVENTF_LEFTUP As Integer = &H4

    <DllImport("user32.dll")> _
    Friend Shared Sub mouse_event(ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal dwData As Integer, ByVal dwExtraInfo As IntPtr)
    End Sub
    <DllImport("user32.dll")> _
   Private Shared Sub SetCursorPos(ByVal x As Integer, ByVal y As Integer)
    End Sub
    Friend Shared Sub MouseLeftClick()
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, New System.IntPtr)
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, New System.IntPtr)
    End Sub

    Friend Shared Sub MouseSetCursor(ByVal x As Integer, ByVal y As Integer)
        SetCursorPos(x, y)
    End Sub

    Friend Shared Sub MouseLeftClick(ByVal x As Integer, ByVal y As Integer)
        MouseSetCursor(x, y)
        MouseLeftClick()
    End Sub

    Friend Shared Function MouseGetCursor() As POINTAPI
        Dim lpPoint As New POINTAPI
        GetCursorPos(lpPoint)
        Return lpPoint
    End Function
    Friend Shared Sub MouseSetCursor(ByVal pos As POINTAPI)
        SetCursorPos(pos.x, pos.y)
    End Sub
    <DllImport("user32.dll")> _
  Friend Shared Function GetCursorPos(ByRef lpPoint As POINTAPI) As Integer
    End Function
End Class
