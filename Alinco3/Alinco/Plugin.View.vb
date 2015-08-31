Option Explicit On
Option Strict On
Option Infer On

Imports System.IO
Imports Decal.Adapter

Partial Public Class Plugin

    <Flags()> _
    Public Enum eNotifyOptions
        Notify_Corpses
        _Show_All_Corpses
        Notify_Salvage
        Notify_On_Portals
        Detect_Unknows_Scrolls_Lv7
        _Trained_Schools_Only
        _All_Levels
        _Detect_on_Tradebot
        _Use_Global_Spellbook
        Notify_On_Tell_Recieved
        Notify_Allegiance_Members
        Notify_All_Players
        Notify_All_Mobs
    End Enum

    <Flags()> _
     Public Enum eOtherOptions
        Use_DCS_Color_palette_xml
        Show_Hud
        Show_Quickslots_Hud
        _Display_Vuln_Icons
        _Display_Corpses
        Show_Info_on_Identify
        _Copy_To_Clipboard
        ' _Ignore_Wielded_Items
        '_Ignore_Mobs
        Salvage_High_Value_Items
        World_Based_Salvaging_Profile
        World_Based_Rules_Profile
        Character_Salvaging_Profile
        Character_Mobs_Profile
        Character_Loot_Profile
        Show_3D_Arrow
        Mute
        Auto_Stacking
        Auto_Pickup
        Auto_Ust
        ' _Salvage_When_Closing_Corpse
        Filter_Vendor_Tells
        Filter_Melee_Evades
        Filter_Resists
        Filter_Spells_Expire
        Filter_Spellcasting
        Windowed_Fullscreen
    End Enum

    'Tabpage click event
    <ControlEvent("nbSetupsetup", "Change")> _
   Private Sub nbSetupsetup_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        Try
            If mCharconfig IsNot Nothing Then
                If e.Index = 3 Then
                    populateSalvageListBox()
                ElseIf e.Index = 2 Then
                    populateMobsListBox()
                ElseIf e.Index = 1 Then
                    populateThropyListBox()
                End If
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    <ControlReference("sldVolume")> _
    Private sldVolume As Decal.Adapter.Wrappers.SliderWrapper

    <ControlEvent("sldVolume", "Change")> _
    Private Sub sldVolumeChange(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        mPluginConfig.wavVolume = e.Index
        If mplayer Is Nothing Then
            wtcw("Media Player wmp.lib not found, using dot.net for playing sounds, but no volume control available")
        Else
            If cboAlertSound.Selected >= 0 Then
                PlaySoundFile(cboAlertSound.Text(cboAlertSound.Selected), mPluginConfig.wavVolume)
            End If
        End If

    End Sub
#Region "Alert Editor"
    Private mSelectedAlert As Alert
    Private mprevAlertwav As Integer = -1
    <ControlReference("sldAlertVolume")> _
    Private sldAlertVolume As Decal.Adapter.Wrappers.SliderWrapper

    <ControlEvent("sldAlertVolume", "Change")> _
    Private Sub sldAlertVolumeChange(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)

        If mplayer Is Nothing Then
            wtcw("Media Player wmp.lib not found")
        Else
            If mSelectedAlert IsNot Nothing Then
                mSelectedAlert.volume = e.Index

                If cboAlertSound.Selected >= 0 Then
                    PlaySoundFile(cboAlertSound.Text(cboAlertSound.Selected), mSelectedAlert.volume)
                End If

            End If
        End If
    End Sub

    <ControlReference("cboAlertScroll")> _
    Private cboAlertScroll As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertFinished")> _
    Private cboAlertFinished As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertThropy")> _
    Private cboAlertThropy As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertMobc")> _
    Private cboAlertMobc As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertSalvagec")> _
    Private cboAlertSalvagec As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertPortalc")> _
    Private cboAlertPortalc As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertEdit")> _
    Private cboAlertEdit As Decal.Adapter.Wrappers.ChoiceWrapper

    <ControlReference("cboAlertSound")> _
    Private cboAlertSound As Decal.Adapter.Wrappers.ChoiceWrapper

    Private Sub populateAlertView()
        Try

            cboAlertEdit.Clear()

            'clear defaults
            cboAlertMobc.Clear()
            cboAlertPortalc.Clear()
            cboAlertSalvagec.Clear()
            cboAlertThropy.Clear()
            cboRuleAlert.Clear()
            cboThropysetupAlert.Clear()
            cboMobsetupAlert.Clear()
            cboAlertScroll.Clear()

            If mPluginConfig.Alerts IsNot Nothing Then

                For Each kv As KeyValuePair(Of String, Alert) In mPluginConfig.Alerts
                    If kv.Value IsNot Nothing Then
                        cboAlertEdit.Add(kv.Key, kv.Value)
                        cboAlertMobc.Add(kv.Key, kv.Key)
                        cboAlertPortalc.Add(kv.Key, kv.Key)
                        cboAlertThropy.Add(kv.Key, kv.Key)
                        cboAlertSalvagec.Add(kv.Key, kv.Key)
                        cboThropysetupAlert.Add(kv.Key, kv.Key)
                        cboMobsetupAlert.Add(kv.Key, kv.Key)
                        cboRuleAlert.Add(kv.Key, kv.Key)
                        cboAlertScroll.Add(kv.Key, kv.Key)
                    End If
                Next

                'set defaults
                setcbvalue(cboAlertMobc, mPluginConfig.AlertKeyMob)
                setcbvalue(cboAlertPortalc, mPluginConfig.AlertKeyPortal)
                setcbvalue(cboAlertThropy, mPluginConfig.AlertKeyThropy)
                setcbvalue(cboAlertSalvagec, mPluginConfig.AlertKeySalvage)
                setcbvalue(cboAlertFinished, mPluginConfig.Alertwawfinished)
                setcbvalue(cboAlertScroll, mPluginConfig.AlertKeyScroll)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub setcbvalue(ByVal cbo As Decal.Adapter.Wrappers.ChoiceWrapper, ByVal wname As String)
        If Not String.IsNullOrEmpty(wname) Then
            For i As Integer = 0 To cbo.Count - 1
                If wname = cbo.Text(i) Then
                    cbo.Selected = i
                    Return
                End If
            Next
        End If

        cbo.Selected = -1
    End Sub

    Private Sub EditAlert()
        Try
            If mSelectedAlert IsNot Nothing Then


                If mSelectedAlert.chatcolor > 0 Then
                    txtAlertColor.Text = CStr(mSelectedAlert.chatcolor)
                Else
                    txtAlertColor.Text = String.Empty
                End If

                If mSelectedAlert.showinchatwindow > 0 Then
                    txtAlertTarget.Text = CStr(mSelectedAlert.showinchatwindow)
                Else
                    txtAlertTarget.Text = String.Empty
                End If
                mprevAlertwav = -1 'block play sound event
                setcbvalue(cboAlertSound, mSelectedAlert.wavfilename)
                sldAlertVolume.SliderPostition = mSelectedAlert.volume

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlReference("txtAlertName")> _
   Private txtAlertName As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlReference("txtAlertTarget")> _
   Private txtAlertTarget As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtAlertTarget", "End")> _
    Private Sub txtAlertTarget_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedAlert IsNot Nothing Then
            Dim iint As Integer
            If Integer.TryParse(txtAlertTarget.Text, iint) Then
                mSelectedAlert.showinchatwindow = iint
            Else
                txtAlertTarget.Text = String.Empty
                mSelectedAlert.showinchatwindow = -1
            End If
        End If
    End Sub

    <ControlReference("txtAlertColor")> _
   Private txtAlertColor As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtAlertColor", "End")> _
    Private Sub txtAlertColor_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mSelectedAlert IsNot Nothing Then
            Dim iint As Integer
            If Integer.TryParse(txtAlertColor.Text, iint) Then
                mSelectedAlert.chatcolor = iint
            Else
                txtAlertColor.Text = String.Empty
                mSelectedAlert.chatcolor = -1
            End If
        End If
    End Sub

    <ControlEvent("btnAlertDelete", "Click")> _
    Private Sub btnAlertDelete_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If mPluginConfig.Alerts IsNot Nothing AndAlso cboAlertEdit.Selected >= 0 Then
                mPluginConfig.Alerts.Remove(cboAlertEdit.Text(cboAlertEdit.Selected))
                populateAlertView()
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("cboAlertEdit", "Change")> _
   Private Sub cboAlertEdit_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then 'not login completed

            Dim keyname As String = cboAlertEdit.Text(cboAlertEdit.Selected)
            If mPluginConfig.Alerts IsNot Nothing AndAlso mPluginConfig.Alerts.ContainsKey(keyname) Then
                Dim a As Alert = mPluginConfig.Alerts(keyname)

                If Not a Is Nothing Then
                    mSelectedAlert = a
                    EditAlert()
                End If
            Else
                wtcw("Programmer Error can not find alert:" & keyname)
            End If

        End If
    End Sub

    <ControlEvent("cboAlertScroll", "Change")> _
   Private Sub cboAlertScroll_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            If cboAlertScroll.Selected >= 0 Then
                mPluginConfig.AlertKeyScroll = cboAlertScroll.Text(cboAlertScroll.Selected)
            End If
        End If
    End Sub

    <ControlEvent("cboAlertFinished", "Change")> _
    Private Sub cboAlertFinished_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            If cboAlertFinished.Selected >= 0 Then
                mPluginConfig.Alertwawfinished = cboAlertFinished.Text(cboAlertFinished.Selected)
            End If
        End If
    End Sub

    <ControlEvent("cboAlertThropy", "Change")> _
   Private Sub cboAlertThropy_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            If cboAlertThropy.Selected >= 0 Then
                mPluginConfig.AlertKeyThropy = cboAlertThropy.Text(cboAlertThropy.Selected)
            End If
        End If
    End Sub

    <ControlEvent("cboAlertMobc", "Change")> _
   Private Sub cboAlertMobc_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            If cboAlertMobc.Selected >= 0 Then
                mPluginConfig.AlertKeyMob = cboAlertMobc.Text(cboAlertMobc.Selected)
            End If
        End If
    End Sub

    <ControlEvent("cboAlertSalvagec", "Change")> _
   Private Sub cboAlertSalvagec_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            If cboAlertSalvagec.Selected >= 0 Then
                mPluginConfig.AlertKeySalvage = cboAlertSalvagec.Text(cboAlertSalvagec.Selected)
            End If
        End If
    End Sub

    <ControlEvent("cboAlertPortalc", "Change")> _
    Private Sub cboAlertPortalc_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then
            If cboAlertPortalc.Selected >= 0 Then
                mPluginConfig.AlertKeyPortal = cboAlertPortalc.Text(cboAlertPortalc.Selected)
            End If
        End If
    End Sub

    <ControlEvent("btnAlertRename", "Click")> _
    Private Sub btnAlertRename_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If String.IsNullOrEmpty(txtAlertName.Text) OrElse String.IsNullOrEmpty(txtAlertName.Text.Trim) Then
                Return
            End If

            If mPluginConfig.Alerts Is Nothing Then
                mPluginConfig.Alerts = New SDictionary(Of String, Alert)
            End If

            If mSelectedAlert IsNot Nothing Then
                Dim previouskey As String = mSelectedAlert.name
                If mPluginConfig.Alerts.ContainsKey(previouskey) Then
                    mPluginConfig.Alerts.Remove(previouskey)
                End If

                If Not mPluginConfig.Alerts.ContainsKey(mSelectedAlert.name) Then
                    mSelectedAlert.name = txtAlertName.Text
                    mPluginConfig.Alerts.Add(mSelectedAlert.name, mSelectedAlert)
                End If

                populateAlertView()
                setcbvalue(cboAlertEdit, mSelectedAlert.name)
                txtAlertName.Text = String.Empty
            End If

        Catch ex As Exception

        End Try
    End Sub

    'add new
    <ControlEvent("btnAlertUpdate", "Click")> _
   Private Sub btnAlertUpdate_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            If mPluginConfig.Alerts Is Nothing Then
                mPluginConfig.Alerts = New SDictionary(Of String, Alert)
            End If

            Dim keyname As String = txtAlertName.Text
            If Not String.IsNullOrEmpty(keyname) Then

                If Not mPluginConfig.Alerts.ContainsKey(keyname) Then
                    mSelectedAlert = New Alert
                    mSelectedAlert.name = keyname
                    Dim ii As Integer
                    Integer.TryParse(txtAlertColor.Text, ii)
                    mSelectedAlert.chatcolor = ii
                    ii = 0
                    Integer.TryParse(txtAlertTarget.Text, ii)
                    mSelectedAlert.showinchatwindow = ii

                    If cboAlertSound.Selected >= 0 Then
                        mSelectedAlert.wavfilename = cboAlertSound.Text(cboAlertSound.Selected)
                    End If

                    mPluginConfig.Alerts.Add(keyname, mSelectedAlert)
                    populateAlertView()
                End If
                setcbvalue(cboAlertEdit, keyname)
                txtAlertName.Text = String.Empty
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    <ControlEvent("cboAlertSound", "Change")> _
    Private Sub cboAlertSound_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)

        If cboAlertSound.Selected = e.Id Then
            wtcw("cboAlertSound.Selected = e.Id")
        End If

        If mSelectedAlert IsNot Nothing And cboAlertSound.Selected >= 0 Then
            mSelectedAlert.wavfilename = cboAlertSound.Text(cboAlertSound.Selected)
            If mprevAlertwav <> -1 Then
                mprevAlertwav = cboAlertSound.Selected
                PlaySoundFile(mSelectedAlert.wavfilename, mSelectedAlert.volume)
            Else
                mprevAlertwav = cboAlertSound.Selected
            End If
        End If

    End Sub

    <ControlEvent("btnAlertColor", "Click")> _
       Private Sub btnAlertColor_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        For i As Integer = 0 To 20
            wtcw("This is color # " & i, i)
        Next
        For i As Integer = 1 To 5
            Host.Actions.AddChatText("This is chat window # " & i, 5, i)
        Next
    End Sub

