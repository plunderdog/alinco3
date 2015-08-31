Option Explicit On
Option Strict On
Option Infer On

Imports System.IO
Imports Decal.Adapter.Wrappers
Imports Decal.Adapter

Partial Public Class Plugin

    'experimental ------------------------
    Private Class InspectObject
        Public Id As Integer
        Public Name As String

    End Class

    Private mInspectObjectQueue As New Queue(Of InspectObject)
    Private mNotifyObjectQueue As New Queue(Of notify)
    'experimental ------------------------


    Friend Enum eScanresult
        nomatch = 0
        needsident
        trophy
        salvage
        rare
        spell
        rule
        monstertokill
        portalOrLifestone
        corpse
        corpseself
        corpsewithrare
        value
        manatank
        allegmembers
        npc
        other
    End Enum
    

    Private Const NOTIFYLINK_ID As Integer = 221112
    Private Const GOARROWLINK_ID As Integer = 110011

    Private mCurrentContainer As Integer 'chest or corpse or box ..
    Private mCurrentContainerContent As New Dictionary(Of Integer, Integer)

    Private mProtectedCorpses As New List(Of String)

    Private mCorpseCache As New Queue(Of Integer) 'when you reenter an area it is annoying to see the corpses again

    Private mActiveThropyProfile As SDictionary(Of String, ThropyInfo)
    Private mActiveMobProfile As SDictionary(Of String, NameLookup)
    Private mActiveSalvageProfile As SDictionary(Of Integer, SalvageSettings)
    Private mActiveRulesProfile As RulesCollection

    Private mNotifiedCorpses As New Dictionary(Of Integer, notify)
    Private mNotifiedItems As New Dictionary(Of Integer, notify)

    Private mUstItems As New Dictionary(Of Integer, salvageustinfo)
    Private mColScanInventoryItems As New Hashtable

    Private mIdqueue As New Hashtable


    Private Sub IdqueueAdd(ByVal id As Integer)
        If Not mIdqueue.ContainsKey(id) Then
            mIdqueue.Add(id, Now)
            Host.Actions.RequestId(id)
        End If
    End Sub


    Private Function finishedscanning() As Boolean
        Dim result As Boolean = True
        For Each k As KeyValuePair(Of Integer, Integer) In mCurrentContainerContent
            If k.Value = 0 Then
                result = False
            End If
        Next

        Return result
    End Function

    Private Sub scanningcompleted()
        wtcwd("*Corpse scan complete*")
        PlaySoundFile(mPluginConfig.Alertwawfinished, mPluginConfig.wavVolume, True)
        Renderhud()
    End Sub

    'To store mobs and thropy lookup results
    'no need to lookup over and over again in a dungeon with eaters or something
    '(you will not find a special mob there)
    '    Private mLookupCache As New Dictionary(Of String, )

    Private Sub OnWorldFilterCreateObject(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.CreateObjectEventArgs)
        Try
            If mCharconfig IsNot Nothing Then 'login complete
                If Paused Then Return

                If Host.Actions.VendorId <> 0 Then
                    Return
                End If

                With e.[New]

                    If .ObjectClass = ObjectClass.Unknown And .Container = 0 Then

                    ElseIf .Icon = 8384 Then 'ignore house hook

                    Else
                        If .Container = Core.CharacterFilter.Id Then

                            If mPluginConfig.AutoStacking AndAlso .Values(LongValueKey.StackMax) > .Values(LongValueKey.StackCount) AndAlso .Name <> mmanualstackname Then

                                If Not mColStacker.Contains(.Id) Then
                                    'wtcw2("OnWorldFilterCreateObject add to stack " & Hex(.Id) & " " & .Name)

                                    mColStacker.Add(.Id, .Name)
                                End If
                            End If
                            Exit Sub
                        ElseIf .Container = 0 AndAlso mCharconfig.detectscrollsontradebot AndAlso .ObjectClass <> ObjectClass.Corpse Then 'ignore all objects in the landscape
                            Return
                        ElseIf .Container <> 0 AndAlso .Container <> mCurrentContainer Then
                            wtcwd("OnWorldFilterCreateObject skip container item " & .Name)
                            Exit Sub
                        End If

                        Dim result As eScanresult = CheckObjectForMatch(New IdentifiedObject(e.[New]), False)

                        If result <> eScanresult.needsident Then
                            If mCurrentContainerContent.ContainsKey(.Id) Then
                                mCurrentContainerContent.Item(.Id) = 1
                            End If
                        End If

                        If mCurrentContainer <> 0 And .Container = mCurrentContainer And result <> eScanresult.needsident Then

                            Dim finished As Boolean = finishedscanning()
                            If mCorpsScanning And finished Then
                                scanningcompleted()
                            End If
                            mCorpsScanning = Not finished

                        End If

                        If result <> eScanresult.nomatch And result <> eScanresult.needsident Then
                            Renderhud()
                        End If

                        ' wtcwd("OnWorldFilterCreateObject->CheckObjectForMatch returned " & result.ToString)

                    End If

                End With

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    
    Private mpreviousIdentId As Integer

    Private Sub OnIdentObject(ByVal pMsg As Decal.Adapter.Message)
        Try
            Util.Log("OnIdentObject paused: " & Paused & " mFilesLoaded " & mFilesLoaded)
            If Paused Then Return
            If Host.Actions.VendorId <> 0 OrElse Not mFilesLoaded Then
                Return
            End If

            Dim objectId As Integer = CInt(pMsg.Item("object"))
            Dim flags As Integer = CInt(pMsg.Item("flags"))
            Dim success As Integer = CInt(pMsg.Item("success"))
            Util.Log("OnIdentObject 0x" & Hex(objectId))

            '-------------------------------------------- debug

            Dim sm As MessageStruct = Nothing
            'If (flags And &H4) = &H4 Then 'doubles
            '    sm = pMsg.Struct("doubles")
            '    If sm IsNot Nothing Then
            '        wtcw("doubles " & sm.Count)
            '    End If
            'End If

            'If (flags And &H1) = &H1 Then 'dwords
            '    sm = pMsg.Struct("dwords")
            '    If sm IsNot Nothing Then
            '        wtcw("dwords " & sm.Count)
            '    End If

            'End If
          
            'If (flags And &H2000) = &H2000 Then 'qwords (longs)
            '    sm = pMsg.Struct("qwords")
            '    If sm IsNot Nothing Then
            '        ' wtcw("qwords " & sm.Count)
            '        For i As Integer = 0 To sm.Count - 1
            '            Dim key As Integer = CInt(sm.Struct(i).Item("key"))
            '            Dim value As Long = CLng(sm.Struct(i).Item("value"))
            '            If key = 4 Then 'xp in item (rare/aetheria)
            '                '1,2,4,8,16,32
            '            End If
            '            ' wtcw(" key " & Hex(key) & " value " & value.ToString)
            '        Next
            '    End If

            'End If
            '--------------------------------------------debug

            If mpreviousIdentId = objectId Then
                mpreviousIdentId = 0
                Return
            End If

            Dim no As New IdentifiedObject(Core.WorldFilter.Item(objectId))
            If no.isvalid Then

                If (flags And &H100) = &H100 Then
                    Dim flags1 As Integer = CInt(pMsg.Item("flags1"))
                    no.HealthCurrent = CInt(pMsg.Item("health"))
                    no.HealthMax = CInt(pMsg.Item("healthMax"))
                End If
                If (flags And &H2000) = &H2000 Then 'qwords (longs)
                    sm = pMsg.Struct("qwords")
                    If sm IsNot Nothing Then
                        ' wtcw("qwords " & sm.Count)
                        For i As Integer = 0 To sm.Count - 1
                            Dim key As Integer = CInt(sm.Struct(i).Item("key"))
                            Dim value As Long = CLng(sm.Struct(i).Item("value"))
                            If key = 4 Then 'xp in item (rare/aetheria)
                                no.itemxp = value '1,2,4,8,16,32
                            End If
                            ' wtcw(" key " & Hex(key) & " value " & value.ToString)
                        Next
                    End If

                End If

                Dim scanitem As Boolean = False
                Dim copyclipboard As Boolean
                Dim manualIdent As Boolean

                If mColScanInventoryItems.ContainsKey(objectId) Then
                    OnScanInventoryItemForSalvage(no, False)
                    Return
                End If

                If midInventory.ContainsKey(objectId) Then
                    Util.Log("OnIdentObject midInventory" & Hex(objectId))

                    Dim sflag As Integer = midInventory.Item(objectId)
                    midInventory.Remove(objectId)
                    Dim sname As String = Core.CharacterFilter.Name
                    If sflag <> 0 Then
                        sname = mStorageInfo.Item(sflag)
                    End If

                    Dim ii As New InventoryItem With {.charname = sname, _
                    .category = no.ObjectClass.ToString, .description = no.ToString, _
                    .id = no.Id, .name = no.Name, .stack = no.IntValues(LongValueKey.StackCount)}
                    ii.itemset = no.Itemset
                    ii.majorcount = no.countspell("Major")
                    ii.epiccount = no.countspell("Epic")
                    ii.wieldLvl = no.wieldlvl
                    ii.value = no.IntValues(LongValueKey.Value)
                    ii.equipped = CBool(no.IntValues(LongValueKey.Slot) = -1)
                    ii.unenchantable = CBool(no.IntValues(LongValueKey.Unenchantable) <> 0)
                    ii.coverage = no.IntValues(LongValueKey.Coverage)
                    If no.ObjectClass = ObjectClass.Armor Then
                        If no.IntValues(LongValueKey.EquipType) = &H4 Then
                            ii.category = "Shield"
                        End If
                    End If

                    updateinventoryItem(ii)
                    Return
                End If

                If mIdqueue.ContainsKey(objectId) Then 'requested by this plugin
                    scanitem = True
                    mpreviousIdentId = objectId 'block next  

                ElseIf mPluginConfig.OutputManualIdentify Then
                    mpreviousIdentId = 0 'but not when you id manually

                    'If mPluginConfig.OutputManualIgnoreSelf AndAlso (no.Container = Core.CharacterFilter.Id AndAlso no.IntValues(LongValueKey.Slot) = -1) Then
                    '    scanitem = False
                    'ElseIf mPluginConfig.OutputManualIgnoreMobs AndAlso no.ObjectClass = ObjectClass.Monster Then
                    '    scanitem = False
                    'Else
                    scanitem = True
                    If Host.Actions.CurrentSelection = no.Id Then
                        copyclipboard = mPluginConfig.CopyToClipboard
                        manualIdent = True
                    End If

                    ' End If

                End If

                If scanitem Then
                    Dim result As eScanresult = CheckObjectForMatch(no, manualIdent)

                    'check scan finished

                    If mCurrentContainerContent.ContainsKey(no.Id) Then
                        mCurrentContainerContent.Item(no.Id) = 1
                    End If

                    If mCurrentContainer <> 0 And no.Container = mCurrentContainer And Not manualIdent Then
                        Dim finished As Boolean = finishedscanning()
                        If mCorpsScanning And finished Then
                            scanningcompleted()
                        End If
                        mCorpsScanning = Not finished
                    End If


                    If no.MaxItemLevel <> 0 And no.CurrentItemLevel < no.MaxItemLevel Then

                        'auto track xp gain on
                        If mCharconfig.trackobjectxpHudId = 0 AndAlso no.IntValues(LongValueKey.EquippedSlots) <> 0 AndAlso Core.CharacterFilter.Level >= 275 Then
                            mCharconfig.trackobjectxpHudId = no.Id
                            '    wtcw("Auto track xp has selected: " & no.Name & " to monitor")
                        End If

                        If mObjectxph.ContainsKey(no.Id) Then
                            mObjectxph(no.Id).xpcurrent = no.Itemxp
                            If mObjectxph(no.Id).xpstart = 0 Then ' xp reset
                                mObjectxph(no.Id).xpstart = no.Itemxp
                            End If
                        Else
                            Dim x As New objectxph
                            x.xpstart = no.Itemxp
                            x.xpcurrent = no.Itemxp
                            mObjectxph.Add(no.Id, x)
                        End If
                        'If mCharconfig.trackobjectxpHudId = no.Id Then
                        '    If mPluginConfig.hudflags1 = 1 Then
                        '        xphour()
                        '    End If
                        'End If
                    End If


                    If result = eScanresult.nomatch And manualIdent Then
                        Dim tochat As String = no.ToString
                        If tochat <> String.Empty Then

                            If mPluginConfig.Showpalette AndAlso mModelData.ContainsKey(no.Id) Then
                                tochat &= " {" & mModelData.Item(no.Id).ToString & "}"
                            End If

                            If no.Itemxp > 0 AndAlso mObjectxph.ContainsKey(no.Id) Then

                                With mObjectxph.Item(no.Id)
                                    tochat &= " " & xphour(no.Itemxp, .xpstart, no.NextItemlvlxp)
                                End With

                            End If

                            wtcw(tochat)
                        End If

                    ElseIf result <> eScanresult.nomatch Then
                        Renderhud()
                    End If

                End If
            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Function isBlockedContainer(ByVal id As Integer) As Boolean
        Dim blocked As Boolean
        Dim objcontainer As WorldObject = Core.WorldFilter.Item(id)
        If objcontainer IsNot Nothing Then
            If objcontainer.Name = "Storage" Or objcontainer.Id = Core.CharacterFilter.Id Then
                blocked = True
            ElseIf objcontainer.Container <> 0 Then 'pack in storage or corpse?
                Return isBlockedContainer(objcontainer.Container)
            End If
        End If
        Return blocked
    End Function

    Private mCorpseWithRareId As Integer

    Private Function CheckObjectForMatch(ByVal objNotify As IdentifiedObject, ByVal manualIdent As Boolean) As eScanresult
        Dim scanresult As eScanresult = eScanresult.nomatch
        Try
            With objNotify

                Dim tradewindowscan As Boolean = _
                mCollTradeItemRecieved.ContainsKey(.Id)

                Dim xa As Alert = Nothing

                Dim identrecieved As Boolean
                If mIdqueue.ContainsKey(objNotify.Id) Then
                    mIdqueue.Remove(objNotify.Id)
                    identrecieved = True
                End If

                If .isvalid Then

                    Dim sObjectClass As String = .ObjectClass.ToString
                    Dim result As Boolean = False
                    If Not manualIdent Then

                        If .Container <> 0 AndAlso isBlockedContainer(.Container) Then
                            wtcwd("isBlockedContainer")
                            Return eScanresult.nomatch
                        End If
                    End If

                    'Coalesced Aetheria

                    Select Case .ObjectClass
                        Case ObjectClass.Scroll
                            xa = CheckItemForUnknownScroll(.IntValues(LongValueKey.AssociatedSpell))
                            If xa IsNot Nothing Then
                                NotifyObject(sObjectClass, objNotify, xa, True, eScanresult.spell)
                            Else
                                Dim r As rule = MatchingRuleForScroll(.IntValues(LongValueKey.AssociatedSpell), tradewindowscan)
                                If r IsNot Nothing Then
                                    xa = Nothing

                                    xa = getAlert(r.wavfile)

                                    NotifyObject(r.name, objNotify, xa, Not manualIdent, eScanresult.rule)
                                    Return eScanresult.rule
                                End If

                            End If

                        Case ObjectClass.Corpse
                            Dim na As notify
                            If mProtectedCorpses.Contains(.Name) Then

                                na = NotifyObject(sObjectClass, objNotify, Nothing, False, eScanresult.corpseself)
                                If Not mNotifiedCorpses.ContainsKey(.Id) Then
                                    mNotifiedCorpses.Add(.Id, na)
                                End If
                                Return eScanresult.corpseself
                            ElseIf (mPluginConfig.notifycorpses And .IntValues(LongValueKey.Burden) > 6000) Then

                                If Not identrecieved And Not .HasIdData Then
                                    If Not mCorpseCache.Contains(.Id) Then
                                        IdqueueAdd(.Id)
                                        Return eScanresult.needsident
                                    End If
                                ElseIf Not mCorpseCache.Contains(.Id) Then
                                    If mCorpseCache.Count > mPluginConfig.CorpseCache Then
                                        mCorpseCache.Dequeue()
                                    End If
                                    mCorpseCache.Enqueue(.Id)

                                    Dim FullDescription As String = .StringValues(StringValueKey.FullDescription)
                                    If Not String.IsNullOrEmpty(FullDescription) Then
                                        If FullDescription.IndexOf(Core.CharacterFilter.Name) >= 0 Then

                                            If FullDescription.IndexOf("generated a rare item") > 0 Then
                                                mCharconfig.lastrarefound = Now
                                                mCorpseWithRareId = .Id
                                                scanresult = eScanresult.corpsewithrare
                                            Else
                                                scanresult = eScanresult.corpse
                                            End If

                                            na = NotifyObject(sObjectClass, objNotify, Nothing, False, scanresult)
                                            If Not mNotifiedCorpses.ContainsKey(.Id) Then
                                                mNotifiedCorpses.Add(.Id, na)
                                            End If

                                        End If
                                    End If

                                End If

                            End If

                        Case ObjectClass.Monster, ObjectClass.Player

                            If .ObjectClass = ObjectClass.Monster AndAlso mCharconfig.ShowAllMobs Then
                                xa = CheckForNotifyMonster(.Name)
                                If xa Is Nothing Then
                                    Dim lb As Integer = .IntValues(LongValueKey.Landblock)
                                    Dim outdoors As Boolean = CBool((Host.Actions.Landcell And &HFF00&) = 0)
                                    Dim di As Integer = Host.Actions.Landcell >> &H10
                                    di = di Xor &HFFFF0000

                                    'wtcw(.Name & Hex(lb) & " " & CBool((lb And &HFF00&) = 0))

                                    'Dim moboutdoors As Boolean = CBool((lb And &HFF00&) = 0)

                                    If Not outdoors And (&HFFFF482D <> di) Then

                                        xa = getAlert(mPluginConfig.AlertKeyMob)
                                        xa.wavfilename = String.Empty
                                    End If

                                End If


                            ElseIf .ObjectClass = ObjectClass.Player AndAlso mCharconfig.ShowAllPlayers AndAlso .Id <> Core.CharacterFilter.Id Then
                                xa = getAlert(mPluginConfig.AlertKeyMob)
                                xa.wavfilename = String.Empty
                            Else
                                xa = CheckForNotifyMonster(.Name)
                            End If

                            If xa IsNot Nothing Then
                                scanresult = eScanresult.monstertokill
                                NotifyObject(sObjectClass, objNotify, xa, True, scanresult)
                            End If

                            If xa IsNot Nothing Then

                                If mPluginConfig.notifyalleg AndAlso Core.CharacterFilter.Monarch IsNot Nothing AndAlso Core.CharacterFilter.Monarch.Id <> 0 Then
                                    If Core.CharacterFilter.Monarch.Id = .IntValues(LongValueKey.Monarch) Then
                                        scanresult = eScanresult.allegmembers
                                        NotifyObject("Allegiance", objNotify, Nothing, True, scanresult)
                                    End If
                                End If

                            End If


                        Case ObjectClass.Portal, ObjectClass.Lifestone
                            If mPluginConfig.NotifyPortals Then
                                If mPluginConfig.PortalExclude IsNot Nothing Then
                                    For Each s As String In mPluginConfig.PortalExclude
                                        If .Name = s Then
                                            Return eScanresult.nomatch
                                        End If
                                    Next
                                End If
                                xa = getAlert(mPluginConfig.AlertKeyPortal)

                                NotifyObject(sObjectClass, objNotify, xa, True, eScanresult.portalOrLifestone)
                            End If


                        Case Else  'main loot data

                            If .Name = "Coalesced Aetheria" Then
                                If Not .HasIdData Then
                                    IdqueueAdd(.Id)
                                    Return eScanresult.needsident
                                Else
                                    If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey("Coalesced Aetheria") Then
                                        With mActiveThropyProfile.Item("Coalesced Aetheria")
                                            If .lootmax > 0 Then
                                                If objNotify.MaxItemLevel < .lootmax Then
                                                    Return eScanresult.nomatch
                                                End If
                                            End If

                                            xa = getAlert(.Alert)

                                            If xa Is Nothing Then
                                                xa = getAlert(mPluginConfig.AlertKeyThropy)
                                            End If

                                            If xa Is Nothing Then
                                                xa = New Alert
                                                xa.volume = 0
                                                xa.name = "Aetheria"
                                                xa.showinchatwindow = -1
                                            End If
                                            NotifyObject(xa.name, objNotify, xa, Not manualIdent, eScanresult.trophy)
                                            Return eScanresult.trophy
                                        End With
                                    End If
                                End If
                            End If

                            If .IntValues(LongValueKey.RareId) <> 0 Then
                                xa = getAlert(mPluginConfig.AlertKeyThropy)
                                NotifyObject("Rare", objNotify, xa, True, eScanresult.rare)
                                Return eScanresult.rare
                            End If

                            If .ObjectClass <> ObjectClass.MissileWeapon Then 'rock
                                xa = CheckForThropyOrNpc(.Name)
                            End If


                            If xa IsNot Nothing Then
                                Dim rs As eScanresult = eScanresult.trophy

                                If .ObjectClass = ObjectClass.Npc Then
                                    rs = eScanresult.npc
                                End If

                                NotifyObject(xa.name, objNotify, xa, Not manualIdent, rs)
                                Return rs
                            End If

                            If Not identrecieved And Not .HasIdData Then
                                If (mCorpseWithRareId <> 0) AndAlso .Container = mCorpseWithRareId Then
                                    IdqueueAdd(.Id)
                                    wtcwd("id queue add " & .Name & " C:" & Hex(.Container), 3)
                                    Return eScanresult.needsident
                                End If
                            End If

                            'check if a Ident is needed
                            If Not identrecieved And Not .HasIdData AndAlso .DblValues(DoubleValueKey.SalvageWorkmanship) > 0 Then
                                Select Case .ObjectClass
                                    'TODO check against active rules which types we are interrested in
                                    '(no need to ident a dagger if there is no rule for dagger)

                                    Case ObjectClass.MissileWeapon
                                        'bow = 1
                                        'atlan = 4
                                        'xbow = 2
                                        If .IntValues(LongValueKey.MissileType) = 1 Or .IntValues(LongValueKey.MissileType) = 4 Or .IntValues(LongValueKey.MissileType) = 2 Then

                                            IdqueueAdd(.Id)
                                            wtcwd("id queue add " & .Name & " C:" & Hex(.Container), 3)
                                            Return eScanresult.needsident
                                        End If


                                    Case ObjectClass.Armor, ObjectClass.Clothing, ObjectClass.Jewelry, ObjectClass.MeleeWeapon, ObjectClass.WandStaffOrb, ObjectClass.MissileWeapon

                                        IdqueueAdd(.Id)
                                        wtcwd("id queue add " & .Name & " C:" & Hex(.Container), 3)
                                        Return eScanresult.needsident
                                End Select
                            End If

                            Dim r As rule = Nothing

                            If .HasIdData Then
                                r = MatchingRule(objNotify, tradewindowscan)
                            End If

                            Dim msg As String = String.Empty


                            If r IsNot Nothing Then
                                xa = Nothing
                                'find alert
                                xa = getAlert(r.wavfile)

                                NotifyObject(r.name, objNotify, xa, Not manualIdent, eScanresult.rule)
                                Return eScanresult.rule

                            ElseIf Not tradewindowscan Then


                                Dim salvagecheck As String = CheckItemForSalvage(CInt(objNotify.DblValues(DoubleValueKey.SalvageWorkmanship)), .IntValues(LongValueKey.Material))

                                If salvagecheck <> String.Empty Then

                                    xa = getAlert(mPluginConfig.AlertKeySalvage)
                                    NotifyObject(salvagecheck, objNotify, xa, Not manualIdent, eScanresult.salvage)
                                    Return eScanresult.salvage
                                Else
                                    If .DblValues(DoubleValueKey.SalvageWorkmanship) > 0 AndAlso CheckItemForValue(.IntValues(LongValueKey.Value), .IntValues(LongValueKey.Burden)) Then
                                        NotifyObject("Value", objNotify, Nothing, Not manualIdent, eScanresult.value)

                                        Return eScanresult.value
                                    ElseIf CheckItemForMana(.IntValues(LongValueKey.CurrentMana)) Then
                                        NotifyObject("Manatank", objNotify, Nothing, Not manualIdent, eScanresult.manatank)
                                        Return eScanresult.manatank
                                    End If
                                    'check for mana/value

                                    If identrecieved Then
                                        wtcwd("no match after Ident: " & .Name & ":" & .ObjectClass.ToString, 5)
                                    Else
                                        If .ObjectClass = ObjectClass.MissileWeapon And objNotify.DblValues(DoubleValueKey.SalvageWorkmanship) = 0 Then
                                            Return eScanresult.nomatch  'not logging flying objects (rock/arrows)
                                        End If
                                        wtcwd("no match NO ident : " & .Name & ":" & .ObjectClass.ToString, 4)
                                    End If

                                End If
                            End If


                    End Select
                Else
                    wtcw("not a valid object")
                End If

            End With
        Catch ex As Exception
            Util.ErrorLogger(ex)
        Finally

        End Try

        Return scanresult
    End Function

    Private Class notify
        Public icon As Integer
        Public id As Integer
        Public description As String
        Public name As String
        Public scantype As eScanresult
        Public range As Double
        Public ColorArgb As Integer

        'expirimental
        Public msg As String
        Public xa As Alert
        Public playsound As Boolean
        Public markobject As Boolean
    End Class

    Private Sub NotifyObjectsFromQueue()
        Try

            Do
                If mNotifyObjectQueue.Count > 0 Then
                    Dim no As notify
                    no = mNotifyObjectQueue.Dequeue

                    If no.xa IsNot Nothing Then
                        If no.playsound Then

                            If no.markobject Then
                                markobject1(no.id)
                            End If

                            PlaySoundFile(no.xa.wavfilename, no.xa.volume)
                        End If

                        If no.xa.showinchatwindow > 0 Then
                            Host.Actions.AddChatText(no.msg & no.markobject & no.playsound, no.xa.chatcolor, no.xa.showinchatwindow)
                        End If

                    Else
                        wtcw(no.msg, 14)
                    End If
                End If

            Loop Until mNotifyObjectQueue.Count = 0

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    Private Function NotifyObject(ByVal sDescription As String, ByVal no As IdentifiedObject, ByVal xa As Alert, ByVal addtoLoot As Boolean, ByVal scantype As eScanresult) As notify
        Dim newobject As New notify
        Const testpreformacequeue As Boolean = False


        Try

            newobject.icon = no.Icon
            newobject.id = no.Id
            newobject.name = no.Name
            newobject.description = sDescription
            newobject.scantype = scantype
            Select Case scantype

                Case eScanresult.corpsewithrare
                    newobject.ColorArgb = &HFF000000 Or eSomeTestColorsArg.Pink
                Case eScanresult.corpseself
                    newobject.ColorArgb = &HFF000000 Or eSomeTestColorsArg.Blue
                Case eScanresult.portalOrLifestone
                    newobject.ColorArgb = &HFF000000 Or eSomeTestColorsArg.Gold
                Case Else
                    newobject.ColorArgb = &HFF000000 Or eSomeTestColorsArg.White

            End Select


            Dim bplaysound As Boolean

            If addtoLoot AndAlso Not mNotifiedItems.ContainsKey(no.Id) Then
                bplaysound = True
                mNotifiedItems.Add(no.Id, newobject)
            End If

            If xa IsNot Nothing Then
                Dim msg As String = notifystring(sDescription, no, False)

                If bplaysound Then
                    If no.Container = 0 And scantype <> eScanresult.portalOrLifestone Then
                        If Not testpreformacequeue Then
                            markobject1(no.Id)
                        End If

                        newobject.markobject = True
                    End If
                    If Not testpreformacequeue Then
                        PlaySoundFile(xa.wavfilename, xa.volume)
                    End If

                End If
                If Not testpreformacequeue Then
                    If xa.showinchatwindow > 0 Then
                        Host.Actions.AddChatText(msg, xa.chatcolor, xa.showinchatwindow)
                    End If
                End If

                newobject.playsound = bplaysound
                newobject.msg = msg
                newobject.xa = xa
            Else
                Dim msg As String = notifystring(sDescription, no, False)
                If Not testpreformacequeue Then
                    wtcw(msg, 14)
                End If

                newobject.msg = msg
            End If

            If testpreformacequeue Then
                mNotifyObjectQueue.Enqueue(newobject)
            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

        Return newobject
    End Function

    Private Function minSalvageFromString(ByVal str As String) As Double
        Dim value As Double

        If str IsNot Nothing AndAlso str.Trim.Length > 0 Then
            Dim arr() As String
            arr = Split(str, ",")
            Double.TryParse(arr(0), value)
        End If

        Return value
    End Function

    Private Sub setMinMaxSalvage(ByRef minsalvage As Integer, ByRef maxsalvage As Integer, ByVal Material As Integer, ByVal Workmanship As Integer)
        If mActiveSalvageProfile.ContainsKey(Material) Then
            Dim SalvageSetup As SalvageSettings = mActiveSalvageProfile.Item(Material)
            minsalvage = 0
            maxsalvage = 0
            If Not SalvageSetup Is Nothing Then
                Dim arrCombine() As String = Split(SalvageSetup.combinestring, ",")
                maxsalvage = 10
                minsalvage = CInt(arrCombine(0))

                If Workmanship < minsalvage Then
                    maxsalvage = minsalvage - 1
                    minsalvage = 1
                    Return
                End If

                For i As Integer = 1 To UBound(arrCombine)
                    maxsalvage = CInt(arrCombine(i))

                    If (Workmanship >= minsalvage) AndAlso (Workmanship <= maxsalvage - 1) Then
                        maxsalvage -= 1
                        Return
                    End If
                    minsalvage = maxsalvage
                Next
                If minsalvage > maxsalvage Then
                    maxsalvage = 10
                End If
            End If
        End If
    End Sub



    Private Function coordsToString(ByVal coords As CoordsObject) As String
        Dim result As String = String.Empty

        If coords IsNot Nothing Then
            result = String.Concat(New String() {Math.Abs(coords.NorthSouth).ToString("0.00", Util.NumberFormatInfo), CStr(IIf((coords.NorthSouth >= 0), "N", "S")), ", ", Math.Abs(coords.EastWest).ToString("0.00", Util.NumberFormatInfo), CStr(IIf((coords.EastWest >= 0), "E", "W"))})

            result = "(" & "<Tell:IIDString:" & GOARROWLINK_ID & ":" & result + ">" & result & "<\Tell>" & ") "
        End If

        Return result
    End Function

    Private Function notifystring(ByVal sDescription As String, ByVal no As IdentifiedObject, ByVal manual As Boolean) As String
        Dim msg As String = String.Empty
        Dim link As String = String.Empty
        Dim colorstring As String = String.Empty
        If mPluginConfig.Showpalette AndAlso mModelData.ContainsKey(no.Id) Then
            colorstring = " {" & mModelData.Item(no.Id).ToString & "}"
        End If

        If manual Then
            msg = no.ToString() & colorstring
            If msg = String.Empty Then
                Return msg
            End If
            If mPluginConfig.CopyToClipboard Then
                Try
                    Windows.Forms.Clipboard.Clear()
                    Windows.Forms.Clipboard.SetText(msg)
                Catch ex As Exception

                End Try
            End If

            If sDescription <> String.Empty Then
                link = " <Tell:IIDString:" & NOTIFYLINK_ID & ":" & no.Id & ">(" & sDescription & ")<\\Tell>"
            End If

            If no.Container = 0 Then
                msg &= link & " " & coordsToString(no.Coordinates)
            Else
                msg &= link
            End If
        Else
            If sDescription <> String.Empty Then
                link = "<Tell:IIDString:" & NOTIFYLINK_ID & ":" & no.Id & ">(" & sDescription & ")<\\Tell>"
            End If

            If no.Container = 0 Then
                msg = link & no.ToString() & colorstring & " " & coordsToString(no.Coordinates)
            Else
                msg = link & no.ToString() & colorstring
            End If
        End If


        Return msg
    End Function



    Private Sub removeNotifyObject(ByVal id As Integer)
        Try

            If mNotifiedCorpses.ContainsKey(id) Then
                mNotifiedCorpses.Remove(id)
            End If
            If mNotifiedItems.ContainsKey(id) Then
                mNotifiedItems.Remove(id)
            End If
            If mUstItems.ContainsKey(id) Then
                mUstItems.Remove(id)
                For i As Integer = 0 To lstUstList.RowCount - 1
                    Dim lRow As Decal.Adapter.Wrappers.ListRow
                    lRow = lstUstList(i)
                    Dim idrow As Integer = CType(lRow(3)(0), Integer)
                    If id = idrow Then
                        lstUstList.Delete(i)
                        lstUstList.JumpToPosition(0)
                        Exit For
                    End If
                Next
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    ' for when the loot maximum is set
    Private Function countnotify(ByVal snaam As String) As Integer
        Dim count As Integer = 0

        Try

            For Each d As KeyValuePair(Of Integer, notify) In mNotifiedItems
                Dim pObject As WorldObject = Core.WorldFilter.Item(CInt(d.Key))
                If pObject IsNot Nothing Then
                    If pObject.Name = snaam Then
                        count += 1
                    End If
                End If
            Next

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return count
    End Function

    Private Function CountInventoryItem(ByVal name As String) As Integer
        Dim oCol As Decal.Adapter.Wrappers.WorldObjectCollection = Core.WorldFilter.GetInventory
        Dim count As Integer = 0

        If oCol IsNot Nothing Then
            For Each b As Decal.Adapter.Wrappers.WorldObject In oCol
                If b.Name = name Then

                    count += b.Values(LongValueKey.StackCount, 1)
                End If
            Next
        End If

        Return count
    End Function

    Private Function CheckItemForMana(ByVal mana As Integer) As Boolean
        If mPluginConfig.notifyItemmana > 0 AndAlso mana > mPluginConfig.notifyItemmana Then
            Return True
        End If
    End Function

    Private Function CheckItemForValue(ByVal value As Integer, ByVal burden As Integer) As Boolean

        Dim ratio As Double = value / burden

        If mPluginConfig.notifyItemvalue > 0 AndAlso value > mPluginConfig.notifyItemvalue AndAlso ratio > mPluginConfig.notifyValueBurden Then
            Return True
        End If

    End Function

    Private Function CheckItemForSalvage(ByVal Workmanship As Integer, ByVal Material As Integer) As String
        mSalvagelookup.totalq += 1
        Dim sw As New System.Diagnostics.Stopwatch
        sw.Start()

        Try
            If mActiveSalvageProfile IsNot Nothing AndAlso mActiveSalvageProfile.ContainsKey(Material) Then

                Dim entry As SalvageSettings = mActiveSalvageProfile.Item(Material)

                If Not entry Is Nothing AndAlso entry.checked AndAlso Workmanship >= minSalvageFromString(entry.combinestring) Then

                    Dim minsal, maxsal As Integer
                    setMinMaxSalvage(minsal, maxsal, Material, Workmanship)

                    If minsal > 0 And maxsal <= 10 Then
                        If minsal = maxsal Then
                            Return "Sal " & minsal
                        Else
                            Return "Sal " & minsal & "-" & maxsal
                        End If
                    End If

                End If

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        Finally
            sw.Stop()
            Dim ms As Long = sw.ElapsedTicks
            If mSalvagelookup.totalq = 1 Then
                mSalvagelookup.fastest = sw.Elapsed
                mSalvagelookup.slowest = sw.Elapsed
                mSalvagelookup.totalspend = sw.Elapsed
            Else
                mSalvagelookup.totalspend += sw.Elapsed
                If mSalvagelookup.fastest > sw.Elapsed Then
                    mSalvagelookup.fastest = sw.Elapsed
                End If
                If mSalvagelookup.slowest < sw.Elapsed Then
                    mSalvagelookup.slowest = sw.Elapsed
                End If
            End If
        End Try



        Return String.Empty
    End Function

    Private Function CheckForThropyOrNpc(ByVal name As String) As Alert
        If mActiveThropyProfile IsNot Nothing AndAlso Not String.IsNullOrEmpty(name) Then
            mThropylookup.totalq += 1
            Dim sw As New System.Diagnostics.Stopwatch
            sw.Start()

            Try
                Dim selection = From m In mActiveThropyProfile Select m _
                                 Where (m.Value.checked) AndAlso _
                                 ((Not m.Value.ispartial And name = m.Key) OrElse (m.Value.ispartial AndAlso name.ToLower.IndexOf(m.Key.ToLower) >= 0))

                If selection.Count > 0 Then
                    Dim th As ThropyInfo = Nothing

                    For Each x In selection
                        th = mActiveThropyProfile.Item(x.Key)
                        If th.lootmax > 0 Then

                            'count not looted yet
                            Dim ncount As Integer = countnotify(name)

                            If ncount >= th.lootmax Then
                                Return Nothing
                            End If

                            'count in inventory
                            If CountInventoryItem(name) + ncount >= th.lootmax Then
                                Return Nothing
                            End If

                        End If
                    Next

                    If th IsNot Nothing Then
                        Dim xa As Alert = getAlert(th.Alert)

                        If xa Is Nothing Then
                            xa = getAlert(mPluginConfig.AlertKeyThropy)
                        End If

                        If xa Is Nothing Then
                            xa = New Alert
                            xa.volume = 0
                            xa.name = "Trophy"
                            xa.showinchatwindow = -1
                        End If

                        Return xa
                    End If

                End If
            Catch ex As Exception
                Util.ErrorLogger(ex)
            Finally
                sw.Stop()
                Dim ms As Long = sw.ElapsedTicks
                If mThropylookup.totalq = 1 Then
                    mThropylookup.fastest = sw.Elapsed
                    mThropylookup.slowest = sw.Elapsed
                    mThropylookup.totalspend = sw.Elapsed
                Else
                    mThropylookup.totalspend += sw.Elapsed
                    If mThropylookup.fastest > sw.Elapsed Then
                        mThropylookup.fastest = sw.Elapsed
                    End If
                    If mThropylookup.slowest < sw.Elapsed Then
                        mThropylookup.slowest = sw.Elapsed
                    End If
                End If
            End Try

        End If

        Return Nothing
    End Function

    Private Function CheckForNotifyMonster(ByVal name As String) As Alert
        mMoblookup.totalq += 1
        Dim sw As New System.Diagnostics.Stopwatch
        sw.Start()
        Try


            If mActiveMobProfile IsNot Nothing AndAlso Not String.IsNullOrEmpty(name) Then

                Dim selection = From m In mActiveMobProfile Select m _
                             Where (m.Value.checked) AndAlso _
                             ((Not m.Value.ispartial And name = m.Key) OrElse (m.Value.ispartial AndAlso name.ToLower.IndexOf(m.Key.ToLower) >= 0))

                If selection.Count > 0 Then
                    For Each x In selection
                        Dim xa As Alert = getAlert(x.Value.Alert)

                        If xa Is Nothing Then
                            xa = getAlert(mPluginConfig.AlertKeyMob)

                        End If

                        Return xa

                    Next
                Else
                    Return Nothing
                End If


            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        Finally
            sw.Stop()
            Dim ms As Long = sw.ElapsedTicks
            If mMoblookup.totalq = 1 Then
                mMoblookup.fastest = sw.Elapsed
                mMoblookup.slowest = sw.Elapsed
                mMoblookup.totalspend += sw.Elapsed
            Else
                mMoblookup.totalspend += sw.Elapsed
                If mMoblookup.fastest > sw.Elapsed Then
                    mMoblookup.fastest = sw.Elapsed
                End If
                If mMoblookup.slowest < sw.Elapsed Then
                    mMoblookup.slowest = sw.Elapsed
                End If
            End If
        End Try

        Return Nothing
    End Function

    Private mDebugRules As Boolean = False

    Private Sub rulelog(ByVal msg As String)
        If mDebugRules Then
            Host.Actions.AddChatText(msg, 11, 2)
        End If
    End Sub

    Private Function MatchingRuleForScroll(ByVal assoSpellId As Integer, ByVal tradewindow As Boolean) As rule
        Try
            If mActiveRulesProfile IsNot Nothing AndAlso assoSpellId <> 0 Then

                For Each r As rule In mActiveRulesProfile
                    If Not r Is Nothing AndAlso r.enabled AndAlso ((tradewindow = False And r.tradebotonly = False) Or (tradewindow = True And (r.tradebot = True Or r.tradebotonly = True))) Then
                        If (r.appliesToFlag And eIdflags.Scroll) = eIdflags.Scroll Then
                            If r.spells IsNot Nothing AndAlso r.spells.Count > 0 Then
                                If r.spells.Contains(assoSpellId) Then
                                    Return r
                                End If
                            End If
                        End If
                    End If
                Next

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

        Return Nothing
    End Function

    Private Function MatchingRule(ByVal bo As IdentifiedObject, ByVal tradewindow As Boolean) As rule
        mRulelookup.totalq += 1
        Dim sw As New System.Diagnostics.Stopwatch
        sw.Start()

        Try
            Dim wield As Integer
            Dim wieldtype As Integer
            Dim subType As Integer
            Dim meleemod As Double
            Dim armorlevel As Integer
            Dim armorcoverage As Integer
            Dim armortype As Integer

            Dim mcmodattackbonus As Double

            Dim mindamage As Double
            Dim maxdamage As Double
            Dim magicdbonus As Double
            Dim damagetypes As Integer
            Dim isweapon As Boolean = False

            'only loot
            If Not bo.IntValues(LongValueKey.Workmanship) > 0 Then
                Return Nothing
            End If

            Select Case bo.ObjectClass
                Case ObjectClass.Armor, ObjectClass.WandStaffOrb, ObjectClass.Clothing, ObjectClass.Jewelry, ObjectClass.MeleeWeapon, ObjectClass.MissileWeapon
                Case Else
                    Return Nothing
            End Select

            If bo.ObjectClass = ObjectClass.WandStaffOrb OrElse bo.ObjectClass = ObjectClass.MeleeWeapon OrElse bo.ObjectClass = ObjectClass.MissileWeapon Then
                isweapon = True

                damagetypes = bo.IntValues(LongValueKey.DamageType)
                meleemod = bo.WeaponMeleeBonus
                magicdbonus = bo.WeaponMagicDBonus

                If bo.ObjectClass = ObjectClass.MissileWeapon Then

                    subType = bo.IntValues(LongValueKey.MissileType)
                    mcmodattackbonus = bo.DamageBonusMissile
                    maxdamage = bo.IntValues(LongValueKey.ElementalDmgBonus)

                ElseIf bo.ObjectClass = ObjectClass.WandStaffOrb Then
                    subType = 0
                    mcmodattackbonus = bo.WeaponManaCBonus
                    maxdamage = bo.DamageVsMonsters

                Else 'melee
                    subType = bo.IntValues(LongValueKey.WieldReqAttribute)
                    mcmodattackbonus = bo.WeaponAttackBonus
                    maxdamage = bo.WeaponMaxDamage
                    Dim Variance As Double = bo.DblValues(DoubleValueKey.Variance)
                    mindamage = Math.Round((maxdamage - (maxdamage * Variance)), 2)

                End If

            ElseIf bo.ObjectClass = ObjectClass.Armor Then
                armorlevel = bo.IntValues(LongValueKey.ArmorLevel)
                armorcoverage = bo.IntValues(LongValueKey.Coverage)
                armortype = 0
                For Each xo As GameData.NameId In Plugin.GameData.RuleArmorTypes
                    If bo.Name.IndexOf(xo.name) >= 0 Then
                        armortype = xo.Id
                        Exit For
                    End If
                Next
                If armortype = 0 Then
                    armortype = 2048
                End If
            End If
            wieldtype = bo.IntValues(LongValueKey.WieldReqType)
            wield = bo.IntValues(LongValueKey.WieldReqValue)
            Dim work As Integer = bo.IntValues(LongValueKey.Workmanship)
            Dim burden As Integer = bo.IntValues(LongValueKey.Burden)
            Dim value As Integer = bo.IntValues(LongValueKey.Value)
            Dim nmatch As Integer = 0
            Dim setid As Integer = bo.PieceOfSetId
            Dim ivoryable As Boolean = bo.BoolValues(BoolValueKey.Ivoryable)

            If wieldtype = &H7 And bo.ObjectClass = ObjectClass.WandStaffOrb Then
                wield = 0
            End If
            If maxdamage < 0 Then maxdamage = 0

            If mActiveRulesProfile IsNot Nothing Then

                For Each r As rule In mActiveRulesProfile


                    If Not r Is Nothing AndAlso r.enabled AndAlso ((tradewindow = False And r.tradebotonly = False) Or (tradewindow = True And (r.tradebot = True Or r.tradebotonly = True))) AndAlso r.EmptyRule = False Then
                        rulelog("enter rule: " & r.name & " with-> " & bo.Name)
                        nmatch = 0

                        If isweapon Then


                            If r.weapontype <> eRuleWeaponTypes.notapplicable AndAlso (r.weaponsubtype <> subType) Then
                                rulelog("next rule eRuleWeaponTypesnotapplicable " & r.weaponsubtype & " <> " & subType)

                                GoTo nextrule
                            ElseIf r.weapontype <> eRuleWeaponTypes.notapplicable Then

                                Select Case r.weapontype
                                    Case eRuleWeaponTypes.atlan, eRuleWeaponTypes.bow, eRuleWeaponTypes.crossbow
                                        If bo.ObjectClass <> ObjectClass.MissileWeapon Then

                                            rulelog("next !=eObjectClass.eMissileWeapon ")

                                            GoTo nextrule
                                        Else

                                            rulelog("match ==eObjectClass.eMissileWeapon ")

                                            nmatch += 1
                                        End If
                                    Case eRuleWeaponTypes.mage
                                        If bo.ObjectClass <> ObjectClass.WandStaffOrb Then

                                            rulelog("next eWandStaffOrb ")

                                            GoTo nextrule
                                        Else

                                            rulelog("match eWandStaffOrb  ")

                                            nmatch += 1
                                        End If
                                    Case Else
                                        If bo.ObjectClass <> ObjectClass.MeleeWeapon Then

                                            rulelog("next eMeleeWeapon  ")

                                            GoTo nextrule
                                        Else
                                            rulelog("match eMeleeWeapon  ")

                                            nmatch += 1
                                        End If
                                End Select


                                If r.minmeleebonus > 0 AndAlso meleemod < r.minmeleebonus Then

                                    rulelog("next minmeleebonus : " & meleemod)

                                    GoTo nextrule
                                ElseIf r.minmeleebonus > 0 Then

                                    rulelog("match minmeleebonus : " & meleemod)

                                    nmatch += 1
                                End If

                                If r.minmagicdbonus > 0 AndAlso magicdbonus < r.minmagicdbonus Then

                                    rulelog("next magicdbonus : " & magicdbonus)

                                    GoTo nextrule

                                ElseIf r.minmagicdbonus > 0 Then

                                    rulelog("match magicdbonus : " & magicdbonus)

                                    nmatch += 1
                                End If

                                If r.minmcmodattackbonus > 0 AndAlso CInt(mcmodattackbonus) < r.minmcmodattackbonus Then

                                    rulelog("next minmcmodattackbonus : " & mcmodattackbonus & " " & r.minmcmodattackbonus)

                                    GoTo nextrule
                                ElseIf r.minmcmodattackbonus > 0 Then

                                    rulelog("match minmcmodattackbonus : " & mcmodattackbonus)

                                    nmatch += 1
                                End If
                                ' skip when item is ivory able and rule ivoryalbl
                                Dim skip As Boolean
                                skip = r.ivoryable = True AndAlso ivoryable = True

                                If Not skip Then ' skip damage

                                    Dim wm As Boolean
                                    For Each d As rule.damagerange In r.damage

                                        If d.enabled AndAlso d.maxwield >= 0 AndAlso wield = d.maxwield Then

                                            If d.mindamage > 0 AndAlso mindamage < d.mindamage Then

                                                rulelog("next mindamage : " & mindamage & " < " & d.mindamage)

                                                GoTo nextrule
                                            End If

                                            If d.maxdamage > 0 AndAlso maxdamage < d.maxdamage Then

                                                rulelog("next maxdamage : " & maxdamage)

                                                GoTo nextrule

                                            ElseIf d.maxdamage >= 0 Then

                                                rulelog("match maxdamage : " & maxdamage)

                                                wm = True
                                            End If

                                        ElseIf Not d.enabled AndAlso d.maxwield >= 0 AndAlso wield = d.maxwield Then

                                            rulelog("sub wield disabled : " & maxdamage)


                                            GoTo nextrule
                                        End If
                                    Next

                                    If Not wm Then
                                        rulelog("next no maxdamage match: " & maxdamage & " wield = " & wield)
                                        GoTo nextrule
                                    Else
                                        nmatch += 1
                                    End If

                                End If

                                If r.damagetypeFlag > 0 Then
                                    If (damagetypes And r.damagetypeFlag) = damagetypes Then

                                        rulelog("match damagetype  ")

                                        nmatch += 1
                                    Else

                                        rulelog("nextrule damagetype  " & damagetypes)

                                        GoTo nextrule
                                    End If
                                End If
                            ElseIf bo.ObjectClass = ObjectClass.MeleeWeapon Then ' == notapplicable

                                If Not (r.appliesToFlag And eIdflags.MeleeWeapon) = eIdflags.MeleeWeapon Then

                                    rulelog("next rule: r.appliesToFlag meleeweapon")

                                    GoTo nextrule
                                End If
                            ElseIf bo.ObjectClass = ObjectClass.MissileWeapon Then  ' == notapplicable

                                If Not (r.appliesToFlag And eIdflags.MissileWeapon) = eIdflags.MissileWeapon Then

                                    rulelog("next rule: r.appliesToFlag missileweapon")

                                    GoTo nextrule
                                End If

                            ElseIf bo.ObjectClass = ObjectClass.WandStaffOrb Then ' == notapplicable

                                If Not (r.appliesToFlag And eIdflags.Wand) = eIdflags.Wand Then

                                    rulelog("next rule: r.appliesToFlag Wand")

                                    GoTo nextrule
                                End If

                            End If

                        ElseIf bo.ObjectClass = ObjectClass.Armor Then

                            If bo.IntValues(LongValueKey.EquipType) = &H4 Then
                                If Not (r.appliesToFlag And eIdflags.Shield) = eIdflags.Shield Then
                                    rulelog("next rule: r.appliesToFlag Shield")
                                    GoTo nextrule
                                End If
                            ElseIf Not (r.appliesToFlag And eIdflags.Armor) = eIdflags.Armor Then
                                rulelog("next rule: r.appliesToFlag Armor")
                                GoTo nextrule
                            End If

                        ElseIf bo.ObjectClass = ObjectClass.Jewelry Then
                            If bo.IntValues(LongValueKey.EquipableSlots) = &H4000000 Then
                                If Not (r.appliesToFlag And eIdflags.Trinket) = eIdflags.Trinket Then
                                    rulelog("next rule: r.appliesToFlag Trincket")
                                    GoTo nextrule
                                End If
                            ElseIf Not (r.appliesToFlag And eIdflags.Jewelry) = eIdflags.Jewelry Then

                                rulelog("next rule: r.appliesToFlag Jewelry")

                                GoTo nextrule
                            End If
                        ElseIf bo.ObjectClass = ObjectClass.Clothing Then
                            If Not (r.appliesToFlag And eIdflags.Clothing) = eIdflags.Clothing Then

                                rulelog("next rule: r.appliesToFlag Clothing")

                                GoTo nextrule
                            End If
                        End If

                        If r.minarmorlevel > 0 And bo.ObjectClass <> ObjectClass.Armor Then
                            GoTo nextrule
                        ElseIf r.minarmorlevel > 0 Then
                            If armorlevel < r.minarmorlevel Then

                                rulelog("next minarmorlevel : " & armorlevel)

                                GoTo nextrule
                            Else

                                rulelog("match minarmorlevel : " & armorlevel)

                                nmatch += 1

                                If r.armorcoverageFlag > 0 AndAlso (armorcoverage And r.armorcoverageFlag) = armorcoverage Then
                                    nmatch += 1

                                    rulelog("yes armorcoverage")

                                ElseIf r.armorcoverageFlag > 0 Then

                                    rulelog("nextrule armorcoverage")

                                    GoTo nextrule
                                End If

                                If r.armortypeFlag > 0 AndAlso (armortype And r.armortypeFlag) = armortype Then
                                    nmatch += 1

                                    rulelog("yes armortype")

                                ElseIf r.armortypeFlag > 0 Then

                                    rulelog("nextrule armortype")

                                    GoTo nextrule
                                End If
                            End If
                        End If

                        If r.maxburden > 0 And burden > r.maxburden Then  'no match

                            rulelog("next rule: burden " & burden)

                            GoTo nextrule
                        ElseIf r.maxburden > 0 Then

                            rulelog("match rule: burden " & burden)

                            nmatch = 1
                        End If

                        If (r.maxcraft > 0 And work > r.maxcraft) Then

                            rulelog("next rule: maxcraft " & work)

                            GoTo nextrule
                        ElseIf r.maxcraft > 0 Then

                            rulelog("match rule: maxcraft " & work)

                            nmatch += 1
                        End If
                        If (r.maxvalue > 0 And value > r.maxvalue) Then

                            rulelog("next rule: value " & work)

                            GoTo nextrule
                        ElseIf r.maxvalue > 0 Then

                            rulelog("match rule: value " & work)

                            nmatch += 1
                        End If
                        If r.keywords <> String.Empty AndAlso Not bo.Name.ToLower.IndexOf(r.keywords.ToLower) >= 0 Then

                            rulelog("next keywords name: " & r.keywords)

                            GoTo nextrule
                        ElseIf r.keywords <> String.Empty Then

                            rulelog("match keywords name: " & r.keywords)

                            nmatch += 1
                        End If
                        If r.keywordsnot <> String.Empty AndAlso bo.Name.ToLower.IndexOf(r.keywordsnot.ToLower) >= 0 Then

                            rulelog("next keywords excluding name: " & r.keywordsnot)

                            GoTo nextrule
                        ElseIf r.keywords <> String.Empty Then

                            rulelog("match keywords excluding name: " & r.keywordsnot)

                            nmatch += 1
                        End If


                        'item must contain x of the spells in the list
                        If r.spells IsNot Nothing AndAlso r.spells.Count > 0 And r.spellmatches > 0 Then
                            Dim nspellmatches As Integer = 0
                            For i As Integer = 0 To bo.SpellCount - 1
                                If r.spells.Contains(bo.Spell(i)) Then
                                    nspellmatches += 1
                                End If
                            Next

                            If nspellmatches >= r.spellmatches Then
                                nmatch += 1
                            Else
                                GoTo nextrule
                            End If
                        End If

                        If r.Specificset IsNot Nothing AndAlso r.Specificset.Count > 0 Then
                            If setid = 0 OrElse Not r.Specificset.Contains(setid) Then
                                GoTo nextrule
                            End If
                            nmatch += 1

                        End If

                        If r.anyset Then
                            If setid = 0 Then
                                GoTo nextrule
                            End If
                            nmatch += 1
                        End If

                        If nmatch > 0 Then

                            rulelog("MATCH : " & nmatch)

                            Return r
                        End If
                    End If

