Option Explicit On
Option Strict On

Imports System.IO
Imports Decal.Adapter
Imports System.Runtime.InteropServices
Imports Microsoft.Win32
Imports Decal.Adapter.Wrappers
Imports Decal.Interop.Filters
Imports Decal.Filters

Friend Enum eObjectFlags
    MissileWeaponAndAmmo = 256
    Caster = &H8000F
    Armor = 2
    Clothing = 4
    MeleeWeapon = 1
End Enum

<WireUpBaseEvents(), FriendlyName("Alinco3 Buffs"), View("Alinco3Buffs.mainview.xml", viewname:="Default")> _
Public Class Plugin
    Inherits PluginBase


    Private mcharconfig As CharConfig
    Private mpluginconfig As PluginConfig
    Private mdtSpellList As DataTable
    Private mFileService As Decal.Filters.FileService
    Private mbuffselectionChanged As Boolean
    Private mConfigFilename As String
    Private mTimer As System.Windows.Forms.Timer

    Protected Overrides Sub Shutdown()
        Try
            If mTimer IsNot Nothing Then
                mTimer.Stop()
                RemoveHandler mTimer.Tick, AddressOf Maintimer_Tick
            End If

            If Core.CharacterFilter IsNot Nothing Then
                RemoveHandler Core.CharacterFilter.LoginComplete, AddressOf OnCharacterFilterLoginCompleted
                RemoveHandler Core.CharacterFilter.SpellCast, AddressOf OnCharacterFilterSpellCast
                RemoveHandler Core.CharacterFilter.StatusMessage, AddressOf OnCharacterStatusMessage
                RemoveHandler Core.CharacterFilter.ActionComplete, AddressOf OnCharacterActionComplete
                RemoveHandler Core.CharacterFilter.SpellbookChange, AddressOf OnCharacterSpellbookChange
            End If
            RemoveHandler Core.CommandLineText, AddressOf Plugin_CommandLineText


            If mcharconfig IsNot Nothing Then
                Util.SerializeObject(mConfigFilename, mcharconfig)
            End If

            mFileService = Nothing
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Protected Overrides Sub Startup()
        Try

            If Core.CharacterFilter IsNot Nothing Then
                AddHandler Core.CharacterFilter.LoginComplete, AddressOf OnCharacterFilterLoginCompleted
                AddHandler Core.CharacterFilter.SpellCast, AddressOf OnCharacterFilterSpellCast
                AddHandler Core.CharacterFilter.StatusMessage, AddressOf OnCharacterStatusMessage
                AddHandler Core.CharacterFilter.ActionComplete, AddressOf OnCharacterActionComplete
                AddHandler Core.CharacterFilter.SpellbookChange, AddressOf OnCharacterSpellbookChange
            End If
            AddHandler Core.CommandLineText, AddressOf Plugin_CommandLineText


            mFileService = Core.Filter(Of FileService)()

            mTimer = New System.Windows.Forms.Timer
            AddHandler mTimer.Tick, AddressOf Maintimer_Tick

            mRenderService = Host.Render
            mBaseFontName = "Times New Roman"
            mBaseFontSize = 14
            mBaseFontweight = Wrappers.FontWeight.DoNotCare
            setFontColor(&HFFFFFF)

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

    End Sub

    Private Sub Plugin_CommandLineText(ByVal sender As Object, ByVal e As Decal.Adapter.ChatParserInterceptEventArgs)
        Try
            If e.Text.StartsWith("/bbtest") Then
                e.Eat = True
                trycraftConsumable("Elaborate Field Rations")

                Dim b As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(Host.Actions.CurrentSelection)

                If b IsNot Nothing Then
                    wtcw("selected : " & b.Name)
                    wtcw("Category    " & Hex(b.Category))
                    wtcw("Type        " & Hex(b.Type))
                    wtcw("MissileType " & b.Values(Decal.Adapter.Wrappers.LongValueKey.MissileType))
                    wtcw("EquipType   " & b.Values(Decal.Adapter.Wrappers.LongValueKey.EquipType))
                    wtcw(" ")
                    wtcw("IsItemInInventory   " & IsItemInInventory(b))
                    wtcw("AssociatedSpell     " & b.Values(Decal.Adapter.Wrappers.LongValueKey.AssociatedSpell))
                    wtcw("AffectsVitalAmt     " & b.Values(Decal.Adapter.Wrappers.LongValueKey.AffectsVitalAmt))
                    wtcw("AffectsVitalId      " & b.Values(Decal.Adapter.Wrappers.LongValueKey.AffectsVitalId))
                End If

                wtcw("mBuffs.Count  " & mBuffs.Count)
                wtcw("BuffTimeRemaining " & BuffTimeRemaining(eMagicSchool.Creature))
                wtcw("BuffTimeRemaining " & BuffTimeRemaining(eMagicSchool.Life))
                wtcw("BuffTimeRemaining " & BuffTimeRemaining(eMagicSchool.Life Or eMagicSchool.Creature))
                wtcw("BuffTimeRemaining " & BuffTimeRemaining(eMagicSchool.Item))


                Dim spell As Decal.Filters.Spell
                Dim n As Integer = mFileService.SpellTable.Length
                For i As Integer = 0 To n - 1
                    spell = Nothing

                    Try
                        spell = mFileService.SpellTable(i)
                    Catch

                    End Try

                    If spell IsNot Nothing Then
                        'If spell IsNot Nothing AndAlso (spell.School.Id >= 2 And spell.School.Id <= 4) And Not spell.IsOffensive Then
                        'If spell.Name.StartsWith("Incantation") And spell.Name.IndexOf(" Other") < 0 And spell.Name.IndexOf("Nullify") < 0 And spell.Name.IndexOf(" Vulnerability") < 0 Then
                        '    Util.LogToFile("Spells.txt", spell.Name & vbTab & spell.Id & vbTab & CStr(spell.School.Id))
                        'End If

                        If spell.Name.StartsWith("Incantation") Then
                            Util.LogToFile("Spells.txt", spell.Name & vbTab & spell.Id & vbTab & CStr(spell.School.Id))
                        End If
                    End If
                Next

            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try


    End Sub
    Public Sub New()
        Try
            mInstance = Me
            Util.docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            Util.docPath = IO.Path.Combine(Util.docPath, "Decal Plugins\Alinco3Buffs")
            Util.appPath = System.IO.Path.GetDirectoryName(Me.GetType().Module.Assembly.Location)

            If Not Directory.Exists(Util.docPath) Then
                Directory.CreateDirectory(Util.docPath)
            End If

            Dim asm As System.Reflection.Assembly = System.Reflection.Assembly.GetExecutingAssembly
            Dim AppVersion As System.Version = asm.GetName().Version
            Util.dllversion = AppVersion.ToString

            Util.StartLog()

            If File.Exists((Util.docPath & "\settings.xml")) Then
                Me.mpluginconfig = DirectCast(Util.DeSerializeObject((Util.docPath & "\settings.xml"), GetType(PluginConfig)), PluginConfig)
            End If
            mpluginconfig = New PluginConfig

            If System.IO.File.Exists(Util.appPath & "\SpellData.xml") Then
                Dim tmpds As New DataSet
                tmpds.ReadXml(Util.appPath & "\SpellData.xml")
                If tmpds.Tables.Contains("Spellist") Then
                    mdtSpellList = tmpds.Tables("Spellist")
                    Dim pkey(0) As DataColumn
                    pkey(0) = mdtSpellList.Columns("SpellId")
                    mdtSpellList.PrimaryKey = pkey
                End If
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub wtcw(ByVal msg As String, Optional ByVal color As Integer = 13)
        Try
            Host.Actions.AddChatText(msg, color, 1)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    Private Sub wtcw2a(ByVal msg As String, Optional ByVal color As Integer = 13)
        Try
            Host.Actions.AddChatText(msg, color, 2)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub wtcw2(ByVal msg As String, Optional ByVal color As Integer = 13)
        Try
            Util.Log(msg)
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub


    Private Sub OnCharacterFilterLoginCompleted(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Util.Log("OnCharacterFilterLoginCompleted")

            If String.IsNullOrEmpty(Core.CharacterFilter.Server) Then 'insane checks
                wtcw("Core.CharacterFilter.Server name not set")
                Return
            End If

            If String.IsNullOrEmpty(Core.CharacterFilter.Name) Then
                wtcw("Core.CharacterFilter.Name not set")
                Return
            End If

            mConfigFilename = Util.docPath & "\" & Util.normalizePath(Core.CharacterFilter.Server)
            If Not Directory.Exists(mConfigFilename) Then
                Directory.CreateDirectory(mConfigFilename)
            End If

            mConfigFilename &= "\" & Util.normalizePath(Core.CharacterFilter.Name) & ".xml"

            If File.Exists(mConfigFilename) Then
                mcharconfig = CType(Util.DeSerializeObject(mConfigFilename, GetType(CharConfig)), CharConfig)
            End If

            If mcharconfig Is Nothing Then
                mcharconfig = New CharConfig
                wtcw2("Abuffs: Creating new config file")
                mainTabs.ActiveTab = 4 'settings
            End If
            mcharconfig.Fastcasting = True

            mcharconfig.validateProfiles()
            If mcharconfig.consumables Is Nothing Then
                mcharconfig.consumables = New SDictionary(Of String, consumable)
            End If

            loadspelldata()

            For Each kvp As KeyValuePair(Of String, consumable) In mcharconfig.consumables
                If kvp.Value IsNot Nothing Then
                    ListItemadd(lstConsumables, kvp.Value.icon, kvp.Key)
                End If
            Next
            If Not cboProfile Is Nothing Then
                cboProfile.Clear()
                For Each b As CharConfig.Buffprofile In mcharconfig.buffs
                    cboProfile.Add(b.Profilename, b)
                Next
                cboProfile.Selected = mcharconfig.profile
            End If

            If Not sldMana Is Nothing Then
                sldMana.SliderPostition = mcharconfig.RegenManapct
            End If

            If Not sldStamina Is Nothing Then
                sldStamina.SliderPostition = mcharconfig.RegenStaminapct
            End If

            If mcharconfig.profile = 0 Then
                If mcharconfig.selfbuffarmor.Count = 0 And mcharconfig.selfbuffweaponbuffs.Count = 0 Then
                    setInitialBuffs()
                End If
            End If

            If cboLevelCreature IsNot Nothing Then
                cboLevelCreature.Selected = magicleveltoIndex(mcharconfig.creaturemagiclevel)
            End If

            If cboLevelLife IsNot Nothing Then
                cboLevelLife.Selected = magicleveltoIndex(mcharconfig.lifemagiclevel)
            End If

            If cboLevelItem IsNot Nothing Then
                cboLevelItem.Selected = magicleveltoIndex(mcharconfig.itemmagiclevel)
            End If
            If cboAugmentations IsNot Nothing AndAlso mcharconfig.ArchmageEnduranceAugmentation >= 0 AndAlso mcharconfig.ArchmageEnduranceAugmentation <= 5 Then
                cboAugmentations.Selected = mcharconfig.ArchmageEnduranceAugmentation
            End If

            If chkFilter IsNot Nothing Then
                chkFilter.Checked = mcharconfig.filtercastself
            End If

            If chkHud IsNot Nothing Then
                chkHud.Checked = mcharconfig.simplehud
            End If

            loadbuffprofile()

            loadbuffs()
            loadItemTimers()
            txtminmana.Text = CStr(mcharconfig.minmanaForCasting)

            mTimer.Interval = 500
            mTimer.Start()

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

    Private Function GetEquippedCaster() As Decal.Adapter.Wrappers.WorldObject

        Dim caster As Decal.Adapter.Wrappers.WorldObject = Nothing

        caster = Core.WorldFilter.Item(mcharconfig.buffingwandid)
        If caster Is Nothing Then
            Dim oCol As Decal.Adapter.Wrappers.WorldObjectCollection = Core.WorldFilter.GetByContainer(Core.CharacterFilter.Id)
            If oCol IsNot Nothing Then
                For Each b As Decal.Adapter.Wrappers.WorldObject In oCol
                    If b.Values(Decal.Adapter.Wrappers.LongValueKey.Slot) = -1 AndAlso b.Category = eObjectFlags.Caster Then
                        caster = b
                        Exit For
                    End If
                Next
            End If
        End If

        Return caster
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

    Private Function GetFindItemFromInventory(ByVal id As Integer) As Decal.Adapter.Wrappers.WorldObject
        Dim oWo As Decal.Adapter.Wrappers.WorldObject = Core.WorldFilter.Item(id)

        If oWo IsNot Nothing Then
            If IsItemInInventory(oWo) Then
                Return oWo
            End If
        End If

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

    Private Sub CheckSetCreatureskill(ByVal eSkill As Decal.Adapter.Wrappers.CharFilterSkillType, ByVal spellId As Integer)
        If hasSkill(eSkill) Then
            mcharconfig.selfbuffcreature.Add(spellId)
        End If
    End Sub

    Private Sub setInitialBuffs() 'sets the most logical set of buffs automaticly for a new char
        Try
            mcharconfig.selfbufflife.Add(2149) : mcharconfig.selfbufflife.Add(2151)
            mcharconfig.selfbufflife.Add(2153) : mcharconfig.selfbufflife.Add(2155)
            mcharconfig.selfbufflife.Add(2157) : mcharconfig.selfbufflife.Add(2159)
            mcharconfig.selfbufflife.Add(2161) : mcharconfig.selfbufflife.Add(2183)
            mcharconfig.selfbufflife.Add(2185) : mcharconfig.selfbufflife.Add(2187)
            mcharconfig.selfbufflife.Add(2053)

            mcharconfig.selfbuffarmor.Add(Core.CharacterFilter.Id)

            mcharconfig.selfbuffbanes.Add(2108)
            mcharconfig.selfbuffbanes.Add(2098)
            mcharconfig.selfbuffbanes.Add(2094)
            mcharconfig.selfbuffbanes.Add(2113)
            mcharconfig.selfbuffbanes.Add(2110)
            mcharconfig.selfbuffbanes.Add(2104)
            mcharconfig.selfbuffbanes.Add(2102)
            mcharconfig.selfbuffbanes.Add(2092)

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.CreatureEnchantment, 2215)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.LifeMagic, 2267)

            

            mcharconfig.selfbuffcreature.Add(2087)
            mcharconfig.selfbuffcreature.Add(2091)
            mcharconfig.selfbuffcreature.Add(2081)
            mcharconfig.selfbuffcreature.Add(2059)
            mcharconfig.selfbuffcreature.Add(2067)
            mcharconfig.selfbuffcreature.Add(2061)

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Mace, 2275)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Axe, 2203)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Bow, 2207)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Spear, 2299)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Sword, 2309)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.ThrownWeapons, 2313)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.WarMagic, 2323)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Crossbow, 2219)

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Dagger, 2223)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Unarmed, 2316)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Staff, 2305)

            'remove racial skill
            Dim objSkillInfo As Decal.Adapter.Wrappers.SkillInfoWrapper

            If Core.CharacterFilter.Race.StartsWith("A") Then
                objSkillInfo = Core.CharacterFilter.Skills(Wrappers.CharFilterSkillType.Dagger)
                If objSkillInfo.Training = Wrappers.TrainingType.Trained Then
                    mcharconfig.selfbuffcreature.Remove(2223)
                End If
            ElseIf Core.CharacterFilter.Race.StartsWith("S") Then
                objSkillInfo = Core.CharacterFilter.Skills(Wrappers.CharFilterSkillType.Unarmed)
                If objSkillInfo.Training = eTrainingType.eTrainTrained Then
                    mcharconfig.selfbuffcreature.Remove(2316)
                End If
            ElseIf Core.CharacterFilter.Race.StartsWith("G") Then
                objSkillInfo = Core.CharacterFilter.Skills(Wrappers.CharFilterSkillType.Staff)
                If objSkillInfo.Training = eTrainingType.eTrainTrained Then
                    mcharconfig.selfbuffcreature.Remove(2305)
                End If
            End If

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.ItemEnchantment, 2249)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Healing, 2241)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.MagicDefense, 2281)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.ManaConversion, 2287)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.MeleeDefense, 2245)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.MissileDefense, 2243)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.ArcaneLore, 2195)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Fletching, 2237)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Jump, 2257)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Run, 2301)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Leadership, 2263)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Loyalty, 2233)

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.ItemTinkering, 2251)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.ArmorTinkering, 2197)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.WeaponTinkering, 2325)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.MagicItemTinkering, 2277)

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Alchemy, 2191)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Cooking, 2211)
            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Lockpick, 2271)

            CheckSetCreatureskill(Wrappers.CharFilterSkillType.Deception, 2227)

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    'TODO gets messed up after a few logoffs
    Private Sub loadItemTimers()
        Try


            If mcharconfig.ItemTimers IsNot Nothing Then
                Dim itemtotal As Integer = 0
                Dim itemlows As Integer = 36000

                For Each d As KeyValuePair(Of String, BuffInfo) In mBuffs
                    If d.Value.suspended = False Then
                        If d.Value.school = eMagicSchool.Item Then
                            If mcharconfig.ItemTimers IsNot Nothing Then
                                For Each t As CharConfig.itemBuffInfo In mcharconfig.ItemTimers
                                    If t.spellId = d.Value.SpellId AndAlso t.targetId = d.Value.TargetId Then
                                        d.Value.PlayerAgeCasted = t.playerAgeCasted
                                        wtcw2("stored timer secondsremaining " & t.secondsremaining, d.Value.SpellId)
                                        If t.playerAgeCasted > 0 Then
                                            ' wtcwDebug("stored timer playerAgeCasted " & t.playerAgeCasted)
                                            Dim diff As Integer

                                            diff = t.playerAgeCasted + CInt(d.Value.duration) ' expires
                                            diff = diff - Core.CharacterFilter.Age

                                            wtcw2("stored timer secondsremaining2 " & diff)
                                            d.Value.secondsremaining = diff
                                        End If

                                        If t.secondsremaining < 0 Then
                                            t.secondsremaining = 0
                                        End If

                                        wtcw2("adjusted to " & d.Value.secondsremaining)
                                        If d.Value.secondsremaining < itemlows Then
                                            itemlows = d.Value.secondsremaining
                                        End If
                                        Exit For
                                    End If
                                Next
                            End If

                            d.Value.TimeCasted = DateAdd(DateInterval.Second, -(d.Value.duration - d.Value.secondsremaining), Now)

                            Dim nSecondsPastCasting As Double = DateDiff(DateInterval.Second, d.Value.TimeCasted, Now)
                            d.Value.secondsremaining = CInt(d.Value.duration - nSecondsPastCasting)

                            wtcw2("checkback seconds remain " & d.Value.secondsremaining)
                            wtcw2("checkback TimeCasted " & d.Value.TimeCasted)

                        End If
                    End If
                Next
                wtcw2("Total Item spells: " & itemtotal)
                wtcw2(" lowest remain: " & itemlows)
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub


    Private nheading As Double
    Private Function headingdiff(ByVal x As Double, ByVal y As Double) As Integer
        Dim diff As Integer
        If x > y Then
            diff = CInt(x - y)
        Else
            diff = CInt(y - x)
        End If
        Return diff
    End Function
    Private mUseManaCharges As Boolean
    Private m10seconds As Integer
    Private Sub Maintimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            m10seconds += 1
            If m10seconds >= 8 Then
                m10seconds = 0
                UpdateStatus(mcharconfig.PendingbuffsTimeout)
                If mRenderService IsNot Nothing Then
                    Renderhud()
                End If
            End If
            If (Host.Actions.BusyState <> 0) Or (Host.Actions.PointerState = 100683124) Then
                Return
            End If
            If Not mPause And Host.Actions.BusyState = 0 Then

                If nheading <> 0 Then
                    If headingdiff(nheading, Host.Actions.Heading) > 25 Then
                        Host.Actions.FaceHeading(nheading, False)
                        Return
                    Else
                        nheading = 0
                    End If
                End If

                If mbuffselectionChanged Then
                    loadbuffs()
                End If

                

                If mbuffing Then
                    If mBuffsPending = 0 Then
                        mbuffing = False
                        FinishedBuffing()
                    End If

                    If mcurrentcast IsNot Nothing AndAlso mcurrentcast.Timeout Then

                        If mbuffing Then
                            wtcw2("Casting timeout")
                        End If

                        mcurrentcast = Nothing
                    End If

                    If mWaitAfterAction > 0 Then ' result cast is to fast, you still have to wait a bit
                        mWaitAfterAction -= 1

                    ElseIf mUseManaCharges Then
                        mUseManaCharges = False

                        If UseConsumable(eConsumableType.ManaStone, 0) Then
                            wtcw("try to use manacharge")
                        Else
                            wtcw("no manacharges ")
                        End If

                    ElseIf mcurrentcast Is Nothing Then
                        docasting()
                    End If
                ElseIf mWaitAfterAction > 0 Then ' result cast is to fast, you still have to wait a bit
                    mWaitAfterAction -= 1
                ElseIf mUseManaCharges Then
                    mUseManaCharges = False

                    If UseConsumable(eConsumableType.ManaStone, 0) Then
                        wtcw("try to use manacharge")
                    Else
                        wtcw("no manacharges ")
                    End If

                End If


            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

End Class