#End Region

#Region "Items To Salvage Ust"
    <ControlReference("txtsalvageaug")> _
    Private txtsalvageaug As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtsalvageaug", "End")> _
   Private Sub txtsalvageaug_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)
        If mCharconfig IsNot Nothing Then
            Dim iint As Integer
            If Integer.TryParse(txtsalvageaug.Text, iint) Then
                mCharconfig.salvageaugmentations = iint
            Else
                txtsalvageaug.Text = CStr(0)
                mCharconfig.salvageaugmentations = 0
            End If
        End If
    End Sub

    <ControlReference("lstUstList")> _
   Private lstUstList As Decal.Adapter.Wrappers.ListWrapper
    <ControlEvent("lstUstList", "Selected")> _
      Private Sub OnlstSetupSalvageSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow
        Dim id As String

        lRow = lstUstList(e.Row)
        id = CStr(lRow(3)(0))

        If e.Column = 2 Then 'delete
            mUstItems.Remove(CInt(id))
            lstUstList.Delete(e.Row)
        Else
            Host.Actions.SelectItem(CInt(id))
        End If

    End Sub

    <ControlEvent("btnUstItems", "Click")> _
   Private Sub btnUstItems_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        If mUstItems.Count = 0 Then
            scannInventoryForSalvage(Core.CharacterFilter.Id)
        Else
            loadSalvagePanel()
        End If
    End Sub

    <ControlEvent("btnUstClear", "Click")> _
    Private Sub btnUstClear_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        lstUstList.Clear()
        mUstItems.Clear()
    End Sub

    <ControlEvent("btnUstSalvage", "Click")> _
    Private Sub btnUstSalvage_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        salvage()
    End Sub

