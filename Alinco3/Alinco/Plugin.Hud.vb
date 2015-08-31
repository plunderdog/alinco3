Option Strict On
Option Infer On

Imports Decal.Adapter.Wrappers
Imports System.Drawing
Imports Decal.Adapter
Imports Alinco.Mobdata

Partial Public Class Plugin

    Private mHud, mQuickSlotsHud As Hud
    Private mHudPos As System.Drawing.Point
    Private mBaseFontColor As Color
    Private mBaseFontName As String = "Times New Roman"
    Private mBaseFontSize As Integer = 14

    Private mListboxFontName As String = "Times New Roman"
    Private mListboxFontSize As Integer = 14
    Private mListboxFontWeight As FontWeight = FontWeight.DoNotCare

    Private mBaseFontweight As FontWeight
    Private mHudCanvasHeight As Integer = 600
    Private mHudCanvasWidth As Integer = 600
    Private mHudVirtualWidth As Integer = 300

    Private mXPStart As Long
    Private mXPStartTime As DateTime
    Private mKills As Integer

    Private mXPH As String
    Private mXPChange As String
    Private mprevXPtotal As Long

    Private mVulnIcons() As Integer = {&H6001385}

    Private Class objectxph
        Public xpstart As Long
        Public xpcurrent As Long
    End Class

    Private mObjectxph As New Dictionary(Of Integer, objectxph)

    Private Function xphour(ByVal TotalXP As Long, ByVal XPStart As Long, ByVal neededxpforlevel As Long) As String
        Dim result As String = String.Empty
        Try
            Dim t As TimeSpan = Now.Subtract(mXPStartTime)
            Dim XPAvg, XPAvgs As Long
            Dim xpdiv As Long

            xpdiv = TotalXP - XPStart
            If xpdiv < 0 Then
                xpdiv = 0
            End If
            If t.TotalSeconds = 0 Then Return String.Empty

            XPAvg = CLng(xpdiv / t.TotalSeconds)        'xp per second
            XPAvgs = XPAvg 'seconds
            XPAvg = 3600 * XPAvg 'hour

            Dim suffix As String = " xp/h"
            If XPAvg > 1000000 Then
                XPAvg = CLng(XPAvg / 1000000)
                suffix = "M xp/h"
            ElseIf XPAvg > 1000 Then
                XPAvg = CLng(XPAvg / 1000)
                suffix = "K xp/h"
            End If


            If XPAvgs > 0 And neededxpforlevel > 0 Then 'average xp per minute
                result = Format(XPAvg, "##,##0") & suffix
                Dim seconds As Long = CLng(neededxpforlevel / XPAvgs)
                suffix = " xp/h"

                If seconds > 3600 * 24 Then
                    If neededxpforlevel > 1000000 Then
                        neededxpforlevel = CLng(neededxpforlevel / 1000000)
                        suffix = "M xp"
                    ElseIf XPAvg > 1000 Then
                        neededxpforlevel = CLng(neededxpforlevel / 1000)
                        suffix = "K xp"
                    End If
                    result &= ", " & Format(neededxpforlevel, "##,##0") & suffix & " next lvl"
                Else
                    result &= ", eta lvl up " & secondstoTimeString(seconds, True)
                End If

            ElseIf neededxpforlevel > 0 Then
                If neededxpforlevel > 1000000 Then
                    neededxpforlevel = CLng(neededxpforlevel / 1000000)
                    suffix = "M xp"
                ElseIf XPAvg > 1000 Then
                    neededxpforlevel = CLng(neededxpforlevel / 1000)
                    suffix = "K xp"
                End If
                result = Format(neededxpforlevel, "##,##0") & suffix & " next lvl"
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

        Return result
    End Function

    Private Sub xphour()
        Try
            Dim t As TimeSpan = Now.Subtract(mXPStartTime)
            Dim XPAvg As Long
            Dim xpdiv As Long

            Dim xpchange As Long

            xpdiv = Core.CharacterFilter.TotalXP - mXPStart
            If xpdiv < 0 Then
                xpdiv = 0
            End If
            If t.TotalSeconds = 0 Then Return

            If mCharconfig.trackobjectxpHudId <> 0 Then
                If mObjectxph.ContainsKey(mCharconfig.trackobjectxpHudId) Then
                    With mObjectxph.Item(mCharconfig.trackobjectxpHudId)
                        xpdiv = .xpcurrent - .xpstart
                    End With
                End If
            End If

            XPAvg = CLng(xpdiv / t.TotalSeconds)        'xp per second
            XPAvg *= 3600
            Dim suffix As String = " xp/h"
            If XPAvg > 1000000 Then
                XPAvg = CLng(XPAvg / 1000000)
                suffix = "M xp/h"
            ElseIf XPAvg > 1000 Then
                XPAvg = CLng(XPAvg / 1000)
                suffix = "K xp/h"

            End If

            mXPH = Format(XPAvg, "##,##0") & suffix

            'xpchange

            If mprevXPtotal <> xpdiv Then
                xpchange = xpdiv - mprevXPtotal
                If xpchange > 1000 Then
                    suffix = " xp change"
                    If xpchange > 1000000 Then
                        xpchange = CLng(xpchange / 1000000)
                        suffix = "M xp change"
                    ElseIf xpchange > 1000 Then
                        xpchange = CLng(xpchange / 1000)
                        suffix = "K xp change"
                    End If

                    mXPChange = Format((xpchange), "##,##0") & suffix
                Else
                    mXPChange = String.Empty
                End If

                mprevXPtotal = xpdiv
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private mhudregion As Region
    Private mOffsetY As Integer = 22
    Private mMaxQuickslots As Integer

    <BaseEvent("RegionChange3D")> _
   Private Sub Plugin_RegionChange3D(ByVal sender As Object, ByVal e As Decal.Adapter.RegionChange3DEventArgs)
        If RenderServiceForHud IsNot Nothing Then
            'The hud is alligned to the right of the 3darea
            mHudPosQuickSlots.X = e.Right - mHudCanvasWidthqs

            'just over the 3darea (the combat interface is not part of the 3darea)
            mHudPosQuickSlots.Y = e.Bottom + e.Top + mOffsetY
            If mQuickSlotsHud IsNot Nothing Then
                mQuickSlotsHud.Region = New Rectangle(mHudPosQuickSlots.X, mHudPosQuickSlots.Y, mHudCanvasWidthqs, mHudCanvasHeightqs)
            End If

            'calculate max slots possible on hud
            If Host.Actions.CombatMode = CombatState.Peace Then
                mMaxQuickslots = 0
            Else
                Dim iconwidth As Integer = 20

                Dim leftmargin As Integer = 380
                Dim rightmargin As Integer = mrightmarginMissile
                If Host.Actions.CombatMode = CombatState.Magic Then
                    iconwidth = 24
                    rightmargin = mrightmarginMagic
                    leftmargin = 200
                End If
                iconwidth += mhorizontalspacing
                Dim hw As Integer = e.Right - e.Left
                If hw > mHudCanvasWidthqs Then
                    hw = mHudCanvasWidthqs
                End If

                If e.Right - hw < leftmargin Then
                    hw -= leftmargin - (e.Right - hw)
                End If

                hw = hw - rightmargin

                If hw > 0 Then
                    mMaxQuickslots = hw \ iconwidth
                End If

            End If

            mHudPos.Y = e.Bottom + e.Top - mHudCanvasHeight
            mHudVirtualWidth = e.Right - e.Left
            If mHudVirtualWidth > mHudCanvasWidth Then
                mHudVirtualWidth = mHudCanvasWidth
            End If

            If mHud IsNot Nothing Then
                mHud.Region = New Rectangle(mHudPos.X, mHudPos.Y, mHudCanvasWidth, mHudCanvasHeight)
            End If
            If mCharconfig IsNot Nothing Then
                Renderhud()
                RenderQuickslotsHud()
            End If

        End If
    End Sub

    'happens in full screen mode ( ALT-TAB )
    Private Sub OnGraphicsReset(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            If mCharconfig IsNot Nothing Then

                Renderhud()
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Function secondstoTimeString(ByVal seconds As Long, ByVal labels As Boolean) As String
        Dim d As String = String.Empty
        Dim t As TimeSpan = TimeSpan.FromSeconds(seconds)
        Dim label As String = String.Empty
        If labels Then
            label = "h"
        End If
        If t.Hours > 9 Then
            d = CStr(t.Hours) & label & ":"
        ElseIf t.Hours > 0 Then
            d = "0" & CStr(t.Hours) & label & ":"
        End If
        If labels Then
            label = "m"
        End If
        If t.Minutes > 9 Then
            d &= CStr(t.Minutes) & label
        Else
            d &= "0" & CStr(t.Minutes) & label
        End If
        If labels Then
            label = "s"
        End If
        If t.Seconds > 9 Then
            d &= ":" & CStr(t.Seconds) & label
        Else
            d &= ":" & "0" & CStr(t.Seconds) & label
        End If

        Return d
    End Function

    Private Function MeasureStringWidth(ByVal text As String, ByVal estyle As System.Drawing.FontStyle) As Integer
        Try
            Using mBitmap As New Bitmap(1, 1)

                Using mGraphics As Graphics = Graphics.FromImage(mBitmap)

                    Using mFont As New System.Drawing.Font(mBaseFontName, mBaseFontSize, estyle, GraphicsUnit.Pixel)

                        Dim r As SizeF = mGraphics.MeasureString(text, mFont)
                        Return CInt(r.Width)

                    End Using
                End Using
            End Using
        Catch ex As Exception

        End Try

    End Function

    Private mHudlistboxItems As New Dictionary(Of Integer, notify)
    Private Sub fillhudlistbox()
        Try
            mHudlistboxItems.Clear()

            Dim p = From m In mNotifiedItems Order By m.Value.range

            For Each noti As KeyValuePair(Of Integer, notify) In p
                If Not mHudlistboxItems.ContainsKey(noti.Key) Then
                    mHudlistboxItems.Add(noti.Key, noti.Value)
                End If
            Next
            If mPluginConfig.Showhudcorpses Then
                p = From m In mNotifiedCorpses Order By m.Value.range
                For Each noti As KeyValuePair(Of Integer, notify) In p
                    If Not mHudlistboxItems.ContainsKey(noti.Key) Then
                        mHudlistboxItems.Add(noti.Key, noti.Value)
                    End If
                Next
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub
    Private Enum eSomeTestColorsArg
        White = &HFFFFFF
        Aqua = &HFFFFF
        Blue = &H4169FF
        Gold = &HFFD700
        Green = &H90EE90
        DarkGreen = &H116E11
        DarkRed = &H800000
        Pink = &HFA06B4
        Red = &HFF0000
        Yellow = &HFFFF00
    End Enum

    Private Function getFontColor(ByVal color As eSomeTestColorsArg) As System.Drawing.Color
        Dim argb As Integer
        argb = &HFF000000 Or color
        Return System.Drawing.Color.FromArgb(argb)
    End Function

    Private Function getcoloralpha(ByVal range As Double, ByVal argColor As Integer) As Drawing.Color
        Dim c As Drawing.Color = System.Drawing.Color.FromArgb(&HFF000000 Or argColor)

        Dim n As Integer
        n = CInt(255 - (range / 2))
        If n > 255 Then
            n = 255
        ElseIf n < 220 Then
            n = 220
        End If
        Return System.Drawing.Color.FromArgb(n, c)
    End Function


    Private Sub setlistboxranges()
        Try
            For Each noti As KeyValuePair(Of Integer, notify) In mNotifiedItems
                Dim onn As notify = noti.Value
                Dim wo As WorldObject = Core.WorldFilter.Item(onn.id)

                If wo IsNot Nothing AndAlso wo.Container = 0 Then
                    Dim l As Location = PhysicObjectLocation(onn.id)
                    onn.range = DistanceTo(l)
                Else
                    onn.range = 0
                End If

            Next

            For Each noti As KeyValuePair(Of Integer, notify) In mNotifiedCorpses
                Dim onn As notify = noti.Value
                Dim l As Location = PhysicObjectLocation(onn.id)
                onn.range = DistanceTo(l)
            Next
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private mHudPosQuickSlots As System.Drawing.Point
    Private mHudCanvasHeightqs As Integer = 60
    Private mHudCanvasWidthqs As Integer = 780
    Private mrightmarginMissile As Integer = 90
    Private mrightmarginMagic As Integer = 110
    Private mhorizontalspacing As Integer = 10

    Private Sub checkhudquickslotsclick(ByVal msg As Short, ByVal MousePosX As Integer, ByVal MousePosY As Integer)

        Dim slotIndexClicked As Integer = -1

        If Host.Actions.CombatMode <> CombatState.Peace Then
            Dim iconwidth As Integer = 20
            Dim rightmargin As Integer = mrightmarginMissile

            Dim offsety As Integer = 20

            If Host.Actions.CombatMode = CombatState.Magic Then
                iconwidth = 24
                rightmargin = mrightmarginMagic
                offsety = 0
            End If

            Dim wY2 As Integer = mQuickSlotsHud.Region.Y + mQuickSlotsHud.Region.Height - offsety
            Dim wY1 As Integer = wY2 - iconwidth
            If MousePosY >= wY1 AndAlso MousePosY <= wY2 Then 'Icon bar height
                If MousePosX >= mQuickSlotsHud.Region.X And MousePosX <= mQuickSlotsHud.Region.X + mQuickSlotsHud.Region.Width - rightmargin Then


                    For slotindex As Integer = 1 To mMaxQuickslots
                        Dim spacingx As Integer = mhorizontalspacing * (slotindex - 1)
                        Dim xstart As Integer = mHudCanvasWidthqs - rightmargin - spacingx - ((iconwidth * slotindex))

                        If MousePosX >= mQuickSlotsHud.Region.X + xstart And MousePosX <= mQuickSlotsHud.Region.X + xstart + iconwidth Then
                            slotIndexClicked = slotindex
                            Exit For
                        End If
                    Next

                End If
            End If
        End If

        If slotIndexClicked > 0 And slotIndexClicked <= mMaxQuickslots Then
            Dim combatmode As Integer = Host.Actions.CombatMode
            Dim objSlot As QuickSlotInfo = Nothing
            Dim key As Integer = (combatmode * 100) + slotIndexClicked
            If mCharconfig.quickslots IsNot Nothing AndAlso mCharconfig.quickslots.ContainsKey(key) Then
                objSlot = mCharconfig.quickslots.Item(key)
            End If

            If objSlot Is Nothing Then
                'wtcw("Slot " & slotIndexClicked)
                Dim current As Integer = Host.Actions.CurrentSelection

                If current <> 0 Then
                    Dim wo As Wrappers.WorldObject = Core.WorldFilter.Item(current)
                    If wo IsNot Nothing AndAlso IsItemInInventory(wo) Then
                        If Not wo.HasIdData Then
                            wtcw("Identify the item first")
                        Else
                            Dim newslot As New QuickSlotInfo
                            newslot.Name = wo.Name
                            newslot.Guid = wo.Id
                            newslot.Icon = wo.Icon
                            newslot.IconOverlay = wo.Values(LongValueKey.IconOverlay)
                            newslot.IconUnderlay = wo.Values(LongValueKey.IconUnderlay)
                            newslot.ImbueId = wo.Values(LongValueKey.Imbued)
                            newslot.ObjectClass = wo.ObjectClass
                            newslot.MissleType = wo.Values(LongValueKey.MissileType)
                            newslot.EquipType = wo.Values(LongValueKey.EquipType)
                            mCharconfig.quickslots.Add(key, newslot)
                            wtcw("added " & newslot.Name & " (right mouse click to empty it again)")
                            RenderQuickslotsHud()
                        End If
                    Else
                        wtcw("Select and Identify an item from your inventory first")
                    End If
                End If
            Else

                If msg = &H205 Then 'right click, empty slot
                    mCharconfig.quickslots.Remove(key)
                    RenderQuickslotsHud()
                Else 'left click do actions
                    Dim wo As WorldObject = Core.WorldFilter.Item(objSlot.Guid)
                    If wo IsNot Nothing AndAlso IsItemInInventory(wo) Then
                        Host.Actions.UseItem(objSlot.Guid, 0)
                    Else
                        ''missile ammo
                        'If objSlot.ObjectClass = Wrappers.ObjectClass.MissileWeapon AndAlso objSlot.EquipType = 3 Then

                        'End If
                        'find by name
                        Dim oCol As Decal.Adapter.Wrappers.WorldObjectCollection
                        oCol = Core.WorldFilter.GetByName(objSlot.Name)
                        If oCol IsNot Nothing Then
                            For Each b As Decal.Adapter.Wrappers.WorldObject In oCol
                                If IsItemInInventory(b) Then
                                    Host.Actions.UseItem(b.Id, 0)
                                    Exit For
                                End If
                            Next
                        End If

                    End If
                End If

            End If


        End If

    End Sub

    Private Sub renderhudcombat(ByVal combatmode As Integer, ByVal hudheight As Integer, ByVal iconwidth As Integer, ByVal rightmargin As Integer)
        Try
            Try

                If mQuickSlotsHud Is Nothing Or (mQuickSlotsHud IsNot Nothing AndAlso mQuickSlotsHud.Lost) Then
                    mQuickSlotsHud = RenderServiceForHud.CreateHud(New Rectangle(mHudPosQuickSlots.X, mHudPosQuickSlots.Y, mHudCanvasWidthqs, mHudCanvasHeightqs))
                    mQuickSlotsHud.Alpha = 255
                End If

                If mQuickSlotsHud IsNot Nothing Then
                    mQuickSlotsHud.Clear()
                    mQuickSlotsHud.Enabled = True
                    Try
                        mQuickSlotsHud.BeginRender()

                        'draw icons from right to left 
                        Dim baseiconSet As Integer = 4562
                        Dim baseiconEmpty As Integer = 6431 ' 17x17
                        Dim srcrectb As New Rectangle(0, 0, 17, 17) 'icon

                        Dim slotindex As Integer = 1


                        Dim x As Integer
                        Do
                            Dim spacingx As Integer = mhorizontalspacing * (slotindex - 1)
                            x = mHudCanvasWidthqs - ((iconwidth * slotindex) + spacingx) - rightmargin
                            Dim slotrect As New Rectangle(x, hudheight - iconwidth, iconwidth, iconwidth)
                            If x >= 0 Then
                                Dim key As Integer = (combatmode * 100) + slotindex
                                If mCharconfig.quickslots IsNot Nothing AndAlso mCharconfig.quickslots.ContainsKey(key) Then
                                    Dim slot As QuickSlotInfo = mCharconfig.quickslots.Item(key)

                                    If slot IsNot Nothing Then

                                        If slot.IconUnderlay <> 0 Then
                                            mQuickSlotsHud.DrawPortalImage(&H6000000 + slot.IconUnderlay, slotrect) 'base underlay
                                        Else
                                            mQuickSlotsHud.DrawPortalImage(&H6000000 + baseiconSet, srcrectb, slotrect) 'base underlay
                                        End If

                                        If slot.Icon <> 0 Then
                                            Dim srcrect As New Rectangle(0, 0, 32, 32) 'icon
                                            mQuickSlotsHud.DrawPortalImage(&H6000000 + slot.Icon, 255, srcrect, slotrect)

                                            If slot.IconOverlay <> 0 Then
                                                mQuickSlotsHud.DrawPortalImage(&H6000000 + slot.IconOverlay, 255, srcrect, slotrect)
                                            End If
                                        End If

                                    End If

                                Else
                                    mQuickSlotsHud.DrawPortalImage(&H6000000 + baseiconEmpty, srcrectb, slotrect) 'base underlay
                                End If

                            End If

                            slotindex += 1
                        Loop Until x <= 0 Or slotindex > mMaxQuickslots

                    Catch ex As Exception
                        Util.ErrorLogger(ex)
                    Finally
                        mQuickSlotsHud.EndRender()
                    End Try

                End If
            Catch ex As Exception
                Util.ErrorLogger(ex)
            End Try
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private mUstopen As Boolean
    Private Sub RenderQuickslotsHud()
        Try
            If RenderServiceForHud IsNot Nothing Then
                If mQuickSlotsHud IsNot Nothing AndAlso mQuickSlotsHud.Lost Then
                    mQuickSlotsHud.Enabled = False
                    Host.Render.RemoveHud(mQuickSlotsHud)
                    mQuickSlotsHud.Dispose()
                    mQuickSlotsHud = Nothing
                End If

                If Host.Actions.CombatMode = CombatState.Peace Then
                    mUstopen = False
                End If

                If mCharconfig.ShowhudQuickSlots Then
                    If mUstopen Or mCurrentContainer <> 0 Then
                        If mQuickSlotsHud IsNot Nothing Then
                            mQuickSlotsHud.Enabled = False
                        End If
                    ElseIf Host.Actions.CombatMode = CombatState.Missile Then
                        renderhudcombat(CombatState.Missile, 42, 20, mrightmarginMissile)
                    ElseIf Host.Actions.CombatMode = CombatState.Magic Then
                        renderhudcombat(CombatState.Magic, 60, 24, mrightmarginMagic)
                    ElseIf Host.Actions.CombatMode = CombatState.Melee Then
                        renderhudcombat(CombatState.Melee, 42, 20, mrightmarginMissile)
                    ElseIf mQuickSlotsHud IsNot Nothing Then
                        mQuickSlotsHud.Enabled = False
                    End If
                ElseIf mQuickSlotsHud IsNot Nothing Then
                    If mQuickSlotsHud.Enabled Then
                        mQuickSlotsHud.Clear()
                    End If
                    mQuickSlotsHud.Enabled = False
                End If

            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub Renderhud()

        Try
            If RenderServiceForHud IsNot Nothing Then
                mHudPos.X = 0

                If mHud IsNot Nothing AndAlso mHud.Lost Then
                    mHud.Enabled = False
                    Host.Render.RemoveHud(mHud)
                    mHud.Dispose()
                    mHud = Nothing
                End If

                If mHud Is Nothing Then
                    mHud = RenderServiceForHud.CreateHud(New Rectangle(mHudPos.X, mHudPos.Y, mHudCanvasWidth, mHudCanvasHeight))
                    mHud.Alpha = 255
                End If

                If mPluginConfig.Showhud Then
                    mHud.Enabled = True
                Else
                    If mHud.Enabled Then
                        mHud.Clear()
                    End If
                    mHud.Enabled = False
                    Return
                End If

                Try
                    Dim bShowItems As Boolean = True

                    mHud.Clear()
                    mHud.BeginRender()


                    'create bottom info screen
                    Dim bottominfoheight As Integer = 16
                    Dim yinfoheight As Integer
                    yinfoheight = mHudCanvasHeight - bottominfoheight

                    If bShowItems Then
                        mHud.BeginText(mListboxFontName, mListboxFontSize, mListboxFontWeight, False)
                        fillhudlistbox()

                        'draw from the bottom up
                        Dim i As Integer = 1
                        Dim iRowpos As Integer

                        For Each noti As KeyValuePair(Of Integer, notify) In mHudlistboxItems
                            Dim onn As notify = noti.Value
                            iRowpos = yinfoheight - (i * 17)
                            If iRowpos < 0 Then
                                Exit For
                            End If
                            Dim rrIcon As Rectangle = New Rectangle(2, iRowpos, 16, 16)
                            Dim rrLabel As Rectangle = New Rectangle(16 + 4, iRowpos, 200, 16)

                            i += 1
                            Dim tcolor As System.Drawing.Color = Color.AliceBlue
                            If onn.scantype = eScanresult.monstertokill Then '' mCharconfig.ShowAllMobs AndAlso
                                tcolor = getcoloralpha(onn.range, &HFF000000 Or eSomeTestColorsArg.White) 'no vulns yet red

                                If mMobs.ContainsKey(onn.id) Then
                                    If mMobs.Item(onn.id).vulns IsNot Nothing Then
                                        Dim vulned As Boolean = mMobs.Item(onn.id).hasSpellids(mVuls)
                                        Dim imped As Boolean = mMobs.Item(onn.id).hasSpellids(mImps)

                                        If imped And vulned Then
                                            tcolor = getcoloralpha(onn.range, &HFF000000 Or eSomeTestColorsArg.Red)
                                        ElseIf imped Then
                                            tcolor = getcoloralpha(onn.range, &HFF000000 Or eSomeTestColorsArg.Yellow)
                                        ElseIf vulned Then
                                            tcolor = getcoloralpha(onn.range, &HFF000000 Or eSomeTestColorsArg.Pink)
                                        End If

                                    End If

                                End If
                            Else
                                tcolor = getcoloralpha(onn.range, onn.ColorArgb)
                            End If


                            mHud.WriteText(onn.name, tcolor, WriteTextFormats.None, rrLabel)
                            mHud.DrawPortalImage(&H6000000 + onn.icon, rrIcon)
                        Next
                        mHud.EndText()
                    End If

                    mHud.BeginText(mBaseFontName, mBaseFontSize, mBaseFontweight, False)

                    Dim rsource As New Rectangle(0, 0, 300, 150)
                    Dim infotextlabelsy As Integer = yinfoheight + 2

                    rsource = New Rectangle(0, 0, 300, bottominfoheight) 'strip the bottom border
                    Dim rdest As New Rectangle(0, yinfoheight, 300, bottominfoheight)

                    mHud.DrawPortalImage(&H6001397, 255, rsource, rdest)

                    rsource = New Rectangle(0, 0, 34, 16)
                    rdest = New Rectangle(266, yinfoheight - 1, 34, 16)
                    Dim selectedicon As Integer

                    If Not mPaused AndAlso Not mCorpsScanning Then
                        selectedicon = &H1116
                    Else
                        selectedicon = &H1118
                    End If

                    mHud.DrawPortalImage(&H6000000 + selectedicon, 255, rsource, rdest)


                    Dim r As New Rectangle(4, infotextlabelsy, 100, 14)

                    Dim strInfo As String = String.Empty
                    Dim iq As Integer = mIdqueue.Count + mColScanInventoryItems.Count + midInventory.Count

                    Dim buffs As String = String.Empty
                    If mPluginConfig.hudflags1 = 0 Then

                        mHud.WriteText(Format(Now, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern), mBaseFontColor, WriteTextFormats.None, r)

                        strInfo = "Items : " & mNotifiedCorpses.Count + mNotifiedItems.Count
                        r = New Rectangle(55, infotextlabelsy, 100, 14)
                        mHud.WriteText(strInfo, mBaseFontColor, WriteTextFormats.None, r)

                        If iq > 0 Then
                            r = New Rectangle(115, infotextlabelsy, 100, 14)
                            mHud.WriteText(CStr(iq), mBaseFontColor, WriteTextFormats.None, r)
                        End If

                        'buffing status string
                        If malincobuffsAvailable Then
                            buffs = buffingstring()
                            Dim xbuffs As Integer = buffpending()
                            If xbuffs > 0 Then
                                buffs = "{" & xbuffs & "} " & buffs
                            End If
                        End If

                        If Not String.IsNullOrEmpty(buffs) Then
                            Dim buffstringw As Integer = MeasureStringWidth(buffs, FontStyle.Regular)
                            r = New Rectangle(280 - buffstringw, infotextlabelsy, buffstringw + 2, 14)
                            mHud.WriteText(buffs, mBaseFontColor, WriteTextFormats.None, r)


                        End If
                    ElseIf mPluginConfig.hudflags1 = 1 Then 'show xp mode
                        mHud.WriteText(Format(Now, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern), mBaseFontColor, WriteTextFormats.None, r)

                        strInfo = mXPH
                        r = New Rectangle(65, infotextlabelsy, 100, 14)
                        mHud.WriteText(strInfo, mBaseFontColor, WriteTextFormats.None, r)

                        r = New Rectangle(135, infotextlabelsy, 100, 14)
                        mHud.WriteText(mXPChange, mBaseFontColor, WriteTextFormats.None, r)

                        'buffing status string
                        If malincobuffsAvailable Then
                            buffs = buffingstring2()
                            Dim xbuffs As Integer = buffpending()
                            If xbuffs > 0 Then
                                buffs = "(" & xbuffs & "} " & buffs
                            End If
                        End If

                        If Not String.IsNullOrEmpty(buffs) Then
                            Dim buffstringw As Integer = MeasureStringWidth(buffs, FontStyle.Regular)
                            r = New Rectangle(280 - buffstringw, infotextlabelsy, buffstringw + 2, 14)
                            mHud.WriteText(buffs, mBaseFontColor, WriteTextFormats.None, r)
                        End If
                    End If

                    If mPluginConfig.Showhudvulns Then
                        Dim current As Integer = Host.Actions.CurrentSelection
                        With mMobs
                            If .ContainsKey(current) Then
                                If .Item(current).vulns IsNot Nothing Then
                                    Dim pos As Integer = 0
                                    If pos < 500 Then
                                        For Each ik As KeyValuePair(Of Integer, vulninfo) In .Item(current).vulns
                                            Dim nsecsleft As Integer = ik.Value.getseconds
                                            If nsecsleft > 0 Then
                                                r = New Rectangle(320 + pos, infotextlabelsy - 8, 20, 20)

                                                If nsecsleft < 60 Then
                                                    mHud.DrawPortalImage(&H6003359, r)
                                                ElseIf nsecsleft < 4 * 60 Then
                                                    mHud.DrawPortalImage(&H600335B, r)
                                                End If

                                                mHud.DrawPortalImage(ik.Value.icon, r)
                                                pos += 30
                                            End If
                                        Next
                                    End If

                                End If
                            End If
                        End With
                    End If

                Catch ex As Exception
                    Util.ErrorLogger(ex)
                Finally
                    mHud.EndText()
                    mHud.EndRender()
                End Try
            End If

        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try
    End Sub

    Private Sub removeoutofrange()
        Dim range As Double
        Dim n As notify
        Dim pObject As WorldObject


        For Each d As KeyValuePair(Of Integer, notify) In mHudlistboxItems
            n = CType(d.Value, notify)
            pObject = Core.WorldFilter.Item(d.Key)

            If Not pObject Is Nothing Then
                range = 0
                range = DistanceTo(pObject.Id)
                If range > 300 Then
                    removeNotifyObject(pObject.Id)
                End If
            End If
        Next
    End Sub

End Class
