Option Strict On
Option Infer On

Imports Decal.Adapter
Imports Decal.Adapter.Wrappers
Imports System.Runtime.InteropServices
Imports System.Xml.Linq



Partial Public Class Plugin
    Private mVuls() As Integer = {2174, &H484, 1155, 1154, 2164, 1132, 1131, 1130, 2170, 1108, 1107, 2168, 1065, 1064, 1063, 2162, 526, 525, 524, 2166, 1053, 1052, 1051, 2172, 1089, 1088, 1087}
    Private mImps() As Integer = {2074, 1156, 1155, 1154}
    Private Function valmob(ByVal id As Integer, ByVal selectionflag1 As Integer, ByVal selectionflag2 As Integer) As Boolean
        Dim hasvulnobject As Boolean

        If selectionflag1 = 0 And selectionflag2 = 0 Then
            Return True
        End If

        If mMobs.ContainsKey(id) Then
            hasvulnobject = CBool(mMobs.Item(id).vulns IsNot Nothing)
        Else
            hasvulnobject = False
        End If
        Dim selected As Boolean

        If selectionflag1 = 2 Then 'select mob that does not have imp
            If Not hasvulnobject OrElse Not mMobs.Item(id).hasSpellids(mImps) Then
                selected = True
            End If
        End If

        If selectionflag2 = 2 Then 'select  mob that does not have vuln
            If Not hasvulnobject OrElse Not mMobs.Item(id).hasSpellids(mVuls) Then
                selected = True
            End If
        End If

        If hasvulnobject Then
            If selectionflag1 = 1 And selectionflag2 = 1 Then 'interested in mobs that has a vuln AND a imp
                If mMobs.Item(id).hasSpellids(mImps) AndAlso _
                   mMobs.Item(id).hasSpellids(mVuls) Then

                    selected = True
                End If
            ElseIf selectionflag1 = 1 Then 'select mob that does have imp
                If mMobs.Item(id).hasSpellids(mImps) Then
                    selected = True
                End If
            ElseIf selectionflag2 = 1 Then 'select  mob that does  have vuln
                If mMobs.Item(id).hasSpellids(mVuls) Then
                    selected = True
                End If
            End If
        End If

        Return selected
    End Function

    Private Function GetSelectionAutoTarget(ByVal excludeId As List(Of Integer), ByVal selectionflag1 As Integer, ByVal selectionflag2 As Integer) As Integer
        Try

            Dim selectedlist As New SortedList(Of Double, Mobdata)

            Dim playerlocation As Location = PhysicObjectLocation(Core.CharacterFilter.Id)
            Dim maxrange As Double = 80

            Dim drange As Double = -1
            Dim dClosestRange As Double = -1
            Dim selectedmob As Integer = 0

            'dont care
            Dim closestmobany As Integer = 0
            Dim closestmobanyrange As Double = -1

            Dim oCol As Decal.Adapter.Wrappers.WorldObjectCollection = Core.WorldFilter.GetByObjectClass(ObjectClass.Monster)


            For Each b As Decal.Adapter.Wrappers.WorldObject In oCol

                'If excludemobname(b.Name) Then
                '    Continue For
                'End If

                Dim destloc As Location = PhysicObjectLocation(b.Id)

                If destloc Is Nothing Then
                    Continue For
                End If

                drange = DistanceTo(playerlocation, destloc, True)


                If Not excludeId.Contains(b.Id) AndAlso drange <> -1 AndAlso (drange < maxrange OrElse maxrange = 0) Then

                    If closestmobanyrange = -1 OrElse closestmobanyrange > drange Then
                        closestmobanyrange = drange
                        closestmobany = b.Id
                    End If

                    Dim selected As Boolean = True

                    If Not (selectionflag1 = 0 And selectionflag2 = 0) Then
                        selected = valmob(b.Id, selectionflag1, selectionflag2)
                    End If

                    If selected Then
                        If dClosestRange = -1 OrElse dClosestRange > drange Then
                            dClosestRange = drange
                            selectedmob = b.Id
                        End If
                    End If

                End If


            Next

            If selectedmob = 0 AndAlso Host.Actions.CombatMode = CombatState.Peace Then
                selectedmob = closestmobany
            End If

            Return selectedmob

        Catch ex As Exception
            wtcw(ex.Message & " " & ex.StackTrace)
        End Try
        Return 0
    End Function

#Region "Play wav file"

    Private mLastsound As DateTime
    Private mSoundPlayer As System.Media.SoundPlayer

    Private Sub PlaySoundFile(ByVal filename As String, ByVal volume As Integer, Optional ByVal alwaysplay As Boolean = False)
        Try


            If Not mPluginConfig Is Nothing AndAlso Not mPluginConfig.MuteAll And volume > 0 Then
                If Not String.IsNullOrEmpty(filename) Then
                    Dim wav As String = IO.Path.Combine(Util.wavPath, filename)

                    If System.IO.File.Exists(wav) Then
                        If mplayer Is Nothing Then
                            If mSoundPlayer Is Nothing Then
                                mSoundPlayer = New System.Media.SoundPlayer
                            End If
                            Dim x As Long = DateDiff(DateInterval.Second, mLastsound, Now)
                            If x > 1 Or alwaysplay Then
                                mLastsound = Now
                                mSoundPlayer.SoundLocation = wav
                                mSoundPlayer.Play()
                            End If
                        Else
                            mplayer.Volume = volume
                            mplayer.playsoundfile(wav)
                        End If

                    End If

                End If

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