#End Region

#Region "Settings and Notify Options"
    <ControlReference("txtmaxmana")> _
      Private txtmaxmana As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmaxmana", "Change")> _
       Private Sub OntxtmaxmanaChange(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxChangeEventArgs)
        If mPluginConfig IsNot Nothing Then
            Dim value As Integer
            If Integer.TryParse(txtmaxmana.Text, value) Then
                mPluginConfig.notifyItemmana = value
            Else
                txtmaxmana.Text = String.Empty
                mPluginConfig.notifyItemmana = 0
            End If
        End If
    End Sub

    <ControlReference("txtsettingscw")> _
     Private txtsettingscw As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtsettingscw", "Change")> _
       Private Sub OntxtsettingscwChange(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxChangeEventArgs)
        If mPluginConfig IsNot Nothing Then
            Dim value As Integer
            If Integer.TryParse(txtsettingscw.Text, value) AndAlso (value >= 0 And value < 5) Then
                mPluginConfig.chattargetwindow = value
            Else
                txtsettingscw.Text = String.Empty
                mPluginConfig.chattargetwindow = 1
            End If
        End If
    End Sub


    <ControlReference("txtmaxvalue")> _
      Private txtmaxvalue As Decal.Adapter.Wrappers.TextBoxWrapper
    <ControlEvent("txtmaxvalue", "Change")> _
       Private Sub OntxtmaxvalueChange(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxChangeEventArgs)
        If mPluginConfig IsNot Nothing Then
            Dim value As Integer
            If Integer.TryParse(txtmaxvalue.Text, value) Then
                mPluginConfig.notifyItemvalue = value
            Else
                txtmaxvalue.Text = String.Empty
                mPluginConfig.notifyItemvalue = 0
            End If
        End If
    End Sub

    <ControlReference("txtvbratio")> _
     Private txtvbratio As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlEvent("txtvbratio", "Change")> _
    Private Sub OntxtvbratioChange(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxChangeEventArgs)
        If mPluginConfig IsNot Nothing Then
            Dim value As Integer
            If Integer.TryParse(txtvbratio.Text, value) Then
                mPluginConfig.notifyValueBurden = value
            Else
                txtvbratio.Text = String.Empty
                mPluginConfig.notifyValueBurden = 0
            End If
        End If
    End Sub

    <ControlReference("chksalvageAll")> _
   Private chksalvageAll As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chksalvageAll", "Change")> _
       Private Sub v_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mPluginConfig IsNot Nothing Then
            mPluginConfig.SalvageHighValue = e.Checked
            If e.Checked Then
                wtcw("Adds the high value items to the ust list, to sell the salvage:")
                wtcw("Open a Vendor/Merchant and type the command /sell to add the bags to the list.")
            End If

        End If
    End Sub

    <ControlReference("lstOtherOptions")> _
   Private lstOtherOptions As Decal.Adapter.Wrappers.ListWrapper

    <ControlReference("lstNotifyOptions")> _
  Private lstNotifyOptions As Decal.Adapter.Wrappers.ListWrapper
    <ControlEvent("lstNotifyOptions", "Selected")> _
    Private Sub OnlstNotifyOptionsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        If mCharconfig Is Nothing Or e.Column <> 2 Then Return

        Dim lRow As Decal.Adapter.Wrappers.ListRow
        lRow = lstNotifyOptions(e.Row)
        Dim id As Integer = CType(lRow(3)(0), Integer)
        Dim checked As Boolean = CBool(lRow(2)(0))

        Select Case CType(id, eNotifyOptions)
            Case eNotifyOptions.Notify_Corpses
                mPluginConfig.notifycorpses = checked
                If checked Then
                    wtcw("Shows corpses from critters killed by you and also your own corpse.")
                End If
            Case eNotifyOptions._Show_All_Corpses
                If checked Then
                    wtcw("Notifies all corpses with more then 6000 burden units.")
                End If
                mPluginConfig.showallcorpses = checked

            Case eNotifyOptions.Notify_On_Portals
                mPluginConfig.NotifyPortals = checked

            Case eNotifyOptions.Detect_Unknows_Scrolls_Lv7
                If checked Then
                    wtcw("Detects unknown level 7 scrolls.")
                End If
                mPluginConfig.unknownscrolls = checked
                'TODO    setspellbookflags()

            Case eNotifyOptions._All_Levels
                If checked Then
                    wtcw("Detects all levels scrolls in the school that is trained or specialized.")
                End If
                mPluginConfig.unknownscrollsAll = checked
                'TODO setspellbookflags()

            Case eNotifyOptions._Trained_Schools_Only
                If checked Then
                    wtcw("Detects only scrolls in the school that is trained or specialized.")
                End If
                mPluginConfig.trainedscrollsonly = checked
                'TODO setspellbookflags()

            Case eNotifyOptions._Detect_on_Tradebot
                If checked Then
                    wtcw("Searches for unknown scrolls in the trade window")
                End If
                mCharconfig.detectscrollsontradebot = checked
            Case eNotifyOptions.Notify_All_Mobs
                mCharconfig.ShowAllMobs = checked
            Case eNotifyOptions.Notify_All_Players
                mCharconfig.ShowAllPlayers = checked
            Case eNotifyOptions._Use_Global_Spellbook
                If checked Then
                    wtcw("Let your other chars find scrolls for this char (" & Core.CharacterFilter.Name & ")")
                    'TODO fillglobalspellbook()
                Else
                    'TODO clearglobalspellbook()
                End If
                mCharconfig.useglobalspellbook = checked
                'TODO setspellbookflags()
            Case eNotifyOptions.Notify_Allegiance_Members
                If checked Then
                    wtcw("Notifies Allegiance Members")
                End If
                mPluginConfig.notifyalleg = checked
            Case eNotifyOptions.Notify_On_Tell_Recieved
                If checked Then
                    wtcw("Sound on tell on")
                Else
                    wtcw("Sound on tell off")
                End If
                mPluginConfig.notifytells = checked
        End Select
        If checked Then wtcw(" ")
    End Sub

    Private Sub addOption(ByVal checked As Boolean, ByVal eOption As eOtherOptions)
        If Not lstOtherOptions Is Nothing Then
            Dim lRow As Decal.Adapter.Wrappers.ListRow
            Dim idOption As Integer = eOption

            For i As Integer = 0 To lstOtherOptions.RowCount - 1
                lRow = lstOtherOptions(i)

                If idOption = CType(lRow(3)(0), Integer) Then
                    lRow(2)(0) = checked
                    Return
                End If
            Next

            Dim text As String = [Enum].GetName(GetType(Plugin.eOtherOptions), idOption)
            If Not text Is Nothing Then
                If text.StartsWith("_") Then
                    text = "-" & text
                End If
                text = text.Replace("_", " ")

            End If

            lRow = lstOtherOptions.Add
            lRow(0)(1) = 100668300
            lRow(1)(0) = text
            lRow(2)(0) = checked
            lRow(3)(0) = CStr(idOption)
        End If
    End Sub

    Private Sub addNotifyOption(ByVal checked As Boolean, ByVal eOption As eNotifyOptions)
        If Not lstNotifyOptions Is Nothing Then
            Dim lRow As Decal.Adapter.Wrappers.ListRow
            Dim idOption As Integer = eOption

            For i As Integer = 0 To lstNotifyOptions.RowCount - 1
                lRow = lstNotifyOptions(i)

                If idOption = CType(lRow(3)(0), Integer) Then
                    lRow(2)(0) = checked
                    Return
                End If
            Next

            Dim text As String = [Enum].GetName(GetType(Plugin.eNotifyOptions), idOption)
            If Not text Is Nothing Then
                If text.StartsWith("_") Then
                    text = "-" & text
                End If
                text = text.Replace("_", " ")

            End If

            lRow = lstNotifyOptions.Add
            lRow(0)(1) = 100668300
            lRow(1)(0) = text
            lRow(2)(0) = checked
            lRow(3)(0) = CStr(idOption)
        End If
    End Sub
    <ControlEvent("lstOtherOptions", "Selected")> _
    Private Sub OnllstOtherOptionsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        If mCharconfig Is Nothing Or e.Column <> 2 Then Return

        Dim lRow As Decal.Adapter.Wrappers.ListRow
        lRow = lstOtherOptions(e.Row)
        Dim id As Integer = CType(lRow(3)(0), Integer)
        Dim checked As Boolean = CBool(lRow(2)(0))

        Select Case CType(id, eOtherOptions)
            Case eOtherOptions.Use_DCS_Color_palette_xml
                mPluginConfig.Showpalette = checked
            Case eOtherOptions.Show_Hud
                mPluginConfig.Showhud = checked
                Renderhud()
            Case eOtherOptions.Show_Quickslots_Hud
                mCharconfig.ShowhudQuickSlots = checked
                If checked Then
                    wtcw("To Add items: ", 1)
                    wtcw("Identify the item to add first, then click on a empty slot. (don't try to drag drop)", 1)
                End If
                RenderQuickslotsHud()
            Case eOtherOptions._Display_Corpses
                mPluginConfig.Showhudcorpses = checked
                Renderhud()
            Case eOtherOptions._Display_Vuln_Icons
                mPluginConfig.Showhudvulns = checked
                Renderhud()
            Case eOtherOptions.Show_Info_on_Identify
                If checked Then
                    wtcw("Shows info to the chatwindow when you manual Identify an Item", 1)
                End If
                mPluginConfig.OutputManualIdentify = checked
            Case eOtherOptions.Show_3D_Arrow
                If checked Then
                    wtcw("displays a 3d direction arrow at alerts")
                Else
                    wtcw("3d direction arrow off")
                End If
                mPluginConfig.D3DMark0bjects = checked

            Case eOtherOptions._Copy_To_Clipboard
                If checked Then
                    wtcw("Copies the info to clipboard")
                End If
                mPluginConfig.CopyToClipboard = checked
                'Case eOtherOptions._Ignore_Wielded_Items
                '    If checked Then
                '        wtcw("Ignores wielded items (for when you use a manachecker).")
                '    End If
                '    mPluginConfig.OutputManualIgnoreSelf = checked
                'Case eOtherOptions._Ignore_Mobs
                '    If checked Then
                '        wtcw("Ignores mobs (for when you use a plugin that needs mob data).")
                '    End If
                '    mPluginConfig.OutputManualIgnoreMobs = checked
            Case eOtherOptions.Auto_Stacking
                If checked Then
                    wtcw("When a new stackable item appears in your inventory it tries to stack it.")
                End If
                mPluginConfig.AutoStacking = checked

            Case eOtherOptions.Mute
                If checked Then
                    wtcw("All Sounds Off")
                Else
                    wtcw("Sounds On")
                End If
                mPluginConfig.MuteAll = checked
            Case eOtherOptions.Auto_Pickup
                If checked Then
                    wtcw("Auto pick up items when opening a corpse on (not chests)")
                Else
                    wtcw("Auto pick up items")
                End If
                mFreeMainPackslots = CountFreeSlots(Core.CharacterFilter.Id)
                mPluginConfig.AutoPickup = checked
            Case eOtherOptions.Auto_Ust
                If checked Then
                    wtcw("Auto usting on")
                Else
                    wtcw("Auto usting off")
                End If
                mPluginConfig.AutoUst = checked
                'Case eOtherOptions._Salvage_When_Closing_Corpse
                '    mPluginConfig.AutoUstOnCloseCorpse = checked
            Case eOtherOptions.Character_Salvaging_Profile

                If checked Then
                    wtcw("Using a character based salvaging profile")
                Else
                    wtcw("Using the general salvaging profile")
                End If

                If mCharconfig.usesalvageprofile <> checked Then
                    mCharconfig.usesalvageprofile = checked
                    loadsalvage()
                    ' populateSalvageListBox()
                End If

            Case eOtherOptions.Character_Mobs_Profile

                If checked Then
                    wtcw("Using a character based monster notify profile")
                Else
                    wtcw("Using the general based monster notify profile")
                End If

                If mCharconfig.usemobsprofile <> checked Then
                    mCharconfig.usemobsprofile = checked
                    LoadMobsList()
                End If

            Case eOtherOptions.Character_Loot_Profile

                If checked Then
                    wtcw("Using a character based thropy notify profile")
                Else
                    wtcw("Using the general based thropy notify profile")
                End If

                If mCharconfig.uselootprofile <> checked Then
                    mCharconfig.uselootprofile = checked
                    LoadThropyList()
                End If

            Case eOtherOptions.Salvage_High_Value_Items

                If checked Then
                    wtcw("Salvage items flagged for high value on")
                End If

                mPluginConfig.SalvageHighValue = checked
            Case eOtherOptions.World_Based_Salvaging_Profile

                If mPluginConfig.worldbasedsalvage <> checked Then
                    mPluginConfig.worldbasedsalvage = checked
                    loadsalvage()
                End If

            Case eOtherOptions.World_Based_Rules_Profile
                If mPluginConfig.worldbasedrules <> checked Then
                    mPluginConfig.worldbasedrules = checked
                    loadRules()
                    populateRulesView()
                End If
            Case eOtherOptions.Filter_Vendor_Tells
                mPluginConfig.FilterTellsMerchant = checked
            Case eOtherOptions.Filter_Spells_Expire
                mPluginConfig.FilterSpellsExpire = checked
            Case eOtherOptions.Filter_Spellcasting
                mPluginConfig.FilterSpellcasting = checked
            Case eOtherOptions.Filter_Resists
                mPluginConfig.FilterChatResists = checked
            Case eOtherOptions.Filter_Melee_Evades
                mPluginConfig.FilterChatMeleeEvades = checked
            Case eOtherOptions.Windowed_Fullscreen
                mPluginConfig.WindowedFullscreen = checked
                If checked Then
                    wtcw("")
                    wtcw("If the desktop screen resolution is the same as the ac window")
                    wtcw("this option removes the caption (blue title bar) and borders from AC and")
                    wtcw("sets the postion to (0,0) top left on the primary monitor")
                    wtcw("most likely only usefull on a multi-monitor system or when you alt-tab alot")
                    wtcw("don't turn this on in fullscreen mode :)")

                    WinApe.WindowedFullscreen(Host.Decal.Hwnd)

                End If
        End Select
        If checked Then wtcw(" ")
    End Sub
