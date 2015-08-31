Option Strict On
Option Infer On
Imports Decal.Adapter

Partial Public Class Plugin
    Private mSelectedRule As Rule

    Private Enum eIdflags
        Axe = &H1
        Bow = &H2
        Mace = &H4
        Crossbow = &H8
        Thrown = &H10
        Spear = &H20
        Dagger = &H40
        Staff = &H80
        Sword = &H100
        Ua = &H200
        ' Hammer = &H400
        Wand = &H800
        Jewelry = &H10000
        Armor = &H20000
        Clothing = &H40000
        MeleeWeapon = &H80000
        MissileWeapon = &H100000
        Shield = &H200000
        Trinket = &H800000
        Scroll = &H400000
    End Enum


    <Flags()> _
    Public Enum eWeaponDamageTypes
        Slashing = 1
        Piercing = 2
        Bludgeoning = 4
        Cold = 8
        Fire = 16
        Acid = 32
        Electric = 64
    End Enum

    <Flags()> _
  Public Enum eCoverageMask
        UpperLegs = &H100
        LowerLegs = &H200
        Chest = &H400
        Abdomen = &H800
        UpperArms = &H1000
        LowerArms = &H2000
        Head = &H4000
        Hands = &H8000
        Feet = &H10000
    End Enum

    <Flags()> _
        Public Enum eArmorTypes
        Amuli = &H1
        Celdon = &H2
        Chainmail = &H4
        Chiran = &H8
        Covenant = &H10
        Lorica = &H20
        Nariyid = &H40
        Koujia = &H80
        Platemail = &H100
        Scalemail = &H200
        Yoroi = &H400
        Other = &H800
    End Enum

    'used to fill and sort the listboxes in the view
    Private Class checkedItem
        Public checked As Boolean '
        Public key As String
        Public name As String
    End Class

    Private Sub populateRulesView()
        If mActiveRulesProfile IsNot Nothing Then
            cboRules.Clear()
            For Each r As rule In mActiveRulesProfile
                If Not r Is Nothing Then
                    cboRules.Add(r.name, r)
                Else
                    r = New rule
                End If
            Next
        End If
    End Sub

    Private Function nzstring(ByVal s As String) As String
        If s Is Nothing Then
            Return String.Empty
        Else
            Return s
        End If
    End Function

    Private Function nzinteger(ByVal i As Integer) As String
        If i = -1 Then
            Return String.Empty
        Else
            Return CStr(i)
        End If
    End Function

    Private Function damagestring(ByVal index As Integer) As String
        If mSelectedRule.damage(index).mindamage = -1 Then
            If mSelectedRule.damage(index).maxdamage = -1 Then
                Return String.Empty
            Else
                Return CStr(mSelectedRule.damage(index).maxdamage)
            End If
        ElseIf mSelectedRule.damage(index).maxdamage = -1 Then
            Return CStr(mSelectedRule.damage(index).mindamage)
        Else
            Return CStr(mSelectedRule.damage(index).mindamage) & "-" & CStr(mSelectedRule.damage(index).maxdamage)
        End If

    End Function


    Private Sub populatelist(ByVal lst As Decal.Adapter.Wrappers.ListWrapper, ByVal enumtype As Type, ByVal flags As Integer)
        Dim p As checkedItem
        Dim xlist As New List(Of checkedItem)

        Dim Idarray() As Integer = CType([Enum].GetValues(enumtype), Integer())
        For Each id As Integer In Idarray
            Dim tmp As String = [Enum].GetName(enumtype, id)
            p = New checkedItem
            p.checked = (flags And id) = id
            p.name = tmp
            p.key = CStr(id)
            xlist.Add(p)
        Next

        Dim sortedlist = From x As checkedItem In xlist _
                  Order By x.checked Descending, x.name

        Dim lRow As Decal.Adapter.Wrappers.ListRow
        lst.Clear()

        For Each obj As checkedItem In sortedlist
            lRow = lst.Add
            lRow(0)(0) = obj.checked
            lRow(1)(0) = obj.name
            lRow(2)(0) = obj.key
        Next
    End Sub


    Private Sub addApplietos()
        Dim p As checkedItem
        Dim xlist As New List(Of checkedItem)

        Dim Idarray() As Integer = CType([Enum].GetValues(GetType(eIdflags)), Integer())
        For Each id As Integer In Idarray
            Dim tmp As String = [Enum].GetName(GetType(eIdflags), id)

            Select Case id
                Case eIdflags.Armor, eIdflags.Clothing, eIdflags.Jewelry, eIdflags.MeleeWeapon, eIdflags.MissileWeapon, eIdflags.Wand, eIdflags.Shield, eIdflags.Scroll, eIdflags.Trinket
                    p = New checkedItem
                    p.checked = (mSelectedRule.appliesToFlag And id) = id
                    p.name = tmp
                    p.key = CStr(id)
                    xlist.Add(p)
            End Select

        Next

        Dim sortedlist = From x As checkedItem In xlist _
                 Order By x.checked Descending, x.name

        lstruleapplies.Clear()

        Dim lRow As Decal.Adapter.Wrappers.ListRow
        For Each obj As checkedItem In sortedlist
            lRow = lstruleapplies.Add
            lRow(0)(0) = obj.checked
            lRow(1)(0) = obj.name
            lRow(2)(0) = obj.key
        Next
    End Sub

    Private Sub addRuleSets()
        Dim xlist As New List(Of checkedItem)

        Dim p As checkedItem
        For Each obj As GameData.NameId In GameData.SetStrings
            If obj.Flags = 0 Then
                p = New checkedItem
                p.key = CStr(obj.Id)
                p.name = obj.name
                p.checked = False

                If mSelectedRule.Specificset IsNot Nothing Then
                    p.checked = mSelectedRule.Specificset.Contains(CInt(p.key))
                End If

                xlist.Add(p)
            End If
        Next

        Dim sortedlist = From x As checkedItem In xlist _
                 Order By x.checked Descending, x.name

        lstRuleSetIds.Clear()
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        For Each obj As checkedItem In sortedlist
            lRow = lstRuleSetIds.Add

            lRow(0)(1) = 0
            lRow(1)(0) = obj.name
            lRow(2)(0) = obj.checked
            lRow(3)(0) = CStr(obj.key)
        Next

    End Sub

    Private Sub addRuleSpells()
        Dim p As checkedItem
        Dim xlist As New List(Of checkedItem)

        For Each obj As GameData.NameId In GameData.SpellData

            p = New checkedItem
            p.key = CStr(obj.Id)
            p.name = obj.name
            p.checked = False

            If Not mSelectedRule.spells Is Nothing Then
                p.checked = mSelectedRule.spells.Contains(CInt(p.key))
            End If

            If p.checked Then
                xlist.Add(p)
            Else
                If chkFilterMajor.Checked = False And p.name.StartsWith("Major") Then
                    Continue For
                End If
                If chkFilterEpic.Checked = False And p.name.StartsWith("Epic") Then
                    Continue For
                End If
                If chkFilterlvl8.Checked = False And p.name.StartsWith("Incantation") Then
                    Continue For
                End If
                If chkFilterMinor.Checked = False And p.name.StartsWith("Minor") Then
                    Continue For
                End If
                If chkFilterlvl6.Checked = False And p.name.EndsWith("6") Then
                    Continue For
                End If
                If chkFilterlvl7.Checked = False And p.name.EndsWith("7") Then
                    Continue For
                End If
                xlist.Add(p)
            End If

        Next

        Dim sortedlist = From x As checkedItem In xlist _
                 Order By x.checked Descending, x.name

        lstRuleSpells.Clear()
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        For Each obj As checkedItem In sortedlist
            lRow = lstRuleSpells.Add

            lRow(0)(1) = 0
            lRow(1)(0) = obj.name
            lRow(2)(0) = obj.checked
            lRow(3)(0) = CStr(obj.key)
        Next
    End Sub

  

    Private Sub EditRule()
        If mSelectedRule Is Nothing Then Return
        Try

            txtRuleName.Text = mSelectedRule.name
            txtRuleInfo.Text = nzstring(mSelectedRule.info)

            txtRuleMaxBurden.Text = nzinteger(mSelectedRule.maxburden)
            txtRuleMaxCraft.Text = nzinteger(mSelectedRule.maxcraft)
            txtRuleMaxValue.Text = nzinteger(mSelectedRule.maxvalue)
            chkRuleEnabled.Checked = mSelectedRule.enabled
            chkTradebotOnly.Checked = mSelectedRule.tradebotonly

            addApplietos()
            chkTradebot.Checked = mSelectedRule.tradebot

            txtRuleKeywords.Text = nzstring(mSelectedRule.keywords)
            txtRuleExlWords.Text = nzstring(mSelectedRule.keywordsnot)
            mprevwav = -1
            setcbvalue(cboRuleAlert, mSelectedRule.wavfile)

            If cboWeaponAppliesTo.Selected = mSelectedRule.weapontype Then
                SetmaxedvaluesInfo()
            Else
                cboWeaponAppliesTo.Selected = mSelectedRule.weapontype
            End If

            txtmax1.Text = nzinteger(mSelectedRule.minmcmodattackbonus)
            txtmax2.Text = nzinteger(mSelectedRule.minmeleebonus)
            txtmax3.Text = nzinteger(CInt(mSelectedRule.minmagicdbonus))

            If mSelectedRule.damage IsNot Nothing AndAlso UBound(mSelectedRule.damage) = 3 Then
                txtmax5.Text = nzinteger(mSelectedRule.damage(0).maxwield)
                txtmax6.Text = nzinteger(mSelectedRule.damage(1).maxwield)
                txtmax7.Text = nzinteger(mSelectedRule.damage(2).maxwield)
                txtmax8.Text = nzinteger(mSelectedRule.damage(3).maxwield)
                chkRuleMax6.Checked = mSelectedRule.damage(1).enabled
                chkRuleMax7.Checked = mSelectedRule.damage(2).enabled
                chkRuleMax8.Checked = mSelectedRule.damage(3).enabled

                txtmax5B.Text = damagestring(0)
                txtmax6B.Text = damagestring(1)
                txtmax7B.Text = damagestring(2)
                txtmax8B.Text = damagestring(3)
            Else
                ReDim mSelectedRule.damage(3)
                txtmax5.Text = String.Empty
                txtmax6.Text = String.Empty
                txtmax7.Text = String.Empty
                txtmax8.Text = String.Empty
                chkRuleMax6.Checked = False
                chkRuleMax7.Checked = False
                chkRuleMax8.Checked = False

                txtmax5B.Text = String.Empty
                txtmax6B.Text = String.Empty
                txtmax7B.Text = String.Empty
                txtmax8B.Text = String.Empty
            End If

            populatelist(lstDamageTypes, GetType(eWeaponDamageTypes), mSelectedRule.damagetypeFlag)

            txtArmorLevel.Text = nzinteger(mSelectedRule.minarmorlevel)

            populatelist(lstArmorCoverages, GetType(eCoverageMask), mSelectedRule.armorcoverageFlag)

            populatelist(lstArmorTypes, GetType(eArmorTypes), mSelectedRule.armortypeFlag)

            addRuleSpells()
            addRuleSets()
            chkRuleAnySet.Checked = mSelectedRule.anyset
            chkIvoryAble.Checked = mSelectedRule.ivoryable

            txtSpellMatches.Text = nzinteger(mSelectedRule.spellmatches)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    <ControlReference("cboRuleAlert")> _
    Private cboRuleAlert As Decal.Adapter.Wrappers.ChoiceWrapper
    Private mprevwav As Integer = -1
    <ControlEvent("cboRuleAlert", "Change")> _
    Private Sub cboRuleAlert_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mSelectedRule IsNot Nothing And cboRuleAlert.Selected >= 0 Then
            mSelectedRule.wavfile = cboRuleAlert.Text(cboRuleAlert.Selected)
            If mprevwav <> -1 Then
                mprevwav = cboRuleAlert.Selected
                Dim xa As Alert
                If mPluginConfig.Alerts.ContainsKey(mSelectedRule.wavfile) Then
                    xa = mPluginConfig.Alerts.Item(mSelectedRule.wavfile)
                    If xa IsNot Nothing AndAlso Not String.IsNullOrEmpty(xa.wavfilename) Then
                        PlaySoundFile(xa.wavfilename, xa.volume)
                    End If
                End If

            Else
                mprevwav = cboRuleAlert.Selected
            End If

        End If
    End Sub

    <ControlReference("cboRules")> _
     Private cboRules As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlEvent("cboRules", "Change")> _
    Private Sub cboRules_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            'TODO mLootingIDFlag = calcIDFlags(False)

            Dim p As rule = CType(cboRules.Data.Item(cboRules.Selected), rule)
            If Not p Is Nothing Then
                mSelectedRule = p
                EditRule()
            End If
        End If
    End Sub

    Private mtestItemId As Integer

    <ControlEvent("btnRuleNew", "Click")> _
    Private Sub btnRuleNew_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            mSelectedRule = New rule
            If mActiveRulesProfile Is Nothing Then
                mActiveRulesProfile = New RulesCollection
            End If
            cboRules.Add("New Rule", mSelectedRule)
            If mActiveRulesProfile.Count > 0 Then
                mActiveRulesProfile.Insert(1, mSelectedRule)
            Else
                mActiveRulesProfile.Add(mSelectedRule)
            End If

            mSelectedRule.name = "New Rule"
            EditRule()
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnRuleDel", "Click")> _
    Private Sub btnRuleDel_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If cboRules.Selected >= 0 Then
                Dim p As rule = CType(cboRules.Data.Item(cboRules.Selected), rule)
                mActiveRulesProfile.Remove(p)
                populateRulesView()
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnRuleUp", "Click")> _
    Private Sub btnRuleUp_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            Dim prev As Integer = cboRules.Selected

            If prev >= 0 And prev < cboRules.Count - 1 Then

                Dim p As rule = CType(cboRules.Data.Item(prev), rule)
                mActiveRulesProfile.Remove(p)
                mActiveRulesProfile.Insert(prev + 1, p)
                populateRulesView()
                cboRules.Selected = prev + 1
            Else
                PlaySoundFile("lostconnection.wav", mPluginConfig.wavVolume)
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("btnRuleDwn", "Click")> _
    Private Sub btnRuleDwn_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            Dim prev As Integer = cboRules.Selected
            If prev > 0 Then
                Dim p As rule = CType(cboRules.Data.Item(prev), rule)
                mActiveRulesProfile.Remove(p)
                mActiveRulesProfile.Insert(prev - 1, p)
                populateRulesView()
                cboRules.Selected = prev - 1
            Else
                PlaySoundFile("lostconnection.wav", mPluginConfig.wavVolume)
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlReference("txtRuleName")> _
    Private txtRuleName As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleName", "End")> _
    Private Sub txtRuleName_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.name = txtRuleName.Text
            populateRulesView()
        End If
    End Sub

    <ControlReference("txtRuleInfo")> _
    Private txtRuleInfo As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleInfo", "End")> _
    Private Sub txtRuleInfo_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.info = txtRuleInfo.Text
        End If
    End Sub

    <ControlReference("txtRuleMaxBurden")> _
    Private txtRuleMaxBurden As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleMaxBurden", "End")> _
    Private Sub txtRuleMaxBurden_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim result As Integer
            If Integer.TryParse(txtRuleMaxBurden.Text, result) Then
                mSelectedRule.maxburden = result
            Else
                txtRuleMaxBurden.Text = String.Empty
                mSelectedRule.maxburden = -1
            End If
        End If
    End Sub

    <ControlReference("txtRuleMaxCraft")> _
  Private txtRuleMaxCraft As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleMaxCraft", "End")> _
    Private Sub txtRuleMaxCraft_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim result As Integer
            If Integer.TryParse(txtRuleMaxCraft.Text, result) Then
                mSelectedRule.maxcraft = result
            Else
                txtRuleMaxCraft.Text = String.Empty
                mSelectedRule.maxcraft = -1
            End If
        End If
    End Sub

    <ControlReference("txtRuleMaxValue")> _
      Private txtRuleMaxValue As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleMaxValue", "End")> _
    Private Sub txtRuleMaxValue_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim result As Integer
            If Integer.TryParse(txtRuleMaxValue.Text, result) Then
                mSelectedRule.maxvalue = result
            Else
                txtRuleMaxValue.Text = String.Empty
                mSelectedRule.maxvalue = -1
            End If
        End If
    End Sub

    <ControlReference("txtRuleExlWords")> _
    Private txtRuleExlWords As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleExlWords", "End")> _
    Private Sub txtRuleExlWords_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.keywordsnot = txtRuleExlWords.Text

        End If
    End Sub

    <ControlReference("txtRuleKeywords")> _
    Private txtRuleKeywords As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtRuleKeywords", "End")> _
    Private Sub txtRuleKeywords_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.keywords = txtRuleKeywords.Text

        End If
    End Sub
   
    <ControlReference("chkIvoryAble")> _
    Private chkIvoryAble As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlEvent("chkIvoryAble", "Change")> _
      Private Sub chkIvoryAble_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.ivoryable = chkIvoryAble.Checked
            If chkIvoryAble.Checked Then
                '  wtcw("When this option is set, the rule does not look to weapon damage")
            End If
        End If
    End Sub

    <ControlReference("chkFilterEpic")> _
    Private chkFilterEpic As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkFilterlvl8")> _
    Private chkFilterlvl8 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkFilterMajor")> _
     Private chkFilterMajor As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkFilterMinor")> _
     Private chkFilterMinor As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkFilterlvl7")> _
     Private chkFilterlvl7 As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlReference("chkFilterlvl6")> _
     Private chkFilterlvl6 As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkFilterMinor", "Change"), ControlEvent("chkFilterEpic", "Change"), ControlEvent("chkFilterlvl8", "Change"), ControlEvent("chkFilterMajor", "Change"), ControlEvent("chkFilterlvl6", "Change"), ControlEvent("chkFilterlvl7", "Change")> _
    Private Sub chkFilterMinor_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            addRuleSpells()
        End If
    End Sub
  
    <ControlReference("chkRuleEnabled")> _
      Private chkRuleEnabled As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkRuleEnabled", "Change")> _
    Private Sub chkRuleEnabled_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.enabled = e.Checked
        End If
    End Sub
    <ControlReference("chkTradebot")> _
     Private chkTradebot As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkTradebot", "Change")> _
    Private Sub chkTradebot_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.tradebot = e.Checked
        End If
    End Sub

    <ControlReference("chkTradebotOnly")> _
     Private chkTradebotOnly As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkTradebotOnly", "Change")> _
    Private Sub chkTradebotOnly_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.tradebotonly = e.Checked
        End If
    End Sub
    <ControlReference("cboWeaponAppliesTo")> _
     Private cboWeaponAppliesTo As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlEvent("cboWeaponAppliesTo", "Change")> _
       Private Sub cboWeaponAppliesTo_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.weapontype = CType(e.Index, eRuleWeaponTypes)
            mSelectedRule.weaponsubtype = CType(cboWeaponAppliesTo.Data(cboWeaponAppliesTo.Selected), Integer)
            Select Case mSelectedRule.weapontype
                Case eRuleWeaponTypes.notapplicable
                    lblRulemcmodattack.Text = "mc/mod%/attack"
                Case eRuleWeaponTypes.bow, eRuleWeaponTypes.atlan, eRuleWeaponTypes.crossbow 'missile
                    lblRulemcmodattack.Text = "Damage Mod:"
                Case eRuleWeaponTypes.mage 'wand
                    lblRulemcmodattack.Text = "Mana Conversion:"
                Case Else 'melee
                    lblRulemcmodattack.Text = "Attack Mod:"
            End Select
            If mSelectedRule.weapontype <> eRuleWeaponTypes.notapplicable Then
                If mSelectedRule.minarmorlevel >= 0 Then
                    mSelectedRule.minarmorlevel = -1
                    txtArmorLevel.Text = ""
                End If
            End If
            SetmaxedvaluesInfo()
        End If
    End Sub

    'TODO Load max values info from gamedata
    Private Sub SetmaxedvaluesInfo()
        lblmaxed5.Text = "420"
        lblmaxed6.Text = "400"
        lblmaxed7.Text = "370"
        lblmaxed8.Text = "350"


        lblmaxed4.Text = "15"
        lblmaxed4B.Text = "15"
        lblmaxed4C.Text = ""
        Select Case mSelectedRule.weapontype
            Case eRuleWeaponTypes.notapplicable
                lblmaxed5.Text = String.Empty
                lblmaxed6.Text = String.Empty
                lblmaxed7.Text = String.Empty
                lblmaxed8.Text = String.Empty
                lblmaxed5B.Text = String.Empty
                lblmaxed6B.Text = String.Empty
                lblmaxed7B.Text = String.Empty
                lblmaxed8B.Text = String.Empty
                lblmaxed4.Text = String.Empty
                lblmaxed4B.Text = String.Empty
                lblmaxed4C.Text = String.Empty
            Case eRuleWeaponTypes.mage
                lblmaxed4.Text = "10"
                lblmaxed5.Text = "375"
                lblmaxed6.Text = "355"
                lblmaxed7.Text = "330"
                lblmaxed8.Text = "310"

                lblmaxed5B.Text = "12"
                lblmaxed6B.Text = "10"
                lblmaxed7B.Text = "8"
                lblmaxed8B.Text = "5"

            Case eRuleWeaponTypes.atlan, eRuleWeaponTypes.bow, eRuleWeaponTypes.crossbow
                lblmaxed4.Text = "130"
                lblmaxed5.Text = "375"
                lblmaxed6.Text = "360"
                lblmaxed7.Text = "335"
                lblmaxed8.Text = "315"
                lblmaxed5B.Text = "16"
                lblmaxed6B.Text = "12"
                lblmaxed7B.Text = "8"
                lblmaxed8B.Text = "4"

            Case eRuleWeaponTypes.axe
                If mSelectedRule.keywords IsNot Nothing AndAlso mSelectedRule.keywords = "Hammer" Then
                    lblmaxed5B.Text = "??-45"
                    lblmaxed6B.Text = "25-43"
                    lblmaxed7B.Text = "23-39"
                    lblmaxed8B.Text = "21-35"
                Else
                    lblmaxed5B.Text = "??-50"
                    lblmaxed6B.Text = "27-46"
                    lblmaxed7B.Text = "25-42"
                    lblmaxed8B.Text = "22-38"
                End If
            Case eRuleWeaponTypes.twohanded
                lblmaxed5B.Text = "10-10"
                lblmaxed6B.Text = "10-15"
                lblmaxed7B.Text = "10-20"
                lblmaxed8B.Text = "10-35"
            Case eRuleWeaponTypes.sword
                lblmaxed5B.Text = "36-60"
                lblmaxed6B.Text = "33-55"
                lblmaxed7B.Text = "30-50"
                lblmaxed8B.Text = "27-45"

            Case eRuleWeaponTypes.mace
                lblmaxed5B.Text = "30-44"
                lblmaxed6B.Text = "30-42"
                lblmaxed7B.Text = "28-38"
                lblmaxed8B.Text = "27-36"
            Case eRuleWeaponTypes.ua
                lblmaxed4B.Text = "20"
                lblmaxed5B.Text = "14-28"
                lblmaxed6B.Text = "13-26"
                lblmaxed7B.Text = "11-22"
                lblmaxed8B.Text = "10-20"

            Case eRuleWeaponTypes.dagger
                lblmaxed5B.Text = "??-28"
                lblmaxed6B.Text = "18-26"
                lblmaxed7B.Text = "16-24"
                lblmaxed8B.Text = "14-20"

            Case eRuleWeaponTypes.spear
                lblmaxed5B.Text = "24-44"
                lblmaxed6B.Text = "22-40"
                lblmaxed7B.Text = "19-36"
                lblmaxed8B.Text = "17-32"

            Case eRuleWeaponTypes.staff
                lblmaxed5B.Text = "19-28"
                lblmaxed6B.Text = "19-26"
                lblmaxed7B.Text = "18-24"
                lblmaxed8B.Text = "15-20"
           

        End Select

    End Sub


    <ControlReference("lblmaxed4")> Private lblmaxed4 As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed4B")> Private lblmaxed4B As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed4C")> Private lblmaxed4C As Decal.Adapter.Wrappers.StaticWrapper

    <ControlReference("lblmaxed5")> Private lblmaxed5 As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed6")> Private lblmaxed6 As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed7")> Private lblmaxed7 As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed8")> Private lblmaxed8 As Decal.Adapter.Wrappers.StaticWrapper

    <ControlReference("lblmaxed5B")> Private lblmaxed5B As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed6B")> Private lblmaxed6B As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed7B")> Private lblmaxed7B As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblmaxed8B")> Private lblmaxed8B As Decal.Adapter.Wrappers.StaticWrapper
    <ControlReference("lblRulemcmodattack")> Private lblRulemcmodattack As Decal.Adapter.Wrappers.StaticWrapper

    <ControlReference("txtmax1")> _
   Private txtmax1 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax1", "End")> _
    Private Sub txtmax1_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim d As Integer
            If Integer.TryParse(txtmax1.Text, d) Then
                mSelectedRule.minmcmodattackbonus = d
            Else
                mSelectedRule.minmcmodattackbonus = -1
            End If
        End If
    End Sub

    <ControlReference("txtmax2")> _
      Private txtmax2 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax2", "End")> _
    Private Sub txtmax2_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim d As Integer
            If Integer.TryParse(txtmax2.Text, d) Then
                mSelectedRule.minmeleebonus = d
            Else
                mSelectedRule.minmeleebonus = -1
            End If
        End If
    End Sub

    <ControlReference("txtmax3")> _
      Private txtmax3 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax3", "End")> _
    Private Sub txtmax3_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim d As Integer
            If Integer.TryParse(txtmax3.Text, d) Then
                mSelectedRule.minmagicdbonus = d
            Else
                mSelectedRule.minmagicdbonus = -1
            End If
        End If
    End Sub

    <ControlReference("chkRuleMax6")> _
      Private chkRuleMax6 As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkRuleMax6", "Change")> _
    Private Sub chkRuleMax6_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            mSelectedRule.damage(1).enabled = e.Checked
        End If
    End Sub

    <ControlReference("chkRuleMax7")> _
     Private chkRuleMax7 As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkRuleMax7", "Change")> _
    Private Sub chkRuleMax7_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            mSelectedRule.damage(2).enabled = e.Checked
        End If
    End Sub

    <ControlReference("chkRuleMax8")> _
     Private chkRuleMax8 As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkRuleMax8", "Change")> _
    Private Sub chkRuleMax8_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            mSelectedRule.damage(3).enabled = e.Checked
        End If
    End Sub

    <ControlReference("txtmax5")> _
     Private txtmax5 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax5", "End")> _
    Private Sub txtmax5_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            Dim v As Integer
            If Not Integer.TryParse(txtmax5.Text, v) Then v = -1
            mSelectedRule.damage(0).maxwield = v
        End If
    End Sub

    <ControlReference("txtmax6")> _
         Private txtmax6 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax6", "End")> _
    Private Sub txtmax6_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            Dim v As Integer
            If Not Integer.TryParse(txtmax6.Text, v) Then v = -1
            mSelectedRule.damage(1).maxwield = v
        End If
    End Sub
    <ControlReference("txtmax7")> _
     Private txtmax7 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax7", "End")> _
    Private Sub txtmax7_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            Dim v As Integer
            If Not Integer.TryParse(txtmax7.Text, v) Then v = -1
            mSelectedRule.damage(2).maxwield = v
        End If
    End Sub
    <ControlReference("txtmax8")> _
     Private txtmax8 As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax8", "End")> _
    Private Sub txtmax8_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then
            Dim v As Integer
            If Not Integer.TryParse(txtmax8.Text, v) Then v = -1
            mSelectedRule.damage(3).maxwield = v
        End If
    End Sub

    Private Sub updatemaxdammage(ByVal index As Integer, ByVal txt As String)
        Try
            If mSelectedRule IsNot Nothing AndAlso ((Not mSelectedRule.damage Is Nothing) AndAlso UBound(mSelectedRule.damage) = 3) Then

                Dim arr As String() = Split(txt, "-")
                Dim a1 As Double
                Dim a2 As Integer

                If UBound(arr) = 1 Then
                    If Not Double.TryParse(arr(0), a1) Then a1 = -1
                    mSelectedRule.damage(index).mindamage = a1

                    If Not Integer.TryParse(arr(1), a2) Then a2 = -1
                    mSelectedRule.damage(index).maxdamage = a2

                Else
                    If Not Integer.TryParse(txt, a2) Then a2 = -1
                    mSelectedRule.damage(index).mindamage = -1
                    mSelectedRule.damage(index).maxdamage = a2
                End If

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub
    <ControlReference("txtmax5B")> _
    Private txtmax5B As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax5B", "End")> _
    Private Sub txtmax5B_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)

        updatemaxdammage(0, txtmax5B.Text)

    End Sub

    <ControlReference("txtmax6B")> _
   Private txtmax6B As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax6B", "End")> _
    Private Sub txtmax6B_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)

        updatemaxdammage(1, txtmax6B.Text)

    End Sub

    <ControlReference("txtmax7B")> _
   Private txtmax7B As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax7B", "End")> _
    Private Sub txtmax7B_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)

        updatemaxdammage(2, txtmax7B.Text)

    End Sub

    <ControlReference("txtmax8B")> _
   Private txtmax8B As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmax8B", "End")> _
    Private Sub txtmax8B_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)

        updatemaxdammage(3, txtmax8B.Text)

    End Sub

    <ControlReference("lstruleapplies")> _
    Private lstruleapplies As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstruleapplies", "Selected")> _
    Private Sub lstruleappliesSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        Dim checked As Boolean
        lRow = lstruleapplies(e.Row)
        id = CType(lRow(2)(0), Integer)
        checked = CBool(lRow(0)(0))
        If Not mSelectedRule Is Nothing Then
            If checked Then
                mSelectedRule.appliesToFlag = mSelectedRule.appliesToFlag Or id
            Else
                mSelectedRule.appliesToFlag = mSelectedRule.appliesToFlag Xor id
            End If
        End If
    End Sub


    <ControlReference("lstDamageTypes")> _
    Private lstDamageTypes As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstDamageTypes", "Selected")> _
    Private Sub lstDamageTypesSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        Dim checked As Boolean
        lRow = lstDamageTypes(e.Row)
        id = CType(lRow(2)(0), Integer)
        checked = CBool(lRow(0)(0))
        If Not mSelectedRule Is Nothing Then
            If checked Then
                mSelectedRule.damagetypeFlag = mSelectedRule.damagetypeFlag Or id
            Else
                mSelectedRule.damagetypeFlag = mSelectedRule.damagetypeFlag Xor id
            End If
        End If
    End Sub

    <ControlReference("txtArmorLevel")> _
        Private txtArmorLevel As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtArmorLevel", "End")> _
    Private Sub txtArmorLevel_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim i As Integer
            If Not Integer.TryParse(txtArmorLevel.Text, i) Then i = -1
            mSelectedRule.minarmorlevel = i
            If mSelectedRule.minarmorlevel >= 0 Then
                If mSelectedRule.weapontype <> eRuleWeaponTypes.notapplicable Then
                    mSelectedRule.weapontype = eRuleWeaponTypes.notapplicable
                    cboWeaponAppliesTo.Selected = 0
                End If
            End If
        End If
    End Sub

    <ControlReference("lstArmorCoverages")> _
    Private lstArmorCoverages As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstArmorCoverages", "Selected")> _
    Private Sub lstArmorCoveragesSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        Dim checked As Boolean
        lRow = lstArmorCoverages(e.Row)
        id = CType(lRow(2)(0), Integer)
        checked = CBool(lRow(0)(0))
        If Not mSelectedRule Is Nothing Then
            If checked Then
                mSelectedRule.armorcoverageFlag = mSelectedRule.armorcoverageFlag Or id
            Else
                mSelectedRule.armorcoverageFlag = mSelectedRule.armorcoverageFlag Xor id
            End If
        End If
    End Sub

    <ControlReference("lstArmorTypes")> _
    Private lstArmorTypes As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstArmorTypes", "Selected")> _
    Private Sub llstArmorTypesSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        Dim checked As Boolean
        lRow = lstArmorTypes(e.Row)
        id = CType(lRow(2)(0), Integer)
        checked = CBool(lRow(0)(0))
        If Not mSelectedRule Is Nothing Then
            If checked Then
                mSelectedRule.armortypeFlag = mSelectedRule.armortypeFlag Or id
            Else
                mSelectedRule.armortypeFlag = mSelectedRule.armortypeFlag Xor id
            End If
        End If
    End Sub

    <ControlReference("txtSpellMatches")> _
    Private txtSpellMatches As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtSpellMatches", "End")> _
    Private Sub txtSpellMatches_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedRule IsNot Nothing Then
            Dim i As Integer
            If Not Integer.TryParse(txtSpellMatches.Text, i) Then i = -1
            mSelectedRule.spellmatches = i
        End If
    End Sub

    <ControlReference("lstRuleSpells")> _
       Private lstRuleSpells As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstRuleSpells", "Selected")> _
    Private Sub lstRuleSpellsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        Dim checked As Boolean
        lRow = lstRuleSpells(e.Row)

        id = CType(lRow(3)(0), Integer)
        checked = CBool(lRow(2)(0))
        If Not mSelectedRule Is Nothing Then
            If mSelectedRule.spells Is Nothing Then
                mSelectedRule.spells = New Integerlist
            End If

            If checked Then
                mSelectedRule.spells.Add(id)
            Else
                mSelectedRule.spells.Remove(id)
                If mSelectedRule.spells.Count = 0 Then
                    mSelectedRule.spells = Nothing
                End If
            End If
        End If
    End Sub

    <ControlReference("lstRuleSetIds")> _
     Private lstRuleSetIds As Decal.Adapter.Wrappers.ListWrapper

    <ControlEvent("lstRuleSetIds", "Selected")> _
    Private Sub lstRuleSetIdsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As Integer
        Dim checked As Boolean
        lRow = lstRuleSetIds(e.Row)

        id = CType(lRow(3)(0), Integer)
        checked = CBool(lRow(2)(0))
        If Not mSelectedRule Is Nothing Then
            If mSelectedRule.Specificset Is Nothing Then
                mSelectedRule.Specificset = New Integerlist
            End If

            If checked Then
                mSelectedRule.Specificset.Add(id)
            Else
                mSelectedRule.Specificset.Remove(id)
                If mSelectedRule.Specificset.Count = 0 Then
                    mSelectedRule.Specificset = Nothing
                End If
            End If
        End If
    End Sub

    <ControlReference("chkRuleAnySet")> _
     Private chkRuleAnySet As Decal.Adapter.Wrappers.CheckBoxWrapper
    <ControlEvent("chkRuleAnySet", "Change")> _
      Private Sub chkRuleAnySet_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mSelectedRule IsNot Nothing Then
            mSelectedRule.anyset = chkRuleAnySet.Checked

            If mSelectedRule.anyset Then
                mSelectedRule.Specificset = Nothing
                addRuleSets()
            End If

        End If
    End Sub

End Class
