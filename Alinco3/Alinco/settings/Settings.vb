Public Class Alert
    Public name As String
    Public wavfilename As String
    Public showinchatwindow As Integer
    Public chatcolor As Integer
    Public volume As Integer
End Class


Public Class QuickSlotInfo
    Public Name As String
    Public Guid As Integer
    Public ObjectClass As Integer
    Public ImbueId As Integer
    Public Icon As Integer
    Public IconUnderlay As Integer
    Public IconOverlay As Integer
    Public MissleType As Integer
    Public EquipType As Integer
    Public Flags As Integer
End Class

Public Class SalvageSettings
    Public checked As Boolean
    Public name As String
    Public combinestring As String
    Public Alert As String

    Sub New()

    End Sub
    Sub New(ByVal name As String, ByVal checked As Boolean, ByVal combine As String)
        Me.name = name
        Me.checked = checked
        Me.combinestring = combine
    End Sub
End Class

Public Class NameLookup
    Public checked As Boolean '
    Public ispartial As Boolean
    Public Alert As String
    Sub New()

    End Sub
    Sub New(ByVal checked As Boolean, ByVal ispartial As Boolean)
        Me.checked = checked
        Me.ispartial = ispartial
    End Sub
End Class

Public Class ThropyInfo
    Inherits NameLookup

    Public lootmax As Integer
    Public npc As String
    Public npcloc As Location
    Sub New()

    End Sub
    Sub New(ByVal checked As Boolean, ByVal ispartial As Boolean, ByVal lootmax As Integer)
        Me.checked = checked
        Me.ispartial = ispartial
        Me.lootmax = lootmax
    End Sub
End Class

'''<summary>Profiles</summary>
Public Class Settings
    Public Version As Integer = 1

    Public SalvageProfile As SDictionary(Of Integer, SalvageSettings)
    Public MobsList As SDictionary(Of String, NameLookup)
    Public ThropyList As SDictionary(Of String, ThropyInfo)
    Public Rules As RulesCollection

End Class

Public Class PluginSettings
    Inherits Settings
    Public Shortcuts As SDictionary(Of String, String)

    Public Alerts As SDictionary(Of String, Alert)
    Public AlertKeyPortal As String
    Public AlertKeySalvage As String
    Public AlertKeyThropy As String
    Public AlertKeyMob As String
    Public wavVolume As Integer = 50
    Public Alertwawfinished As String
    Public AlertKeyScroll As String
    Public FilterTellsMerchant As Boolean = True
    Public FilterChatMeleeEvades As Boolean = True
    Public FilterChatResists As Boolean = True
    Public FilterSpellcasting As Boolean = True
    Public FilterSpellsExpire As Boolean = True
    Public ResistsSound As Boolean = False
    Public PortalExclude As String() = {"House Portal"}
    Public PackOrCorpseOrChestExclude As String() = {"Storage"}
    Public Showhud As Boolean = True
    Public Showhudvulns As Boolean = True
    Public Showhudcorpses As Boolean = True
    Public Showpalette As Boolean = False
    Public NotifyPortals As Boolean = True
    Public OutputManualIdentify As Boolean = True
   
    Public D3DMark0bjects As Boolean = True
    Public CopyToClipboard As Boolean
    Public AutoStacking As Boolean
    Public MuteAll As Boolean
    Public AutoUst As Boolean

    Public AutoPickup As Boolean
    Public SalvageHighValue As Boolean
    Public CorpseCache As Integer = 1000
    Public WindowedFullscreen As Boolean

    Public worldbasedsalvage As Boolean
    Public worldbasedrules As Boolean

    Public notifyItemmana As Integer = 4000
    Public notifyItemvalue As Integer = 30000
    Public notifyValueBurden As Integer

    Public notifycorpses As Boolean = True
    Public notifytells As Boolean
    Public notifyalleg As Boolean
    Public showallcorpses As Boolean
    Public unknownscrolls As Boolean
    Public trainedscrollsonly As Boolean
    Public unknownscrollsAll As Boolean
    Public chattargetwindow As Integer = 1

    Public hudflags1 As Integer
    Public Sub New()

    End Sub
End Class

Public Class WorldSettings
    Inherits Settings
    Public worldname As String
End Class

Public Class CharSettings
    Inherits Settings

    Public usesalvageprofile As Boolean
    Public uselootprofile As Boolean
    Public usemobsprofile As Boolean
    Public detectscrollsontradebot As Boolean
    Public useglobalspellbook As Boolean
    Public lastrarefound As DateTime
    Public salvageaugmentations As Integer
    Public quickslots As Alinco.SDictionary(Of Integer, QuickSlotInfo)
    Public ShowhudQuickSlots As Boolean
    Public ShowAllMobs As Boolean
    Public ShowAllPlayers As Boolean = True
    Public trackobjectxpHudId As Integer
End Class