#End Region

#Region "Thropy List"
    Private mLastnpcId As Integer


    <ControlReference("cboThropysetupAlert")> _
    Private cboThropysetupAlert As Decal.Adapter.Wrappers.ChoiceWrapper


    <ControlEvent("cboThropysetupAlert", "Change")> _
   Private Sub cboThropysetupAlert_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then 'not login completed

            Dim keyname As String = cboThropysetupAlert.Text(cboThropysetupAlert.Selected)
            If cboThropysetupAlert.Selected >= 0 And Not String.IsNullOrEmpty(mSelectedThropyKey) Then

                If mPluginConfig.Alerts IsNot Nothing AndAlso mPluginConfig.Alerts.ContainsKey(keyname) Then
                    If Not String.IsNullOrEmpty(mPluginConfig.AlertKeyThropy) Then
                        If keyname = mPluginConfig.AlertKeyThropy Then
                            keyname = String.Empty
                        End If
                    End If

                    If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey(mSelectedThropyKey) Then
                        Dim tinfo As ThropyInfo = mActiveThropyProfile.Item(mSelectedThropyKey)
                        tinfo.Alert = keyname
                    End If
                End If

            End If


        End If
    End Sub

    <ControlReference("lstmyItems")> _
     Private lstmyItems As Decal.Adapter.Wrappers.ListWrapper

    <ControlReference("chkItemExact")> _
    Private chkItemExact As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlReference("txtLootName")> _
   Private txtLootName As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlEvent("chkItemExact", "Change")> _
       Private Sub chkItemExact_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey(mSelectedThropyKey) Then
            Dim tinfo As ThropyInfo = mActiveThropyProfile.Item(mSelectedThropyKey)
            tinfo.ispartial = Not e.Checked
        End If
    End Sub
    <ControlEvent("txtLootMax", "End")> _
  Private Sub txtLootMax_End(ByVal sender As Object, ByVal e As Decal.Adapter.TextBoxEndEventArgs)

        If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey(mSelectedThropyKey) Then
            Dim tinfo As ThropyInfo = mActiveThropyProfile.Item(mSelectedThropyKey)

            Dim result As Integer
            If Integer.TryParse(txtLootMax.Text, result) Then
                tinfo.lootmax = result
            Else
                txtLootMax.Text = String.Empty
                tinfo.lootmax = 0
            End If

        End If

    End Sub
    <ControlReference("btnAttachLootItem")> _
    Private btnAttachLootItem As Decal.Adapter.Wrappers.PushButtonWrapper

    <ControlEvent("btnAttachLootItem", "Click")> _
     Private Sub btnAttachLootItem_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey(mSelectedThropyKey) Then
            Dim thInfo As ThropyInfo = mActiveThropyProfile.Item(mSelectedThropyKey)

            If thInfo.npc Is Nothing Then
                Dim o As Decal.Adapter.Wrappers.WorldObject = _
                 Core.WorldFilter.Item(Host.Actions.CurrentSelection)

                If o IsNot Nothing Then
                    If o.ObjectClass = Wrappers.ObjectClass.Npc Then
                        thInfo.npc = o.Name
                        thInfo.npcloc = New Location(o.Values(Wrappers.LongValueKey.Landblock), o.RawCoordinates)
                        wtcw(mSelectedThropyKey & " attached to " & o.Name & ", you can now use it with the 'Give to npc' hotkey")
                        btnAttachLootItem.Text = "Detach npc"
                        mLastnpcId = o.Id
                    ElseIf o.ObjectClass = Wrappers.ObjectClass.Vendor Then
                        wtcw("Doesn't work with Merchants, select a NPC")
                    Else
                        wtcw("Select a NPC")
                    End If
                Else
                    wtcw("Select a NPC")
                End If
            Else
                thInfo.npc = Nothing
                thInfo.npcloc = Nothing
                wtcw(mSelectedThropyKey & " detached ")
                btnAttachLootItem.Text = "Attach npc"
            End If
        End If
    End Sub
    Private Function Addnewlootitem(ByVal sname As String) As Boolean
        If Not String.IsNullOrEmpty(sname) AndAlso Not mActiveThropyProfile.ContainsKey(sname) Then
            Dim ispartial As Boolean


            ispartial = Not chkItemExact.Checked


            mActiveThropyProfile.Add(sname, New ThropyInfo(True, ispartial, 0))

            Dim i As Integer = populateThropyListBox(sname)
            mSelectedThropyKey = sname

            lstmyItems.JumpToPosition(i)
            lstmyItems(i)(1).Color = System.Drawing.Color.FromArgb(argmeddark)
            Return True
        End If
    End Function

    <ControlEvent("btnAddLootItem", "Click")> _
   Private Sub btnAddLootItem_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            Dim objCursorItem As Wrappers.WorldObject = Core.WorldFilter.Item(Host.Actions.CurrentSelection)
            If objCursorItem IsNot Nothing Then
                Select Case objCursorItem.ObjectClass
                    Case Wrappers.ObjectClass.Monster, Wrappers.ObjectClass.Monster, Wrappers.ObjectClass.Vendor, Wrappers.ObjectClass.Player
                        objCursorItem = Nothing
                End Select
            End If


            Dim sinput As String = txtLootName.Text
            If sinput IsNot Nothing AndAlso sinput.Trim.Length > 0 Then
                sinput = txtLootName.Text.Trim
            Else
                sinput = Nothing
            End If

            If sinput IsNot Nothing Then
                If Addnewlootitem(sinput) Then
                    wtcw("Add to items: " & sinput)
                    Return
                End If
            End If

            If objCursorItem IsNot Nothing Then
                If Addnewlootitem(objCursorItem.Name) Then
                    wtcw("Add to items: " & objCursorItem.Name)
                    Return
                Else
                    sinput = objCursorItem.Name
                End If
            End If

            If sinput IsNot Nothing Then
                For i As Integer = 0 To lstmyItems.RowCount - 1
                    Dim name As String = CStr(lstmyItems.Item(i)(1)(0))
                    If name = sinput Then
                        lstmyItems.JumpToPosition(i)
                    End If
                Next
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private mSelectedThropyKey As String

    Private Sub AddToThropyOrMobList(ByVal lst As Decal.Adapter.Wrappers.ListWrapper, ByVal vchecked As Boolean, ByVal vname As String, ByVal count As Integer)
        Try
            Dim newRow As Decal.Adapter.Wrappers.ListRow

            newRow = lst.Add

            newRow(0)(0) = vchecked
            newRow(1)(0) = vname

            If count > 0 Then
                newRow(2)(0) = CStr(count)
            End If

            newRow(3)(1) = &H6005E6A

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    Private Function populateThropyListBox(Optional ByVal newname As String = "") As Integer
        If mActiveThropyProfile IsNot Nothing AndAlso lstmyItems IsNot Nothing Then
            mSelectedThropyKey = String.Empty
            chkItemExact.Checked = True
            txtLootName.Text = String.Empty
            lstmyItems.Clear()
            Dim sorted As System.Linq.IOrderedEnumerable(Of KeyValuePair(Of String, ThropyInfo))

            sorted = From x As KeyValuePair(Of String, ThropyInfo) _
                  In mActiveThropyProfile _
                  Order By x.Value.checked Descending, x.Key
            Dim position As Integer = -1
            Dim jumptoid As Integer = 0
            For Each a As KeyValuePair(Of String, ThropyInfo) In sorted
                position += 1
                If Not String.IsNullOrEmpty(newname) AndAlso newname = a.Key Then
                    jumptoid = position
                End If
                AddToThropyOrMobList(lstmyItems, a.Value.checked, a.Key, 0)
            Next
            Return jumptoid
        End If
    End Function

    <ControlReference("txtLootMax")> _
   Private txtLootMax As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlEvent("lstmyItems", "Selected")> _
  Private Sub OnlstmyItemsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        For i As Integer = 0 To lstmyItems.RowCount - 1
            lRow = lstmyItems(i)
            lRow(1).Color = colorw
        Next

        'save old maxloot, if the cursor is in txtmaxloot and click on an other item you loose it
        If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey(mSelectedThropyKey) Then
            Dim thInfo As ThropyInfo = mActiveThropyProfile.Item(mSelectedThropyKey)
            Dim ncount As Integer
            If Integer.TryParse(txtLootMax.Text, ncount) Then
                thInfo.lootmax = ncount
            Else
                txtLootMax.Text = String.Empty
                thInfo.lootmax = 0
            End If
        End If

        lRow = lstmyItems(e.Row)
        lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark) 'monsters
        Dim checked As Boolean = CBool(lRow(0)(0))

        mSelectedThropyKey = (CStr(lRow(1)(0)))
        If mActiveThropyProfile IsNot Nothing AndAlso mActiveThropyProfile.ContainsKey(mSelectedThropyKey) Then
            Dim thInfo As ThropyInfo = mActiveThropyProfile.Item(mSelectedThropyKey)
            setcbvalue(cboThropysetupAlert, thInfo.Alert)

            If e.Column = 0 Then
                thInfo.checked = checked
            ElseIf e.Column = 1 Then
                If thInfo.lootmax = 0 Then
                    txtLootMax.Text = String.Empty
                Else
                    txtLootMax.Text = CStr(thInfo.lootmax)
                End If

                chkItemExact.Checked = Not thInfo.ispartial
                txtLootName.Text = mSelectedThropyKey
                If thInfo.npcloc IsNot Nothing AndAlso thInfo.npc IsNot Nothing Then
                    btnAttachLootItem.Text = "Detach npc"
                    Dim msg As String = "NPC: " & thInfo.npc & ", " & thInfo.npcloc.ToString(True)
                    wtcw(msg)
                Else
                    btnAttachLootItem.Text = "Attach npc"
                End If

            ElseIf e.Column = 3 Then ' delete
                mActiveThropyProfile.Remove(mSelectedThropyKey)
                lstmyItems.Delete(e.Row)
                mSelectedThropyKey = String.Empty
                txtLootName.Text = String.Empty
            End If

        End If


    End Sub