#End Region

    'normaly the changes are written to disk on logout

    Private Sub forcesave()
        Try
            If mCharconfig IsNot Nothing Then
                Util.SerializeObject(mCharConfigfilename, mCharconfig)
            End If

            If mPluginConfig IsNot Nothing Then
                Util.SerializeObject(mPluginConfigfilename, mPluginConfig)
            End If

            If mWorldConfig IsNot Nothing Then
                Util.SerializeObject(mWorldConfigfilename, mWorldConfig)
            End If
            If mGlobalInventory IsNot Nothing Then
                Util.SerializeObject(mWorldInventoryname, mGlobalInventory, "type='text/xsl' href='Inventory.xslt'")
                transforminventory1()
            End If
            If mStorageInfo IsNot Nothing Then
                Util.SerializeObject(mWorldInventoryname & "storage", mStorageInfo)
            End If
        Catch ex As Exception
            wtcw(ex.Message)
        End Try
    End Sub

    Private Sub tradeust()
        Try
            Dim nMaxcount As Integer
            If Not mTradeWindowOpen Then
                wtcw("Open a tradewindow")
            Else
                nMaxcount = 0
                For Each d As KeyValuePair(Of Integer, salvageustinfo) In mUstItems  'for each item in the toUst list
                    Dim itemguid As Integer = CInt(d.Key)
                    Host.Actions.TradeAdd(itemguid)
                Next
            End If
        Catch ex As Exception
            wtcw(ex.Message)
        End Try
    End Sub

    Private Sub tradesalvage(ByVal partials As Boolean)
        Try
            Dim nMaxcount As Integer
            If Not mTradeWindowOpen Then
                wtcw("Open a tradewindow")
            Else
                nMaxcount = 0
                Dim wocol As WorldObjectCollection = Core.WorldFilter.GetInventory
                For Each wo As WorldObject In wocol
                    If wo.ObjectClass = ObjectClass.Salvage Then
                        If Not partials OrElse wo.Values(LongValueKey.UsesRemaining) < 100 Then
                            Host.Actions.TradeAdd(wo.Id)
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            wtcw(ex.Message)
        End Try
    End Sub

    <BaseEvent("CommandLineText")> _
   Private Sub Plugin_CommandLineText(ByVal sender As Object, ByVal e As Decal.Adapter.ChatParserInterceptEventArgs)
        Try
            If e.Text.StartsWith("/") Or e.Text.StartsWith("@") Then
                Dim cmd As String = e.Text.Substring(1).ToLower
                If cmd.StartsWith("sell") Then
                    sellsalvage()
                    e.Eat = True

                ElseIf cmd.StartsWith("trackxp") Then

                    Dim b As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(Host.Actions.CurrentSelection)
                    If b Is Nothing Then
                        wtcw("no object selected")
                    ElseIf b.Id = Core.CharacterFilter.Id Then
                        wtcw("Track item xp off")
                        mCharconfig.trackobjectxpHudId = 0

                    ElseIf Not b.HasIdData Then
                        wtcw("Ident the selected object first then try again")

                    ElseIf Not IsItemInInventory(b) Then
                        wtcw("The selected object must be in inventory")

                    Else
                        wtcw("trackxp on")
                        mCharconfig.trackobjectxpHudId = b.Id
                    End If

                ElseIf cmd.StartsWith("alincodebug") Then
                    mDebugRules = Not mDebugRules
                    wtcw("Debugging rules, chatwindow 2 " & mDebugRules)
                ElseIf cmd.StartsWith("save") Then
                    forcesave()
                    e.Eat = True
                ElseIf cmd.StartsWith("inventory export") Then
                    transforminventory1()
                    e.Eat = True
                ElseIf cmd.StartsWith("inventory rescan") Then
                    startscanInventoryforSerialize(True)
                    e.Eat = True
                ElseIf cmd.StartsWith("inventory update") Then
                    startscanInventoryforSerialize(False)
                    e.Eat = True
                ElseIf cmd.StartsWith("inventory find") Then
                    e.Eat = True
                ElseIf cmd.StartsWith("tradesalvagep") Then
                    tradesalvage(True)
                    e.Eat = True
                ElseIf cmd.StartsWith("tradesalvage") Then
                    tradesalvage(False)
                    e.Eat = True
                ElseIf cmd.StartsWith("tradeust") Then
                    tradeust()
                    e.Eat = True
                ElseIf cmd.StartsWith("resetprofiles") Then
                    resetprofiles()

                ElseIf cmd.StartsWith("reset") Then
                    Util.TotalErrors = 0
                    mKills = 0
                    mXPStart = Core.CharacterFilter.TotalXP
                    mXPStartTime = Now
                    mIdqueue.Clear()
                    mMoblookup.reset()
                    mThropylookup.reset()
                    mSalvagelookup.reset()
                    mRulelookup.reset()
                ElseIf e.Text.StartsWith("clear") Then
                    mNotifiedCorpses.Clear()
                    mNotifiedItems.Clear()
                    mColScanInventoryItems.Clear()
                    mColStacker.Clear()
                    mIdqueue.Clear()
                    midInventory.Clear()

                ElseIf cmd.StartsWith("find ") Then
                    Dim p As Integer = e.Text.IndexOf(" ")
                    Dim Idarray() As Integer = CType([Enum].GetValues(GetType(eSomeTestColorsArg)), Integer())
                    Dim ii As Integer = 1


                    If p > 0 And p < e.Text.Length Then
                        Dim ncount As Integer = 0
                        Dim s As String = e.Text.Substring(p + 1).ToLower
                        wtcw("searching for->" & s)
                        Dim id As Integer
                        Dim wocol As WorldObjectCollection = Core.WorldFilter.GetLandscape
                        For Each wo As WorldObject In wocol
                            If wo.Name.ToLower.IndexOf(s) >= 0 Then
                                markobject1(wo.Id)
                                If Not mNotifiedItems.ContainsKey(wo.Id) AndAlso Not mNotifiedCorpses.ContainsKey(wo.Id) Then
                                    Dim newobject As New notify
                                    newobject.icon = wo.Icon
                                    newobject.id = wo.Id
                                    newobject.name = wo.Name
                                    newobject.description = wo.ObjectClass.ToString
                                    newobject.ColorArgb = &HFF000000 Or eSomeTestColorsArg.White
                                    If wo.ObjectClass = ObjectClass.Corpse Then
                                        newobject.scantype = eScanresult.corpse
                                        mNotifiedCorpses.Add(wo.Id, newobject)
                                    Else
                                        newobject.scantype = eScanresult.other
                                        mNotifiedItems.Add(wo.Id, newobject)
                                    End If
                                End If
                                id = wo.Id
                                ncount += 1
                            End If
                        Next

                        If ncount = 1 Then
                            Host.Actions.SelectItem(id)
                        Else
                            wtcw(ncount & " items found")
                        End If
                    End If
                ElseIf cmd.StartsWith("alincotest") Then

                    wtcw("Alinco3 version: " & Util.dllversion)
                    wtcw(".NET Framework : " & System.Environment.Version.ToString)


                    Dim b As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(Host.Actions.CurrentSelection)
                    If b Is Nothing Then
                        wtcw("no object selected")
                    Else

                        wtcw("selected : " & b.Name)
                        wtcw("Category    " & Hex(b.Category))
                        wtcw("Id    0x" & Hex(b.Id))
                        wtcw("Icon    " & b.Icon)
                        wtcw("ObjectClass    " & b.ObjectClass.ToString)
                        wtcw("Container    0x" & Hex(b.Container))
                        wtcw("HouseOwner    0x" & Hex(b.Values(LongValueKey.HouseOwner)))
                        wtcw("Wielder    0x" & Hex(b.Values(LongValueKey.Wielder)))
                        wtcw(" ")
                        wtcw("HasIdData : " & b.HasIdData)
                        wtcw("EquipType    0x" & Hex(b.Values(LongValueKey.EquipType)))
                        wtcw("Coverage    0x" & Hex(b.Values(LongValueKey.Coverage)))
                        wtcw("Behavior    0x" & Hex(b.Behavior))
                        wtcw("WieldReqType    0x" & Hex(b.Values(LongValueKey.WieldReqType)))
                        wtcw("WieldReqValue   0x" & (b.Values(LongValueKey.WieldReqValue)))
                        wtcw("WieldReqAttribute   0x" & (b.Values(LongValueKey.WieldReqAttribute)))
                        wtcw("MissileType    " & (b.Values(LongValueKey.MissileType)))
                        wtcw("AssociatedSpell    " & (b.Values(LongValueKey.AssociatedSpell)))
                        wtcw("EquipableSlots    0x" & Hex(b.Values(LongValueKey.EquipableSlots)))
                        wtcw("EquippedSlots    0x" & Hex(b.Values(LongValueKey.EquippedSlots)))
                        wtcw("ActivationReqSkillId    0x" & Hex(b.Values(LongValueKey.ActivationReqSkillId)))
                        wtcw("EquipSkill    0x" & Hex(b.Values(LongValueKey.EquipSkill)))

                        wtcw("Type    0x" & Hex(b.Values(LongValueKey.Type)))
                        wtcw("setid    0x" & Hex(b.Values(CType(&H109, LongValueKey))))
                        wtcw(" ")
                        If b.Values(DoubleValueKey.SalvageWorkmanship) > 0 Then
                            wtcw("ExpectedSalvageReturn " & _
                            ExpectedSalvageReturn(CInt(b.Values(DoubleValueKey.SalvageWorkmanship)), salvageskill, mCharconfig.salvageaugmentations))
                        End If
                        wtcw("HealKitSkillBonus    " & (b.Values(LongValueKey.HealKitSkillBonus)))
                        wtcw("KeysHeld    " & (b.Values(LongValueKey.KeysHeld)))
                        wtcw("UsesRemaining    " & (b.Values(LongValueKey.UsesRemaining)))
                        wtcw("UsesTotal    " & (b.Values(LongValueKey.UsesTotal)))

                        'Dim strspells As String = String.Empty

                        'For i As Integer = 1 To b.SpellCount - 1

                        '    Dim oSpell As Decal.Filters.Spell = Plugin.FileService.SpellTable.GetById(b.Spell(i))
                        '    If Not oSpell Is Nothing Then
                        '        strspells &= ", " & oSpell.Name & " 0x" & Hex(oSpell.Id)
                        '    End If
                        'Next
                        'If strspells <> String.Empty Then
                        '    wtcw("  " & strspells)
                        'End If

                        'For i As Integer = 0 To Plugin.FileService.SpellTable.Length - 1
                        '    Try
                        '        Dim oSpell As Decal.Filters.Spell = Plugin.FileService.SpellTable.Item((i))
                        '        If Not oSpell Is Nothing AndAlso Not oSpell.IsDebuff Then

                        '            If oSpell.Name.ToLower.Contains("two handed") Then
                        '                wtcw(oSpell.Name & " &H" & Hex(oSpell.Id))
                        '            End If

                        '        End If
                        '    Catch ex As Exception

                        '    End Try

                        'Next

                    End If


                ElseIf cmd.StartsWith("alinco") Then

                    Dim frequency As Long = Stopwatch.Frequency
                    Dim nanosecPerTick As Double = (1000L * 1000L * 1000L) / frequency
                    wtcw(String.Format("Lookup Statistics: Timer is accurate within {0} nanoseconds", nanosecPerTick.ToString("0.00")))
                    wtcw("Mobs: " & mMoblookup.tostring)
                    wtcw("Thropies: " & mThropylookup.tostring)
                    wtcw("Salvage: " & mSalvagelookup.tostring)
                    wtcw("Rules: " & mRulelookup.tostring)
                    wtcw(" ")
                    wtcw("Alinco3 version " & Util.dllversion & ", available commands: ")
                    wtcw(" salvage mule:")
                    wtcw(" /tradeust      => adds the items from the ust list to a open tradewindow")
                    wtcw(" /tradesalvage  => adds all salvage to a open tradewindow")
                    wtcw(" /tradesalvagep => partial bags only (< 100 units)")
                    wtcw(" ")
                    wtcw(" /inventory export => serializes inventory to userdocuments\decal plugins\Alinco3\inventory")
                    wtcw(" /inventory update => update inventory")
                    wtcw(" /inventory find   => not implemented")
                    wtcw(" ")
                    wtcw(" /save         => forces to save all settings to disk")
                    wtcw(" /reset        => reset xp/h")
                    wtcw(" /find         => searches the landscape")
                    wtcw(" /clear        => clears the listboxes with matched items and corpses")
                    wtcw(" /sell         => Adds the salvage to the vendors trade window")


                    e.Eat = True


                Else
                    If mPluginConfig IsNot Nothing AndAlso mPluginConfig.Shortcuts IsNot Nothing Then
                        For Each x As KeyValuePair(Of String, String) In mPluginConfig.Shortcuts
                            If Not String.IsNullOrEmpty(x.Key) AndAlso (e.Text.ToLower.StartsWith("/" & x.Key.ToLower) Or e.Text.ToLower.StartsWith("@" & x.Key.ToLower)) Then
                                e.Eat = True
                                Host.Actions.InvokeChatParser("/" & x.Value)
                                Exit For
                            End If
                        Next
                    End If

                End If

            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

