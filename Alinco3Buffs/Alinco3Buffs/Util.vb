

Option Explicit On
Option Strict On

Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization

Friend Class Util
    Public Shared docPath As String = String.Empty
    Public Shared appPath As String = String.Empty
    Public Shared dllversion As String = String.Empty
    Public Shared NumberFormatInfo As System.Globalization.NumberFormatInfo

    Private Shared Sub AddText(ByVal fs As FileStream, ByVal value As String)
        Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(value)
        fs.Write(info, 0, info.Length)
    End Sub

    Public Shared Function normalizePath(ByVal strName As String) As String
        Const alpha As String = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWZYZ"
        Dim r As String = ""

        For Each c As Char In strName
            If alpha.IndexOf(c) >= 0 Then
                r &= c
            Else
                r &= "_"
            End If
        Next
        Return r
    End Function

    Public Shared Sub ErrorLogger(ByVal ex As Exception)

        Try
            Dim fs As New IO.FileStream(docPath & "\Errors.txt", FileMode.Append, FileAccess.Write)
            Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(Format(Now, "MM-dd-yy HH:mm:ss ") & ":" & ex.Message & Environment.NewLine & ex.StackTrace & Environment.NewLine)
            fs.Write(info, 0, info.Length)

            If ex.InnerException IsNot Nothing Then
                info = New System.Text.UTF8Encoding(True).GetBytes(Format(Now, "MM-dd-yy HH:mm:ss ") & ex.InnerException.Message & Environment.NewLine & ex.InnerException.StackTrace & Environment.NewLine)
                fs.Write(info, 0, info.Length)
            End If

            fs.Close()

        Catch exo As Exception
            ' empty catch
        End Try

    End Sub

    Public Shared Function SerializeObject(ByVal filename As String, ByVal instance As Object) As Boolean
        Try
            If instance Is Nothing Then
                Return False
            End If
            Dim serializer As New XmlSerializer(instance.GetType)
            Dim writer As New StreamWriter(filename)

            serializer.Serialize(writer, instance)
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

    Private Shared Sub bcast(ByVal msg As String)
        Try
            If Not String.IsNullOrEmpty(msg) Then
                If msg.EndsWith(vbNewLine) Then
                    msg = msg.Replace(vbNewLine, String.Empty)
                End If
                msg = "03:" & msg
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
    <Conditional("DEBUGTEST")> _
    Public Shared Sub Log(ByVal str As String)

        Try
            bcast(str)
            Dim fs As New IO.FileStream(docPath & "\Log.txt", FileMode.Append, FileAccess.Write)
            Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(str & Environment.NewLine)
            fs.Write(info, 0, info.Length)

            fs.Close()

        Catch exo As Exception
            ' empty catch
        End Try

    End Sub
    Public Shared Sub LogToFile(ByVal filename As String, ByVal str As String)

        Try
            Dim fs As New IO.FileStream(docPath & "\" & filename, FileMode.Append, FileAccess.Write)
            Dim info As Byte() = New System.Text.UTF8Encoding(True).GetBytes(str & Environment.NewLine)
            fs.Write(info, 0, info.Length)

            fs.Close()

        Catch exo As Exception
            ' empty catch
        End Try

    End Sub
    <Conditional("DEBUGTEST")> _
    Public Shared Sub StartLog()
        Try
            If File.Exists(docPath & "\Log.txt") Then
                File.Delete(docPath & "\Log.txt")
            End If
            Log("Log started " & Format(Now, "MM-dd-yy HH:mm:ss "))
        Catch exo As Exception
            ' empty catch
        End Try

    End Sub
End Class



'''<summary>Represents a Serializeable Dictionary.</summary>
Public Class SDictionary(Of TKey, TValue)
    Inherits Dictionary(Of TKey, TValue)
    Implements IXmlSerializable

    Public Function GetSchema() As System.Xml.Schema.XmlSchema Implements System.Xml.Serialization.IXmlSerializable.GetSchema
        Return Nothing
    End Function

    Public Sub ReadXml(ByVal reader As System.Xml.XmlReader) Implements System.Xml.Serialization.IXmlSerializable.ReadXml
        Dim xmlKey As New Xml.Serialization.XmlSerializer(GetType(TKey))
        Dim xmlValue As New Xml.Serialization.XmlSerializer(GetType(TValue))

        If reader.IsEmptyElement Then
            reader.Read()
            Return
        End If

        reader.Read()
        reader.MoveToContent()

        While (reader.NodeType <> XmlNodeType.EndElement And reader.Name = "item")

            reader.ReadStartElement("item")

            reader.ReadStartElement("key")
            Dim key As TKey = DirectCast(xmlKey.Deserialize(reader), TKey)
            reader.ReadEndElement()

            reader.ReadStartElement("value")
            Dim value As TValue = DirectCast(xmlValue.Deserialize(reader), TValue)
            reader.ReadEndElement()

            Add(key, value)
            reader.ReadEndElement()
            reader.MoveToContent()
        End While

        reader.ReadEndElement()
    End Sub

    Public Sub WriteXml(ByVal writer As System.Xml.XmlWriter) Implements System.Xml.Serialization.IXmlSerializable.WriteXml
        Dim xmlKey As New Xml.Serialization.XmlSerializer(GetType(TKey))
        Dim xmlValue As New Xml.Serialization.XmlSerializer(GetType(TValue))

        For Each k As TKey In Me.Keys
            writer.WriteStartElement("item")

            writer.WriteStartElement("key")
            xmlKey.Serialize(writer, k)
            writer.WriteEndElement()

            writer.WriteStartElement("value")
            Dim value As TValue = Me(k)
            xmlValue.Serialize(writer, value)
            writer.WriteEndElement()

            writer.WriteEndElement()

        Next

    End Sub
End Class