#End Region

#Region "Mobs List"

    <ControlReference("cboMobsetupAlert")> _
  Private cboMobsetupAlert As Decal.Adapter.Wrappers.ChoiceWrapper


    <ControlEvent("cboMobsetupAlert", "Change")> _
   Private Sub cboMobsetupAlert_Change(ByVal sender As Object, ByVal e As Decal.Adapter.IndexChangeEventArgs)
        If mCharconfig IsNot Nothing Then 'not login completed

            Dim keyname As String = cboMobsetupAlert.Text(cboMobsetupAlert.Selected)
            If cboMobsetupAlert.Selected >= 0 And Not String.IsNullOrEmpty(mSelectedMobListKey) Then

                If mPluginConfig.Alerts IsNot Nothing AndAlso mPluginConfig.Alerts.ContainsKey(keyname) Then
                    If Not String.IsNullOrEmpty(mPluginConfig.AlertKeyMob) Then
                        If keyname = mPluginConfig.AlertKeyMob Then
                            keyname = String.Empty
                        End If
                    End If

                    If mActiveMobProfile IsNot Nothing AndAlso mActiveMobProfile.ContainsKey(mSelectedMobListKey) Then
                        Dim tinfo As NameLookup = mActiveMobProfile.Item(mSelectedMobListKey)
                        tinfo.Alert = keyname
                    End If
                End If

            End If

        End If
    End Sub

    <ControlReference("lstmyMobs")> _
    Private lstmyMobs As Decal.Adapter.Wrappers.ListWrapper
    Private mSelectedMobListKey As String

    <ControlReference("txtmyMobName")> _
   Private txtmyMobName As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlReference("chkmyMobExact")> _
    Private chkmyMobExact As Decal.Adapter.Wrappers.CheckBoxWrapper

    <ControlEvent("chkmyMobExact", "Change")> _
    Private Sub chkmyMobExact_Changed(ByVal sender As Object, ByVal e As Decal.Adapter.CheckBoxChangeEventArgs)
        If mActiveMobProfile IsNot Nothing AndAlso mActiveMobProfile.ContainsKey(mSelectedMobListKey) Then
            Dim mobinfo As NameLookup = mActiveMobProfile.Item(mSelectedMobListKey)

            mobinfo.ispartial = Not e.Checked
        End If
    End Sub

    <ControlEvent("lstmyMobs", "Selected")> _
   Private Sub lstmyMobsSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        For i As Integer = 0 To lstmyMobs.RowCount - 1
            lRow = lstmyMobs(i)
            lRow(1).Color = colorw
        Next
        lRow = lstmyMobs(e.Row)
        lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark)
        Dim checked As Boolean = CBool(lRow(0)(0))

        mSelectedMobListKey = (CStr(lRow(1)(0)))

        If mActiveMobProfile IsNot Nothing AndAlso mActiveMobProfile.ContainsKey(mSelectedMobListKey) Then
            Dim mobinfo As NameLookup = mActiveMobProfile.Item(mSelectedMobListKey)

            If e.Column = 0 Then
                mobinfo.checked = checked
            ElseIf e.Column = 1 Then
                setcbvalue(cboMobsetupAlert, mobinfo.Alert)
                txtmyMobName.Text = mSelectedMobListKey
                chkmyMobExact.Checked = Not mobinfo.ispartial
            ElseIf e.Column = 3 Then ' delete
                mActiveMobProfile.Remove(mSelectedMobListKey)
                lstmyMobs.Delete(e.Row)
                mSelectedMobListKey = String.Empty
                txtmyMobName.Text = String.Empty
            End If
        End If
    End Sub

    Private Function Addnewmobitem(ByVal sname As String) As Boolean
        If Not String.IsNullOrEmpty(sname) AndAlso Not mActiveMobProfile.ContainsKey(sname) Then
            Dim ispartial As Boolean

            ispartial = Not chkmyMobExact.Checked

            mActiveMobProfile.Add(sname, New NameLookup(True, ispartial))

            Dim i As Integer = populateMobsListBox(sname)
            mSelectedMobListKey = sname

            lstmyMobs.JumpToPosition(i)
            lstmyMobs(i)(1).Color = System.Drawing.Color.FromArgb(argmeddark)
            Return True
        End If
    End Function

    <ControlEvent("btnAddMobItem", "Click")> _
