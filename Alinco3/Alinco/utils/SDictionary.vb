Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization

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


