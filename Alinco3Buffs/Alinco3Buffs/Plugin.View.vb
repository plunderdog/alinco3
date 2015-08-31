Option Explicit On
Option Strict On

Imports Decal.Adapter.Wrappers
Imports Decal.Adapter
Imports Decal.Interop.Filters
Imports Alinco3Buffs.CharConfig

Partial Public Class Plugin

    Const argNormal As Integer = &HFFFFFFFF
    Const argFallback As Integer = &HFFFFFF Or &HFDB5E8
    Const argUnknown As Integer = &HFF5FA3EC


    Private Sub ListItemadd(ByVal lst As Decal.Adapter.Wrappers.ListWrapper, ByVal lngIcon As Long, ByVal strText As String, ByVal nID As Integer)
        Try
            If Not lst Is Nothing Then

                Dim lRow As Decal.Adapter.Wrappers.ListRow

                For i As Integer = 0 To lst.RowCount - 1
                    lRow = lst(i)
                    Dim id As Integer = CType(lRow(3)(0), Integer)
                    If nID = id Then

                        Return ' duplicate found
                    End If
                Next

                lRow = lst.Add
                lRow(0)(1) = lngIcon + &H6000000
                lRow(1)(0) = strText
                lRow(2)(1) = &H6005E6A
                lRow(3)(0) = nID

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub
    Private Sub ListItemadd(ByVal lst As Decal.Adapter.Wrappers.ListWrapper, ByVal lngIcon As Long, ByVal strText As String)
        Try
            If Not lst Is Nothing Then

                Dim lRow As Decal.Adapter.Wrappers.ListRow

                For i As Integer = 0 To lst.RowCount - 1
                    lRow = lst(i)
                    Dim idName As String = CType(lRow(1)(0), String)
                    If idName = strText Then

                        Return ' duplicate found
                    End If
                Next

                lRow = lst.Add
                lRow(0)(1) = lngIcon + &H6000000
                lRow(1)(0) = strText
                lRow(2)(1) = &H6005E6A
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub
    Private Sub ListCreatureLifeAdd(ByVal lst As Decal.Adapter.Wrappers.ListWrapper, ByVal lngIcon As Long, ByVal strText As String, ByVal bChecked As Boolean, ByVal spellID As String)
        Try
            If Not lst Is Nothing Then
                Dim newRow As Decal.Adapter.Wrappers.ListRow
                newRow = lst.Add
                newRow(0)(1) = lngIcon
                newRow(1)(0) = strText
                newRow(2)(0) = bChecked
                newRow(3)(0) = spellID
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    'create a sorted list just like the ingame spells, but attibutes at the top
    'spec first, trained, not trained

    Private Sub loadspelldata()

        If mdtSpellList Is Nothing Then
            Return
        End If

        For Each dr As DataRow In mdtSpellList.Rows
            If Not dr("SkillId") Is System.DBNull.Value Then
                If Val(dr("school")) = 4 And Val(dr("SkillId")) > 0 Then

                    Dim objSkillInfo As Decal.Adapter.Wrappers.SkillInfoWrapper
                    Dim i As Integer
                    i = CType(dr("SkillId"), Integer)
                    objSkillInfo = Core.CharacterFilter.Skills(CType(i, Wrappers.CharFilterSkillType))

                    If objSkillInfo.Training = eTrainingType.eTrainSpecialized Then
                        dr("Sort") = 7
                        dr("Flags") = 1
                    ElseIf objSkillInfo.Training = eTrainingType.eTrainTrained Then
                        dr("Sort") = 8
                        dr("Flags") = 1
                    Else
                        dr("Sort") = 9
                        dr("Flags") = 0
                    End If

                End If
            End If
        Next
        Dim DV As New DataView(mdtSpellList)
        DV.Sort = "school,sort,name"

        Dim ploop As Integer
        For ploop = 0 To DV.Count - 1

            Dim spellicon As String = String.Empty
            spellicon = String.Empty
            If Not DV.Item(ploop).Row("Icon") Is System.DBNull.Value Then
                spellicon = CType(DV.Item(ploop).Row("Icon"), String)
            End If
            If spellicon.Length = 0 Then
                spellicon = "H0600138C"
            End If

            If CType(DV.Item(ploop).Row("school"), Integer) = 4 Then
                ListCreatureLifeadd(lstCritter, CType(Val(spellicon), Long), CType(DV.Item(ploop).Row("Name"), String), False, CType(DV.Item(ploop).Row("SpellID"), String))
            ElseIf CType(DV.Item(ploop).Row("school"), Integer) = 2 Then
                ListCreatureLifeadd(lstLife, CType(Val(spellicon), Long), CType(DV.Item(ploop).Row("Name"), String), False, CType(DV.Item(ploop).Row("SpellID"), String))
            End If
        Next
    End Sub

    Private Function isweapon(ByVal objItem As Decal.Adapter.Wrappers.WorldObject) As Boolean
        If objItem.ObjectClass = ObjectClass.WandStaffOrb OrElse _
           objItem.Category = eObjectFlags.MeleeWeapon Then
            Return True
        ElseIf objItem.Category = eObjectFlags.MissileWeaponAndAmmo AndAlso objItem.Values(Decal.Adapter.Wrappers.LongValueKey.EquipType) = 2 Then
            Return True
        End If
    End Function

    'input level7
    'returns level of spell 0,1,2
    Private Function mapspellLevel(ByRef spellId As Integer, ByRef spellname As String, ByVal flag As Boolean) As Integer
        Dim spell As Decal.Filters.Spell

        Dim dr As DataRow = mdtSpellList.Rows.Find(spellId)
        Dim spelllevel As Integer = 0
        Dim result As Integer = 1

        spellname = String.Empty
        Dim lvl7 As Integer = spellId

        If Not dr Is Nothing Then
            Dim lvl5 As Integer = CInt(dr("lv5"))
            Dim lvl6 As Integer = CInt(dr("lv6"))
            Dim lvl8 As Integer = CInt(dr("lv8"))

            spell = mFileService.SpellTable.GetById(spellId)

            If Not spell Is Nothing Then
                spellname = spell.Name
                Select Case spell.School.Id
                    Case 4 ' Creature
                        spelllevel = mcharconfig.creaturemagiclevel
                    Case 3 ' Item
                        spelllevel = mcharconfig.itemmagiclevel
                    Case 2 ' Life
                        spelllevel = mcharconfig.lifemagiclevel
                End Select

                If spelllevel >= 5 And spelllevel <= 8 Then

                    If spelllevel = 5 Then
                        spellId = lvl5
                    ElseIf spelllevel = 6 Then
                        spellId = lvl6
                    ElseIf spelllevel = 7 Then
                        spellId = lvl7
                    ElseIf spelllevel = 8 Then
                        spellId = lvl8
                    End If

                    If Not Core.CharacterFilter.IsSpellKnown(spellId) Then 'fallback
                        spellId = 0
                        result = 0

                        If flag And spelllevel > 5 Then

                            Do
                                spelllevel -= 1

                                If spelllevel = 5 AndAlso Core.CharacterFilter.IsSpellKnown(lvl5) Then
                                    spellId = lvl5
                                    result = 2
                                ElseIf spelllevel = 6 AndAlso Core.CharacterFilter.IsSpellKnown(lvl6) Then
                                    spellId = lvl6
                                    result = 2
                                ElseIf spelllevel = 7 AndAlso Core.CharacterFilter.IsSpellKnown(lvl7) Then
                                    spellId = lvl7
                                    result = 2
                                End If
                            Loop Until spellId <> 0 OrElse spelllevel <= 5

                        End If
                    End If
                End If
            End If
        End If

        If spellId <> 0 Then
            spell = mFileService.SpellTable.GetById(spellId)
            If Not spell Is Nothing Then
                spellname = spell.Name
            End If
        End If

        Return result
    End Function

    'sets the controls in gui from the charconfig settings
    Private Sub loadbuffprofile()
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim s As String = String.Empty

        For i As Integer = 0 To lstCritter.RowCount - 1
            lRow = lstCritter(i)
            Dim spellid As Integer = CType(lRow(3)(0), Integer)
            If mCharconfig.selfbuffcreature.Contains(spellid) Then
                lRow(2)(0) = True
            Else
                lRow(2)(0) = False
            End If

            Dim m As Integer = mapspellLevel(spellid, s, False)
            If m = 1 Then
                lRow(1).Color = System.Drawing.Color.FromArgb(argNormal)
            ElseIf m = 2 Then 'fallback
                lRow(1).Color = System.Drawing.Color.FromArgb(argFallback)
            Else
                lRow(1).Color = System.Drawing.Color.FromArgb(argUnknown)
            End If
        Next

        For i As Integer = 0 To lstLife.RowCount - 1
            lRow = lstLife(i)
            Dim spellid As Integer = CType(lRow(3)(0), Integer)
            If mCharconfig.selfbufflife.Contains(spellid) Then
                lRow(2)(0) = True
            Else
                lRow(2)(0) = False
            End If

            Dim m As Integer = mapspellLevel(spellid, s, False)
            If m = 1 Then
                lRow(1).Color = System.Drawing.Color.FromArgb(argNormal)
            ElseIf m = 2 Then 'fallback
                lRow(1).Color = System.Drawing.Color.FromArgb(argFallback)
            Else
                lRow(1).Color = System.Drawing.Color.FromArgb(argUnknown)
            End If
        Next

        lstArmor.Clear()
        For Each nItemID As Integer In mCharconfig.selfbuffarmor
            If nItemID = Core.CharacterFilter.Id Then
                ListItemadd(lstArmor, 0, Core.CharacterFilter.Name, nItemID)
            Else

                Dim objItem As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(nItemID)
                If Not objItem Is Nothing Then
                    ListItemadd(lstArmor, objItem.Icon, objItem.Name, objItem.Id)
                End If

            End If
        Next
        'chkFastcasting.Checked = mcharconfig.Fastcasting
        '   chkUseManaPots.Checked = mCharconfig.usemanapots
        chkUseHealthToMana.Checked = mCharconfig.usehealthtomana

        chkIcon1.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon1.Id)
        chkIcon2.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon2.Id)
        chkIcon3.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon3.Id)
        chkIcon4.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon4.Id)
        chkIcon5.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon5.Id)
        chkIcon6.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon6.Id)
        chkIcon7.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon7.Id)
        chkIcon8.Checked = mCharconfig.selfbuffbanes.Contains(chkIcon8.Id)

        lstWeapons.Clear()
        If mcharconfig.selfbuffweaponbuffs IsNot Nothing Then

            'upgrade code, can be removed later
            If mcharconfig.selfbuffweaponbuffs.Count < mcharconfig.selfbuffweapons.Count Then
                For Each sItemID As Integer In mcharconfig.selfbuffweapons
                    mcharconfig.selfbuffweaponbuffs.Add(sItemID, New Integerlist)
                Next
                mcharconfig.selfbuffweapons.Clear()
            End If

            For Each kv As KeyValuePair(Of Integer, Integerlist) In mcharconfig.selfbuffweaponbuffs
                Dim objItem As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(kv.Key)
                If Not objItem Is Nothing Then
                    ListItemadd(lstWeapons, objItem.Icon, objItem.Name, objItem.Id)
                End If
            Next

        End If

     
    End Sub

    <ControlReference("nbItemMagic")> _
     Private mainTabs As Decal.Adapter.Wrappers.NotebookWrapper

    <ControlReference("lstLife")> _
     Private lstLife As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstLife", "Selected")> _
    Private Sub OnlstLifeSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)

        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim spellid As Integer
        Dim checked As Boolean
        Dim spellidMapped As Integer
        lRow = lstLife(e.Row)
        spellid = CType(lRow(3)(0), Integer) 'fixed
        checked = CType(lRow(2)(0), Boolean)
        spellidMapped = spellid
        Dim spellname As String = String.Empty
        Dim m As Integer = mapspellLevel(spellidMapped, spellname, True)

        If mCharconfig.selfbufflife.Contains(spellid) Then
            If checked = False Then
                mCharconfig.selfbufflife.Remove(spellid)
            End If
        Else
            If checked = True Then
                If m <> 0 Then
                    mcharconfig.selfbufflife.Add(spellid)
                Else
                    wtcw("spell not in spellbook " & spellname)
                End If
            End If
        End If

        If e.Column < 2 Then
            wtcw(spellname)
        End If

        mbuffselectionChanged = True
    End Sub

    <ControlReference("lstCritter")> _
 Private lstCritter As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstCritter", "Selected")> _
    Private Sub OnlstCritterSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim spellid As Integer
        Dim spellidMapped As Integer
        Dim checked As Boolean

        lRow = lstCritter(e.Row)
        spellid = CType(lRow(3)(0), Integer)
        checked = CType(lRow(2)(0), Boolean)
        spellidMapped = spellid

        Dim spellname As String = String.Empty
        Dim m As Integer = mapspellLevel(spellidMapped, spellname, True)

        If mcharconfig.selfbuffcreature.Contains(spellid) Then
            If checked = False Then
                mcharconfig.selfbuffcreature.Remove(spellid)
            End If
        Else
            If checked = True Then

                If m <> 0 Then
                    mcharconfig.selfbuffcreature.Add(spellid)
                Else
                    wtcw("spell not in spellbook " & spellname)
                End If
            End If
        End If
        If e.Column < 2 Then
            wtcw(spellname)
        End If

        mbuffselectionChanged = True
    End Sub

    <ControlEvent("chkIcon1", "Change"), ControlEvent("chkIcon2", "Change"), _
     ControlEvent("chkIcon3", "Change"), ControlEvent("chkIcon4", "Change"), _
     ControlEvent("chkIcon5", "Change"), ControlEvent("chkIcon6", "Change"), _
     ControlEvent("chkIcon7", "Change"), ControlEvent("chkIcon8", "Change")> _
     Private Sub chkBaneIcons_Change(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        Dim spellidMapped As Integer
        Dim spellid As Integer = e.Id
        spellidMapped = spellid

        Dim spellname As String = String.Empty
        Dim m As Integer = mapspellLevel(spellidMapped, spellname, True)

        If mcharconfig.selfbuffbanes.Contains(e.Id) Then
            If Not e.Checked Then
                mcharconfig.selfbuffbanes.Remove(e.Id)
            End If
        ElseIf e.Checked Then

            If m <> 0 Then
                wtcw("adding: " & spellname)
                mcharconfig.selfbuffbanes.Add(e.Id)
            Else
                wtcw("spell not in spellbook " & spellname)
            End If
            mbuffselectionChanged = True
        End If

        'If e.Checked = False Then
        '    If mcharconfig.selfbuffbanes.Contains(e.Id) Then
        '        mcharconfig.selfbuffbanes.Remove(e.Id)
        '    End If
        'ElseIf Not mcharconfig.selfbuffbanes.Contains(e.Id) Then
        '    Dim chk As Decal.Adapter.Wrappers.CheckBoxWrapper = CType(sender, Wrappers.CheckBoxWrapper)
        '    Dim oSpell As Decal.Filters.Spell = mFileService.SpellTable.GetById(e.Id)

        '    If Core.CharacterFilter.IsSpellKnown(CInt(e.Id)) Then
        '        mcharconfig.selfbuffbanes.Add(e.Id)
        '        wtcw("adding: " & oSpell.Name)
        '    Else
        '        chk.Checked = False
        '        wtcw("spell not in spellbook: " & oSpell.Name)
        '    End If



        'End If
    End Sub

    <ControlReference("cboProfile")> _
     Private cboProfile As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlEvent("cboProfile", "Change")> _
    Private Sub cboProfileChange(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)

        If cboProfile.Selected >= 0 Then
            If mcharconfig.profile <> cboProfile.Selected Then
                mcharconfig.profile = cboProfile.Selected
                loadbuffprofile()
                mbuffselectionChanged = True
            End If
        End If
    End Sub
    <ControlReference("sldMana")> _
    Private sldMana As Decal.Adapter.Wrappers.SliderWrapper

    <ControlEvent("sldMana", "Change")> _
    Private Sub OnsldManaChange(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        mCharconfig.RegenManapct = e.Index
    End Sub

    <ControlReference("sldStamina")> _
    Private sldStamina As Decal.Adapter.Wrappers.SliderWrapper

    <ControlEvent("sldStamina", "Change")> _
    Private Sub OnsldStaminaChange(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        mCharconfig.RegenStaminapct = e.Index
    End Sub


    ' <ControlReference("chkFastcasting")> _
    'Private chkFastcasting As Decal.Adapter.Wrappers.CheckBoxWrapper
    ' <ControlEvent("chkFastcasting", "Change")> _
    ' Private Sub OnchkFastcasting_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
    '     If mCharconfig IsNot Nothing Then
    '         mcharconfig.Fastcasting = e.Checked
    '     End If
    ' End Sub
    <ControlReference("chkHud")> _
      Private chkHud As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlEvent("chkHud", "Change")> _
    Private Sub OnchkHud_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mcharconfig IsNot Nothing Then
            mcharconfig.simplehud = e.Checked
        End If
    End Sub

    <ControlReference("chkFilter")> _
   Private chkFilter As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlEvent("chkFilter", "Change")> _
    Private Sub OnchkFilter_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mcharconfig IsNot Nothing Then
            mcharconfig.filtercastself = e.Checked
        End If
    End Sub

    <ControlReference("chkUseHealthToMana")> _
    Private chkUseHealthToMana As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlEvent("chkUseHealthToMana", "Change")> _
    Private Sub OnchkUseHealthToMana_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            mCharconfig.usehealthtomana = e.Checked
        End If
    End Sub

    <ControlReference("cboLevelLife")> _
  Private cboLevelLife As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlEvent("cboLevelLife", "Change")> _
    Private Sub cboLevelLife_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            Select Case e.Index
                Case 0
                    mcharconfig.lifemagiclevel = 0
                Case 1
                    mcharconfig.lifemagiclevel = 8
                Case 2
                    mcharconfig.lifemagiclevel = 7
                Case 3
                    mcharconfig.lifemagiclevel = 6
                Case 4
                    mcharconfig.lifemagiclevel = 5
            End Select
            loadbuffprofile()
            mbuffselectionChanged = True
        End If
    End Sub

    Private Function magicleveltoIndex(ByVal nlevel As Integer) As Integer
        Dim nIndex As Integer = 0
        Select Case nlevel
            Case 8
                nIndex = 1
            Case 7
                nIndex = 2
            Case 6
                nIndex = 3
            Case 5
                nIndex = 4
        End Select

        Return nIndex
    End Function
    <ControlReference("cboLevelCreature")> _
    Private cboLevelCreature As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlEvent("cboLevelCreature", "Change")> _
        Private Sub cboLevelCreature_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            Select Case e.Index
                Case 0
                    mcharconfig.creaturemagiclevel = 0
                Case 1
                    mcharconfig.creaturemagiclevel = 8
                Case 2
                    mcharconfig.creaturemagiclevel = 7
                Case 3
                    mcharconfig.creaturemagiclevel = 6
                Case 4
                    mcharconfig.creaturemagiclevel = 5
            End Select
            loadbuffprofile()
            mbuffselectionChanged = True
        End If
    End Sub

    <ControlReference("cboAugmentations")> _
    Private cboAugmentations As Decal.Adapter.Wrappers.ChoiceWrapper
    <ControlEvent("cboAugmentations", "Change")> _
     Private Sub cboAugmentations_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mcharconfig IsNot Nothing Then

            mcharconfig.ArchmageEnduranceAugmentation = e.Index

            loadbuffprofile()
            mbuffselectionChanged = True
        End If
    End Sub

    <ControlReference("cboLevelItem")> _
    Private cboLevelItem As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlEvent("cboLevelItem", "Change")> _
    Private Sub cboLevelItem_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            Select Case e.Index
                Case 0
                    mcharconfig.itemmagiclevel = 0
                Case 1
                    mcharconfig.itemmagiclevel = 8
                Case 2
                    mcharconfig.itemmagiclevel = 7
                Case 3
                    mcharconfig.itemmagiclevel = 6
                Case 4
                    mcharconfig.itemmagiclevel = 5
            End Select
            loadbuffprofile()
            mbuffselectionChanged = True
        End If
    End Sub
    <ControlReference("chkIcon1")> Private chkIcon1 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon2")> Private chkIcon2 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon3")> Private chkIcon3 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon4")> Private chkIcon4 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon5")> Private chkIcon5 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon6")> Private chkIcon6 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon7")> Private chkIcon7 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkIcon8")> Private chkIcon8 As Decal.Adapter.Wrappers.CheckBoxWrapper

   

    <ControlReference("chkBuffBD")> Private chkBuffBD As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkBuffDef")> Private chkBuffDef As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkBuffHS")> Private chkBuffHS As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkBuffSK")> Private chkBuffSK As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkBuffWandMana")> Private chkBuffWandMana As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkBuffWandSpirit")> Private chkBuffWandSpirit As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlReference("chkBuffWandDefault")> Private chkBuffWandDefault As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkBuffWandDefault", "Change")> _
    Private Sub chkBuffWandDefault_Change(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mcharconfig IsNot Nothing Then
            Dim wo As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(mselectedweaponId)
            If wo Is Nothing OrElse wo.ObjectClass <> ObjectClass.WandStaffOrb Then
                wtcw("Error: No wand selected in the listbox")
            Else
                If e.Checked Then
                    mcharconfig.buffingwandid = mselectedweaponId
                    wtcw("Sets the default wand for buffing")
                Else
                    mcharconfig.buffingwandid = 0
                End If
            End If


        End If
    End Sub

    <ControlEvent("chkBuffBD", "Change"), ControlEvent("chkBuffDef", "Change"), _
    ControlEvent("chkBuffHS", "Change"), ControlEvent("chkBuffSK", "Change"), _
    ControlEvent("chkBuffWandSpirit", "Change"), ControlEvent("chkBuffWandMana", "Change")> _
   Private Sub chkWeaponIcons_Change(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mcharconfig IsNot Nothing Then

            If mcharconfig.selfbuffweaponbuffs.ContainsKey(mselectedweaponId) Then

                If mcharconfig.selfbuffweaponbuffs(mselectedweaponId) Is Nothing Then
                    mcharconfig.selfbuffweaponbuffs(mselectedweaponId) = New Integerlist
                End If

                If e.Checked = False Then
                    If mcharconfig.selfbuffweaponbuffs(mselectedweaponId).Contains(e.Id) Then
                        mcharconfig.selfbuffweaponbuffs(mselectedweaponId).Remove(e.Id)
                    End If
                ElseIf Not mcharconfig.selfbuffweaponbuffs(mselectedweaponId).Contains(e.Id) Then
                    mcharconfig.selfbuffweaponbuffs(mselectedweaponId).Add(e.Id)

                    Dim oSpell As Decal.Filters.Spell = mFileService.SpellTable.GetById(e.Id)
                    If Not oSpell Is Nothing Then
                        wtcw("adding: " & oSpell.Name)
                    End If
                End If
            Else
                wtcw("Error: No weapon selected in the listbox")
            End If

            mbuffselectionChanged = True
        End If

    End Sub

    Private Sub setweaponbuffIcons()
        If mcharconfig.selfbuffweaponbuffs.ContainsKey(mselectedweaponId) Then
            Dim buffs As Integerlist = mcharconfig.selfbuffweaponbuffs(mselectedweaponId)
            If buffs Is Nothing Then
                buffs = New Integerlist
                mcharconfig.selfbuffweaponbuffs(mselectedweaponId) = buffs
            End If
            chkBuffBD.Checked = buffs.Contains(chkBuffBD.Id)
            chkBuffDef.Checked = buffs.Contains(chkBuffDef.Id)
            chkBuffHS.Checked = buffs.Contains(chkBuffHS.Id)
            chkBuffSK.Checked = buffs.Contains(chkBuffSK.Id)
            chkBuffWandMana.Checked = buffs.Contains(chkBuffWandMana.Id)
            chkBuffWandSpirit.Checked = buffs.Contains(chkBuffWandSpirit.Id)
        End If

    End Sub
    
    <ControlReference("lstWeapons")> _
  Private lstWeapons As Decal.Adapter.Wrappers.ListWrapper
    Const argnearlight As Integer = &HFFFFFFFF
    Const argmeddark As Integer = &HFF5FA3EC
    Private mselectedweaponId As Integer = -1

    Private Sub weaponlistensurevisible(ByVal id As Integer)
        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        Dim jumpto As Integer = 0

        For i As Integer = 0 To lstWeapons.RowCount - 1
            lRow = lstWeapons(i)
            lRow(1).Color = colorw
            If CType(lRow(3)(0), Integer) = id Then
                mselectedweaponId = id
                lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark)
                jumpto = i
            End If
        Next
        lstWeapons.JumpToPosition(jumpto)
        setweaponbuffIcons()
    End Sub


    <ControlEvent("lstWeapons", "Selected")> _
    Private Sub OnlstWeaponsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)

        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        For i As Integer = 0 To lstWeapons.RowCount - 1
            lRow = lstWeapons(i)
            lRow(1).Color = colorw
        Next


        lRow = lstWeapons(e.Row)
        lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark)

        mselectedweaponId = CType(lRow(3)(0), Integer)

        chkBuffWandDefault.Checked = CBool(mselectedweaponId = mcharconfig.buffingwandid)

        If e.Column = 2 Then
            lstWeapons.Delete(e.Row)
            lstWeapons.JumpToPosition(0)
            If mcharconfig.selfbuffweaponbuffs.ContainsKey(mselectedweaponId) Then
                mcharconfig.selfbuffweaponbuffs.Remove(mselectedweaponId)
            End If
            mbuffselectionChanged = True

        Else
            Host.Actions.SelectItem(mselectedweaponId)
        End If

        setweaponbuffIcons()
    End Sub

    <ControlReference("txtminmana")> _
   Private txtminmana As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlEvent("txtminmana", "Change")> _
       Private Sub OntxtminmanaChange(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxChangeEventArgs)
        If mCharconfig IsNot Nothing AndAlso IsNumeric(txtminmana.Text) Then
            mCharconfig.minmanaForCasting = CInt(txtminmana.Text)
        End If
    End Sub

    <ControlReference("lstArmor")> _
    Private lstArmor As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstArmor", "Selected")> _
    Private Sub OnlstArmorSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim Itemid As Integer
        lRow = lstArmor(e.Row)
        Itemid = CType(lRow(3)(0), Integer)

        If e.Column = 2 Then
            lstArmor.Delete(e.Row)
            lstArmor.JumpToPosition(0)
            If mCharconfig.selfbuffarmor.Contains(Itemid) Then
                mCharconfig.selfbuffarmor.Remove(Itemid)
            End If
            mbuffselectionChanged = True

        Else
            Host.Actions.SelectItem(Itemid)
        End If
    End Sub
    <ControlReference("lstConsumables")> _
    Private lstConsumables As Decal.Adapter.Wrappers.ListWrapper
    Private Sub Consumablesensurevisible(ByVal key As String)
        'TODO jump to item already in list

        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        Dim jumpto As Integer = 0

        For i As Integer = 0 To lstConsumables.RowCount - 1
            lRow = lstConsumables(i)
            lRow(1).Color = colorw
            If CType(lRow(1)(0), String) = key Then
                lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark)
                jumpto = i
            End If
        Next
        lstConsumables.JumpToPosition(jumpto)
    End Sub

    <ControlEvent("lstConsumables", "Selected")> _
  Private Sub OnlstConsumablesSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)

        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        For i As Integer = 0 To lstConsumables.RowCount - 1
            lRow = lstConsumables(i)
            lRow(1).Color = colorw
        Next

        lRow = lstConsumables(e.Row)
        lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark)

        Dim idName As String = CType(lRow(1)(0), String)

        If e.Column = 2 Then
            lstConsumables.Delete(e.Row)
            lstConsumables.JumpToPosition(0)
            If mcharconfig.consumables.ContainsKey(idName) Then
                mcharconfig.consumables.Remove(idName)
            End If
        End If
    End Sub

    'btnAddConsumables
    <ControlEvent("btnAddConsumables", "Click")> _
    Private Sub btnAddConsumables_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            Dim id As Integer = Host.Actions.CurrentSelection
            Dim objItem As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(id)

            If objItem IsNot Nothing Then
                If Not mcharconfig.consumables.ContainsKey(objItem.Name) Then
                    If objItem.HasIdData Then
                        Dim nd As New consumable With {.icon = objItem.Icon, .vitalId = objItem.Values(Wrappers.LongValueKey.AffectsVitalId)}
                        If objItem.ObjectClass = ObjectClass.ManaStone Then
                            If objItem.Name.ToLower.IndexOf("charge") > 0 Then
                                nd.Constype = eConsumableType.ManaStone
                            End If
                        ElseIf objItem.ObjectClass = ObjectClass.HealingKit Then
                            nd.Constype = eConsumableType.HealingKit
                        ElseIf objItem.ObjectClass = ObjectClass.Gem Then
                            nd.Constype = eConsumableType.Gem
                            nd.Amt = objItem.Values(Wrappers.LongValueKey.AssociatedSpell)
                        Else
                            nd.Amt = objItem.Values(Wrappers.LongValueKey.AffectsVitalAmt)
                        End If

                        mcharconfig.consumables.Add(objItem.Name, nd)
                        ListItemadd(lstConsumables, objItem.Icon, objItem.Name)
                    Else
                        wtcw("Identify the Item first")
                    End If
                Else
                    Consumablesensurevisible(objItem.Name)
                End If
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnSelectedWeapon", "Click")> _
    Private Sub btnSelectedWeapon_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try

            Dim id As Integer = Host.Actions.CurrentSelection
            Dim objItem As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(id)
            If Not objItem Is Nothing Then
                If Not mcharconfig.selfbuffweaponbuffs.ContainsKey(objItem.Id) Then
                    If isweapon(objItem) Then

                        mcharconfig.selfbuffweaponbuffs.Add(objItem.Id, New Integerlist)
                        ListItemadd(lstWeapons, objItem.Icon, objItem.Name, objItem.Id)

                    Else
                        wtcw("error add " & objItem.Name)
                    End If

                    mbuffselectionChanged = True
                End If
                weaponlistensurevisible(objItem.Id)
            Else
                lstWeapons.JumpToPosition(lstWeapons.RowCount)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnSelectedArmor", "Click")> _
    Private Sub btnSelectedArmor_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try

            Dim id As Integer = Host.Actions.CurrentSelection
            Dim objItem As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(id)

            If Not mcharconfig.selfbuffarmor.Contains(id) Then

                If id = Core.CharacterFilter.Id Then
                    mcharconfig.selfbuffarmor.Add(id)
                    ListItemadd(lstArmor, 0, Core.CharacterFilter.Name, id)

                    mbuffselectionChanged = True
                ElseIf Not objItem Is Nothing Then

                    If objItem.Category = eObjectFlags.Armor Or _
                           objItem.Category = eObjectFlags.Clothing Then

                        mcharconfig.selfbuffarmor.Add(objItem.Id)
                        ListItemadd(lstArmor, objItem.Icon, objItem.Name, objItem.Id)

                        mbuffselectionChanged = True

                    End If
                End If
            Else
                lstArmor.JumpToPosition(lstWeapons.RowCount)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlReference("btnPause")> _
    Private btnPause As Decal.Adapter.Wrappers.PushButtonWrapper

    <ControlEvent("btnPause", "Click")> _
    Private Sub btnPause_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        mPause = Not mPause
        If mPause Then
            btnPause.Text = "Cont."
        Else
            btnPause.Text = "Pause"
        End If
    End Sub

    <ControlReference("btnBuffItem")> _
    Private btnBuffItem As Decal.Adapter.Wrappers.PushButtonWrapper

    <ControlEvent("btnBuffItem", "Click")> _
    Private Sub btnBuffItem_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try

            If validateprebuff(True) Then
                Dim bBuffPending As Boolean
                bBuffPending = CBool((Windows.Forms.Control.ModifierKeys And Windows.Forms.Keys.Control) <> 0)

                If btnBuffItem.Text <> "Cancel" Then
                    btnBuffItem.Text = "Cancel"
                    StartBuffs(eMagicSchool.Item, bBuffPending)
                Else
                    btnBuffItem.Text = "Item"
                    cancelbuffs(eMagicSchool.Item)
                End If
            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlReference("btnBuffCreature")> _
   Private btnBuffCreature As Decal.Adapter.Wrappers.PushButtonWrapper

    <ControlEvent("btnBuffCreature", "Click")> _
    Private Sub btnBuffCreature_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try

            If validateprebuff(False) Then
                Dim bBuffPending As Boolean
                bBuffPending = CBool((Windows.Forms.Control.ModifierKeys And Windows.Forms.Keys.Control) <> 0)

                If btnBuffCreature.Text <> "Cancel" Then
                    btnBuffCreature.Text = "Cancel"
                    StartBuffs(eMagicSchool.Creature Or eMagicSchool.Life, bBuffPending)
                Else
                    btnBuffCreature.Text = "Life/Critter"
                    cancelbuffs(eMagicSchool.Creature Or eMagicSchool.Life)
                End If

            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

End Class