#Region "calc"
    Private Function PhysicObjectLocation(ByVal id As Integer) As Location
        Dim physicsObject As IntPtr = Host.Actions.PhysicsObject(id)
        If Not IntPtr.Zero.Equals(physicsObject) Then

            Dim r As New Location
            Dim source As IntPtr = New IntPtr((physicsObject.ToInt32 + &H84))
            Dim destination As Byte() = New Byte(11) {}
            Marshal.Copy(source, destination, 0, 12)

            r.landblock = Marshal.ReadInt32(physicsObject, &H4C)
            r.x = (BitConverter.ToSingle(destination, 0))
            r.y = (BitConverter.ToSingle(destination, 4))
            r.z = BitConverter.ToSingle(destination, 8)
            r.ew = ((((Marshal.ReadByte(physicsObject, &H4F) * 8) + (r.x / 24.0!)) - 1019.5) / 10)
            r.ns = ((((Marshal.ReadByte(physicsObject, &H4E) * 8) + (r.y / 24.0!)) - 1019.5) / 10)


            Return r
        End If
        Return Nothing
    End Function

    Private Function headingdiff(ByVal x As Double, ByVal y As Double) As Integer
        Dim diff As Integer
        If x > y Then
            diff = CInt(x - y)
        Else
            diff = CInt(y - x)
        End If
        Return diff
    End Function

    Private Function DistanceTo(ByVal playerlocation As Location, ByVal DestLoc As Location, Optional ByVal Face As Boolean = False) As Double
        Try

            If playerlocation Is Nothing Or DestLoc Is Nothing Then
                Return -1
            End If

            Dim dx As Double = Math.Abs(DestLoc.ew - playerlocation.ew)
            Dim dy As Double = Math.Abs(DestLoc.ns - playerlocation.ns)
            Dim dz As Double = Math.Abs((playerlocation.z - DestLoc.z) / 240)

            Dim dDistance As Double
            dDistance = Math.Sqrt((dx ^ 2) + (dy ^ 2) + (dz ^ 2))

            dDistance *= 263.26

            If Face Then
                Dim dheading As Double
                If DestLoc.ns - playerlocation.ns < 0 Then
                    dheading = ((180 / Math.PI) * Math.Atan((DestLoc.ew - playerlocation.ew) / (DestLoc.ns - playerlocation.ns))) + 180
                Else
                    dheading = (((180 / Math.PI) * Math.Atan((DestLoc.ew - playerlocation.ew) / (DestLoc.ns - playerlocation.ns))) + 360) Mod 360
                End If
                dheading -= Host.Actions.Heading
                If dheading > 180 Then
                    dheading -= 360
                ElseIf dheading < -180 Then
                    dheading += 360
                End If
                dheading = Math.Abs(dheading)
                If dheading > 0 Then
                    Dim facedistance As Double = (dheading / 50)
                    If playerlocation.inDungeon Then
                        facedistance *= 1.2
                    End If

                    dDistance += facedistance
                End If
            End If

            Return Math.Round(dDistance, 2)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

    Private Function DistanceTo(ByVal DestObject As Decal.Adapter.Wrappers.Vector3Object, Optional ByVal Face As Boolean = False) As Double
        Try
            Dim playerlocation As Location = PhysicObjectLocation(Core.CharacterFilter.Id)
            Dim DestLoc As Location = Nothing

            If DestObject IsNot Nothing Then
                DestLoc = New Location
                DestLoc.x = DestObject.X
                DestLoc.y = DestObject.Y
                DestLoc.x = DestObject.Z
            End If

            Return DistanceTo(playerlocation, DestLoc, Face)

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

    Private Function DistanceTo(ByVal DestLocId As Integer, Optional ByVal Face As Boolean = False) As Double
        Try

            Dim playerlocation As Location = PhysicObjectLocation(Core.CharacterFilter.Id)
            Dim DestLoc As Location = PhysicObjectLocation(DestLocId)

            Return DistanceTo(playerlocation, DestLoc, Face)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

    Private Function DistanceTo(ByVal DestLoc As Location, Optional ByVal Face As Boolean = False) As Double
        Try

            Dim playerlocation As Location = PhysicObjectLocation(Core.CharacterFilter.Id)

            Return DistanceTo(playerlocation, DestLoc, Face)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function