nextrule:
                Next

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        Finally

            sw.Stop()
            Dim ms As Long = sw.ElapsedTicks
            If mRulelookup.totalq = 1 Then
                mRulelookup.totalspend = sw.Elapsed
                mRulelookup.fastest = sw.Elapsed
                mRulelookup.slowest = sw.Elapsed
            Else
                mRulelookup.totalspend += sw.Elapsed
                If mRulelookup.fastest > sw.Elapsed Then
                    mRulelookup.fastest = sw.Elapsed
                End If
                If mRulelookup.slowest < sw.Elapsed Then
                    mRulelookup.slowest = sw.Elapsed
                End If
            End If
        End Try

        Return Nothing
    End Function

    Private Function hasSkill(ByVal eSkill As Decal.Adapter.Wrappers.CharFilterSkillType) As Boolean
        Dim objSkillInfo As Decal.Adapter.Wrappers.SkillInfoWrapper
        objSkillInfo = Core.CharacterFilter.Skills(eSkill)
        If objSkillInfo.Training = Wrappers.TrainingType.Specialized Or _
            objSkillInfo.Training = Wrappers.TrainingType.Trained Then
            Return True
        End If

    End Function

    Private Function CheckItemForUnknownScroll(ByVal spellid As Integer) As Alert
        Dim bReturn As Boolean
        Dim oSpell As Decal.Filters.Spell = Nothing

        If (mPluginConfig.unknownscrolls Or mPluginConfig.unknownscrollsAll) Then

            oSpell = FileService.SpellTable.GetById(spellid)
            If Core.CharacterFilter.IsSpellKnown(spellid) = False Then

                If Not oSpell Is Nothing Then
                    If oSpell.Difficulty >= 290 And oSpell.Mana <> 240 Then 'level 7, hack on manacost for nullify spells which are level 6 with a high diff

                        If mPluginConfig.trainedscrollsonly Then
                            Select Case oSpell.School.Id
                                Case 1 'war
                                    bReturn = hasSkill(Wrappers.CharFilterSkillType.WarMagic)
                                Case 4 'Creature
                                    bReturn = hasSkill(Wrappers.CharFilterSkillType.CreatureEnchantment)
                                Case 3 'Item
                                    bReturn = hasSkill(Wrappers.CharFilterSkillType.ItemEnchantment)
                                Case 2 'Life
                                    bReturn = hasSkill(Wrappers.CharFilterSkillType.LifeMagic)
                            End Select

                        Else
                            bReturn = True
                        End If
                    ElseIf mPluginConfig.unknownscrollsAll Then
                        Select Case oSpell.School.Id
                            Case 1 'war
                                bReturn = hasSkill(Wrappers.CharFilterSkillType.WarMagic)
                            Case 4 'Creature
                                bReturn = hasSkill(Wrappers.CharFilterSkillType.CreatureEnchantment)
                            Case 3 'Item
                                bReturn = hasSkill(Wrappers.CharFilterSkillType.ItemEnchantment)
                            Case 2 'Life
                                bReturn = hasSkill(Wrappers.CharFilterSkillType.LifeMagic)
                        End Select
                    End If
                End If
            End If

        End If
        Dim xa As Alert = Nothing

        If bReturn Then
            xa = getAlert(mPluginConfig.AlertKeyScroll)
        End If

        Return xa
    End Function

    Private Function getAlert(ByVal key As String) As Alert
        If Not String.IsNullOrEmpty(key) AndAlso mPluginConfig.Alerts.ContainsKey(key) Then

            Return mPluginConfig.Alerts.Item(key)
        End If
        Return Nothing
    End Function

End Class
