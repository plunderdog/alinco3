Option Explicit On
Option Strict On
Option Infer On

Imports Decal.Adapter
Imports System.IO
Imports System.Xml.Serialization
Imports Microsoft.Win32
Imports Decal.Filters
Imports System.Runtime.InteropServices
Imports Decal.Adapter.Wrappers
Imports System.Drawing
Imports System.Xml.Linq
Imports System.ComponentModel

<WireUpBaseEvents(), View("Alinco.mainview.xml")> _
Public Class Plugin
    Inherits PluginBase

    Friend Shared FileService As FileService
    Friend Shared GameData As GameData
    Friend Shared RenderServiceForHud As RenderServiceWrapper
    Friend Shared HooksForErrorHandler As Decal.Adapter.Wrappers.HooksWrapper

    Private malincobuffs As Alinco3Buffs.Plugin
    Private malincobuffsAvailable As Boolean

    Private mPluginConfig As PluginSettings
    Private mCharconfig As CharSettings
    Private mWorldConfig As WorldSettings

    Private mPluginConfigfilename As String
    Private mCharConfigfilename As String
    Private mWorldConfigfilename As String
    Private mWorldInventoryname As String
    Private mExportInventoryname As String

    Private mSalvageBasefilename As String
    Private mMainTimer As Windows.Forms.Timer

    Private mInportalSpace As Boolean

    Private mplayer As mediaplayer
    Private mBackgroundworker As BackgroundWorker
    Private mFilesLoaded As Boolean

  

    Sub New()
        Try
            minstance = Me

            Util.docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            Util.docPath = IO.Path.Combine(Util.docPath, "Decal Plugins\Alinco3")
            Util.wavPath = System.IO.Path.GetDirectoryName(Me.GetType().Module.Assembly.Location)
            Util.wavPath = IO.Path.Combine(Util.wavPath, "Wav")

            If Not Directory.Exists(Util.docPath) Then
                Directory.CreateDirectory(Util.docPath)
            End If

            Dim asm As System.Reflection.Assembly = System.Reflection.Assembly.GetExecutingAssembly
            Dim AppVersion As System.Version = asm.GetName().Version
            Util.dllversion = AppVersion.ToString

            Util.StartLog()
            mPluginConfigfilename = (Util.docPath & "\settings.xml")
            'If File.Exists(mPluginConfigfilename) Then
            '    Me.mPluginConfig = DirectCast(Util.DeSerializeObject(mPluginConfigfilename, GetType(PluginSettings)), PluginSettings)
            'End If
            'this plugin is made on a os where it is the other way arround
            Util.NumberFormatInfo = New System.Globalization.NumberFormatInfo
            Util.NumberFormatInfo.NumberDecimalSeparator = "."
            Util.NumberFormatInfo.NumberGroupSeparator = ","

        
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Protected Overrides Sub Shutdown()
        Try
            Util.Log("Shutdown")

            If mMainTimer IsNot Nothing Then
                mMainTimer.Stop()
                RemoveHandler mMainTimer.Tick, AddressOf MainTimer_Tick
            End If

            Try
                If RenderServiceForHud IsNot Nothing Then

                    If mHud IsNot Nothing Then
                        mHud.Enabled = False
                        Host.Render.RemoveHud(mHud)
                        mHud.Dispose()
                        mHud = Nothing
                    End If

                    If mQuickSlotsHud IsNot Nothing Then
                        mQuickSlotsHud.Enabled = False
                        Host.Render.RemoveHud(mQuickSlotsHud)
                        mQuickSlotsHud.Dispose()
                        mQuickSlotsHud = Nothing
                    End If

                End If
            Catch ex As Exception

            End Try

            'RemoveHandler MyBase.GraphicsReset, AddressOf OnGraphicsReset

            If mCharconfig IsNot Nothing Then
                Util.SerializeObject(mCharConfigfilename, mCharconfig)
            End If

            If mGlobalInventory IsNot Nothing Then
                Util.SerializeObject(mWorldInventoryname, mGlobalInventory, "type='text/xsl' href='Inventory.xslt'")
            End If
            If mStorageInfo IsNot Nothing Then
                Util.SerializeObject(mWorldInventoryname & "storage", mStorageInfo)
            End If
            If mPluginConfig IsNot Nothing Then
                Util.SerializeObject(mPluginConfigfilename, mPluginConfig)
            End If

            If mWorldConfig IsNot Nothing Then
                Util.SerializeObject(mWorldConfigfilename, mWorldConfig)
            End If



            If Core.CharacterFilter IsNot Nothing Then
                RemoveHandler Core.CharacterFilter.LoginComplete, AddressOf OnCharacterFilterLoginCompleted
                RemoveHandler Core.CharacterFilter.ChangeOption, AddressOf OnCharacterFilterChangeOption
                RemoveHandler Core.CharacterFilter.StatusMessage, AddressOf OnCharacterStatusMessage
                RemoveHandler Core.CharacterFilter.ChangePortalMode, AddressOf OnCharacterChangePortalMode
            End If

            If Core.WorldFilter IsNot Nothing Then
                RemoveHandler Core.WorldFilter.CreateObject, AddressOf OnWorldFilterCreateObject
                RemoveHandler Core.WorldFilter.ReleaseObject, AddressOf OnWorldFilterDeleteObject
                RemoveHandler Core.WorldFilter.ResetTrade, AddressOf OnWorldfilterTradeReset
                RemoveHandler Core.WorldFilter.EndTrade, AddressOf OnWorldfilterTradeEnd
                RemoveHandler Core.WorldFilter.AddTradeItem, AddressOf OnWorldfilterAddTradeItem
                RemoveHandler Core.WorldFilter.EnterTrade, AddressOf OnWorldfilterTradeEnter
            End If

            If Not Core.HotkeySystem Is Nothing Then
                RemoveHandler Core.HotkeySystem.Hotkey, AddressOf ACHotkeys_Hotkey
            End If

            RemoveHandler Core.ContainerOpened, AddressOf OnOpenContainer
            mD3DService = Nothing


            GC.Collect()
            GC.WaitForPendingFinalizers()

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Protected Overrides Sub Startup()
        Try
            Util.Log("Startup")
            'AddHandler MyBase.GraphicsReset, AddressOf OnGraphicsReset
            If Core.CharacterFilter IsNot Nothing Then
                AddHandler Core.CharacterFilter.LoginComplete, AddressOf OnCharacterFilterLoginCompleted
                AddHandler Core.CharacterFilter.ChangeOption, AddressOf OnCharacterFilterChangeOption
                AddHandler Core.CharacterFilter.StatusMessage, AddressOf OnCharacterStatusMessage
                AddHandler Core.CharacterFilter.ChangePortalMode, AddressOf OnCharacterChangePortalMode


            End If
            AddHandler Core.ContainerOpened, AddressOf OnOpenContainer


            If Core.WorldFilter IsNot Nothing Then
                AddHandler Core.WorldFilter.ReleaseObject, AddressOf OnWorldFilterDeleteObject
                AddHandler Core.WorldFilter.CreateObject, AddressOf OnWorldFilterCreateObject
                AddHandler Core.WorldFilter.ResetTrade, AddressOf OnWorldfilterTradeReset
                AddHandler Core.WorldFilter.AddTradeItem, AddressOf OnWorldfilterAddTradeItem
                AddHandler Core.WorldFilter.EnterTrade, AddressOf OnWorldfilterTradeEnter
                AddHandler Core.WorldFilter.EndTrade, AddressOf OnWorldfilterTradeEnd
            End If

            FileService = Core.Filter(Of FileService)()

            RenderServiceForHud = Host.Render
            mBaseFontName = "Times New Roman"
            mBaseFontSize = 14
            mBaseFontweight = Wrappers.FontWeight.DoNotCare

            mListboxFontName = "Times New Roman"
            mListboxFontSize = 14
            mListboxFontWeight = Wrappers.FontWeight.DoNotCare

            mMainTimer = New System.Windows.Forms.Timer
            AddHandler mMainTimer.Tick, AddressOf MainTimer_Tick


            mD3DService = CType(Host.GetObject("services\D3DService.Service"), Decal.Interop.D3DService.ID3DService)

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub
    Private Function dcsxmlfile() As String
        Dim path As String
        Try ' first try to load original file
            Dim regKey As RegistryKey = Nothing

            regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Decal\Plugins\{5522f031-4d14-4bc1-a6f1-09a1fb36f90e}", False)
            If regKey IsNot Nothing Then
                path = CStr(regKey.GetValue("Path", String.Empty))
                path = IO.Path.Combine(path, "dcs.xml")

                If IO.File.Exists(path) Then
                    Return path
                Else


                End If
            End If
            ' 
        Catch ex As Exception

        End Try

        ' use distributed file
        path = IO.Path.Combine(Util.docPath, "dcs.xml")
        If IO.File.Exists(path) Then
            Return path
        End If

        Return String.Empty
    End Function

    Private Sub loadcolortable()
        Try
            Dim xmlfilename As String = dcsxmlfile()
            If System.IO.File.Exists(xmlfilename) Then
                Dim xdoc As XDocument = XDocument.Load(xmlfilename)

                For Each a In xdoc.Root.Element("colortable").Elements()
                    Dim id As Integer = 0

                    If Integer.TryParse(CStr(a.Attribute("id")), id) Then

                        If Not mColorStrings.ContainsKey(id) Then
                            mColorStrings.Add(id, CStr(a.Attribute("name")))
                        End If

                    End If

                Next

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

   


    Private mModelData As New Dictionary(Of Integer, modeldata)
    Private mColorStrings As New Dictionary(Of Integer, String)

    Private Sub OnCreateObject(ByVal pMsg As Decal.Adapter.Message)
        Try
            Dim Id As Integer = 0
            If Integer.TryParse(CStr(pMsg.Item("object")), Id) Then

                If Not mModelData.ContainsKey(Id) Then
                    Dim pGamedata As MessageStruct = CType(pMsg.Item("game"), MessageStruct)
                    Dim category As Integer = 0
                    Integer.TryParse(CStr(pGamedata.Item("category")), category)

                    'Armor = &H2
                    'Clothing = &H4
                    If (category And 6) <> 0 Then

                        Dim pModelData As MessageStruct = CType(pMsg.Item("model"), MessageStruct)
                        Dim paletteCount As Byte = 0
                        If Byte.TryParse(CStr(pModelData.Item("paletteCount")), paletteCount) Then
                            If paletteCount > 0 Then

                                Dim msg As String = String.Empty
                                Dim unknown As String = String.Empty
                                'Dim pcolors As New List(Of Integer)
                                Dim newmodel As New modeldata
                                newmodel.colors = New List(Of Integer)

                                Dim pVector As Decal.Adapter.MessageStruct = pModelData.Struct("palettes")
                                For i As Integer = 0 To pVector.Count - 1
                                    Dim pal As Integer = CInt(pVector.Struct(i).Item("palette"))
                                    If Not newmodel.colors.Contains(pal) Then  'remove duplicate colors
                                        newmodel.colors.Add(pal)
                                        Dim colorstring As String = CStr(pal)

                                        If mColorStrings.ContainsKey(pal) Then
                                            colorstring = mColorStrings.Item(pal) & "-" & pal
                                        Else
                                            unknown = "Unknown "
                                        End If

                                        If msg <> String.Empty Then
                                            msg &= "," & colorstring
                                        Else
                                            msg = colorstring
                                        End If
                                    End If

                                Next
                                newmodel.description = unknown & msg
                                mModelData.Add(Id, newmodel)

                            End If
                        End If

                    End If

                End If

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    Private Sub LoadfilesBackground(ByVal sender As System.Object, ByVal e As DoWorkEventArgs)
        Dim aerror As String = String.Empty
        Try
            If sender IsNot Nothing Then
                Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
                RemoveHandler worker.DoWork, AddressOf LoadfilesBackground
            End If


            If File.Exists(mPluginConfigfilename) Then
                Me.mPluginConfig = DirectCast(Util.DeSerializeObject(mPluginConfigfilename, GetType(PluginSettings)), PluginSettings)
            End If

            If mPluginConfig Is Nothing Then 'create default file
                mPluginConfig = New PluginSettings

                mPluginConfig.Shortcuts = New SDictionary(Of String, String)
                mPluginConfig.Shortcuts.Add("hr", "House Recall")
                mPluginConfig.Shortcuts.Add("mr", "House Mansion_Recall")
                mPluginConfig.Shortcuts.Add("ah", "Allegiance Hometown")
                mPluginConfig.Shortcuts.Add("ls", "Lifestone")
                mPluginConfig.Shortcuts.Add("mp", "Marketplace")
                mPluginConfig.Alerts = getbaseAlerts()
                mPluginConfig.AlertKeyMob = "Monster"
                mPluginConfig.AlertKeyPortal = "Portal"
                mPluginConfig.AlertKeySalvage = "Salvage"
                mPluginConfig.AlertKeyScroll = "Salvage"
                mPluginConfig.AlertKeyThropy = "Trophy"
                mPluginConfig.Alertwawfinished = "finished.wav"

            End If

            mplayer = New mediaplayer
            mplayer.Volume = mPluginConfig.wavVolume
            loadcolortable()


            If Not File.Exists(Util.docPath & "\GameData.xml") Then

                Plugin.GameData = New GameData
                Plugin.GameData.defaultfill()
                Util.SerializeObject(Util.docPath & "\GameData.xml", Plugin.GameData)

            End If

            If File.Exists(Util.docPath & "\GameData.xml") Then
                'updates
                Plugin.GameData = CType(Util.DeSerializeObject(Util.docPath & "\GameData.xml", GetType(GameData)), Alinco.GameData)


                If Plugin.GameData IsNot Nothing AndAlso Plugin.GameData.version <> 5 Then
                    If Not Plugin.GameData.SetStrings.ContainsKey(35) Then
                        Plugin.GameData.SetStrings.Add("Sigil of Defense Set", 35)
                    End If
                    If Not Plugin.GameData.SetStrings.ContainsKey(36) Then
                        Plugin.GameData.SetStrings.Add("Sigil of Destruction Set", 36)
                    End If
                    If Not Plugin.GameData.SetStrings.ContainsKey(37) Then
                        Plugin.GameData.SetStrings.Add("Sigil of Fury Set", 37)
                    End If
                    If Not Plugin.GameData.SetStrings.ContainsKey(38) Then
                        Plugin.GameData.SetStrings.Add("Sigil of Growth Set", 38)
                    End If
                    If Not Plugin.GameData.SetStrings.ContainsKey(39) Then
                        Plugin.GameData.SetStrings.Add("Sigil of Vigor Set", 39)
                    End If

                    Plugin.GameData.version = 5

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13CF) Then
                        Plugin.GameData.SpellData.Add("Minor Gear Craft Aptitude", &H13CF)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13CD) Then
                        Plugin.GameData.SpellData.Add("Major Gear Craft Aptitude", &H13CD)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13C4) Then
                        Plugin.GameData.SpellData.Add("Incantation of Gear Craft Mastery Other", &H13C4)
                    End If
                    If Not Plugin.GameData.RuleAppliesTo.ContainsKey(&H400000) Then
                        Plugin.GameData.RuleAppliesTo.Add("Scroll", &H400000)
                    End If

                    If Not Plugin.GameData.EquipSkill.ContainsKey(&H29) Then
                        Plugin.GameData.EquipSkill.Add("Two Handed Combat", &H29)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13AA) Then
                        Plugin.GameData.SpellData.Add("Epic Two Handed Combat Aptitude", &H13AA)
                    End If
                    If Not Plugin.GameData.SpellData.ContainsKey(&H13CE) Then
                        Plugin.GameData.SpellData.Add("Major Two Handed Combat Aptitude", &H13CE)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13A8) Then
                        Plugin.GameData.SpellData.Add("Incantation of Two Handed Combat Mastery Self", &H13A8)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13CE) Then
                        Plugin.GameData.SpellData.Add("Major Two Handed Combat Aptitude", &H13CE)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13E8) Then
                        Plugin.GameData.SpellData.Add("Two Handed Combat 6", &H13E8)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13EA) Then
                        Plugin.GameData.SpellData.Add("Incantation of Two Handed Combat Mastery Other", &H13EA)
                    End If

                    If Not Plugin.GameData.SpellData.ContainsKey(&H13F0) Then
                        Plugin.GameData.SpellData.Add("Two Handed Combat Mastery Self 6", &H13F0)
                    End If

                    Util.SerializeObject(Util.docPath & "\GameData.xml", Plugin.GameData)
                End If
            End If


            mWorldConfigfilename = Util.docPath & "\" & Util.normalizePath(Core.CharacterFilter.Server)

            If Not Directory.Exists(mWorldConfigfilename) Then
                Directory.CreateDirectory(mWorldConfigfilename)
            End If

            mExportInventoryname = Util.docPath & "\Inventory"
            If Not Directory.Exists(mExportInventoryname) Then
                Directory.CreateDirectory(mExportInventoryname)
                writebasexslt(mExportInventoryname & "\Inventory.xslt")
            End If

            mExportInventoryname &= "\" & Core.CharacterFilter.Server & ".xml"
            mWorldInventoryname = Util.docPath & "\" & Util.normalizePath(Core.CharacterFilter.Server) & "\Inventory.xml"
            If File.Exists(mWorldInventoryname) Then
                Dim tobj As Object = Util.DeSerializeObject(mWorldInventoryname, GetType(SDictionary(Of Integer, InventoryItem)))
                If tobj IsNot Nothing Then
                    mGlobalInventory = CType(tobj, Global.Alinco.SDictionary(Of Integer, Global.Alinco.InventoryItem))

                    mStorageInfo = CType(Util.DeSerializeObject(mWorldInventoryname & "storage", GetType(SDictionary(Of Integer, String))), Global.Alinco.SDictionary(Of Integer, String))

                Else
                    mGlobalInventory = New SDictionary(Of Integer, InventoryItem)
                End If
            Else
                writebasexslt(Util.docPath & "\" & Util.normalizePath(Core.CharacterFilter.Server) & "\Inventory.xslt")
            End If

            mCharConfigfilename = mWorldConfigfilename
            mWorldConfigfilename &= "\Settings.xml"
            If File.Exists(mWorldConfigfilename) Then
                mWorldConfig = CType(Util.DeSerializeObject(mWorldConfigfilename, GetType(WorldSettings)), WorldSettings)
            End If

            mCharConfigfilename &= "\" & Util.normalizePath(Core.CharacterFilter.Name) & ".xml"
            If File.Exists(mCharConfigfilename) Then
                mCharconfig = CType(Util.DeSerializeObject(mCharConfigfilename, GetType(CharSettings)), CharSettings)
            End If

            If mWorldConfig Is Nothing Then
                mWorldConfig = New WorldSettings
            End If

            If mCharconfig Is Nothing Then
                mCharconfig = New CharSettings
            End If

            mProtectedCorpses.Add("Corpse of " & Core.CharacterFilter.Name)

            mFilesLoaded = True

        Catch ex As Exception
            aerror = ex.Message & ex.StackTrace
            Util.ErrorLogger(ex)
        End Try

        If aerror <> String.Empty Then
            Util.bcast(aerror)
        End If


    End Sub

    Private Sub initializeView()
        Try
            HooksForErrorHandler = Host.Actions
            cboAlertSound.Clear()

            If Directory.Exists(Util.wavPath) Then
                Dim wavfiles = From f In Directory.GetFiles(Util.wavPath) Order By f

                If Not wavfiles Is Nothing Then
                    For Each w As String In wavfiles
                        Dim wfile As String = System.IO.Path.GetFileName(w)
                        cboAlertFinished.Add(wfile, w)
                        cboAlertSound.Add(wfile, w)
                    Next
                End If
            Else
                Directory.CreateDirectory(Util.wavPath)
            End If

            addOption(mPluginConfig.Showpalette, eOtherOptions.Use_DCS_Color_palette_xml)
            addOption(mPluginConfig.Showhud, eOtherOptions.Show_Hud)
            addOption(mPluginConfig.Showhudvulns, eOtherOptions._Display_Vuln_Icons)
            addOption(mPluginConfig.Showhudcorpses, eOtherOptions._Display_Corpses)

            addOption(mCharconfig.ShowhudQuickSlots, eOtherOptions.Show_Quickslots_Hud)

            addOption(mPluginConfig.OutputManualIdentify, eOtherOptions.Show_Info_on_Identify)
            addOption(mPluginConfig.CopyToClipboard, eOtherOptions._Copy_To_Clipboard)
            'addOption(mPluginConfig.OutputManualIgnoreSelf, eOtherOptions._Ignore_Wielded_Items)
            'addOption(mPluginConfig.OutputManualIgnoreMobs, eOtherOptions._Ignore_Mobs)
            addOption(mPluginConfig.worldbasedsalvage, eOtherOptions.World_Based_Salvaging_Profile)
            addOption(mPluginConfig.worldbasedrules, eOtherOptions.World_Based_Rules_Profile)
            addOption(mCharconfig.usesalvageprofile, eOtherOptions.Character_Salvaging_Profile)
            addOption(mCharconfig.uselootprofile, eOtherOptions.Character_Loot_Profile)
            addOption(mCharconfig.usemobsprofile, eOtherOptions.Character_Mobs_Profile)
            addOption(mPluginConfig.SalvageHighValue, eOtherOptions.Salvage_High_Value_Items)
            addOption(mPluginConfig.FilterChatMeleeEvades, eOtherOptions.Filter_Melee_Evades)
            addOption(mPluginConfig.FilterChatResists, eOtherOptions.Filter_Resists)
            addOption(mPluginConfig.FilterSpellcasting, eOtherOptions.Filter_Spellcasting)
            addOption(mPluginConfig.FilterSpellsExpire, eOtherOptions.Filter_Spells_Expire)
            addOption(mPluginConfig.FilterTellsMerchant, eOtherOptions.Filter_Vendor_Tells)
            addOption(mPluginConfig.D3DMark0bjects, eOtherOptions.Show_3D_Arrow)
            addOption(mPluginConfig.MuteAll, eOtherOptions.Mute)
            addOption(mPluginConfig.AutoStacking, eOtherOptions.Auto_Stacking)
            addOption(mPluginConfig.AutoPickup, eOtherOptions.Auto_Pickup)
            addOption(mPluginConfig.AutoUst, eOtherOptions.Auto_Ust)
            '     addOption(mPluginConfig.AutoUstOnCloseCorpse, eOtherOptions._Salvage_When_Closing_Corpse)

            addOption(mPluginConfig.WindowedFullscreen, eOtherOptions.Windowed_Fullscreen)

            addNotifyOption(mPluginConfig.notifycorpses, eNotifyOptions.Notify_Corpses)
            addNotifyOption(mPluginConfig.showallcorpses, eNotifyOptions._Show_All_Corpses)
            addNotifyOption(mPluginConfig.NotifyPortals, eNotifyOptions.Notify_On_Portals)
            addNotifyOption(mPluginConfig.unknownscrolls, eNotifyOptions.Detect_Unknows_Scrolls_Lv7)
            addNotifyOption(mPluginConfig.trainedscrollsonly, eNotifyOptions._Trained_Schools_Only)
            addNotifyOption(mPluginConfig.unknownscrollsAll, eNotifyOptions._All_Levels)
            addNotifyOption(mCharconfig.detectscrollsontradebot, eNotifyOptions._Detect_on_Tradebot)
            addNotifyOption(mCharconfig.ShowAllPlayers, eNotifyOptions.Notify_All_Players)
            addNotifyOption(mCharconfig.ShowAllMobs, eNotifyOptions.Notify_All_Mobs)
            addNotifyOption(mCharconfig.useglobalspellbook, eNotifyOptions._Use_Global_Spellbook)
            addNotifyOption(mPluginConfig.notifyalleg, eNotifyOptions.Notify_Allegiance_Members)
            addNotifyOption(mPluginConfig.notifytells, eNotifyOptions.Notify_On_Tell_Recieved)

            txtmaxmana.Text = CStr(mPluginConfig.notifyItemmana)
            txtvbratio.Text = CStr(mPluginConfig.notifyValueBurden)
            txtmaxvalue.Text = CStr(mPluginConfig.notifyItemvalue)
            txtsettingscw.Text = CStr(mPluginConfig.chattargetwindow)
            chksalvageAll.Checked = mPluginConfig.SalvageHighValue
            loadsalvage()
            LoadThropyList()
            LoadMobsList()
            loadRules()

            populateRulesView()
            populateAlertView()
            If cboAlertEdit.Count > 0 Then
                cboAlertEdit.Selected = 0
            End If
           
            txtsalvageaug.Text = CStr(mCharconfig.salvageaugmentations)
            If mPluginConfig.Shortcuts Is Nothing Then
                mPluginConfig.Shortcuts = New SDictionary(Of String, String)
                mPluginConfig.Shortcuts.Add("hr", "House Recall")
                mPluginConfig.Shortcuts.Add("mr", "House Mansion_Recall")
                mPluginConfig.Shortcuts.Add("ah", "Allegiance Hometown")
                mPluginConfig.Shortcuts.Add("ls", "Lifestone")
                mPluginConfig.Shortcuts.Add("mp", "Marketplace")
            End If

            If Not Core.HotkeySystem Is Nothing Then
                AddHandler Core.HotkeySystem.Hotkey, AddressOf ACHotkeys_Hotkey
                setupHotkey("Alinco3", "alinco3:useloadust", "Use Ust and load salvage panel ")
                setupHotkey("Alinco3", "alinco3:salvage", "Presses the salvage button")
                setupHotkey("Alinco3", "alinco3:lootitem", "Loots the first matched item or opens corpse")
                setupHotkey("Alinco3", "alinco3:healself", "Use healkit")
                setupHotkey("Alinco3", "alinco3:givenpc", "Gives one item to the selected npc")
                setupHotkey("Alinco3", "alinco3:onclickust", "load ust and salvage")
                setupHotkey("Alinco3", "alinco3:onoff", "Toggle on off")

                setupHotkey("Alinco3", "alinco3:targetmob", "Select nearest mob, double click for next target")
                setupHotkey("Alinco3", "alinco3:targetimp", "switch")
                setupHotkey("Alinco3", "alinco3:targetvuln", "switch")
            End If

            Try
                tryloadAlincoBuffs()
            Catch ex As Exception
                '      Util.ErrorLogger(ex)
            End Try

            If mPluginConfig.WindowedFullscreen Then
                WinApe.WindowedFullscreen(Host.Decal.Hwnd)
            End If

            mFreeMainPackslots = CountFreeSlots(Core.CharacterFilter.Id)

            Lostfocus = Not WinApe.GetActiveWindow.Equals(Host.Decal.Hwnd)
            mXPH = String.Empty
            mXPChange = String.Empty

            mXPStart = Core.CharacterFilter.TotalXP
            mXPStartTime = Now
            sldVolume.SliderPostition = mPluginConfig.wavVolume
            If mCharconfig.quickslots Is Nothing Then
                mCharconfig.quickslots = New Alinco.SDictionary(Of Integer, QuickSlotInfo)
            End If

            mCurrentContainer = 0

            'auto xp track
            If mCharconfig.trackobjectxpHudId <> 0 Then
                If Core.WorldFilter.Item(mCharconfig.trackobjectxpHudId) Is Nothing Then
                    mCharconfig.trackobjectxpHudId = 0
                Else
                    wtcw("xp tracking is on: " & Core.WorldFilter.Item(mCharconfig.trackobjectxpHudId).Name)
                    Host.Actions.RequestId(mCharconfig.trackobjectxpHudId)
                End If
            End If

            If Core.CharacterFilter.Level >= 275 And mCharconfig.trackobjectxpHudId = 0 Then
                For Each wo As WorldObject In Core.WorldFilter.GetInventory
                    With wo
                        If .Values(LongValueKey.EquippedSlots) <> 0 AndAlso .Name = "Aetheria" Then
                            Host.Actions.RequestId(.Id)
                        End If
                    End With
                Next
            End If

        Catch ex As Exception

        End Try
    End Sub

    Private Sub startworker()
        mBackgroundworker = New BackgroundWorker
        mBackgroundworker.WorkerSupportsCancellation = True
        AddHandler mBackgroundworker.DoWork, AddressOf LoadfilesBackground
        mBackgroundworker.RunWorkerAsync()
    End Sub

    Private Sub OnCharacterFilterLoginCompleted(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            mBaseFontColor = Color.White

            Util.Log("OnCharacterFilterLoginCompleted")

            If String.IsNullOrEmpty(Core.CharacterFilter.Server) Then
                wtcw("Core.CharacterFilter.Server name not set")
                mPaused = True
                Return
            End If

            If String.IsNullOrEmpty(Core.CharacterFilter.Name) Then
                wtcw("Core.CharacterFilter.Name not set")
                mPaused = True
                Return
            End If
            mFilesLoaded = False

            mMainTimer.Interval = 500
            mMainTimer.Start()

            startworker()

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub resetprofiles()
        Try
            mPluginConfig.Alerts = getbaseAlerts()
            If mCharconfig.uselootprofile Then
                mCharconfig.ThropyList = getbaseThropyList()
                mActiveThropyProfile = mCharconfig.ThropyList
            Else
                mPluginConfig.ThropyList = getbaseThropyList()
                mActiveThropyProfile = mPluginConfig.ThropyList
            End If

            If mCharconfig.usemobsprofile Then
                mCharconfig.MobsList = getbaseMobsList()
                mActiveMobProfile = mCharconfig.MobsList
            Else
                mPluginConfig.MobsList = getbaseMobsList()
                mActiveMobProfile = mPluginConfig.MobsList
            End If

            If mCharconfig.usesalvageprofile Then
                mCharconfig.SalvageProfile = getbaseSalvageProfile()
                mActiveSalvageProfile = mCharconfig.SalvageProfile
            ElseIf mPluginConfig.worldbasedsalvage Then
                mWorldConfig.SalvageProfile = getbaseSalvageProfile()
                mActiveSalvageProfile = mWorldConfig.SalvageProfile
            Else
                mPluginConfig.SalvageProfile = getbaseSalvageProfile()

                mActiveSalvageProfile = mPluginConfig.SalvageProfile
            End If

            If mPluginConfig.worldbasedrules Then
                mWorldConfig.Rules = getbaseRules()
                mActiveRulesProfile = mWorldConfig.Rules
            Else
                mPluginConfig.Rules = getbaseRules()
                mActiveRulesProfile = mPluginConfig.Rules
            End If

        Catch ex As Exception

        End Try
    End Sub

    Private mCollTradeItemRecieved As New Dictionary(Of Integer, Integer)
    Private mTradeWindowOpen As Boolean
    Private Sub OnWorldfilterTradeReset(ByVal sender As Object, ByVal e As Wrappers.ResetTradeEventArgs)
        Try
            mCollTradeItemRecieved.Clear()

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnWorldfilterAddTradeItem(ByVal sender As Object, ByVal e As Wrappers.AddTradeItemEventArgs)
        Try
            If e.SideId = 2 Then
                If Not mCollTradeItemRecieved.ContainsKey(e.ItemId) Then
                    mCollTradeItemRecieved.Add(e.ItemId, 0)
                End If

                If Paused Then Return

                Dim wo As WorldObject = Core.WorldFilter.Item(e.ItemId)
                If wo IsNot Nothing Then
                    Select Case wo.ObjectClass
                        Case ObjectClass.Armor, ObjectClass.Clothing, ObjectClass.Jewelry, ObjectClass.MeleeWeapon, ObjectClass.WandStaffOrb, ObjectClass.MissileWeapon
                            IdqueueAdd(wo.Id)
                    End Select
                End If
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnWorldfilterTradeEnd(ByVal sender As Object, ByVal e As Wrappers.EndTradeEventArgs)
        Try
            mCollTradeItemRecieved.Clear()
            mTradeWindowOpen = False
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnWorldfilterTradeEnter(ByVal sender As Object, ByVal e As Wrappers.EnterTradeEventArgs)
        Try
            mCollTradeItemRecieved.Clear()
            mTradeWindowOpen = True
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub tryloadAlincoBuffs()
        Try
            malincobuffs = Alinco3Buffs.Plugin.Instance
            malincobuffsAvailable = True
            Return
        Catch ex As Exception
            '  Util.ErrorLogger(ex)
        End Try
        malincobuffsAvailable = False
    End Sub

    <BaseEvent("ChatNameClicked")> _
    Private Sub Plugin_ChatNameClicked(ByVal sender As Object, ByVal e As Decal.Adapter.ChatClickInterceptEventArgs)
        Try


            Select Case e.Id
                Case NOTIFYLINK_ID

                    Dim Itemid As Integer = CInt(e.Text)
                    If mHudlistboxItems.ContainsKey(Itemid) Then
                        huditemclick(True, CType(mHudlistboxItems.Item(Itemid), Global.Alinco.Plugin.notify), True)
                    End If
                    e.Eat = True

                Case Util.ERRORLINK_ID

                    Dim url As String
                    url = Util.docPath & "\Errors.txt"

                    If File.Exists(url) Then

                        Dim myProcess As System.Diagnostics.Process = New System.Diagnostics.Process
                        myProcess.StartInfo.FileName = "notepad.exe"
                        myProcess.StartInfo.Arguments = url
                        myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
                        myProcess.Start()

                    End If
                    e.Eat = True
            End Select

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Function actornamefromtell(ByVal tell As String) As String
        Dim actor As String = String.Empty

        If Not String.IsNullOrEmpty(tell) Then
            Dim pos1, pos2 As Integer
            Dim cmd As String
            pos1 = tell.IndexOf(" tells you, ")
            If pos1 > 1 Then

                cmd = tell.Substring(pos1 + 12)
                cmd = Replace(cmd, """", "")

                pos2 = tell.IndexOf(">")
                If pos2 > 0 Then
                    Dim strTemp As String = tell.Substring(pos2 + 1)

                    pos2 = strTemp.IndexOf("<")
                    If pos2 > 0 Then
                        strTemp = strTemp.Substring(0, pos2)
                        Util.Log(":" & strTemp)
                        actor = strTemp
                    End If
                Else 'vendor / npc chat
                    Dim strTemp As String = tell.Substring(0, pos1)

                    actor = strTemp
                End If
            End If
        End If
        Return actor
    End Function

    <BaseEvent("ChatBoxMessage")> _
    Private Sub Plugin_ChatBoxMessage(ByVal sender As Object, ByVal e As Decal.Adapter.ChatTextInterceptEventArgs)
        Try
            If mCharconfig IsNot Nothing Then

                Dim msg As String = Left(e.Text, e.Text.Length - 1) 'strip linefeed
                Dim pos As Integer

                Select Case e.Color
                    Case 0, 24
                        If msg.EndsWith("This permission will last one hour.") Then

                            pos = msg.IndexOf(" has given you permission to loot")
                            If pos >= 0 Then
                                Dim playercorpse As String = "Corpse of " & msg.Substring(0, pos)

                                If Not mProtectedCorpses.Contains(playercorpse) Then
                                    wtcw("Alinco added " & playercorpse & " to protected corpses.", 0)
                                    mProtectedCorpses.Add(playercorpse)
                                End If

                            End If
                        ElseIf msg.EndsWith("DTLN") Then
                            msg &= "= Air (West)"
                            wtcw(msg, e.Color)
                            e.Eat = True
                        ElseIf msg.EndsWith("DBTNK") Then
                            msg &= " = Water (South)"
                            wtcw(msg, e.Color)
                            e.Eat = True
                        ElseIf msg.EndsWith("NTLN") Then
                            msg &= " = Fire (North)"
                            wtcw(msg, e.Color)
                            e.Eat = True
                        ElseIf msg.EndsWith("ZTNK") Then
                            msg &= " = Earth (East)"
                            wtcw(msg, e.Color)
                            e.Eat = True
                        End If
                    Case 7 'spellcasting
                        If mPluginConfig.FilterSpellcasting AndAlso msg.StartsWith("The spell") Then
                            e.Eat = True
                        ElseIf mPluginConfig.FilterSpellsExpire AndAlso msg.EndsWith("has expired.") Then
                            e.Eat = True ' resists your spell
                        ElseIf mPluginConfig.FilterChatResists AndAlso msg.StartsWith("You resist the spell") Then
                            e.Eat = True

                        End If
                    Case 17
                        If msg.IndexOf("Cruath Quareth") >= 0 Then
                            mSpellwords = "Cruath Quareth"
                        ElseIf msg.IndexOf("Cruath Quasith") >= 0 Then
                            mSpellwords = "Cruath Quasith"
                        ElseIf msg.IndexOf("Equin Opaj") >= 0 Then
                            mSpellwords = "Equin Opaj"
                        ElseIf msg.IndexOf("Equin Ozael") >= 0 Then
                            mSpellwords = "Equin Ozael"
                        ElseIf msg.IndexOf("Equin Ofeth") >= 0 Then
                            mSpellwords = "Equin Ofeth"
                        ElseIf mPluginConfig.FilterSpellcasting Then
                            If msg.StartsWith("The spell") Then
                                e.Eat = True
                            ElseIf msg.StartsWith("You say, ") Then
                                e.Eat = True
                            ElseIf msg.IndexOf("says,""") > 0 Then
                                e.Eat = True
                            End If
                        End If

                    Case 21 'melee evades
                        If mPluginConfig.FilterChatMeleeEvades Then
                            If msg.IndexOf("You evaded") >= 0 Then
                                e.Eat = True
                            End If
                        End If
                    Case 3
                        If mPluginConfig.FilterTellsMerchant Or mPluginConfig.notifytells Then
                            Dim actorname As String = actornamefromtell(msg)

                            Dim cursel As WorldObject = Core.WorldFilter.Item(Host.Actions.CurrentSelection)
                            Dim actors As WorldObjectCollection = Core.WorldFilter.GetByName(actorname)

                            Dim vendorTell As Boolean = False
                            Dim npcTell As Boolean = False

                            For Each x As WorldObject In actors 'multipe actors possible?
                                If x.ObjectClass = ObjectClass.Npc Then
                                    npcTell = True
                                ElseIf x.ObjectClass = ObjectClass.Vendor Then
                                    vendorTell = True
                                End If
                            Next

                            If mPluginConfig.FilterTellsMerchant AndAlso vendorTell Then

                                e.Eat = True
                                Return
                            End If

                            If mPluginConfig.notifytells And Not vendorTell And Not npcTell Then

                                If cursel Is Nothing OrElse ((Not (msg.IndexOf(cursel.Name) >= 0))) Then

                                    PlaySoundFile("rcvtell.wav", mPluginConfig.wavVolume)

                                End If

                            End If

                        End If
                End Select
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Const AC_SET_OBJECT_LINK As Integer = &H2DA
    Private Const AC_GAME_EVENT As Integer = &HF7B0
    Private Const GE_SETPACK_CONTENTS As Integer = &H196
    Private Const GE_IDENTIFY_OBJECT As Integer = &HC9
    Private Const AC_ADJUST_STACK As Integer = &H197
    Private Const AC_APPLY_VISUALSOUND As Integer = &HF755
    Private Const AC_CREATE_OBJECT As Integer = &HF745
    Private Const AC_SET_OBJECT_DWORD As Integer = &H2CE

    <BaseEvent("ServerDispatch")> _
       Private Sub Plugin_ServerDispatch(ByVal sender As Object, ByVal e As Decal.Adapter.NetworkMessageEventArgs)
        Try
            Select Case e.Message.Type
                Case AC_ADJUST_STACK
                    OnSetStack(e.Message)
                Case AC_APPLY_VISUALSOUND
                    OnAppyVisualSound(e.Message)
                Case AC_CREATE_OBJECT
                    OnCreateObject(e.Message)
                Case AC_SET_OBJECT_LINK
                    OnSetObjectLink(e.Message)
                Case AC_GAME_EVENT
                    Dim iEvent As Integer
                    iEvent = 0

                    Try
                        iEvent = CInt(e.Message.Item("event"))  'used to crash sometimes
                    Catch ex As Exception

                    End Try

                    Select Case iEvent
                        Case GE_SETPACK_CONTENTS
                            OnSetPackContents(e.Message)
                        Case GE_IDENTIFY_OBJECT
                            OnIdentObject(e.Message)
                    End Select
                    'Case AC_SET_OBJECT_DWORD
                    '    OnSetObjectWord(e.Message)
            End Select
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

  

    'Private Sub OnSetObjectWord(ByVal pMsg As Decal.Adapter.Message)
    '    If mFilesLoaded Then

    '        Dim Id As Integer = CInt(pMsg.Item("object"))
    '        Dim objItem As WorldObject = Nothing

    '        objItem = Core.WorldFilter.Item(Id)
    '        If objItem Is Nothing Then
    '            Return
    '        End If

    '        Dim key As Integer = CInt(pMsg.Item("key"))
    '        Dim value As Integer = CInt(pMsg.Item("value"))
    '        If objItem.ObjectClass = Decal.Adapter.Wrappers.ObjectClass.Gem Then
    '            Dim t As Integer = objItem.Values(LongValueKey.EquipableSlots)
    '            If t = &H10000000 OrElse t = &H20000000 OrElse t = &H40000000 Then 'Aetheria
    '                ' result &= "(" & AetheMaxItemLevel & ")" & SetString() & WieldString() & SpellDescriptions()
    '                wtcw(objItem.Name & " key " & Hex(key) & " value " & value)
    '            End If
    '        End If
    '    End If

    'End Sub

    Private mSpellwords As String = String.Empty
    Private mMobs As New Dictionary(Of Integer, Mobdata)
    Private Sub OnAppyVisualSound(ByVal pMsg As Decal.Adapter.Message)
        Dim guid As Integer
        guid = CInt(pMsg.Item("object"))
        If guid <> Core.CharacterFilter.Id Then
            Dim oWo As Decal.Adapter.Wrappers.WorldObject = Nothing
            oWo = Core.WorldFilter.Item(guid)
            If oWo IsNot Nothing AndAlso oWo.Category = 16 Then
                Dim effect As Integer = CInt(pMsg.Item("effect"))
                If effect = 23 OrElse effect = 38 OrElse effect = 44 OrElse effect = 46 _
                  OrElse effect = 48 OrElse effect = 50 OrElse effect = 52 OrElse effect = 54 OrElse effect = 56 Then

                    If Not mMobs.ContainsKey(oWo.Id) Then
                        mMobs.Add(oWo.Id, New Mobdata)
                    End If

                    Dim bo As Mobdata
                    bo = mMobs.Item(oWo.Id)
                    If bo IsNot Nothing Then
                        bo.UpdateEffect(effect, mSpellwords)
                    End If

                End If

            End If
        End If
    End Sub

    Private mmanualstackname As String = String.Empty
    Private Sub OnSetStack(ByVal pMsg As Decal.Adapter.Message)
        Dim id, stack, value As Integer
        id = CInt(pMsg.Item("item"))
        stack = CInt(pMsg.Item("count"))
        value = CInt(pMsg.Item("value"))
        Dim obj As WorldObject = Core.WorldFilter.Item(id)
        If obj IsNot Nothing Then
            Util.Log("OnSetStack " & Hex(id) & " " & obj.Name)
            If obj.Container = Core.CharacterFilter.Id AndAlso obj.Values(LongValueKey.Slot) <> -1 Then
                '  wtcw2("Manualstack " & obj.Name & " stack " & stack & " wostat " & obj.Values(LongValueKey.StackCount))
                mmanualstackname = obj.Name
            End If
        End If
    End Sub
    Private Sub wtcw(ByVal msg As String, Optional ByVal color As Integer = 14)
        Try
            Host.Actions.AddChatText(msg, color, mPluginConfig.chattargetwindow)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub


    <Conditional("DEBUGTEST")> _
    Private Sub wtcwd(ByVal msg As String, Optional ByVal color As Integer = 13)
        Try
            Host.Actions.AddChatText(msg, color, 2)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private mColStacker As New Hashtable
    'move object to container or equipper
    Private Sub OnSetObjectLink(ByVal pMsg As Decal.Adapter.Message)
        Try
            If mPluginConfig Is Nothing OrElse mCharconfig Is Nothing Then
                Return
            End If
            Dim Id, key As Integer
            Id = CInt(pMsg.Item("object"))



            Dim obj As WorldObject = Core.WorldFilter.Item(Id)

            If obj IsNot Nothing Then
                key = CInt(pMsg.Item("key"))

                If key = 2 Then ' move to container

                    If mNotifiedItems.ContainsKey(Id) Then

                        Dim dn As notify = CType(mNotifiedItems.Item(Id), Global.Alinco.Plugin.notify)
                        If dn.scantype = eScanresult.salvage OrElse (dn.scantype = eScanresult.value AndAlso mPluginConfig.SalvageHighValue) Then
                            Dim wo As WorldObject = Core.WorldFilter.Item(Id)
                            If wo IsNot Nothing Then
                                With wo
                                    If Not mUstItems.ContainsKey(.Id) Then 'add to ustlist

                                        If CInt(.Values(DoubleValueKey.SalvageWorkmanship)) > 0 Then

                                            Dim newinfo As salvageustinfo = getsalvageinfo(.Id, .Name, _
                                             CInt(.Values(DoubleValueKey.SalvageWorkmanship)), .Values(LongValueKey.Material), .Values(LongValueKey.UsesRemaining), False)
                                            mUstItems.Add(.Id, newinfo)
                                            addToUstList(.Name, .Id, dn.description)

                                        End If

                                    End If
                                End With

                            End If


                        End If


                        mNotifiedItems.Remove(Id)
                    End If
                    If mPluginConfig.AutoStacking Then
                        If Not String.IsNullOrEmpty(mmanualstackname) And mmanualstackname = obj.Name Then
                            'wtcw2("block stacking ")
                        Else
                            If obj.Values(LongValueKey.StackMax) > obj.Values(LongValueKey.StackCount) Then
                                If Not mColStacker.ContainsKey(obj.Id) Then
                                    'wtcw2(" add to stacking queue")
                                    mColStacker.Add(obj.Id, obj.Name)
                                End If

                            End If
                        End If
                    End If


                End If

            End If

            If mNotifiedItems.ContainsKey(Id) Then
                mNotifiedItems.Remove(Id)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    Private Function FindStackableItemGuidbyName(ByVal ItemToStackName As String, ByVal ItemToStackId As Integer) As Integer
        Dim packid As Integer = Core.CharacterFilter.Id
        Dim wocol As WorldObjectCollection = Core.WorldFilter.GetByName(ItemToStackName)
        For Each obj As WorldObject In wocol
            If IsItemInInventory(obj) Then
                If obj.Id <> ItemToStackId Then ' self
                    If obj.Name = ItemToStackName Then
                        If obj.Values(LongValueKey.StackMax) > 1 Then ' stackable
                            If obj.Values(LongValueKey.StackCount) < obj.Values(LongValueKey.StackMax) Then
                                Return obj.Id
                            ElseIf obj.Container <> packid Then
                                packid = obj.Container
                            End If
                        End If
                    End If
                End If
            End If
        Next

        If packid <> Core.CharacterFilter.Id Then
            Return packid
        End If

        Return 0
    End Function

    Private Function Stacking() As Boolean

        If mColStacker.Count > 0 Then
            ' wtcw2("Trystacking 1")
            Dim removeid As Integer = 0
            Dim objectname As String = String.Empty

            For Each xs As DictionaryEntry In mColStacker
                removeid = CInt(xs.Key)
                objectname = CStr(xs.Value)
                Exit For
            Next

            mColStacker.Remove(removeid)
            ' wtcw2("Trystacking " & Hex(removeid) & " " & objectname)
            Dim x As Integer = FindStackableItemGuidbyName(objectname, removeid)
            If x <> 0 Then
                Host.Actions.MoveItem(removeid, x)
                Return True
            End If
        Else
            mmanualstackname = String.Empty
        End If

    End Function

    Private Sub OnOpenContainer(ByVal sender As Object, ByVal e As Decal.Adapter.ContainerOpenedEventArgs)
        Try
            If Paused Then Return

            If e.ItemGuid = 0 Then
                wtcwd("OnCloseContainer ")
                mCorpsScanning = False
                'todo check if valid to just clear them
                'it is verry possible to close the container while the scanprocess is bussy
                If mNotifiedCorpses.ContainsKey(mCurrentContainer) Then
                    mNotifiedCorpses.Remove(mCurrentContainer)
                    wtcwd("OnCloseContainer  removed , wierd missed open event")
                    Renderhud()
                End If
                mCurrentContainer = 0
                If mCurrentContainerContent IsNot Nothing Then
                    mCurrentContainerContent.Clear()
                End If

                If mPluginConfig.AutoPickup Or mPluginConfig.AutoUst Then
                    mFreeMainPackslots = CountFreeSlots(Core.CharacterFilter.Id)
                End If
            Else
                mwaitonopen = False
                If mNotifiedCorpses.ContainsKey(e.ItemGuid) Then
                    mCorpsScanning = finishedscanning()
                    mNotifiedCorpses.Remove(e.ItemGuid)
                    wtcwd("OnCloseContainer removed")

                    Renderhud()
                End If
                Util.Log("OnOpenContainer " & Hex(e.ItemGuid))
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub setupHotkey(ByVal sPlugin As String, ByVal sTitle As String, ByVal sDescription As String)
        Try
            If Not Core.HotkeySystem.Exists(sTitle) Then

                Core.HotkeySystem.AddHotkey(sPlugin, sTitle, sDescription)

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    'opening a pack,container,chest,.. also inventory packs at startup
    'or moving packs in inventory
    Private Sub OnSetPackContents(ByVal pMsg As Decal.Adapter.Message)
        Try
            If Paused OrElse mCharconfig Is Nothing Then Return
            If Host.Actions.VendorId <> 0 Then
                Return
            End If

            Dim pItems As Decal.Adapter.MessageStruct
            Dim pItem As Decal.Adapter.MessageStruct
            Dim ItemId As Integer
            Dim itemcount As Integer
            mCurrentContainer = CInt(pMsg.Item("container"))
            itemcount = CInt(pMsg.Item("itemCount"))

            Dim objContainer As WorldObject = Core.WorldFilter.Item(mCurrentContainer)


            If objContainer IsNot Nothing AndAlso mPluginConfig.PackOrCorpseOrChestExclude IsNot Nothing Then
                For Each s As String In mPluginConfig.PackOrCorpseOrChestExclude
                    If objContainer.Name = s Then
                        mCurrentContainer = 0
                        Return
                    End If
                Next
            End If
            mCurrentContainerContent = New Dictionary(Of Integer, Integer)

            Dim msg As String = String.Empty
            Dim skipContainer As Boolean
            If objContainer IsNot Nothing Then
                msg = objContainer.Name

                If objContainer.Id = Core.CharacterFilter.Id OrElse objContainer.Container = Core.CharacterFilter.Id Then
                    msg &= " skip player pack" 'moving/adding packs in inventory
                    skipContainer = True
                ElseIf objContainer.Container <> 0 Then
                    Dim checkcontainerincontainer As WorldObject = Core.WorldFilter.Item(objContainer.Container)
                    If checkcontainerincontainer IsNot Nothing Then
                        If checkcontainerincontainer.Container = Core.CharacterFilter.Id Then
                            msg &= " skip container in container storage"
                            skipContainer = True
                        End If
                    End If
                End If

                If skipContainer Then
                    Util.Log("OnSetPackContents " & Hex(mCurrentContainer) & " " & msg)
                    mCurrentContainer = 0
                    Return
                End If
            End If

            Util.Log("OnSetPackContents " & Hex(mCurrentContainer) & " " & msg)

            'fill mCurrentContainerContent
            Dim slot As Integer = 0
            If itemcount > 0 Then
                pItems = pMsg.Struct("items")
                For i As Integer = 0 To pItems.Count - 1
                    pItem = pItems.Struct(i)
                    ItemId = CInt(pItem.Item("item"))

                    If Not mCurrentContainerContent.ContainsKey(ItemId) Then
                        mCurrentContainerContent.Add(ItemId, 0)
                        Dim objItem As WorldObject = _
                         Core.WorldFilter.Item(ItemId)
                        If objItem IsNot Nothing AndAlso mCharconfig IsNot Nothing Then
                            Dim result As eScanresult = CheckObjectForMatch(New IdentifiedObject(objItem), False)
                            'check scan finished

                            If result <> eScanresult.needsident Then
                                mCurrentContainerContent.Item(ItemId) = 1
                            Else
                                mCorpsScanning = True
                            End If
                        End If

                    End If

                Next
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnCharacterChangePortalMode(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.ChangePortalModeEventArgs)
        If e.Type = PortalEventType.EnterPortal Then
            mInportalSpace = True
        Else
            mInportalSpace = False
            removeoutofrange()
        End If

    End Sub

    Private Sub OnCharacterStatusMessage(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.StatusMessageEventArgs)
        Try

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnWorldFilterDeleteObject(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.ReleaseObjectEventArgs)
        Try
            With e.Released
                removeNotifyObject(.Id)
                If mMobs.ContainsKey(.Id) Then
                    mMobs.Remove(.Id)
                ElseIf mModelData.ContainsKey(.Id) Then ' armor, clothing only dictionary
                    mModelData.Remove(.Id)
                End If
            End With

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub OnCharacterFilterChangeOption(ByVal sender As Object, ByVal e As Decal.Adapter.Wrappers.ChangeOptionEventArgs)
        Try


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub StartbuffsPending()
        Try
            If malincobuffsAvailable Then
                If malincobuffs.Buffing Then
                    malincobuffs.CancelBuffs(Alinco3Buffs.eMagicSchool.Creature Or Alinco3Buffs.eMagicSchool.Life Or Alinco3Buffs.eMagicSchool.Item)
                Else
                    Dim pendingsbuffsonly As Boolean = (malincobuffs.BuffsPending > 0)
                    malincobuffs.StartBuffs(Alinco3Buffs.eMagicSchool.Creature Or Alinco3Buffs.eMagicSchool.Life Or Alinco3Buffs.eMagicSchool.Item, pendingsbuffsonly)

                End If

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    Private Function buffpending() As Integer
        Try
            If malincobuffsAvailable Then

                Return malincobuffs.BuffsPending
            End If
        Catch ex As Exception

        End Try
    End Function

    Private Function togglebuffing() As Integer
        Try
            If malincobuffsAvailable Then
                If malincobuffs.Buffing Then
                    malincobuffs.Pause = Not malincobuffs.Pause
                End If
            End If
        Catch ex As Exception

        End Try
    End Function

    Private Function buffingstring() As String
        Try
            If malincobuffsAvailable Then
                Dim result As String = String.Empty
                Dim secs1 As Integer = malincobuffs.BuffTimeRemaining(Alinco3Buffs.eMagicSchool.Creature Or Alinco3Buffs.eMagicSchool.Life)
                Dim secs2 As Integer = malincobuffs.BuffTimeRemaining(Alinco3Buffs.eMagicSchool.Item)

                result = secondstoTimeString(secs1, False)

                If Math.Abs(secs1 - secs2) > 180 Then
                    result &= " / " & secondstoTimeString(secs2, False)
                End If
                Return result
            End If
        Catch ex As Exception

        End Try
        Return String.Empty
    End Function
    Private Function buffingstring2() As String
        Try
            If malincobuffsAvailable Then
                Dim result As String = String.Empty
                Dim secs1 As Integer = malincobuffs.BuffTimeRemaining(Alinco3Buffs.eMagicSchool.Creature Or Alinco3Buffs.eMagicSchool.Life)


                result = secondstoTimeString(secs1, False)


                Return result
            End If
        Catch ex As Exception

        End Try
        Return String.Empty
    End Function

    Private Sub writebasexslt(ByVal filename As String)
        Try
            Dim instream As IO.Stream = (Me.GetType().Module.Assembly.GetManifestResourceStream("Alinco.CharInvent.xslt"))
            Dim buff As Byte()
            ReDim buff(CInt(instream.Length - 1))
            instream.Read(buff, 0, buff.Length)
            File.WriteAllBytes(filename, buff)

        Catch ex As Exception
            wtcw("Error creating xslt file: " & ex.Message)
        End Try
    End Sub

    Private Function getbaseAlerts() As SDictionary(Of String, Alert)
        Dim result As SDictionary(Of String, Alert) = Nothing
        Try
            Dim b As New IO.StreamReader(Me.GetType().Module.Assembly.GetManifestResourceStream("Alinco.Alerts.xml"))
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(SDictionary(Of String, Alert)))
            result = CType(serializer.Deserialize(b), SDictionary(Of String, Alert))

            If result Is Nothing Then
                result = New SDictionary(Of String, Alert)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return result
    End Function

    Private Function getbaseRules() As RulesCollection
        Dim result As RulesCollection = Nothing
        Try
            Dim b As New IO.StreamReader(Me.GetType().Module.Assembly.GetManifestResourceStream("Alinco.Rules.xml"))
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(RulesCollection))
            result = CType(serializer.Deserialize(b), RulesCollection)

            If result Is Nothing Then
                result = New RulesCollection
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return result
    End Function

    Private Sub loadRules()
        If mPluginConfig.worldbasedrules Then

            If mWorldConfig.Rules Is Nothing Then
                mWorldConfig.Rules = getbaseRules()
            End If

            mActiveRulesProfile = mWorldConfig.Rules
        Else

            If mPluginConfig.Rules Is Nothing OrElse mPluginConfig.Rules.Count = 0 Then
                mPluginConfig.Rules = getbaseRules()
            End If

            mActiveRulesProfile = mPluginConfig.Rules
        End If
    End Sub

    Private Function getbaseMobsList() As SDictionary(Of String, NameLookup)
        Dim result As SDictionary(Of String, NameLookup) = Nothing
        Try
            Dim b As New IO.StreamReader(Me.GetType().Module.Assembly.GetManifestResourceStream("Alinco.Mobs.xml"))
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(SDictionary(Of String, NameLookup)))
            result = CType(serializer.Deserialize(b), SDictionary(Of String, NameLookup))

            If result Is Nothing Then
                result = New SDictionary(Of String, NameLookup)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return result
    End Function

    Private Sub LoadMobsList()

        If mCharconfig.usemobsprofile Then
            If mCharconfig.MobsList Is Nothing Then
                mCharconfig.MobsList = getbaseMobsList()
            End If
            mActiveMobProfile = mCharconfig.MobsList
        Else
            If mPluginConfig.MobsList Is Nothing Then
                mPluginConfig.MobsList = getbaseMobsList()
            End If
            mActiveMobProfile = mPluginConfig.MobsList
        End If
    End Sub

    Private Function getbaseThropyList() As SDictionary(Of String, ThropyInfo)
        Dim result As SDictionary(Of String, ThropyInfo) = Nothing
        Try
            Dim b As New IO.StreamReader(Me.GetType().Module.Assembly.GetManifestResourceStream("Alinco.Thropies.xml"))
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(SDictionary(Of String, ThropyInfo)))
            result = CType(serializer.Deserialize(b), SDictionary(Of String, ThropyInfo))

            If result Is Nothing Then
                result = New SDictionary(Of String, ThropyInfo)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return result
    End Function

    Private Sub LoadThropyList()

        If mCharconfig.uselootprofile Then

            If mCharconfig.ThropyList Is Nothing Then
                mCharconfig.ThropyList = getbaseThropyList()
            End If

            mActiveThropyProfile = mCharconfig.ThropyList

        Else

            If mPluginConfig.ThropyList Is Nothing Then
                mPluginConfig.ThropyList = getbaseThropyList()
            End If

            mActiveThropyProfile = mPluginConfig.ThropyList
        End If
    End Sub

    Private Function getbaseSalvageProfile() As SDictionary(Of Integer, SalvageSettings)
        Dim result As SDictionary(Of Integer, SalvageSettings) = Nothing
        Try
            Dim b As New IO.StreamReader(Me.GetType().Module.Assembly.GetManifestResourceStream("Alinco.Salvage.xml"))
            Dim serializer As New Xml.Serialization.XmlSerializer(GetType(SDictionary(Of Integer, SalvageSettings)))
            result = CType(serializer.Deserialize(b), SDictionary(Of Integer, SalvageSettings))

            If result Is Nothing Then
                result = New SDictionary(Of Integer, SalvageSettings)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
        Return result
    End Function

    Private Sub loadsalvage()

        If mCharconfig.usesalvageprofile Then

            If mCharconfig.SalvageProfile Is Nothing Then
                mCharconfig.SalvageProfile = getbaseSalvageProfile()
            End If

            mActiveSalvageProfile = mCharconfig.SalvageProfile

        ElseIf mPluginConfig.worldbasedsalvage Then

            If mWorldConfig.SalvageProfile Is Nothing Then
                mWorldConfig.SalvageProfile = getbaseSalvageProfile()
            End If

            mActiveSalvageProfile = mWorldConfig.SalvageProfile
        Else

            If mPluginConfig.SalvageProfile Is Nothing OrElse mPluginConfig.SalvageProfile.Count = 0 Then
                mPluginConfig.SalvageProfile = getbaseSalvageProfile()
            End If

            mActiveSalvageProfile = mPluginConfig.SalvageProfile
        End If
    End Sub

    Friend Structure lookupstats
        Public totalq As Integer
        Public slowest As TimeSpan
        Public avg As TimeSpan
        Public fastest As TimeSpan
        Public totalspend As TimeSpan
        Sub reset()
            totalq = 0
            slowest = TimeSpan.Zero
            avg = TimeSpan.Zero
            fastest = TimeSpan.Zero
            totalspend = TimeSpan.Zero
        End Sub
        Overrides Function tostring() As String

            Dim slow As String = slowest.TotalMilliseconds.ToString("0.000")
            Dim fast As String = fastest.TotalMilliseconds.ToString("0.000")
            Dim totals As String = totalspend.TotalSeconds.ToString("0.00")

            Dim ts As String = totalq.ToString
            Return String.Format("Total Lookup {0} slowest {1}ms,  fastest {2}ms total {3}s ", ts, slow, fast, totals)
        End Function
    End Structure

    Private mThropylookup As lookupstats
    Private mRulelookup As lookupstats
    Private mSalvagelookup As lookupstats
    Private mMoblookup As lookupstats

    Private m10seconds As Integer
    Private m4seconds As Integer
    Private m2seconds As Integer
    Private viewinitialized As Boolean

    Private Sub MainTimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            If (mBackgroundworker IsNot Nothing AndAlso mBackgroundworker.IsBusy) OrElse mCharconfig Is Nothing Then
                Return
            End If

            If mFilesLoaded And Not viewinitialized Then
                viewinitialized = True
                initializeView()
            End If

            m4seconds += 1
            If m4seconds >= 8 Then
                m4seconds = 0
                setlistboxranges()

                xphour()
                If mMarkObject IsNot Nothing AndAlso DateDiff(DateInterval.Second, mMarkObjectDate, Now) > 3 Then
                    mMarkObject.visible = False
                    mMarkObject = Nothing
                End If

            End If
            NotifyObjectsFromQueue()

            m2seconds += 1
            If m2seconds >= 4 Then
                m2seconds = 0
                If RenderServiceForHud IsNot Nothing Then
                    Renderhud()
                    RenderQuickslotsHud()
                End If
            End If

            m10seconds += 1
            If m10seconds >= 12 Then
                m10seconds = 0
                If RenderServiceForHud IsNot Nothing Then
                    If mCharconfig.trackobjectxpHudId <> 0 And mPluginConfig.hudflags1 = 1 Then 'hudflags1 =0 dont care about xp

                        If Host.Actions.IsValidObject(mCharconfig.trackobjectxpHudId) Then
                            Host.Actions.RequestId(mCharconfig.trackobjectxpHudId)
                        End If

                    End If
                End If
            End If

            If Not mPaused Then
                If Host.Actions.BusyState = 0 Then
                    If AutoPickup() Then
                        Return
                    End If

                    If Stacking() Then
                        Return
                    End If

                    If Host.Actions.CombatMode <> CombatState.Peace OrElse (mPluginConfig.AutoUst And mCurrentContainer <> 0) Then

                    ElseIf mPluginConfig.AutoUst Then
                        If AutoUst() Then Return
                    End If

                End If
            End If


        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    Private mMouseDown As Boolean
    <BaseEvent("WindowMessage")> _
    Private Sub Plugin_WindowMessage(ByVal sender As Object, ByVal e As Decal.Adapter.WindowMessageEventArgs)
        Try
            If mCharconfig IsNot Nothing Then
                Const WM_WINDOWPOSCHANGED As Integer = &H47
                Const WM_LBUTTONUP As Short = &H202
                Const WM_RBUTTONUP As Short = &H205
                Const WM_LBUTTONDWN As Short = &H207
                Const WM_RBUTTONDWN As Short = &H208
                Const WM_ACTIVATEAPP As Integer = &H1C

                Select Case e.Msg
                    Case WM_ACTIVATEAPP
                        Dim pos As Byte() = BitConverter.GetBytes(e.WParam)
                        Dim x As Integer = BitConverter.ToInt16(pos, 0)
                        Lostfocus = (x = 0)

                    Case WM_WINDOWPOSCHANGED
                        If mPluginConfig.WindowedFullscreen Then
                            WinApe.WindowedFullscreen(Host.Decal.Hwnd)
                        End If
                    Case WM_LBUTTONDWN, WM_RBUTTONDWN
                        mMouseDown = True

                    Case WM_LBUTTONUP, WM_RBUTTONUP

                        If mHud IsNot Nothing AndAlso mHud.Enabled Then
                            Dim pos As Byte() = BitConverter.GetBytes(e.LParam)
                            Dim MousePosX As Integer = BitConverter.ToInt16(pos, 0)
                            Dim MousePosY As Integer = BitConverter.ToInt16(pos, 2)
                            If mCharconfig.ShowhudQuickSlots AndAlso mQuickSlotsHud IsNot Nothing AndAlso mQuickSlotsHud.Region.Contains(MousePosX, MousePosY) Then
                                checkhudquickslotsclick(e.Msg, MousePosX, MousePosY)

                            ElseIf mPluginConfig.Showhud AndAlso mHud IsNot Nothing AndAlso mHud.Region.Contains(MousePosX, MousePosY) Then

                                Dim x As Integer = 2
                                Dim iconclick As Boolean

                                'info bar click
                                If mHudCanvasHeight - 17 + mHud.Region.Y < MousePosY AndAlso mHudCanvasHeight - 17 + mHud.Region.Y + 17 > MousePosY Then

                                    If MousePosX > mHud.Region.X + 270 And MousePosX < mHud.Region.X + 270 + 34 Then 'icon click off/onn
                                        mPaused = Not mPaused
                                        togglebuffing()
                                    ElseIf MousePosX > mHud.Region.X + 200 Then 'label buff
                                        StartbuffsPending()

                                    ElseIf MousePosX > mHud.Region.X + 125 Then '  xpchange
                                        If mPluginConfig.hudflags1 = 1 Then
                                            mprevXPtotal = 0
                                            mXPChange = String.Empty
                                            xphour()
                                        End If

                                    ElseIf MousePosX > mHud.Region.X + 65 Then 'xph items
                                        If mPluginConfig.hudflags1 = 1 Then
                                            resetxph()
                                           
                                        Else
                                            mNotifiedCorpses.Clear()
                                            mNotifiedItems.Clear()
                                            mIdqueue.Clear()
                                            mHudlistboxItems.Clear()
                                        End If

                                    ElseIf MousePosX <= mHud.Region.X + x + 48 Then 'clock
                                        mPluginConfig.hudflags1 += 1
                                        If mPluginConfig.hudflags1 > 1 Then 'can be increased in the future to toggle more displays
                                            mPluginConfig.hudflags1 = 0
                                        End If

                                        If mPluginConfig.hudflags1 = 1 Then
                                            If mCharconfig.trackobjectxpHudId <> 0 Then
                                                Host.Actions.RequestId(mCharconfig.trackobjectxpHudId)
                                            End If
                                        End If
                                    End If

                                    Renderhud()
                                    Exit Sub
                                End If
                                If MousePosX >= mHud.Region.X + x And MousePosX <= mHud.Region.X + x + 16 Then
                                    iconclick = True
                                End If


                                Dim iRowpos As Integer
                                Dim i As Integer = 2
                                Dim skeys As Integer()
                                ReDim skeys(mHudlistboxItems.Count)
                                mHudlistboxItems.Keys.CopyTo(skeys, 0)
                                For Each skey As Integer In skeys
                                    If mHudlistboxItems.ContainsKey(skey) Then
                                        Dim onn As notify = CType(mHudlistboxItems.Item(skey), Global.Alinco.Plugin.notify)
                                        iRowpos = (mHudCanvasHeight - 1) - (i * 17)
                                        If iRowpos < 0 Then
                                            Exit For
                                        End If
                                        If iRowpos + mHud.Region.Y < MousePosY AndAlso iRowpos + mHud.Region.Y + 17 > MousePosY Then
                                            If Not iconclick Then
                                                'check label click
                                                Dim xsw As Integer = MeasureStringWidth(onn.name, FontStyle.Regular)
                                                If MousePosX <= mHud.Region.X + x + xsw Then
                                                    huditemclick(False, onn, CBool(e.Msg = WM_LBUTTONUP))
                                                    '  e.Eat = True
                                                End If
                                            Else
                                                huditemclick(iconclick, onn, CBool(e.Msg = WM_LBUTTONUP))
                                                ' e.Eat = True
                                            End If

                                            Exit For
                                        End If
                                        i += 1
                                    End If
                                Next

                            End If
                        End If
                End Select
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub resetxph()
        mXPStart = Core.CharacterFilter.TotalXP
        mXPStartTime = Now
        mXPH = String.Empty

        For Each kvp As KeyValuePair(Of Integer, objectxph) In mObjectxph
            kvp.Value.xpstart = 0
            Host.Actions.RequestId(kvp.Key)
        Next
    End Sub
End Class