#End Region
    Private mFreeMainPackslots As Integer

    Private Function lootitem(ByVal id As Integer) As Boolean
        Host.Actions.UseItem(id, 0)
    End Function

    Private mCorpsScanning As Boolean
    Private mlastsalvage As DateTime
    Private Function AutoUst() As Boolean
        If Not mCorpsScanning Then
            If mUstItems.Count > 0 And mFreeMainPackslots > 1 Then
                If DateDiff(DateInterval.Second, mlastsalvage, Now) > 1 Then
                    If mUstItems.Count > 0 AndAlso loadSalvagePanel() > 0 Then
                        salvage()
                        mlastsalvage = Now
                        clickmouse(eMousePositions.CloseSalvage)
                    End If
                    Return True
                End If
            End If
        End If
    End Function

    Private Function CountFreeSlots(ByVal containerId As Integer) As Integer
        Dim xSlots As Integer = 24
        Dim nItems As Integer = 0
        If containerId = Core.CharacterFilter.Id Then
            xSlots = 102
        End If
        Dim invpack As WorldObjectCollection = Core.WorldFilter.GetByContainer(containerId)
        For Each wo As WorldObject In invpack
            If wo.Container = containerId AndAlso wo.Values(LongValueKey.Slot) <> -1 Then
                If wo.ObjectClass = ObjectClass.Container OrElse wo.Name.StartsWith("Foci") Then

                Else
                    nItems += 1
                End If
            End If
        Next
        Return xSlots - nItems
    End Function

    Private Sub sellsalvage()
        Try
            If Host.Actions.VendorId = 0 Then
                wtcw("Open an vendor that buys salvage (armorer for example)")
            Else

                mFreeMainPackslots = CountFreeSlots(Core.CharacterFilter.Id)
                wtcw("freeslots: " & mFreeMainPackslots)
                Dim maxvalue As Integer = 25000 * mFreeMainPackslots
                Dim xvalue As Integer = 0

                Dim inv As WorldObjectCollection = Core.WorldFilter.GetInventory
                For Each wo As WorldObject In inv
                    With wo
                        If .ObjectClass = ObjectClass.Salvage AndAlso mActiveSalvageProfile.ContainsKey(wo.Values(LongValueKey.Material)) Then
                            Dim salvinfo As SalvageSettings = mActiveSalvageProfile.Item(wo.Values(LongValueKey.Material))
                            If Not salvinfo.checked OrElse (CInt(.Values(DoubleValueKey.SalvageWorkmanship)) < minSalvageFromString(salvinfo.combinestring)) Then

                                Dim v As Integer = .Values(LongValueKey.Value)

                                If xvalue + v >= maxvalue Then
                                    Exit For
                                End If

                                xvalue += v

                                If .Container = Core.CharacterFilter.Id Then
                                    maxvalue += 25000
                                End If
                                Host.Actions.VendorAddSellList(.Id)

                            End If

                            If xvalue > maxvalue Then
                                PlaySoundFile("lostconnection.wav", mPluginConfig.wavVolume)
                                wtcw("Enough items in the window for now " & xvalue)
                                Exit For
                            End If
                        End If
                    End With
                Next

                wtcw("Room for Max " & Format(maxvalue, "##,##0") & "p,  Total value> " & Format(xvalue, "##,##0"))
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Function IsItemInInventory(ByVal objcheck As Decal.Adapter.Wrappers.WorldObject) As Boolean
        If objcheck IsNot Nothing Then

            If objcheck.Container = Core.CharacterFilter.Id Then
                Return True
            End If

            Dim pack As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(objcheck.Container)
            If pack IsNot Nothing AndAlso pack.Container = Core.CharacterFilter.Id Then
                Return True
            End If

        End If
        Return False
    End Function

    Private Function GetFindItemFromInventory(ByVal search As String) As Decal.Adapter.Wrappers.WorldObject

        Dim oCol As Decal.Adapter.Wrappers.WorldObjectCollection = Core.WorldFilter.GetByName(search)
        For Each b As Decal.Adapter.Wrappers.WorldObject In oCol
            If IsItemInInventory(b) Then
                Return b
            End If
        Next

        Return Nothing
    End Function
    Private mExcludeIds As New List(Of Integer)
    Private mAttackButtonClicked As DateTime
    Private mClicks As Integer
    Private mInDblClickMode As Boolean
    Private mSelectionFlagImp1 As Integer
    Private mSelectionFlagVuln1 As Integer
    Private mSelectionFlagImp2 As Integer
    Private mSelectionFlagVuln2 As Integer
    Private mhotkeyswitchImp As Boolean = True
    Private mhotkeyswitchvuln As Boolean = False
    Private Sub selectTarget()

        'select nearest monster or double click for next target
        Dim dblclick As Boolean
        Dim excludeid As Integer = 0

        mClicks += 1
        If mClicks > 1 Then
            mClicks = 0
        End If
        Dim expiredsex As Long = DateDiff(DateInterval.Second, mAttackButtonClicked, Now)

        If expiredsex <= 0 Then
            Dim x As TimeSpan = Now.Subtract(mAttackButtonClicked)
            If x.TotalMilliseconds < 700 Then
                dblclick = True
                excludeid = Host.Actions.CurrentSelection
                mInDblClickMode = True
                mClicks = 0
            End If
        ElseIf expiredsex > 4 Then
            mClicks = 0
        End If

        mAttackButtonClicked = Now

        'next dblclick:
        'first keypress = mclicks ==1
        'ignore keypress
        If mClicks = 1 AndAlso mInDblClickMode Then
            Return
        End If

        If Not dblclick Then
            mExcludeIds.Clear()
            mInDblClickMode = False

        ElseIf excludeid <> 0 Then
            If Not mExcludeIds.Contains(excludeid) Then
                mExcludeIds.Add(excludeid)
            End If

        End If

        Dim selectionflag1 As Integer '0 = do not care, 1 = has imp, 2 = does not have imp
        Dim selectionflag2 As Integer '0 = do not care, 1 = has vuln, 2 = does not have vuln on
        If mhotkeyswitchImp Then
            If Host.Actions.CombatMode = CombatState.Magic Then
                If mhotkeyswitchImp Then
                    selectionflag1 = 2
                End If
                If mhotkeyswitchvuln Then
                    selectionflag2 = 2
                End If
            Else
                If mhotkeyswitchvuln Then
                    selectionflag1 = 1
                End If
                If mhotkeyswitchvuln Then
                    selectionflag2 = 1
                End If
            End If
        End If

        Dim id As Integer = GetSelectionAutoTarget(mExcludeIds, selectionflag1, selectionflag2)
        Host.Actions.SelectItem(id)
    End Sub
    Private Sub ACHotkeys_Hotkey(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.HotkeyEventArgs)
        Try
            If Host.Actions.ChatState Then Return

            If e.Title = "alinco3:onoff" Then
                mPaused = Not mPaused
                Return
            End If

            If Paused OrElse Host.Actions.ChatState Then Return

            If e.Title = "alinco3:healself" Then
                e.Eat = healself()
            ElseIf e.Title = "alinco3:targetimp" Then
                mhotkeyswitchImp = Not mhotkeyswitchImp
                If mhotkeyswitchImp Then
                    wtcw("imp on")
                Else
                    wtcw("imp off")
                End If
                selectTarget()
                e.Eat = True
            ElseIf e.Title = "alinco3:targetvuln" Then
                mhotkeyswitchvuln = Not mhotkeyswitchvuln
                If mhotkeyswitchvuln Then
                    wtcw("vuln on")
                Else
                    wtcw("vuln off")
                End If
                selectTarget()
                e.Eat = True
            ElseIf e.Title = "alinco3:targetmob" Then
                selectTarget()
                e.Eat = True
            ElseIf e.Title = "alinco3:givenpc" Then

                If Host.Actions.BusyState = 0 Then

                    Dim oNpc As WorldObject = Core.WorldFilter.Item(Host.Actions.CurrentSelection)
                    If oNpc Is Nothing OrElse (oNpc.ObjectClass <> ObjectClass.Npc) Then
                        oNpc = Core.WorldFilter.Item(mLastnpcId)

                        If oNpc Is Nothing Then ' find nearby
                            Dim closest As Double = 100
                            Dim woc As WorldObjectCollection = Core.WorldFilter.GetByObjectClass(ObjectClass.Npc)
                            For Each oA As WorldObject In woc
                                Dim drange As Double = DistanceTo(oA.Id)
                                If drange < closest Then
                                    closest = drange
                                    oNpc = oA
                                End If
                            Next
                        End If
                    End If

                    If oNpc IsNot Nothing AndAlso (oNpc.ObjectClass = ObjectClass.Npc) Then
                        mLastnpcId = oNpc.Id


                        For Each olootd As KeyValuePair(Of String, ThropyInfo) In mActiveThropyProfile
                            If olootd.Value.npc IsNot Nothing AndAlso olootd.Value.npcloc IsNot Nothing AndAlso olootd.Value.npc = oNpc.Name Then
                                Dim oitem As WorldObject = GetFindItemFromInventory(olootd.Key)

                                If oitem IsNot Nothing AndAlso oitem.Name = olootd.Key Then

                                    'check range
                                    If DistanceTo(olootd.Value.npcloc) < 20 Then

                                        If oitem.Values(LongValueKey.StackCount) > 1 Then
                                            Host.Actions.SelectItem(oitem.Id)
                                            Host.Actions.SelectedStackCount = 1
                                        End If
                                        If oNpc.Name = "Town Crier" And olootd.Key <> "Pyreal" Then
                                            wtcw("only pyreals work with Town Crier, not:" & olootd.Key)
                                        Else
                                            Host.Actions.GiveItem(oitem.Id, oNpc.Id)
                                        End If

                                        e.Eat = True
                                        Return
                                    End If
                                End If
                            End If
                        Next
                        'wtcw("No items found to give to: " & oNpc.Name)
                    Else
                        wtcw("Select a NPC first")
                    End If
                End If
            ElseIf e.Title = "alinco3:useloadust" Then
                e.Eat = True

                loadSalvagePanel()

            ElseIf e.Title = "alinco3:salvage" Then
                e.Eat = True

                salvage()
            ElseIf e.Title = "alinco3:onclickust" Then
                e.Eat = True
                AutoUst()
            ElseIf e.Title = "alinco3:lootitem" Then

                hotkeyloot(True, 80, 0)
                e.Eat = True
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    Private mMarkObject As Decal.Interop.D3DService.CD3DObj
    Private mMarkObjectDate As DateTime
    Private mD3DService As Decal.Interop.D3DService.ID3DService
    Private Sub markobject1(ByVal oid As Integer)
        If mPluginConfig.D3DMark0bjects AndAlso mD3DService IsNot Nothing Then
            Try
                If mMarkObject IsNot Nothing AndAlso DateDiff(DateInterval.Second, mMarkObjectDate, Now) < 2 Then
                    Return
                End If

                If mMarkObject IsNot Nothing Then
                    mMarkObject.visible = False
                    mMarkObject = Nothing
                End If

                mMarkObjectDate = Now
                Dim d As Double = DistanceTo(oid)

                If d > 5 Or d < 0 Then
                    mMarkObject = mD3DService.PointToObject(oid, -32944)
                Else
                    mMarkObject = mD3DService.MarkObjectWithShape(oid, Decal.Interop.D3DService.eShape.eVArrow, -32944)
                End If

            Catch ex As Exception
                Util.ErrorLogger(ex)
            End Try

        End If
    End Sub

    Private Sub huditemclick(ByVal iconclick As Boolean, ByVal onn As notify, ByVal lbutton As Boolean)
        Dim wo As WorldObject = Core.WorldFilter.Item(onn.id)
        If wo IsNot Nothing Then
            If iconclick And Not lbutton Then
                removeNotifyObject(onn.id)
                Renderhud()
            ElseIf iconclick Then 'loot

                If onn.scantype = eScanresult.portalOrLifestone Or onn.scantype = eScanresult.npc Then
                    Host.Actions.UseItem(onn.id, 0)
                ElseIf onn.scantype = eScanresult.monstertokill Then
                    Host.Actions.SelectItem(onn.id)
                Else
                    If mCurrentContainer <> 0 AndAlso wo.Container = mCurrentContainer Then
                        Host.Actions.UseItem(onn.id, 0)
                    ElseIf mCurrentContainer = 0 AndAlso wo.Container <> 0 Then
                        Host.Actions.UseItem(wo.Container, 0)
                    Else
                        Host.Actions.UseItem(onn.id, 0)
                    End If

                End If

            Else
                Dim ido As New IdentifiedObject(wo)
                markobject1(wo.Id)
                Dim msg As String = notifystring(onn.description, ido, False)
                If onn.range > 0 Then
                    wtcw(msg & " Range " & CInt(onn.range))
                Else
                    wtcw(msg)
                End If

                Host.Actions.SelectItem(onn.id)
            End If
        End If

    End Sub

    Private Function healself() As Boolean
        Dim nCurrentHealth As Integer = Host.Actions.Vital.Item(Wrappers.VitalType.CurrentHealth)
        Dim nMaxHealth As Integer = Host.Actions.Vital.Item(Wrappers.VitalType.MaximumHealth)
        Dim nhealthpoints As Integer = nMaxHealth - nCurrentHealth

        If nhealthpoints > 1 Then

            Dim buffedSkill As Integer = 0
            Dim objSkillInfo As Decal.Adapter.Wrappers.SkillInfoWrapper
            objSkillInfo = Core.CharacterFilter.Skills(Wrappers.CharFilterSkillType.Healing)
            If objSkillInfo.Training = Wrappers.TrainingType.Specialized Or _
                objSkillInfo.Training = Wrappers.TrainingType.Trained Then
                buffedSkill = (objSkillInfo.Buffed)
            End If
            If buffedSkill > 1 Then
                Dim nMinusesleft As Integer
                Dim nKitId As Integer
                Dim kitbonus As Integer
                Dim kitcol As Decal.Adapter.Wrappers.WorldObjectCollection = Core.WorldFilter.GetByObjectClass(ObjectClass.HealingKit)

                For Each objkit As Decal.Adapter.Wrappers.WorldObject In kitcol
                    If IsItemInInventory(objkit) Then
                        If Not objkit.HasIdData Then
                            Host.Actions.RequestId(objkit.Id)
                        End If

                        If nMinusesleft = 0 Or objkit.Values(LongValueKey.UsesRemaining) < nMinusesleft Then
                            kitbonus = objkit.Values(LongValueKey.HealKitSkillBonus)
                            nMinusesleft = objkit.Values(LongValueKey.UsesRemaining)
                            nKitId = objkit.Id
                        End If
                    End If

                Next

                If nKitId <> 0 Then
                    '(missing hit points * 2) + 50 points of skill gives you a 90% chance. 
                    '70:         points(over Is 90%)
                    '100:        points(over Is 95%)
                    '150:        points(over Is 100%)
                    Dim nrequiredSkill As Integer
                    If Me.Host.Actions.CombatMode = CombatState.Peace Then
                        nrequiredSkill = (nhealthpoints * 2) + 70
                    Else
                        nrequiredSkill = CInt((nhealthpoints * 1.6) + 70)
                    End If

                    If nrequiredSkill > buffedSkill + kitbonus Then
                        If kitbonus > 0 Then

                        End If

                    End If
                    wtcw("Heal Skill: " & buffedSkill + kitbonus & "(+" & kitbonus & ") required skill for 90%: " & nrequiredSkill)
                    Host.Actions.ApplyItem(nKitId, Core.CharacterFilter.Id)
                    Return True
                Else
                    wtcw("No healkit found")
                End If

            End If
        End If
    End Function

    Private Function salvage() As Boolean
        If mUstItems.Count > 0 Then
            Host.Actions.SalvagePanelSalvage()
            Return True
        End If
    End Function

    Private Sub scannInventoryForSalvage(ByVal idPack As Integer)
        mColScanInventoryItems.Clear()

        Dim inv As WorldObjectCollection = Core.WorldFilter.GetByContainer(idPack)

        For Each objitem As WorldObject In inv
            With objitem
                Dim wrk As Double = .Values(DoubleValueKey.SalvageWorkmanship)
                If wrk > 0 AndAlso Not mUstItems.ContainsKey(.Id) Then

                    'check if it is not a usefull item
                    If .ObjectClass <> ObjectClass.Salvage Then

                        If .HasIdData Then 'check invalid
                            If Not String.IsNullOrEmpty(.Values(StringValueKey.Inscription)) Then
                                Continue For
                            End If
                            If .Values(LongValueKey.Imbued) <> 0 Then
                                Continue For
                            End If
                            If .Values(LongValueKey.NumberTimesTinkered) <> 0 Then
                                Continue For
                            End If
                        End If

                        If CheckItemForSalvage(CInt(wrk), .Values(LongValueKey.Material)) <> String.Empty OrElse _
                        (mPluginConfig.SalvageHighValue AndAlso CheckItemForValue(.Values(LongValueKey.Value), .Values(LongValueKey.Burden))) Then
                            mColScanInventoryItems.Add(objitem.Id, Nothing)
                            'we need to ident again to grab the setId's again
                            'TODO no ident needed when .hasiddata

                            IdqueueAdd(objitem.Id)
                        End If

                    End If

                End If
            End With
        Next

        If mColScanInventoryItems.Count = 0 Then
            wtcw("0 items found for Salvaging")
        Else
            wtcw("Inspecting " & mColScanInventoryItems.Count & " items for salvage")
        End If

    End Sub


    Private Sub addToUstList(ByVal itemname As String, ByVal itemid As Integer, ByVal desc As String)

        Dim newrow As Decal.Adapter.Wrappers.ListRow = lstUstList.Add()
        newrow(0)(0) = itemname
        newrow(1)(0) = desc
        newrow(2)(1) = &H6005E6A
        newrow(3)(0) = itemid
    End Sub

    Private Function salvageskill() As Integer

        Dim objSkillInfo As Decal.Adapter.Wrappers.SkillInfoWrapper
        objSkillInfo = Core.CharacterFilter.Skills(Wrappers.CharFilterSkillType.Salvaging)
        If objSkillInfo.Training = Wrappers.TrainingType.Specialized Or _
            objSkillInfo.Training = Wrappers.TrainingType.Trained Then
            Return (objSkillInfo.Buffed)
        End If

    End Function

    ' called from identify object event
    Private Sub OnScanInventoryItemForSalvage(ByVal objitem As IdentifiedObject, ByVal bCheckFinished As Boolean)
        With objitem
            If .HasIdData Then
                If mColScanInventoryItems.ContainsKey(.Id) Then
                    mColScanInventoryItems.Remove(.Id)
                End If

                If mIdqueue.ContainsKey(.Id) Then
                    mIdqueue.Remove(.Id)
                End If

                If Not String.IsNullOrEmpty(objitem.StringValues(StringValueKey.Inscription)) Then
                    Return
                End If
                If objitem.IntValues(LongValueKey.Imbued) <> 0 Then
                    Return
                End If
                If objitem.IntValues(LongValueKey.NumberTimesTinkered) <> 0 Then
                    Return
                End If

                Dim r As rule = MatchingRule(objitem, False)
                If r IsNot Nothing Then
                    Return
                End If

                Dim desc As String = CheckItemForSalvage(CInt(.DblValues(DoubleValueKey.SalvageWorkmanship)), .IntValues(LongValueKey.Material))
                If desc = String.Empty Then
                    If (mPluginConfig.SalvageHighValue AndAlso CheckItemForValue(.IntValues(LongValueKey.Value), .IntValues(LongValueKey.Burden))) Then
                        desc = "value"
                    Else
                        Return ' should not happen to get here
                    End If
                End If

                Dim newinfo As salvageustinfo = getsalvageinfo(.Id, .Name, _
                CInt(.DblValues(DoubleValueKey.SalvageWorkmanship)), .IntValues(LongValueKey.Material), .IntValues(LongValueKey.UsesRemaining), False)

                mUstItems.Add(.Id, newinfo)
                addToUstList(.Name, .Id, desc)


            End If
        End With

    End Sub

    Private Function ExpectedSalvageReturn(ByVal wrk As Integer, ByVal skill As Integer, ByVal Augmentations As Integer) As Integer
        Dim augfactor As Double = 1.0 + (0.25 * Augmentations)
        If augfactor > 2 Then
            augfactor = 2
        End If

        Dim units As Double = Math.Ceiling(augfactor * skill * wrk / 193.9)

        Return CInt(units)
    End Function

    Private Class salvageustinfo
        Public id As Integer
        Public material As Integer
        Public wrk As Double
        Public units As Integer
        Public name As String
        Public issalvage As Boolean
        Public startrange As Integer
        Public endrange As Integer
        Public expectedsalvageunits As Integer

    End Class

    Private Function getsalvageinfo(ByVal id As Integer, ByVal name As String, ByVal wrk As Integer, ByVal material As Integer, ByVal units As Integer, ByVal issalvage As Boolean) As salvageustinfo
        Dim newinfo As New salvageustinfo
        newinfo.id = id
        newinfo.name = name 'debug info not needed

        newinfo.wrk = wrk
        newinfo.material = material
        setMinMaxSalvage(newinfo.startrange, newinfo.endrange, newinfo.material, CInt(newinfo.wrk))

        newinfo.issalvage = issalvage
        If issalvage Then
            newinfo.units = units
            newinfo.expectedsalvageunits = units
        Else
            newinfo.expectedsalvageunits = ExpectedSalvageReturn(CInt(newinfo.wrk), salvageskill, mCharconfig.salvageaugmentations)
        End If


        Return newinfo
    End Function

    Private Function loadSalvagePanel() As Integer
        Try
            Dim itemsadded As Integer = 0
            Dim oust As WorldObject = GetFindItemFromInventory("Ust")
            If oust IsNot Nothing Then
                Host.Actions.UseItem(oust.Id, 0)
            Else
                wtcw("Ust not found")
                Return 0
            End If

            'create a collection of bags with partial salvage from inventory
            Dim salvageandItems As New Dictionary(Of Integer, salvageustinfo)
            Dim inv As WorldObjectCollection = Core.WorldFilter.GetInventory

            For Each objitem As WorldObject In inv
                With objitem
                    If .ObjectClass = ObjectClass.Salvage AndAlso .Values(LongValueKey.UsesRemaining) < 100 Then

                        Dim newinfo As salvageustinfo = getsalvageinfo(.Id, .Name, _
                        CInt(.Values(DoubleValueKey.SalvageWorkmanship)), .Values(LongValueKey.Material), .Values(LongValueKey.UsesRemaining), True)

                        salvageandItems.Add(.Id, newinfo)

                    End If
                End With
            Next

            ' add looted items to it
            For Each x As KeyValuePair(Of Integer, salvageustinfo) In mUstItems
                If Host.Actions.IsValidObject(x.Key) Then
                    salvageandItems.Add(x.Key, x.Value)
                End If
            Next

            'selection of possible salvage grade combinations 
            Dim smurfl = From p In salvageandItems Select _
                         p.Value.material, p.Value.startrange, p.Value.endrange Distinct

            Dim materialplacedinpanel As New List(Of Integer) ' can only use one material

            For Each o In smurfl

                Dim mat As Integer = o.material
                Dim minsalvage As Integer = o.startrange
                Dim maxsalvage As Integer = o.endrange

                Util.Log("smurf: " & mat & " " & minsalvage & "-" & maxsalvage)


                If Not materialplacedinpanel.Contains(mat) Then

                    Dim selectedsalvage = From p In salvageandItems Select p Where p.Value.material = mat _
                                          AndAlso p.Value.startrange = minsalvage AndAlso p.Value.endrange = maxsalvage Order By p.Value.units Descending

                    Dim ncount As Integer = selectedsalvage.Count
                    Dim unitsadded As Integer
                    Dim xlist As New List(Of Integer)
                    Dim salvageonly As Boolean
                    Dim finished As Boolean
                    Dim xstart As Integer = 0
                    Dim idx As Integer
                    Do
                        finished = False
                        salvageonly = True
                        xlist.Clear()
                        unitsadded = 0
                        idx = 0

                        Util.Log("smurf: xstart: " & xstart & " ncount " & ncount)

                        For Each k As KeyValuePair(Of Integer, salvageustinfo) In selectedsalvage
                            If idx >= xstart Then
                                If Not (ncount = 1 And k.Value.issalvage) Then 'nothing to combine with
                                    If unitsadded < 100 AndAlso (unitsadded + k.Value.expectedsalvageunits < 120) Then

                                        unitsadded += k.Value.expectedsalvageunits
                                        'salvage panel add
                                        xlist.Add(k.Key)
                                        If Not k.Value.issalvage Then
                                            salvageonly = False
                                        End If

                                        Util.Log("smurf: xlist: " & k.Value.expectedsalvageunits & " unitsadded " & unitsadded)

                                        If Not materialplacedinpanel.Contains(mat) Then
                                            materialplacedinpanel.Add(mat)
                                        End If
                                    End If
                                End If
                            End If
                            idx += 1
                        Next

                        If Not (salvageonly And xlist.Count = 1) Then
                            For Each id As Integer In xlist
                                Host.Actions.SalvagePanelAdd(id)
                                itemsadded += 1
                            Next
                            finished = True

                            Util.Log("smurf: finished1: ")

                        Else 'rollback
                            If materialplacedinpanel.Contains(mat) Then
                                materialplacedinpanel.Remove(mat)
                            End If
                            xstart += 1
                            If xstart + 1 >= ncount Then
                                finished = True

                                Util.Log("smurf: finished2: ")

                            Else

                                Util.Log("smurf: rollback: ")

                            End If
                        End If

                    Loop Until finished
                    Util.Log("smurf outside do: xstart ")
                End If
            Next

            Return itemsadded
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function
    Private Enum eMousePositions
        CloseSalvage
        'HighAttack
        'MedAttack
        'LowAttack
        'FellowInvite
    End Enum
    Private Lostfocus As Boolean
    Private Sub clickmouse(ByVal pType As eMousePositions)

        If Lostfocus Then
            Return
        End If

        Dim p As WinApe.POINTAPI
        p = WinApe.MouseGetCursor
        Dim rectWindow, rectClient As WinApe.RECTAPI
        Dim ret As Integer

        ret = WinApe.GetWindowRect(Host.Decal.Hwnd, rectWindow)
        If Not (ret <> 0) Then 'error! ac terminated?
            Exit Sub
        End If

        ret = WinApe.GetClientRect(Host.Decal.Hwnd, rectClient)
        If Not (ret <> 0) Then
            Exit Sub
        End If

        Dim difx As Integer = rectWindow.right - rectWindow.left - rectClient.right
        Dim divy As Integer = rectWindow.bottom - rectClient.bottom - rectClient.top
        If difx <> 0 Then difx = CInt(difx / 2)

        If rectWindow.top <= 0 AndAlso _
           rectWindow.left <= 0 AndAlso _
           rectWindow.bottom <= 0 AndAlso _
           rectWindow.right <= 0 Then
            Util.Log("Asheron's call window minimized")
            Exit Sub 'minimized exit
        End If

        Dim x, y As Integer
        x = rectWindow.left + difx
        y = divy - difx
        Select Case pType
            Case eMousePositions.CloseSalvage
                WinApe.MouseLeftClick(x + Host.Actions.Region3D.Right - 14, y + Host.Actions.Region3D.Bottom + 10)
                WinApe.MouseSetCursor(p)

        End Select

        '  
    End Sub
    Dim mwaitonopen As Boolean
    Dim mtryopenstart As DateTime

    Private Function donelooting() As Boolean
        If (mCurrentContainerContent.Count > 0 And mCurrentContainer <> 0) Then
            For Each d As KeyValuePair(Of Integer, Integer) In mCurrentContainerContent
                If mNotifiedItems.ContainsKey(d.Key) Then
                    Return False
                End If
            Next
        End If

        Return True
    End Function
    Private Function AutoPickup() As Boolean

        If mCurrentContainerContent IsNot Nothing AndAlso (mPluginConfig.AutoPickup And mCurrentContainerContent.Count > 0 And mCurrentContainer <> 0) Then
            If mFreeMainPackslots > 2 Then
                Dim lootid As Integer = 0
                Dim prevmatch As eScanresult = eScanresult.nomatch

                For Each d As KeyValuePair(Of Integer, Integer) In mCurrentContainerContent
                    If mNotifiedItems.ContainsKey(d.Key) Then
                        Dim n As notify = CType(mNotifiedItems.Item(d.Key), Global.Alinco.Plugin.notify)

                        If n.scantype = eScanresult.rare Then
                            lootid = d.Key
                            Exit For
                        ElseIf n.scantype = eScanresult.rule Then
                            lootid = d.Key
                            prevmatch = eScanresult.rule
                        ElseIf prevmatch <> eScanresult.rule Then
                            lootid = d.Key
                            prevmatch = n.scantype
                        Else
                            prevmatch = n.scantype
                            lootid = d.Key
                        End If

                    End If
                Next

                If lootid <> 0 Then
                    lootitem(lootid)

                    mFreeMainPackslots = CountFreeSlots(Core.CharacterFilter.Id)
                    Return True
                End If

            Else
                mPluginConfig.AutoPickup = False
                wtcw("Turned off auto pickup, free slots " & mFreeMainPackslots)
            End If
        End If
    End Function

    Private Function hotkeyloot(ByVal landscape As Boolean, ByVal landscapeitemrange As Double, ByVal maxz As Integer) As Boolean
        '1) loot items from open container
        'use/loot closest landscape item/npc or corpse

        Dim lootid As Integer
        Dim lootlandscapeid As Integer
        Dim corpseid As Integer

        For Each d As KeyValuePair(Of Integer, notify) In mNotifiedItems
            Dim o As WorldObject = Core.WorldFilter.Item(CInt(d.Key))

            If o IsNot Nothing AndAlso d.Value.scantype <> eScanresult.portalOrLifestone AndAlso d.Value.scantype <> eScanresult.monstertokill AndAlso d.Value.scantype <> eScanresult.allegmembers Then
                ' loot items from corpse first
                If mCurrentContainer <> 0 AndAlso o.Container = mCurrentContainer Then
                    lootid = o.Id
                Else
                    Dim objectId As Integer = o.Id

                    If o.Container <> 0 Then 'a closed corpse not looted yet
                        objectId = o.Container 'corpse
                        corpseid = objectId

                    ElseIf Not landscape AndAlso (d.Value.scantype <> eScanresult.corpse AndAlso d.Value.scantype <> eScanresult.corpseself AndAlso d.Value.scantype <> eScanresult.corpsewithrare) Then
                        'container = 0 not a corpse
                        Continue For
                    End If

                    ' calc range
                    Dim r As Double = DistanceTo(objectId)
                    If r < landscapeitemrange Then
                        landscapeitemrange = r
                        lootlandscapeid = objectId
                    End If

                End If
            End If
        Next

        If lootid = 0 Then
            lootid = lootlandscapeid
            If corpseid = lootid Then
                mwaitonopen = True
                mtryopenstart = Now
            End If

            For Each d As KeyValuePair(Of Integer, notify) In mNotifiedCorpses
                Dim o As WorldObject = Core.WorldFilter.Item(CInt(d.Key))
                If o IsNot Nothing Then
                    Dim tr As Double = DistanceTo(o.Id, True)
                    If tr < landscapeitemrange Then
                        mwaitonopen = True
                        landscapeitemrange = tr
                        mtryopenstart = Now
                        lootid = o.Id
                    End If
                End If
            Next
        End If

        If lootid <> 0 Then
            Host.Actions.UseItem(lootid, 0)
            Return True
        End If
    End Function
End Class
