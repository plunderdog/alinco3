Option Strict On
Option Infer On

Imports Decal.Adapter
Imports Decal.Adapter.Wrappers
Imports System.Runtime.InteropServices
Imports System.Xml.Linq

Public Class modeldata
    Public description As String
    Public colors As List(Of Integer)

    Public Overrides Function ToString() As String
        Return description
    End Function
End Class

Public Class InventoryItem
    Public charname As String 'storage  id, storage1 storage 2 storage3

    Public name As String
    Public id As Integer
    Public stack As Integer
    Public category As String
    Public description As String
    Public majorcount As Integer
    Public epiccount As Integer
    Public itemset As String
    Public coverage As Integer
    Public Icon As Integer
    Public wieldLvl As Integer
    Public value As Integer
    Public equipped As Boolean
    Public unenchantable As Boolean
    Public colors As modeldata

End Class

 


Partial Public Class Plugin
    Private mStorageInfo As SDictionary(Of Integer, String)
    Private mGlobalInventory As SDictionary(Of Integer, InventoryItem)

    Private Sub updatecharcombo()
        If mCharconfig IsNot Nothing Then
            If mGlobalInventory Is Nothing Then
                mGlobalInventory = New SDictionary(Of Integer, InventoryItem)
            End If

            cboInvAccounts.Clear()
            cboInvAccounts.Add("All", Nothing)
            Dim xn = From nn In mGlobalInventory Select nn.Value.charname Distinct
            For Each s As String In xn
                cboInvAccounts.Add(s, Nothing)
            Next
        End If
    End Sub

    Private Sub transforminventory1()
        If mGlobalInventory IsNot Nothing Then
            Dim xdoc = New XDocument(New XElement("Inventory", _
            From c In mGlobalInventory _
            Order By c.Value.category, c.Value.majorcount + c.Value.epiccount Descending, c.Value.charname _
                    Select New XElement("Item", _
                              New XElement("category", c.Value.category), _
                              New XElement("itemset", c.Value.itemset), _
                              New XElement("charname", c.Value.charname), _
                              New XElement("name", c.Value.name), _
                               New XElement("stack", c.Value.stack), _
                                 New XElement("epiccount", c.Value.epiccount), _
                                 New XElement("majorcount", c.Value.majorcount), _
                                 New XElement("wieldLvl", c.Value.wieldLvl), _
                                  New XElement("unenchantable", c.Value.unenchantable), _
                                  New XElement("equipped", c.Value.equipped), _
                                   New XElement("coverage", c.Value.coverage), _
                                    New XElement("description", c.Value.description))))
            xdoc.Save(mExportInventoryname)

            wtcw("Inventory exported: ")
            wtcw(mExportInventoryname)
        End If
    End Sub

    Private Sub clearInventory(ByVal charname As String)
        Dim rlist As New List(Of Integer)

        If mGlobalInventory IsNot Nothing Then
            Dim x = From xi In mGlobalInventory Where xi.Value.charname = charname
            For Each k As KeyValuePair(Of Integer, InventoryItem) In x
                rlist.Add(k.Key)
            Next

            For Each delid As Integer In rlist
                mGlobalInventory.Remove(delid)
            Next
        End If
    End Sub

    Private Sub updateinventoryItem(ByVal item As InventoryItem)
        If mGlobalInventory Is Nothing Then
            mGlobalInventory = New SDictionary(Of Integer, InventoryItem)
        End If

        If mGlobalInventory.ContainsKey(item.id) Then
            mGlobalInventory.Item(item.id) = item
        Else
            mGlobalInventory.Add(item.id, item)
        End If
    End Sub

    Private midInventory As New Dictionary(Of Integer, Integer)
    Private Function DeletedInvList(ByVal name As String) As List(Of Integer)
        Dim rlist As New List(Of Integer)
        Dim xcol = From m In mGlobalInventory Where m.Value.charname = name
        For Each ccheck As KeyValuePair(Of Integer, InventoryItem) In xcol
            If Not Host.Actions.IsValidObject(ccheck.Key) Then
                rlist.Add(ccheck.Key)
            End If
        Next
        Return rlist
    End Function

    Private Sub startscanInventoryforSerialize(ByVal fullscann As Boolean)

        If fullscann Then
            clearInventory(Core.CharacterFilter.Name)
        End If

        If mGlobalInventory Is Nothing Then
            mGlobalInventory = New SDictionary(Of Integer, InventoryItem)
        End If

        scanInventoryorstorage(fullscann, Core.CharacterFilter.Id, Core.CharacterFilter.Name, 0)
    End Sub

    Private Sub scanInventoryorstorage(ByVal fullscann As Boolean, ByVal containerid As Integer, ByVal charname As String, ByVal storageflag As Integer)
        Dim rlist As List(Of Integer) = DeletedInvList(charname)
        'remove from collection if not exists in worldfilter

        For Each i As Integer In rlist
            mGlobalInventory.Remove(i)
        Next

        Dim xcount As Integer = 0

        Dim wcol As WorldObjectCollection
        Dim excludestorage As Boolean
        If containerid = Core.CharacterFilter.Id Then
            excludestorage = True
            wcol = Core.WorldFilter.GetInventory
        Else
            wcol = Core.WorldFilter.GetByContainer(containerid)
        End If
        Dim moved As Integer

        For Each o As WorldObject In wcol
            With o
                If Not fullscann AndAlso mGlobalInventory.ContainsKey(o.Id) Then
                    mGlobalInventory.Item(o.Id).equipped = CBool(o.Values(LongValueKey.Slot) = -1)
                    mGlobalInventory.Item(o.Id).stack = o.Values(LongValueKey.StackCount)
                    If mGlobalInventory.Item(o.Id).charname <> charname Then
                        moved += 1
                        mGlobalInventory.Item(o.Id).charname = charname
                    End If

                    Continue For
                End If

                If excludestorage AndAlso mStorageInfo IsNot Nothing AndAlso mStorageInfo.ContainsKey(.Container) Then
                    Continue For
                End If

                Dim scannit As Boolean = False

                Select Case .ObjectClass
                    Case ObjectClass.Armor, ObjectClass.Salvage, ObjectClass.Clothing, ObjectClass.HealingKit, ObjectClass.Jewelry, ObjectClass.MeleeWeapon, ObjectClass.MissileWeapon, ObjectClass.WandStaffOrb
                        scannit = True

                    Case Else
                        If .Name = "Aetheria" Then
                            scannit = True
                        Else
                            Dim ii As New InventoryItem With {.charname = charname}
                            ii.category = .ObjectClass.ToString
                            ii.id = .Id
                            ii.name = .Name
                            ii.stack = .Values(LongValueKey.StackCount)
                            ii.value = .Values(LongValueKey.Value)
                            ii.description = .Name

                            If ii.stack > 1 Then
                                ii.description &= " " & ii.stack
                            End If


                            updateinventoryItem(ii)
                        End If
                        
                End Select

                If scannit Then
                    If Not midInventory.ContainsKey(.Id) Then
                        midInventory.Add(.Id, storageflag)
                    End If

                    Host.Actions.RequestId(.Id)
                    xcount += 1
                End If
            End With
            'If xcount > 20 Then
            '    Exit For
            'End If
        Next
        wtcw("removed " & rlist.Count & "  ident " & xcount & "  moved " & moved)
    End Sub

    'generate a new name
    Private Function getstoragename(ByVal id As Integer) As String

        If mStorageInfo Is Nothing Then
            mStorageInfo = New SDictionary(Of Integer, String)
        End If

        If mStorageInfo.ContainsKey(id) Then
            Return mStorageInfo.Item(id)
        Else
            Dim newnr As Integer = 0
            Dim newname As String = "Storage" & newnr
            'storage1, storage2, ..
            Dim found As Boolean
            Do
                found = False
                newnr += 1
                newname = "Storage" & newnr
                For Each kv As KeyValuePair(Of Integer, String) In mStorageInfo
                    If kv.Value = newname Then
                        found = True
                        Exit For
                    End If
                Next

            Loop Until Not found

            mStorageInfo.Add(id, newname)
            Return newname
        End If

    End Function
    <ControlEvent("btnInvUpdate", "Click")> _
   Private Sub btnInvUpdate_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If mCharconfig IsNot Nothing Then
                'open storage
                updatecharcombo()

                Dim selectedid As Integer = Host.Actions.CurrentSelection
                Dim selecteditem As WorldObject = Core.WorldFilter.Item(selectedid)
                If selecteditem IsNot Nothing AndAlso selecteditem.Name = "Storage" Then


                    Dim wcol As WorldObjectCollection = Core.WorldFilter.GetByContainer(selectedid)
                    Dim packstorageid As Integer = 0

                    For Each obj As WorldObject In wcol
 
                        If obj.ObjectClass = ObjectClass.Container Then
                            packstorageid = obj.Id
                            Exit For
                        End If
                    Next
                    Dim storagename As String = getstoragename(selectedid)

                    wtcw("update storage: " & storagename)
                    scanInventoryorstorage(False, selectedid, storagename, selectedid)
                    If packstorageid <> 0 Then
                        wtcw("update storage pack")
                        scanInventoryorstorage(False, packstorageid, storagename, selectedid)
                    End If

                    Return

                End If

                txtInvSearch.Text = String.Empty
                lstInvFound.Clear()
                startscanInventoryforSerialize(False)
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnInvDelete", "Click")> _
   Private Sub btnInvDelete_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If mCharconfig IsNot Nothing Then
                lstInvFound.Clear()
                txtInvSearch.Text = String.Empty
                wtcw("total entries: " & mGlobalInventory.Count)
                If cboInvAccounts.Selected = 0 Then 'clear all
                    mGlobalInventory.Clear()
                    If mStorageInfo IsNot Nothing Then
                        mStorageInfo.Clear()
                    End If
                    wtcw("Global Inventory Cleared")
                ElseIf cboInvAccounts.Selected > 0 Then
                    Dim delname As String = cboInvAccounts.Text(cboInvAccounts.Selected)

                    clearInventory(delname)
                    wtcw(delname & " removed from Global Inventory")

                End If
                wtcw("total entries: " & mGlobalInventory.Count)
                updatecharcombo()
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnInvClear", "Click")> _
   Private Sub btnInvClear_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If mCharconfig IsNot Nothing Then
                lstInvFound.Clear()
                txtInvSearch.Text = String.Empty

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Function matchinf(ByVal src As String, ByVal inv As InventoryItem) As Boolean
        If inv IsNot Nothing Then
            Dim separator As String = String.Empty
            If src.IndexOf(",") > 0 Then
                separator = ","
            ElseIf src.IndexOf(":") > 0 Then
                separator = ":"
            ElseIf src.IndexOf(";") > 0 Then
                separator = ";"
            End If
            'major;major 2 
            If separator <> String.Empty Then
                Dim searchs As String() = Split(src, separator)
                Dim prev As String = String.Empty

                For Each s In searchs
                    If (Not String.IsNullOrEmpty(s)) Then
                        If prev = s Then
                            '2
                            Dim p As Integer = inv.description.ToLower.IndexOf(s.ToLower)
                            If Not inv.description.ToLower.IndexOf(s.ToLower, p + s.Length) >= 0 Then
                                Return False
                            End If
                        Else
                            If Not inv.description.ToLower.IndexOf(s.ToLower) >= 0 Then
                                Return False
                            End If
                        End If
                        prev = s
                        
                    End If
                Next

                Return True

            ElseIf inv.description.ToLower.IndexOf(src.ToLower) >= 0 Then
                Return True
            End If
        End If
    End Function

    <ControlEvent("btnInvSearch", "Click")> _
   Private Sub btnInvSearch_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        dosearch()
    End Sub

    Private Sub dosearch()
        Try
            If mCharconfig IsNot Nothing AndAlso mGlobalInventory IsNot Nothing Then
                Dim src As String = txtInvSearch.Text
                lstInvFound.Clear()

                src = src.Trim
                If Not String.IsNullOrEmpty(src) Then
                    If cboInvAccounts.Selected <= 0 Then 'find all
                        Dim x = From i In mGlobalInventory Where matchinf(src, i.Value)

                        For Each k As KeyValuePair(Of Integer, InventoryItem) In x
                            addtolist(k.Value, k.Value.charname, True)
                        Next
                    Else
                        Dim searchname As String = cboInvAccounts.Text(cboInvAccounts.Selected)
                        Dim x = From i In mGlobalInventory Where i.Value.charname = searchname AndAlso matchinf(src, i.Value)

                        For Each k As KeyValuePair(Of Integer, InventoryItem) In x
                            addtolist(k.Value, k.Value.charname, True)
                        Next
                    End If

                End If

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    <ControlReference("txtInvSearch")> _
   Private txtInvSearch As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlReference("lstInvFound")> _
   Private lstInvFound As Decal.Adapter.Wrappers.ListWrapper
    <ControlEvent("lstInvFound", "Selected")> _
      Private Sub OnlstInvFoundSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        lRow = lstInvFound(e.Row)
        id = CType(lRow(3)(0), Integer)
        If Host.Actions.IsValidObject(id) Then
            Host.Actions.SelectItem(id)
        End If

        wtcw(CType(lRow(4)(0), String))
    End Sub

    Private Sub addtolist(ByVal pobject As InventoryItem, ByVal charname As String, ByVal sort As Boolean)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim insertAt As Integer = -1

        If sort Then
            For i As Integer = 0 To lstInvFound.RowCount - 1
                Dim name As String = CStr(lstInvFound.Item(i)(1)(0))
                If (name > pobject.name) Then
                    insertAt = i
                    Exit For
                End If
            Next
        End If
        If insertAt = -1 Then
            lRow = lstInvFound.Add
            insertAt = lstInvFound.RowCount
        Else
            lRow = lstInvFound.Insert(insertAt)
        End If

        lRow(0)(1) = pobject.Icon + &H6000000
        lRow(1)(0) = pobject.name
        lRow(2)(0) = charname
        lRow(3)(0) = CStr(pobject.id)
        lRow(4)(0) = CStr(pobject.description)
    End Sub

    <ControlReference("cboInvAccounts")> _
  Private cboInvAccounts As Decal.Adapter.Wrappers.ChoiceWrapper
    <ControlEvent("cboInvAccounts", "Change")> _
     Private Sub cboInvAccounts_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        Try
            lstInvFound.Clear()
            If mCharconfig IsNot Nothing AndAlso mGlobalInventory IsNot Nothing Then
                If cboInvAccounts.Selected >= 0 Then 'find all
                    If Not String.IsNullOrEmpty(txtInvSearch.Text) Then
                        dosearch()
                    ElseIf cboInvAccounts.Selected > 0 Then
                        Dim searchname As String = cboInvAccounts.Text(cboInvAccounts.Selected)
                        Dim x = From i In mGlobalInventory Where i.Value.charname = searchname Order By i.Value.name

                        For Each k As KeyValuePair(Of Integer, InventoryItem) In x
                            addtolist(k.Value, k.Value.charname, True)
                        Next
                    End If
                End If
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
End Class
