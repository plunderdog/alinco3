Imports Decal.Adapter
Imports Alinco3Buffs.CharConfig
Imports Decal.Adapter.Wrappers
Imports System.Runtime.InteropServices


Partial Public Class Plugin

    Friend Class BuffInfo
        Public SpellId As Integer
        Public TargetId As Integer
        Public forcedRebuff As Boolean
        Public suspended As Boolean
        Public school As Integer
        Public TimeCasted As Date
        Public PlayerAgeCasted As Integer
        Public secondsremaining As Integer
        Public isSelfBuff As Boolean
        Public prebuff As Boolean
        Public duration As Double
        Public heading As Double
        Public player As String
        Public Function Key() As String
            Return Hex(TargetId) & "-" & Hex(SpellId)
        End Function
    End Class

    Friend Class CastingInfo
        Public SpellId As Integer
        Public TargetId As Integer
        Public CastTime As Date
        Public TimeoutSeconds As Integer
        Public key As String

        Public Function Timeout() As Boolean
            Return (DateDiff(DateInterval.Second, CastTime, Now) > TimeoutSeconds)
        End Function

        Public Sub New(ByVal spell As Integer, ByVal target As Integer, ByVal timeout As Integer)
            key = Hex(target) & "-" & Hex(spell)
            CastTime = Now
            SpellId = spell
            TargetId = target
            TimeoutSeconds = timeout
        End Sub
    End Class


    Private mBuffs As New Dictionary(Of String, BuffInfo)
    Private mwand As Decal.Adapter.Wrappers.WorldObject
    Private mbuffing As Boolean
    Private mcurrentcast As CastingInfo
    Private mWaitAfterAction As Integer
    Private mCurrentbuffedCreature As Integer
    Private mCurrentbuffedLifemagic As Integer
    Private mCurrentbuffedItemmagic As Integer
    Private mNoManaforspellCounter As Integer

    'TODO add to settings file
    Private mArr_Focus() As Integer = {0, 1421, 1422, 1423, 1424, 1425, 1426, 2067, 2067}
    Private mArr_Self() As Integer = {0, 1445, 1446, 1447, 1448, 1449, 1450, 2091, 2091}
    Private mArr_Creature() As Integer = {0, 557, 558, 559, 560, 561, 562, 2215, 2215}
    Private mArr_Life() As Integer = {0, 605, 606, 607, 608, 609, 610, 2267, 2267}
    Private mArr_ManaWand() As Integer = {0, 1475, 1476, 1477, 1478, 1479, 1480, 2117, 4418}
    Private mArr_Item() As Integer = {0, 581, 582, 583, 584, 585, 586, 2249, 4564}
    Private mArr_ManaConversion() As Integer = {0, 653, 654, 655, 656, 657, 658, 2287, 4602}
    Private mPrebuffs As List(Of Integer)

    'TODO read augmentation, using charconfig now
    'Private mArchmageEnduranceAugmentation As Integer

    Private Sub Suspendbuffs()

        wtcw2("Suspending all buffs ")

        For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
            d.Value.suspended = True
        Next
    End Sub

    Private Sub Addbuff(ByVal SpellID As Integer, ByVal TargetID As Integer, ByVal flag As Boolean)
        Dim b As New BuffInfo
        b.TargetId = TargetID
        b.SpellId = SpellID
        b.isSelfBuff = flag

        If mBuffs Is Nothing Then
            mBuffs = New Dictionary(Of String, BuffInfo)
        End If

        Dim spell As Decal.Filters.Spell
        spell = mFileService.SpellTable.GetById(SpellID)
        If Not spell Is Nothing Then

            Select Case spell.School.Id 'convert to flags for bitwise or
                Case 4
                    b.school = eMagicSchool.Creature
                Case 3
                    b.school = eMagicSchool.Item
                Case 2
                    b.school = eMagicSchool.Life
            End Select
            b.duration = spell.Duration
            b.duration += (b.duration * mcharconfig.ArchmageEnduranceAugmentation * 0.2)

            If Not Core.CharacterFilter.IsSpellKnown(SpellID) Then
                wtcw2("Addbuff, You don't know that spell: " & SpellID)
            ElseIf Not mBuffs.ContainsKey(b.Key) Then
                mBuffs.Add(b.Key, b)
                wtcw2("Addbuff, adding: " & b.Key & spell.Name & " duration " & b.duration & SpellID)
            ElseIf mBuffs.Item(b.Key) Is Nothing Then
                mBuffs.Item(b.Key) = b
            Else
                wtcw2("Updating buff: " & b.Key & spell.Name)
                mBuffs.Item(b.Key).suspended = False
            End If
        Else
            wtcw2("addbuff Decal.Filters.Spell is null " & SpellID)
        End If
    End Sub


    Private Function addbuff(ByVal SpellID As Integer, ByVal TargetID As Integer) As Boolean
        Try
            Dim outspellname As String = String.Empty
            Dim result As Integer = mapspellLevel(SpellID, outspellname, mcharconfig.fallback)
            If result <> 0 Then
                addbuff(SpellID, TargetID, True)
            Else

                wtcw2("spell not known or no fallback spell found for :" & outspellname)

                Return False
            End If
            Return True

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

    Private Sub loadbuffs()
        Try
            mbuffselectionChanged = False

            Suspendbuffs()

            mwand = GetEquippedCaster()

            For Each iSpellId As Integer In mcharconfig.selfbuffcreature
                Addbuff(iSpellId, Core.CharacterFilter.Id)
            Next

            For Each iSpellId As Integer In mcharconfig.selfbufflife
                Addbuff(iSpellId, Core.CharacterFilter.Id)
            Next

            For Each kvp As KeyValuePair(Of Integer, Integerlist) In mcharconfig.selfbuffweaponbuffs
                Dim objitem As Decal.Adapter.Wrappers.WorldObject = GetFindItemFromInventory(kvp.Key)
                If objitem IsNot Nothing Then

                    If kvp.Value IsNot Nothing Then
                        For Each spellId As Integer In kvp.Value
                            Addbuff(spellId, kvp.Key)
                        Next
                    End If

                End If
            Next

            For Each armorId As Integer In mcharconfig.selfbuffarmor
                If armorId = Core.CharacterFilter.Id Then
                    For Each spellId As Integer In mcharconfig.selfbuffbanes
                        Addbuff(spellId, armorId)
                    Next
                Else
                    If GetFindItemFromInventory(armorId) IsNot Nothing Then
                        For Each spellId As Integer In mcharconfig.selfbuffbanes
                            Addbuff(spellId, armorId)
                        Next
                    End If
                End If
            Next

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnCharacterStatusMessage(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.StatusMessageEventArgs)
        If mbuffing Then

            wtcw2("OnDisplayStatusmessage id: 0x" & Hex(e.Type) & " " & e.Text)

            If e.Type = &H1D Then 'bussy
                If mcurrentcast IsNot Nothing Then
                    wtcw2("Bussy mcurrentcast again ")
                    If mcharconfig.Fastcasting Then
                        docasting()
                    End If
                Else
                    wtcw2("Bussy")
                End If

            Else
                If e.Type = &H400 Then 'no more comps
                    wtcw("No more comps, buffs canceled")
                    CancelBuffs(eMagicSchool.Creature Or eMagicSchool.Item Or eMagicSchool.Life)
                    If Not String.IsNullOrEmpty(mcurrentcustomer) Then
                        CancelBotBuffs(mcurrentcustomer)
                        RaiseEvent buffbotcompleted(mcurrentcustomer, "Error: No more comps")
                    End If
                ElseIf e.Type = &H4FA Then ' is an invalid targe
                    If Not String.IsNullOrEmpty(mcurrentcustomer) Then

                        CancelBotBuffs(mcurrentcustomer)
                        RaiseEvent buffbotcompleted(mcurrentcustomer, "Error: " & e.Text & " is an invalid target")
                        mcurrentcast = Nothing
                        mWaitAfterAction = 2

                    Else
                        CancelBotBuffs(mcurrentcustomer)
                    End If
                ElseIf e.Type = &H401 Then
                    wtcw2("No mana")
                    mNoManaforspellCounter += 1
                    docasting()

                ElseIf e.Type = &H402 Then 'fizzle
                    wtcw2("Fizzle")
                    mNoManaforspellCounter += 1

                ElseIf e.Type = &H3FE Then 'unknown spell, programmer error you should not add unknown spells to bufflist
                    If mcurrentcast IsNot Nothing Then
                        Dim spell As Decal.Filters.Spell
                        spell = mFileService.SpellTable.GetById(mcurrentcast.SpellId)
                        If spell IsNot Nothing Then
                            wtcw("You don't know that spell: " & spell.Name)
                        Else
                            wtcw("You don't know that spell: " & Hex(mcurrentcast.SpellId))
                        End If

                        If mBuffs.ContainsKey(mcurrentcast.key) Then
                            mBuffs.Item(mcurrentcast.key).suspended = True
                        End If

                        If mBotBuffs.ContainsKey(mcurrentcast.key) Then
                            mBotBuffs.Item(mcurrentcast.key).suspended = True
                        End If
                    End If
                ElseIf e.Type = 1224 Then

                    If UseConsumable(eConsumableType.ManaStone, 0) Then
                        wtcw("no more mana in casting device, using manastone ")
                    Else
                        wtcw("no more mana in casting device cancel buffs")
                        CancelBuffs(eMagicSchool.Creature Or eMagicSchool.Item Or eMagicSchool.Life)
                    End If
                End If

                mcurrentcast = Nothing
                mWaitAfterAction = 2
            End If

        End If
        'wtcw(e.Text & ": " & e.Type)

    End Sub
    Private Sub OnCharacterSpellbookChange(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.SpellbookEventArgs)
        Try

            loadbuffprofile()
            mbuffselectionChanged = True

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    Private Sub OnCharacterActionComplete(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            If mcurrentcast IsNot Nothing Then
                wtcw2("OnCharacterActionComplete" & mcurrentcast.key)
            ElseIf mcharconfig.Fastcasting Then 'successcheck passed,turbo casting

                wtcw2("OnCharacterActionComplete completed null, docasting again")
                If Not mPause Then
                    docasting()
                End If
            Else
                wtcw2("OnCharacterActionComplete mcurrentcast is nothing ")
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnCharacterFilterSpellCast(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.SpellCastEventArgs)
        Try

            Dim skey As String = Hex(e.TargetId) & "-" & Hex(e.SpellId)
            Dim b As BuffInfo = Nothing

            wtcw2("OnCastSpellTargeted key:" & skey)

            If mBuffs.ContainsKey(skey) Then
                b = mBuffs.Item(skey)

                If mcurrentcast Is Nothing Then
                    mcurrentcast = New CastingInfo(e.SpellId, e.TargetId, 5)

                    If e.TargetId = Core.CharacterFilter.Id Then
                        If b.school <> eMagicSchool.Item Then
                            mcurrentcast.TimeoutSeconds = 2
                        End If
                    End If

                End If

            ElseIf mBotBuffs.ContainsKey(skey) Then
                b = mBotBuffs.Item(skey)
                If mcurrentcast Is Nothing Then
                    mcurrentcast = New CastingInfo(e.SpellId, e.TargetId, 5)
                End If

                If e.TargetId = Core.CharacterFilter.Id Then
                    If b.school <> eMagicSchool.Item Then
                        mcurrentcast.TimeoutSeconds = 2
                    End If
                    b.suspended = True
                End If

            ElseIf mbuffing Then ' revit
                Select Case e.SpellId
                    Case 2345, 1180, 1181, 1182, 2083, 1679, 1680, 1681, 2345, 1702, 1703, 1704, 2332
                        mcurrentcast = New CastingInfo(e.SpellId, e.TargetId, 5)
                End Select

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    ' Private mPendingGembuffs As Integer
    Private Sub UpdateStatus(ByVal ExpirePeriod As Integer)
        'mPendingGembuffs = 0
        'For Each kvp As KeyValuePair(Of String, consumable) In mcharconfig.consumables
        '    If kvp.Value IsNot Nothing AndAlso kvp.Value.Constype = eConsumableType.Gem Then
        '        kvp.Value.expired = True
        '        mPendingGembuffs += 1
        '    End If
        'Next

        mUpdateTime = Now
        Dim nTime As Integer = 0
        Dim Spell As Decal.Adapter.Wrappers.EnchantmentWrapper
        Dim playerage As Integer = Core.CharacterFilter.Age
        For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
            If Not d.Value.suspended Then
                d.Value.secondsremaining = 0

                're-calculate the seconds remaining
                If d.Value.school = eMagicSchool.Item And d.Value.PlayerAgeCasted > 0 Then
                    'Dim nSecondsPastCasting As Double = DateDiff(DateInterval.Second, d.Value.TimeCasted, Now)
                    'd.Value.secondsremaining = CInt(d.Value.duration - nSecondsPastCasting)

                    If d.Value.PlayerAgeCasted > 0 Then
                        Dim diff As Integer
                        diff = d.Value.PlayerAgeCasted + CInt(d.Value.duration) ' expires
                        diff = diff - playerage


                        d.Value.secondsremaining = diff
                    End If

                    If d.Value.secondsremaining <= 0 Then
                        d.Value.secondsremaining = 0
                    End If
                End If

            End If
        Next

        'read time remaining for life and creature 
        Dim b As BuffInfo
        Dim sKeyPre As String = Hex(Core.CharacterFilter.Id) & "-"
        Dim sKey As String
        For i As Integer = 0 To Core.CharacterFilter.Enchantments.Count - 1
            Spell = Core.CharacterFilter.Enchantments(i)
            If Not Spell Is Nothing Then
                sKey = sKeyPre & Hex(Spell.SpellId)
                If mBuffs.ContainsKey(sKey) Then
                    b = mBuffs.Item(sKey)
                    If Spell.TimeRemaining > b.secondsremaining Then    'set to latest spell to experi
                        b.secondsremaining = Spell.TimeRemaining
                    End If
                Else
                    sKey = sKeyPre & Hex(Spell.SpellId + 1)
                    If mBuffs.ContainsKey(sKey) Then
                        b = mBuffs.Item(sKey)
                        If Spell.TimeRemaining > b.secondsremaining Then
                            b.secondsremaining = Spell.TimeRemaining
                        End If
                    Else
                        'For Each kvp As KeyValuePair(Of String, consumable) In mcharconfig.consumables
                        '    If kvp.Value IsNot Nothing AndAlso kvp.Value.Constype = eConsumableType.Gem _
                        '     AndAlso kvp.Value.Amt = Spell.SpellId Then
                        '        kvp.Value.expired = False
                        '        mPendingGembuffs -= 1
                        '    End If
                        'Next

                    End If
                End If
            End If
        Next

        '3 timers, creature, item and life
        mSecsTillFirstBuffC = -1
        mSecsTillFirstBuffI = -1
        mSecsTillFirstBuffL = -1


        mBuffsPending = 0

        For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
            If Not d.Value.suspended And Not d.Value.prebuff Then
                If d.Value.forcedRebuff Then
                    mBuffsPending += 1

                ElseIf d.Value.school <> eMagicSchool.Item Then
                    If (d.Value.secondsremaining <= ExpirePeriod) Then
                        mBuffsPending += 1
                    End If

                    If d.Value.school = eMagicSchool.Creature Then
                        If (d.Value.secondsremaining <= mSecsTillFirstBuffC) Or mSecsTillFirstBuffC = -1 Then
                            mSecsTillFirstBuffC = d.Value.secondsremaining
                        End If
                    End If

                    If d.Value.school = eMagicSchool.Life Then
                        If (d.Value.secondsremaining <= mSecsTillFirstBuffL) Or mSecsTillFirstBuffL = -1 Then
                            mSecsTillFirstBuffL = d.Value.secondsremaining
                        End If
                    End If

                ElseIf d.Value.school = eMagicSchool.Item And d.Value.PlayerAgeCasted > 0 Then

                    If (d.Value.secondsremaining <= mSecsTillFirstBuffI) Or mSecsTillFirstBuffI = -1 Then
                        mSecsTillFirstBuffI = d.Value.secondsremaining
                    End If
                ElseIf d.Value.school = eMagicSchool.Item Then
                    If (d.Value.secondsremaining <= ExpirePeriod) Then
                        mBuffsPending += 1
                    End If
                End If
            End If
        Next

        For Each d As KeyValuePair(Of String, BuffInfo) In mBotBuffs
            If Not d.Value.suspended Then
                mBuffsPending += 1
            End If
        Next
    End Sub

    Private Function magiclevelSkill(ByVal eSkill As Decal.Adapter.Wrappers.CharFilterSkillType) As Integer
        Dim buffed As Integer = 0

        Dim objSkillInfo As Decal.Adapter.Wrappers.SkillInfoWrapper
        objSkillInfo = Core.CharacterFilter.Skills(eSkill)
        If objSkillInfo.Training = Decal.Adapter.Wrappers.TrainingType.Specialized Or _
            objSkillInfo.Training = Decal.Adapter.Wrappers.TrainingType.Trained Then
            buffed = (objSkillInfo.Buffed)
        End If
        If buffed >= mcharconfig.magiclevel8 Then
            Return 8
        ElseIf buffed >= mcharconfig.magiclevel7 Then
            Return 7
        ElseIf buffed >= mcharconfig.magiclevel6 Then
            Return 6
        ElseIf buffed >= mcharconfig.magiclevel5 Then
            Return 5
        ElseIf buffed >= mcharconfig.magiclevel4 Then
            Return 4
        End If

    End Function

    Private Function validateprebuff(ByVal bitem As Boolean) As Boolean
        mwand = GetEquippedCaster()

        If mwand Is Nothing Then
            For Each kvp As KeyValuePair(Of Integer, Integerlist) In mcharconfig.selfbuffweaponbuffs
                Dim objitem As Decal.Adapter.Wrappers.WorldObject = GetFindItemFromInventory(kvp.Key)
                If Not objitem Is Nothing Then
                    If objitem.Category = eObjectFlags.Caster Then
                        mwand = objitem '' first wand in the row
                        Exit For
                    End If
                End If
            Next
        End If

        If mwand Is Nothing Then
            mainTabs.ActiveTab = 3
            wtcw("Add a wand to the weapons list and/or select the right most checkbox that indicates the buffing wand")
            Return False
        End If

        Dim l, c, i As Integer
        If mcharconfig.lifemagiclevel > 0 Then
            l = magiclevelSkill(Decal.Adapter.Wrappers.CharFilterSkillType.LifeMagic)
            c = magiclevelSkill(Decal.Adapter.Wrappers.CharFilterSkillType.CreatureEnchantment)
            i = magiclevelSkill(Decal.Adapter.Wrappers.CharFilterSkillType.ItemEnchantment)

            If c < 7 Or l < 7 Or (i < 7 And bitem) Then
                If (i < 4 And bitem) Then
                    Return False
                End If

                If l < 4 Or c < 4 Or (i < 4 And bitem) Then
                    Return False
                End If
            End If
        End If


        Return True
    End Function

    Public Sub CancelBuffs(ByVal schoolid As eMagicSchool)

        wtcw2("Cancel active buffing")
        Dim stillbuffing As Boolean

        For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
            If (schoolid And d.Value.school) = d.Value.school Then
                d.Value.forcedRebuff = False
            End If
            If d.Value.forcedRebuff Then
                stillbuffing = True
            End If
        Next
        For Each d As KeyValuePair(Of String, BuffInfo) In mBotBuffs
            d.Value.suspended = True
        Next
        If Not stillbuffing Then
            mbuffing = False
        End If

        UpdateStatus(mcharconfig.PendingbuffsTimeout)
    End Sub

    'Friend Function BuffTimeRemaining2(ByVal spID As Integer, ByVal bCheckother As Boolean) As Integer
    '    Dim nTime As Integer = 0
    '    Dim Spell As Decal.Adapter.Wrappers.EnchantmentWrapper

    '    For i As Integer = 0 To Core.CharacterFilter.Enchantments.Count - 1
    '        Spell = Core.CharacterFilter.Enchantments(i)
    '        If Not Spell Is Nothing Then


    '            'buffbot love
    '            'TODO check on "to other" or higher spelllevel spell is casted on the player 

    '            'currently quick and dirty check on other level 7 which is -1 
    '            'also "Incantation x To Other" is also -1  

    '            If Spell.SpellId = spID Or (bCheckother = True And Spell.SpellId = spID - 1) Then
    '                If Spell.TimeRemaining > nTime Then
    '                    nTime = Spell.TimeRemaining
    '                End If
    '            End If
    '        End If
    '    Next

    '    Return nTime
    'End Function


    Private Sub additem(ByVal SpellId As Integer, ByVal TargetId As Integer, ByVal secondsremaining As Integer, ByVal playerage As Integer)
        Dim nindex As Integer = 0
        If mcharconfig.ItemTimers Is Nothing Then
            ReDim mcharconfig.ItemTimers(0)
        Else
            nindex = UBound(mcharconfig.ItemTimers) + 1
            ReDim Preserve mcharconfig.ItemTimers(nindex)
        End If

        mcharconfig.ItemTimers(nindex).targetId = TargetId
        mcharconfig.ItemTimers(nindex).spellId = SpellId
        mcharconfig.ItemTimers(nindex).secondsremaining = secondsremaining ' time casted
        mcharconfig.ItemTimers(nindex).playerAgeCasted = playerage  ' time casted
    End Sub

    Private Sub saveItemTimers()
        ReDim mcharconfig.ItemTimers(-1)
        For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
            If d.Value.suspended = False Then
                If d.Value.school = eMagicSchool.Item Then
                    d.Value.secondsremaining = 0
                    If d.Value.TimeCasted.Year > 2000 Then
                        Dim nSecondsPastCasting As Double = DateDiff(DateInterval.Second, d.Value.TimeCasted, Now)
                        d.Value.secondsremaining = CInt(d.Value.duration - nSecondsPastCasting)
                        If d.Value.secondsremaining < 0 Then
                            d.Value.secondsremaining = 0
                        End If
                    End If
                    additem(d.Value.SpellId, d.Value.TargetId, d.Value.secondsremaining, d.Value.PlayerAgeCasted)
                End If
            End If
        Next
    End Sub

    Private Sub FinishedBuffing()
        btnBuffItem.Text = "Item"
        btnBuffCreature.Text = "Life/Critter"
        btnPause.Text = "Pause"
        mPause = False
        mbuffing = False
        If (Host.Actions.CombatMode = Decal.Adapter.Wrappers.CombatState.Magic) Then
            Host.Actions.SetCombatMode(Decal.Adapter.Wrappers.CombatState.Peace)
        End If
        saveItemTimers()
    End Sub

    Public Function PhysicObjectLocation(ByVal id As Integer) As Location
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
    Public Function DistanceTo(ByVal playerlocation As Location, ByVal DestLoc As Location, Optional ByVal Face As Boolean = False) As Double
        Try

            If playerlocation Is Nothing Or DestLoc Is Nothing Then
                Return -1
            End If

            Dim dx As Double = Math.Abs(DestLoc.ew - playerlocation.ew)
            Dim dy As Double = Math.Abs(DestLoc.ns - playerlocation.ns)
            Dim dz As Double = Math.Abs((playerlocation.z - DestLoc.Z) / 240)

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
    Public Function DistanceTo(ByVal DestLocId As Integer, Optional ByVal Face As Boolean = False) As Double
        Try

            Dim playerlocation As Location = PhysicObjectLocation(Core.CharacterFilter.Id)
            Dim DestLoc As Location = PhysicObjectLocation(DestLocId)

            Return DistanceTo(playerlocation, DestLoc, Face)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function
    Private mcurrentcustomer As String

    Private Function FirstSpellToBuff() As BuffInfo
        Try
            Dim ret As BuffInfo = Nothing

            For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
                If d.Value.forcedRebuff And Not d.Value.suspended Then

                    If d.Value.school <> eMagicSchool.Item And d.Value.secondsremaining <= 0 Then
                        Return d.Value
                    End If

                    If ret Is Nothing Then
                        ret = d.Value

                    ElseIf (ret IsNot Nothing AndAlso ret.secondsremaining > d.Value.secondsremaining) Then
                        If d.Value.school <> eMagicSchool.Item Or (d.Value.school = eMagicSchool.Item And d.Value.TimeCasted.Year > 2000) Then
                            ret = d.Value
                        End If
                    End If
                End If
            Next

            Dim buffbotbuff As Boolean
            Dim checkfailed As String = String.Empty

            For Each d As KeyValuePair(Of String, BuffInfo) In mBotBuffs
                If Not d.Value.suspended Then
                    If d.Value.TargetId = Core.CharacterFilter.Id Then 'portals first

                        Return d.Value
                    End If

                    If ret Is Nothing Then
                        'check if buffbot target still valid
                        'player moved the weapon or armor in inventory
                        'player teleported or 
                        Dim bo As WorldObject = Core.WorldFilter.Item(d.Value.TargetId)
                        If bo Is Nothing Then
                            wtcw2("Is inValidObject " & Hex(d.Value.TargetId))
                            d.Value.suspended = True
                        Else
                            checkfailed = bo.Name
                            ret = d.Value
                            buffbotbuff = True
                        End If

                    End If
                End If
            Next

            If buffbotbuff AndAlso ret IsNot Nothing Then 'check range

                Dim range As Double = DistanceTo(ret.TargetId)

                'todo allow for armor tradewindow banes somehow

                'If range > mcharconfig.buffbotmaxrange Then
                '    CancelBotBuffs(ret.player)
                '    RaiseEvent buffbotcompleted(ret.player, checkfailed & " is out of range " & range.ToString("0.0"))
                'End If

            End If

            If ret IsNot Nothing AndAlso Not String.IsNullOrEmpty(ret.player) Then
                If String.IsNullOrEmpty(mcurrentcustomer) OrElse mcurrentcustomer <> ret.player Then

                    If Not String.IsNullOrEmpty(mcurrentcustomer) Then
                        RaiseEvent buffbotcompleted(mcurrentcustomer, String.Empty)
                    End If

                    mcurrentcustomer = ret.player
                    RaiseEvent buffbotstarted(mcurrentcustomer, 0)
                End If
            End If

            Return ret
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return Nothing
    End Function

    Private Sub castspell(ByVal spellid As Integer, ByVal targetid As Integer)
        wtcw2("castspell " & Hex(targetid) & "-" & Hex(spellid))

        Host.Actions.CastSpell(spellid, targetid)
    End Sub

    Private Function trycraftConsumable(ByVal name As String) As Boolean
        Dim useItem, onitem As Wrappers.WorldObject
        Select Case name
            Case "Elaborate Field Rations"
                useItem = GetFindItemFromInventory("Cooking Pot")
                If useItem IsNot Nothing Then
                    onitem = GetFindItemFromInventory("Elaborate Dried Rations")
                    If onitem IsNot Nothing Then
                        ' wtcw("trycraftConsumable ApplyItem " & useItem.Name & " on " & onitem.Name)

                        Host.Actions.ApplyItem(useItem.Id, onitem.Id)
                        Return True

                    End If

                End If

        End Select
    End Function

    Private Function PendingGemBuffs() As Integer
        Try
            For Each kvp As KeyValuePair(Of String, consumable) In mcharconfig.consumables
                If kvp.Value IsNot Nothing AndAlso kvp.Value.Constype = eConsumableType.Gem AndAlso kvp.Value.Amt <> 0 Then ' kvp.Value.vitalId = vitalid Then

                End If
            Next
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function
    Private Function UseConsumable(ByVal et As eConsumableType, ByVal vitalid As Integer) As Boolean
        For Each kvp As KeyValuePair(Of String, consumable) In mcharconfig.consumables

            If kvp.Value IsNot Nothing AndAlso et = kvp.Value.Constype Then ' kvp.Value.vitalId = vitalid Then
                If et = eConsumableType.Food AndAlso kvp.Value.vitalId = vitalid Then
                    Dim objconsumeit As Decal.Adapter.Wrappers.WorldObject = GetFindItemFromInventory(kvp.Key)
                    If objconsumeit IsNot Nothing Then
                        Host.Actions.UseItem(objconsumeit.Id, 0)
                        mWaitAfterAction = 5
                        Return True
                    Else
                        If trycraftConsumable(kvp.Key) Then
                            mWaitAfterAction = 5
                            Return True
                        End If
                    End If
                ElseIf et = eConsumableType.ManaStone Then
                    Dim objconsumeit As Decal.Adapter.Wrappers.WorldObject = GetFindItemFromInventory(kvp.Key)
                    If objconsumeit IsNot Nothing Then
                        Host.Actions.SelectItem(Core.CharacterFilter.Id)
                        Host.Actions.UseItem(objconsumeit.Id, Core.CharacterFilter.Id)
                        mWaitAfterAction = 5
                        Return True
                    End If
                End If

            End If
        Next

        Return False
    End Function

    Private Function ManaStaminaRecharge(ByVal bHealthToMana As Boolean) As Boolean
        Try
            wtcw2("Enter ManaStaminaRecharge")
            mCurrentbuffedLifemagic = magiclevelSkill(Decal.Adapter.Wrappers.CharFilterSkillType.LifeMagic)

            Dim arrRevit() As Integer = {0, 0, 0, 0, 1180, 1181, 1182, 2083, 2083}
            Dim arrStaminaToMana() As Integer = {0, 0, 0, 0, 1679, 1680, 1681, 2345, 2345}
            Dim arrHealthToMana() As Integer = {0, 0, 0, 0, 1702, 1703, 1704, 2332, 2332}

            Dim PlayerManaPct As Double = (Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.CurrentMana) / Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.MaximumMana)) * 100
            Dim PlayerHealthPct As Double = (Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.CurrentHealth) / Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.MaximumHealth)) * 100
            Dim PlayerStaminaPct As Double = (Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.CurrentStamina) / Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.MaximumStamina)) * 100

            Dim HealthToManaId As Integer = arrHealthToMana(mcharconfig.lifemagiclevel)
            Dim StaminaToManaId As Integer = arrStaminaToMana(mcharconfig.lifemagiclevel)
            Dim RevitId As Integer = arrRevit(mcharconfig.lifemagiclevel)

            If mCurrentbuffedLifemagic > 250 And mCurrentbuffedLifemagic < 300 Then
                HealthToManaId = arrHealthToMana(6)
                StaminaToManaId = arrStaminaToMana(6)
                RevitId = arrRevit(6)
            End If

            If HealthToManaId <> 0 And bHealthToMana AndAlso (Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.CurrentHealth) = Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.MaximumHealth) And _
               PlayerManaPct <= mcharconfig.RegenManapct) Then
                wtcw2("Enter Health to mana check")
                castspell(HealthToManaId, Core.CharacterFilter.Id)
                Return True
            End If

            If PlayerManaPct < mcharconfig.RegenManapct Or mNoManaforspellCounter > 3 Then
                If PlayerStaminaPct >= mcharconfig.RegenStaminapct Then
                    mNoManaforspellCounter = 0
                    wtcw2("Enter Stamina to mana check")
                    If StaminaToManaId <> 0 Then
                        castspell(StaminaToManaId, Core.CharacterFilter.Id)
                        Return True
                    End If
                Else
                    wtcw2("Enter Revit check")
                    mNoManaforspellCounter = 0
                    If RevitId <> 0 Then
                        castspell(RevitId, Core.CharacterFilter.Id)
                        Return True
                    End If
                End If
            End If

            If PlayerStaminaPct <= mcharconfig.RegenStaminapct Then
                wtcw2("Enter Revit check")
                If RevitId <> 0 Then
                    castspell(RevitId, Core.CharacterFilter.Id)
                    Return True
                End If

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Function

    Dim nottofast As New Stopwatch

    Private Sub docasting()
        If mbuffing Then
            If nottofast.IsRunning Then
                If nottofast.ElapsedMilliseconds < 300 Then
                    Return
                End If
            End If

            nottofast.Reset()
            nottofast.Start()

            If mBuffsPending > 0 Then
                wtcw2("FirstSpellToBuff")
                Dim objBuff As BuffInfo = FirstSpellToBuff()
                If Not objBuff Is Nothing Then
                    wtcw2("Buff found")
                    If objBuff.heading <> 0 Then
                        nheading = objBuff.heading
                        If headingdiff(nheading, Host.Actions.Heading) > 25 Then
                            Host.Actions.FaceHeading(nheading, False)
                            Return
                        Else
                            nheading = 0
                        End If
                    End If

                    If Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.CurrentMana) > mcharconfig.minmanaForCasting Then

                        'enough mana, start casting

                        If (Host.Actions.CombatMode = Decal.Adapter.Wrappers.CombatState.Magic) Then
                            If Not ManaStaminaRecharge(mcharconfig.usehealthtomana) Then

                                castspell(objBuff.SpellId, objBuff.TargetId)
                            End If
                        Else  '!= castingmode
                            Dim objwand As Decal.Adapter.Wrappers.WorldObject = Nothing
                            If mwand IsNot Nothing Then
                                objwand = GetFindItemFromInventory(mcharconfig.buffingwandid)
                            End If

                            If objwand Is Nothing Then
                                wtcw("Casting device not found ")
                                CancelBuffs(eMagicSchool.Creature Or eMagicSchool.Life Or eMagicSchool.Item)

                            ElseIf objwand.Values(Decal.Adapter.Wrappers.LongValueKey.Slot) = -1 Then
                                'go into combat
                                Host.Actions.SetCombatMode(Decal.Adapter.Wrappers.CombatState.Magic)
                                mWaitAfterAction = 4

                            Else 'equip wand
                                Host.Actions.UseItem(objwand.Id, 0)
                                mWaitAfterAction = 3
                            End If
                        End If

                    ElseIf UseConsumable(eConsumableType.Food, 6) Then 'use manapot when mana below a certain level

                        mWaitAfterAction = 5

                    ElseIf mwand.Values(Decal.Adapter.Wrappers.LongValueKey.AssociatedSpell) = 1679 Then
                        Dim PlayerStaminaPct As Double = (Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.CurrentStamina) / Host.Actions.Vital.Item(Decal.Adapter.Wrappers.VitalType.MaximumStamina)) * 100

                        If PlayerStaminaPct <= mcharconfig.RegenStaminapct Then
                            If (Host.Actions.CombatMode <> Decal.Adapter.Wrappers.CombatState.Peace) Then
                                Host.Actions.SetCombatMode(Decal.Adapter.Wrappers.CombatState.Peace)
                                mWaitAfterAction = 2
                                Return
                            End If

                            'drink stamina ration or do nothing until enough stamina
                            If UseConsumable(eConsumableType.Food, 4) Then

                            End If

                            Return
                        End If


                        If (Host.Actions.CombatMode <> Decal.Adapter.Wrappers.CombatState.Magic) Then
                            Host.Actions.SetCombatMode(Decal.Adapter.Wrappers.CombatState.Magic)
                            mWaitAfterAction = 4
                            Return
                        End If

                        Host.Actions.SelectItem(Core.CharacterFilter.Id)
                        Host.Actions.UseItem(mwand.Id, 1, Core.CharacterFilter.Id)
                        wtcw2("Casting stein")
                        mWaitAfterAction = 8

                    ElseIf mwand.Values(Decal.Adapter.Wrappers.LongValueKey.AssociatedSpell) = 1702 Then 'Elysa's Wondrous Orb
                        'healing?
                        Host.Actions.SelectItem(Core.CharacterFilter.Id)
                        Host.Actions.UseItem(mwand.Id, 1, Core.CharacterFilter.Id)
                        wtcw2("Elysa's Wondrous Orb")
                        mWaitAfterAction = 8

                    Else
                        wtcw2("wait mana")

                    End If
                Else

                    If Not String.IsNullOrEmpty(mcurrentcustomer) Then
                        RaiseEvent buffbotcompleted(mcurrentcustomer, String.Empty)
                        mcurrentcustomer = String.Empty
                    End If

                    wtcw2("Find first buff returned null")
                    FinishedBuffing()
                End If
            End If
        End If
    End Sub

    Private Function checksuccess(ByVal buff As CastingInfo) As Boolean
        Dim breturn As Boolean

        If mBuffs.ContainsKey(buff.key) Then

            wtcw2("checksuccess true " & buff.key)

            Dim b As BuffInfo = mBuffs.Item(buff.key)
            b.PlayerAgeCasted = Core.CharacterFilter.Age
            b.TimeCasted = Now
            b.forcedRebuff = False
            breturn = True

        ElseIf mBotBuffs.ContainsKey(buff.key) Then

            wtcw2("checksuccess true " & buff.key)

            Dim b As BuffInfo = mBotBuffs.Item(buff.key)
            b.PlayerAgeCasted = Core.CharacterFilter.Age
            b.TimeCasted = Now
            b.suspended = True
            breturn = True
        Else
            wtcw2("checksuccess False " & buff.key)
        End If

        Return breturn
    End Function

    <BaseEvent("ChatBoxMessage")> _
    Private Sub Plugin_ChatBoxMessage(ByVal sender As Object, ByVal e As Decal.Adapter.ChatTextInterceptEventArgs)
        Try
            If mcharconfig Is Nothing Then Return

            Dim msg As String = e.Text.Substring(0, e.Text.Length - 1)
            wtcw2(e.Color & vbTab & e.Text)

            Select Case e.Color
                Case 3
                    'If mcharconfig.Botoptions IsNot Nothing Then
                    '    mTellsqueue.Enqueue(msg)
                    'End If


                Case 17
                    If mcharconfig.filtercastself Then
                        If msg.StartsWith("The spell") Then
                            e.Eat = True
                        ElseIf msg.StartsWith("You say, ") Then
                            e.Eat = True
                        ElseIf msg.IndexOf("says,""") > 0 Then
                            e.Eat = True

                        End If
                    End If
                    If msg.IndexOf("no appropriate targets") >= 0 Then

                        wtcw2("no appropriate targets1 mcurrentcast " & CBool(mcurrentcast Is Nothing))
                        If mcurrentcast IsNot Nothing Then
                            CancelBotBuffs(mcurrentcast.TargetId)
                        End If

                        mcurrentcast = Nothing
                        mWaitAfterAction = 2
                    End If

                Case 7 'spellcasting

                    If msg.IndexOf("low on Mana") > 0 Then
                        wtcw("low on Mana")
                        wtcw("low on Mana")
                        wtcw("low on Mana")
                        wtcw("low on Mana")
                        wtcw("low on Mana")
                        wtcw("low on Mana")
                        mUseManaCharges = True
                    ElseIf mcurrentcast IsNot Nothing Then
                        If msg.IndexOf("You cast Stamina") >= 0 Then

                        ElseIf msg.IndexOf("You cast ") >= 0 Then

                            mWaitAfterAction = 2

                            If checksuccess(mcurrentcast) Then
                                If mcharconfig.filtercastself Then
                                    e.Eat = True
                                End If
                            End If
                            mcurrentcast = Nothing
                        ElseIf msg.IndexOf("You gain") >= 0 Then
                            mcurrentcast = Nothing
                            mWaitAfterAction = 1

                        ElseIf msg.IndexOf("resists your spell") >= 0 Then
                            If checksuccess(mcurrentcast) Then

                            End If
                            mcurrentcast = Nothing
                            mWaitAfterAction = 2

                        ElseIf msg.IndexOf("Target is out of range") >= 0 Then
                            CancelBotBuffs(mcurrentcast.TargetId)
                            mcurrentcast = Nothing
                            mWaitAfterAction = 2
                        ElseIf msg.IndexOf("no appropriate targets") >= 0 Then
                            wtcw2("no appropriate targets2")
                            CancelBotBuffs(mcurrentcast.TargetId)
                            mcurrentcast = Nothing
                            mWaitAfterAction = 2

                        ElseIf msg.IndexOf("is an invalid target") >= 0 Then
                            wtcw2("is an invalid target cancel")
                            CancelBotBuffs(mcurrentcast.TargetId)
                            mcurrentcast = Nothing
                            mWaitAfterAction = 2
                        ElseIf msg.IndexOf("You have been teleported") >= 0 Then
                            mcurrentcast = Nothing
                            mWaitAfterAction = 2

                        ElseIf msg.IndexOf("You cannot summon that portal!") >= 0 Then
                            If checksuccess(mcurrentcast) Then

                            End If
                            mWaitAfterAction = 2
                        End If

                    ElseIf mbuffing And mcharconfig.filtercastself Then
                        If msg.IndexOf("You cast ") >= 0 Then
                            e.Eat = True
                        ElseIf msg.StartsWith("The spell") Then
                            e.Eat = True
                        End If
                    End If

            End Select
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

End Class