Private Sub btnAddMobItem_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Try
            Dim objCursorActor As Decal.Adapter.Wrappers.WorldObject = _
            Core.WorldFilter.Item(Host.Actions.CurrentSelection)

            If objCursorActor IsNot Nothing AndAlso objCursorActor.ObjectClass <> Wrappers.ObjectClass.Monster Then
                objCursorActor = Nothing
            End If

            Dim sinput As String = txtmyMobName.Text
            If sinput IsNot Nothing AndAlso sinput.Trim.Length > 0 Then
                sinput = txtmyMobName.Text.Trim
            Else
                sinput = Nothing
            End If

            If sinput IsNot Nothing Then
                If Addnewmobitem(sinput) Then
                    wtcw("Add to mobs: " & sinput)
                    Return
                End If
            End If

            If objCursorActor IsNot Nothing Then
                If Addnewmobitem(objCursorActor.Name) Then
                    wtcw("Add to mobs: " & objCursorActor.Name)
                    Return
                Else
                    sinput = objCursorActor.Name
                End If
            End If

            If sinput IsNot Nothing Then
                For i As Integer = 0 To lstmyMobs.RowCount - 1
                    Dim name As String = CStr(lstmyMobs.Item(i)(1)(0))
                    If name = sinput Then
                        lstmyMobs.JumpToPosition(i)
                    End If
                Next
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Function populateMobsListBox(Optional ByVal snametoadd As String = "") As Integer
        If mActiveMobProfile IsNot Nothing AndAlso lstmyMobs IsNot Nothing Then
            mSelectedMobListKey = String.Empty
            chkmyMobExact.Checked = True
            txtmyMobName.Text = String.Empty
            lstmyMobs.Clear()
            Dim sorted As System.Linq.IOrderedEnumerable(Of KeyValuePair(Of String, NameLookup))

            sorted = From x As KeyValuePair(Of String, NameLookup) _
                  In mActiveMobProfile _
                  Order By x.Value.checked Descending, x.Key

            Dim position As Integer = -1
            Dim jumpid As Integer = 0
            For Each a As KeyValuePair(Of String, NameLookup) In sorted
                position += 1
                If snametoadd <> String.Empty AndAlso a.Key = snametoadd Then
                    jumpid = position
                End If
                AddToThropyOrMobList(lstmyMobs, a.Value.checked, a.Key, 0)
            Next


            Return jumpid

        End If
    End Function

