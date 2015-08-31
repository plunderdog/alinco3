Option Explicit On
Option Strict On

Imports System.IO
Imports System.Xml.Serialization

Public Class Util
    Public Const ERRORLINK_ID As Integer = 221113
    Public Shared docPath As String = String.Empty
    Public Shared dllversion As String = String.Empty
    Public Shared wavPath As String = String.Empty
    Public Shared TotalErrors As Integer

    'this plugin is made on a dutch system, this is needed for numeric and coords display
    Public Shared NumberFormatInfo As System.Globalization.NumberFormatInfo

    Private Shared Sub AddText(ByVal fs As FileStream, ByVal value As String)
        Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(value)
        fs.Write(info, 0, info.Length)
    End Sub

    Public Shared Sub ErrorLogger(ByVal ex As Exception)

        Try
            bcast("errer: " & ex.Message)
            bcast(ex.StackTrace)
            Dim headertext As String = String.Empty
            If TotalErrors = 0 Then
                System.IO.File.Delete(docPath & "\errors.txt")
            End If

            If Not File.Exists(docPath & "\errors.txt") Then
                headertext = "Alinco3 Version: " & Util.dllversion & vbNewLine
                headertext &= "OSVersion      : " & System.Environment.OSVersion.ToString & vbNewLine
                headertext &= ".NET Framework : " & System.Environment.Version.ToString & vbNewLine & vbNewLine
            End If

            TotalErrors += 1

            If Plugin.HooksForErrorHandler IsNot Nothing And TotalErrors = 10 Then
                Plugin.HooksForErrorHandler.AddChatText("Alinco3: More then 10 errors, log stopped", 11)
            End If

            If TotalErrors >= 10 Then
                Return
            End If

            Dim fs As New IO.FileStream(docPath & "\Errors.txt", FileMode.Append, FileAccess.Write)
            Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(headertext & Format(Now, "MM-dd-yy HH:mm:ss ") & ":" & ex.Message & Environment.NewLine & ex.StackTrace & Environment.NewLine)
            fs.Write(info, 0, info.Length)

            If ex.InnerException IsNot Nothing Then
                info = New System.Text.UTF8Encoding(True).GetBytes(Format(Now, "MM-dd-yy HH:mm:ss ") & ex.InnerException.Message & Environment.NewLine & ex.InnerException.StackTrace & Environment.NewLine)
                fs.Write(info, 0, info.Length)
            End If

            fs.Close()
            If Plugin.HooksForErrorHandler IsNot Nothing Then

                Dim link As String = "<Tell:IIDString:" & ERRORLINK_ID & ":Losado>(errors.txt)<\\Tell>"
                Plugin.HooksForErrorHandler.AddChatText("Alinco3: Exception occured see file " & link & " for more details.", 11)
            End If

        Catch exo As Exception
            ' empty catch
        End Try

    End Sub

    '<?xml version="1.0"?><?xml-stylesheet type='text/xsl' href='CharInvent.xslt'?>

    Public Shared Function SerializeObject(ByVal filename As String, ByVal instance As Object, Optional ByVal stSheet As String = "") As Boolean
        Try
            If instance Is Nothing Then
                Return False
            End If

            Dim serializer As New XmlSerializer(instance.GetType)

            Dim writer As New StreamWriter(filename)

            If Not String.IsNullOrEmpty(stSheet) Then
                Using x As New Xml.XmlTextWriter(writer)
                    '  x.Settings.Indent = True
                    x.Formatting = Xml.Formatting.Indented

                    x.WriteProcessingInstruction("xml-stylesheet", stSheet)

                    serializer.Serialize(x, instance)

                End Using
            Else
                serializer.Serialize(writer, instance)
            End If


            writer.Close()

        Catch ex As Exception
            Util.ErrorLogger(ex)

            Return False
        End Try

        Return True
    End Function

    Public Shared Function DeSerializeObject(ByVal filename As String, ByVal objectType As Type) As Object
        Dim instance As Object = Nothing
        Dim fs As FileStream = Nothing

        Try

            If File.Exists(filename) Then
                Dim serializer As New XmlSerializer(objectType)
                fs = New FileStream(filename, FileMode.Open)
                instance = serializer.Deserialize(fs)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        Finally
            If Not fs Is Nothing Then
                fs.Close()
            End If
        End Try

        Return instance
    End Function
    Private Shared mWCUDPPort As Integer = 1533

    <Conditional("SYSLOG")> _
    Public Shared Sub bcast(ByVal msg As String)
        Try
            If Not String.IsNullOrEmpty(msg) Then
                If msg.EndsWith(vbNewLine) Then
                    msg = msg.Replace(vbNewLine, String.Empty)
                End If
                msg = "05:" & msg
                Dim udp As New Net.Sockets.UdpClient
                udp.EnableBroadcast = True
                Dim ep As New Net.IPEndPoint(Net.IPAddress.Broadcast, mWCUDPPort)
                Dim b() As Byte = System.Text.Encoding.UTF32.GetBytes(msg)
                udp.Send(b, b.Length, ep)
                udp.Close()
            End If
        Catch ex As Exception

        End Try
    End Sub
    <Conditional("OESVH")> _
    Public Shared Sub Log(ByVal str As String)
        bcast(str)
        LogToFile(str, "Log.txt")
    End Sub

    Public Shared Sub LogToFile(ByVal str As String, ByVal filename As String)

        Try
            Dim fs As New IO.FileStream(docPath & "\" & filename, FileMode.Append, FileAccess.Write)
            Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(str & Environment.NewLine)
            fs.Write(info, 0, info.Length)

            fs.Close()

        Catch exo As Exception
            ' empty catch
        End Try

    End Sub

    Public Shared Sub StartLog()
        Try

            If File.Exists(docPath & "\log.txt") Then
                File.Delete(docPath & "\log.txt")
            End If

            
        Catch exo As Exception
            ' empty catch
        End Try

    End Sub

    Public Shared Function normalizePath(ByVal strName As String) As String
        Const alpha As String = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWZYZ"
        Dim r As String = ""

        For Each c As Char In strName
            If alpha.IndexOf(c) > 0 Then
                r &= c
            Else
                r &= "_"
            End If
        Next
        Return r
    End Function
End Class
