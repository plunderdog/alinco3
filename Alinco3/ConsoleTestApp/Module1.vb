Imports Alinco

Module Module1

    Sub Main()

        Dim SalvageData As New SDictionary(Of Integer, String)
        Console.WriteLine("Ed WAs here!")
        'Console.ReadLine()
        Console.WriteLine("SalvageData has " + SalvageData.Keys.Count().ToString() + " keys")

        ' Now let's read us some XML
        Dim doc As XDocument
        Dim itemId As Int16
        Dim itemName As String
        doc = XDocument.Load("C:\Users\Edward\Desktop\alinco3-96133\Alinco3\Alinco\Defaults\Salvage.xml")
        For Each item In doc.<SDictionaryOfInt32SalvageSettings>.<item>
            itemId = item.<key>.<int>.Value
            itemName = item.<value>.<SalvageSettings>.<name>.Value

            Console.WriteLine(item.ToString())
            Console.WriteLine(itemId & " - " & itemName)
            SalvageData.Add(Int(itemId), itemName)
        Next


        Console.WriteLine("SalvageData has " & SalvageData.Keys.Count().ToString() & " keys")
        Console.WriteLine("SalvageData at key: 15:" & SalvageData(15))
        Console.ReadLine()


    End Sub

End Module