#End Region

#Region "Salvage Setup"
    Private mSelectedSalvageKey As Integer
    Const argbyellow As Integer = &HFFFFFF75
    Const argnearlight As Integer = &HFFFFFFFF
    Const argmeddark As Integer = &HFF5FA3EC


    <ControlReference("lstNotifySalvalge")> _
    Private lstNotifySalvalge As Decal.Adapter.Wrappers.ListWrapper

    Private Function addToSalvageSetupList(ByVal vname As String, ByVal dpart As String, ByVal vchecked As Boolean, ByVal id As String) As Integer
        Dim lRow As Decal.Adapter.Wrappers.ListRow

        lRow = lstNotifySalvalge.Add
        lRow(0)(0) = vchecked
        lRow(1)(0) = vname
        lRow(2)(0) = dpart
        lRow(3)(0) = id

    End Function

    Private Sub populateSalvageListBox()

        If mActiveSalvageProfile IsNot Nothing AndAlso lstNotifySalvalge IsNot Nothing Then
            txtSalvageString.Text = String.Empty
            mSelectedSalvageKey = 0
            lstNotifySalvalge.Clear()

            Dim sorted As System.Linq.IOrderedEnumerable(Of KeyValuePair(Of Integer, SalvageSettings))

            sorted = From x As KeyValuePair(Of Integer, SalvageSettings) _
                         In mActiveSalvageProfile _
                         Order By x.Value.checked Descending, x.Value.name

            For Each a As KeyValuePair(Of Integer, SalvageSettings) In sorted
                addToSalvageSetupList(a.Value.name, "(" & a.Value.combinestring & ")", a.Value.checked, CStr(a.Key))
            Next

        End If
    End Sub

    Private Function infoSalvagestring(ByVal str As String) As String
        Try
            Dim s As String = ""

            If str IsNot Nothing AndAlso str.Trim.Length > 0 Then
                Dim arr() As String
                arr = Split(str, ",")

                Dim p As Integer = CInt(arr(0))
                Dim q As Integer = CInt(arr(UBound(arr)))

                If p > 1 Then
                    s = "salvage quality below " & p & " will be ignored. "
                End If

                For i As Integer = 1 To UBound(arr)
                    Dim current As Integer
                    current = CInt(arr(i))

                    If p + 1 <> current Then
                        s &= p & "-" & current - 1 & " into one bag. "
                    Else
                        s &= p & " in one bag. "
                    End If

                    p = current
                Next
                If q < 10 Then
                    s &= q & "-10 in one bag."
                Else
                    s &= "10 in one bag."
                End If
                Return s

            End If

            Return "?"

        Catch ex As Exception
            Util.ErrorLogger(ex)
            Return "?"
        End Try

    End Function

    ' validates an array of numbers separated by a comma "," 
    ' numbers must have a value between 1-10, in order and unique
    ' returns true on a empty string
    Private Function validateSalvageString(ByVal str As String) As Boolean
        Try

            If str IsNot Nothing AndAlso str.Trim.Length > 0 Then
                Dim arr() As String
                arr = Split(str, ",")
                Dim previous As Integer = 0
                For Each entry As String In arr

                    If Not IsNumeric(entry) Then
                        Return False
                    End If

                    Dim i As Integer
                    i = CInt(entry)

                    If i <= 0 Or i > 10 Then
                        Return False
                    End If

                    If previous > 0 And previous >= i Then
                        Return False
                    End If

                    previous = i
                Next
            End If

            Return True

        Catch ex As Exception
            Return False
        End Try

    End Function

    <ControlReference("txtSalvageString")> _
    Private txtSalvageString As Decal.Adapter.Wrappers.TextBoxWrapper

    <ControlEvent("btnUpdateSalvage", "Click")> _
   Private Sub btnUpdateSalvage_Click(ByVal sender As Object, ByVal e As Decal.Adapter.ControlEventArgs)
        Dim sinput As String = txtSalvageString.Text
        If sinput IsNot Nothing AndAlso sinput.Trim.Length > 0 Then
            sinput = txtSalvageString.Text.Trim
        Else
            sinput = Nothing
        End If
        Dim salinfo As SalvageSettings = Nothing

        If mActiveSalvageProfile IsNot Nothing AndAlso mActiveSalvageProfile.ContainsKey(mSelectedSalvageKey) Then
            salinfo = mActiveSalvageProfile.Item(mSelectedSalvageKey)
        End If

        If sinput IsNot Nothing Then
            If salinfo Is Nothing Then
                wtcw("To update the Salvage selection click on an entry first.")
            ElseIf validateSalvageString(sinput) Then

                wtcw("Updating salvage settings for " & salinfo.name & " with:")
                wtcw(infoSalvagestring(sinput))
                salinfo.combinestring = sinput
                For i As Integer = 0 To lstNotifySalvalge.RowCount - 1
                    Dim lRow As Wrappers.ListRow = lstNotifySalvalge(i)
                    If CInt(lRow(3)(0)) = mSelectedSalvageKey Then
                        lRow(2)(0) = "(" & salinfo.combinestring & ")"
                        Exit For
                    End If
                Next
            Else
                wtcw("Invalid input")
                wtcw("Numbers must have a value between 1-10, in order, unique and separated by a comma")
            End If
        Else
            wtcw("To update the Salvage selection click on an entry first.")
        End If
    End Sub


    <ControlEvent("lstNotifySalvalge", "Selected")> _
       Private Sub lstNotifySalvalgeSelected(ByVal sender As Object, ByVal e As Decal.Adapter.ListSelectEventArgs)
        Dim lRow As Decal.Adapter.Wrappers.ListRow = Nothing
        Dim name As String
        Dim checked As Boolean
        Dim colorw As System.Drawing.Color = System.Drawing.Color.FromArgb(argnearlight)
        For i As Integer = 0 To lstNotifySalvalge.RowCount - 1
            lRow = lstNotifySalvalge(i)
            lRow(1).Color = colorw
        Next

        lRow = lstNotifySalvalge(e.Row)
        name = CStr(lRow(1)(0))
        checked = CType(lRow(0)(0), Boolean)

        mSelectedSalvageKey = CInt(lRow(3)(0))
        Dim salinfo As SalvageSettings = Nothing
        If mActiveSalvageProfile IsNot Nothing AndAlso mActiveSalvageProfile.ContainsKey(mSelectedSalvageKey) Then
            salinfo = mActiveSalvageProfile.Item(mSelectedSalvageKey)
        End If

        lRow(1).Color = System.Drawing.Color.FromArgb(argmeddark)
        If salinfo IsNot Nothing Then
            salinfo.checked = checked
            If checked Then
                txtSalvageString.Text = salinfo.combinestring
            Else
                txtSalvageString.Text = String.Empty
            End If
        End If
    End Sub
#End Region

End Class
