Imports Decal.Adapter.Wrappers

Public Class IdentifiedObject

    <Xml.Serialization.XmlIgnore()>
    Private wo As WorldObject

    Sub New(ByVal obj As WorldObject)
        wo = obj
    End Sub

    Sub New()

    End Sub

    'wrappers: the overloads of function Values in wrappers.worldobject makes me type too much
    '
    ReadOnly Property IntValues(ByVal e As Decal.Adapter.Wrappers.LongValueKey) As Integer
        Get
            Return wo.Values(e)
        End Get
    End Property

    ReadOnly Property DblValues(ByVal e As Decal.Adapter.Wrappers.DoubleValueKey) As Double
        Get
            Return wo.Values(e)
        End Get
    End Property

    ReadOnly Property StringValues(ByVal e As Decal.Adapter.Wrappers.StringValueKey) As String
        Get
            Return wo.Values(e)
        End Get
    End Property
    ReadOnly Property BoolValues(ByVal e As Decal.Adapter.Wrappers.BoolValueKey) As Boolean
        Get
            Return wo.Values(e)
        End Get
    End Property

    'not in worldfilter set by OnIdentObject:
    Private mSetId As Integer
    Private mHealthMax As Integer
    Private mHealthCurrent As Integer

    Public ReadOnly Property PieceOfSetId() As Integer
        Get
            If mSetId = 0 Then
                mSetId = Me.IntValues(&H109)
            End If
            Return mSetId
        End Get
    End Property

    Private mItemxp, mnextitemlvlxp As Long
    Public Property Itemxp() As Long
        Get
            Return mItemxp
        End Get
        Friend Set(ByVal value As Long)
            mItemxp = value
        End Set
    End Property

    Public Property NextItemlvlxp() As Long
        Get
            Dim x As Long = CurrentItemLevel()
            Dim nextlvl As Integer = x + 1
            If nextlvl <= MaxItemLevel And mItemxp <> 0 Then
                'aetheria =  2^N-1 billion XP
                'rare = 2000000000
                Dim totalxpneededforlevel As Long
                If IntValues(LongValueKey.RareId) = 0 Then
                    totalxpneededforlevel = ((2 ^ nextlvl) * 1000000000) - 1000000000
                Else
                    totalxpneededforlevel = nextlvl * 2000000000
                End If
                mnextitemlvlxp = totalxpneededforlevel - mItemxp

            End If
            
            Return mnextitemlvlxp
        End Get
        Friend Set(ByVal value As Long)
            mnextitemlvlxp = value
        End Set
    End Property

    Private mItemLevelmax As Integer
    Public ReadOnly Property MaxItemLevel() As Integer
        Get
            If mItemLevelmax = 0 Then
                mItemLevelmax = Me.IntValues(&H13F)
            End If
            Return mItemLevelmax
        End Get
    End Property

    Public Function CurrentItemLevel() As Integer
        Dim x As Integer = MaxItemLevel
        Dim result As Integer = 0
        Dim lvl As Integer = 0
        If x > 0 Then

            If IntValues(LongValueKey.RareId) = 0 Then '1,2,4,8,16
                If mItemxp < 1000000000 Then
                    lvl = 0
                ElseIf mItemxp < 3000000000 Then
                    lvl = 1
                ElseIf mItemxp < 7000000000 Then
                    lvl = 2
                ElseIf mItemxp < 15000000000 Then
                    lvl = 3
                ElseIf mItemxp < 31000000000 Then
                    lvl = 4
                Else
                    lvl = 5
                End If
            Else '2000000000 per level
                lvl = mItemxp / 2000000000
            End If

            Return lvl
        End If
        Return result
    End Function

    Public Function CurrentItemLevelString() As String
        Dim x As Integer = MaxItemLevel
        Dim result As String = String.Empty
        If x > 0 Then
            Return "(" & CurrentItemLevel() & "/" & x & ")"
        End If
        Return result
    End Function
    Public Property HealthMax() As Integer
        Get
            Return mHealthMax
        End Get
        Friend Set(ByVal value As Integer)
            mHealthMax = value
        End Set
    End Property

    

    Public Property HealthCurrent() As Integer
        Get
            Return mHealthCurrent
        End Get
        Friend Set(ByVal value As Integer)
            mHealthCurrent = value
        End Set
    End Property

    Public Function isvalid() As Boolean
        Return CBool(wo IsNot Nothing)
    End Function

    Public Function DamageVsMonsters() As Double
        Dim value As Double = wo.Values(DoubleValueKey.ElementalDamageVersusMonsters)
        If value <> 0 Then
            value -= 1
            value *= 100
        End If
        Return Math.Round(value, 2)
    End Function

    Public Function DamageBonusMissile() As Double
        Dim value As Double = wo.Values(DoubleValueKey.DamageBonus)
        If value <> 0 Then
            value -= 1
            value *= 100
        End If
        Return Math.Round(value, 2)
    End Function

    Public Function WeaponMaxDamage() As Integer
        Return wo.Values(LongValueKey.MaxDamage)
    End Function

    Public Function WeaponMagicDBonus() As Double
        Dim value As Double = wo.Values(DoubleValueKey.MagicDBonus)
        If value <> 0 Then
            value -= 1
            value *= 100
        End If
        Return Math.Round(value, 2)
    End Function

    Public Function WeaponManaCBonus() As Double
        Dim value As Double = wo.Values(DoubleValueKey.ManaCBonus)
        If value <> 0 Then
            value *= 100
            Return Math.Round(value, 2)
        End If
    End Function

    Public Function WeaponAttackBonus() As Double
        Dim value As Double = wo.Values(DoubleValueKey.AttackBonus)
        If value <> 0 Then
            value -= 1
            value *= 100
        End If
        Return Math.Round(value, 2)
    End Function

    Public Function WeaponMeleeBonus() As Double
        Dim value As Double = wo.Values(DoubleValueKey.MeleeDefenseBonus)
        If value <> 0 Then
            value -= 1
            value *= 100
        End If
        Return Math.Round(value, 2)
    End Function

    Public ReadOnly Property SpellCount() As Integer
        Get
            Return wo.SpellCount
        End Get
    End Property

    Public ReadOnly Property Spell(ByVal idx As Integer) As Integer
        Get
            Return wo.Spell(idx)
        End Get
    End Property

    Public ReadOnly Property Id() As Integer
        Get
            Return wo.Id
        End Get
    End Property
    Public ReadOnly Property HasIdData() As Boolean
        Get
            Return wo.HasIdData
        End Get
    End Property
    Public ReadOnly Property Icon() As Integer
        Get
            Return wo.Icon
        End Get
    End Property

    Public ReadOnly Property Name() As String
        Get
            Return wo.Name
        End Get
    End Property

    Public ReadOnly Property ObjectClass() As Decal.Adapter.Wrappers.ObjectClass
        Get
            Return wo.ObjectClass
        End Get
    End Property

    Public ReadOnly Property Coordinates() As Decal.Adapter.Wrappers.CoordsObject
        Get
            If wo.Container <> 0 Then 'Todo not always true, but don't care for now
                Return Nothing
            End If

            Return wo.Coordinates
        End Get
    End Property

    Public ReadOnly Property Container() As Integer
        Get
            Return wo.Container
        End Get
    End Property

    Private Function nz(ByVal descr As String, ByVal num As Integer) As String
        If num > 0 Then
            Return ", " & descr & " " & num
        End If
        Return String.Empty
    End Function

    Private Function ImbueString() As String

        Dim shortname As String = String.Empty
        Dim value As Integer = wo.Values(LongValueKey.Imbued)


        If value > 0 Then

            'Allow for multiple imbues for example: magic absob on missile weapons
            For Each gd As GameData.NameId In Plugin.GameData.ImbueStrings
                If (gd.Id And value) = gd.Id Then
                    If shortname <> String.Empty Then
                        shortname &= "," & gd.name
                    Else
                        shortname = gd.name
                    End If
                End If
            Next

            If shortname = String.Empty Then ' missed the jewels\armor imbues in the list
                shortname = "Imbued(0x" & Hex(value) & ")"
            End If

            Return ", " & shortname
        End If

        Return String.Empty
    End Function

    Private Function SpellDescriptions() As String
        Dim strspells As String = String.Empty

        If Plugin.FileService IsNot Nothing Then


            For i As Integer = 0 To wo.SpellCount - 1
                Dim oSpell As Decal.Filters.Spell = Plugin.FileService.SpellTable.GetById(wo.Spell(i))
                If Not oSpell Is Nothing Then
                    strspells &= ", " & oSpell.Name
                End If
            Next
        Else
            strspells = ("Plugin.FileService exploded")
        End If

        Return strspells
    End Function

    Public Function countspell(ByVal srt As String) As Integer
        Dim n As Integer = 0

        If Plugin.FileService IsNot Nothing Then

            For i As Integer = 0 To wo.SpellCount - 1
                Dim oSpell As Decal.Filters.Spell = Plugin.FileService.SpellTable.GetById(wo.Spell(i))
                If Not oSpell Is Nothing AndAlso oSpell.Name.Contains(srt) Then
                    n += 1
                End If
            Next

        End If

        Return n
    End Function

    Public Function Itemset() As String
        Dim result As String = Nothing

        If PieceOfSetId <> 0 Then
            Dim name As String = Plugin.GameData.SetStrings.GetName(PieceOfSetId)
            If name IsNot String.Empty Then
                result = name
            End If

        End If

        Return result
    End Function

    Private Function SetString() As String
        Dim result As String = String.Empty

        If PieceOfSetId <> 0 Then
            Dim name As String = Plugin.GameData.SetStrings.GetName(PieceOfSetId)
            If name IsNot String.Empty Then
                result = ", " & name & ""
            Else
                result = ", (Set #" & PieceOfSetId & ")"
            End If

        End If

        Return result
    End Function

    Private Function ALString() As String
        Dim result As String = String.Empty
        Dim value As Integer = wo.Values(LongValueKey.ArmorLevel)
        If value <> 0 Then
            result = ", AL " & value
        End If
        Return result
    End Function

    Private Function ActivateString() As String
        Dim result As String = String.Empty

        Dim SkillLevelReq As Integer = wo.Values(LongValueKey.SkillLevelReq)
        If SkillLevelReq <> 0 Then
            Dim ActivationReqSkillId As Integer = wo.Values(LongValueKey.ActivationReqSkillId)
            result = Plugin.GameData.EquipSkill.GetName(ActivationReqSkillId)
            result = ", " & result & " " & SkillLevelReq & " to Activate"
        End If

        Return result
    End Function

    Private Function LoreString() As String
        Dim result As String = String.Empty
        result = nz("Diff", wo.Values(LongValueKey.LoreRequirement))
        Return result
    End Function

    Private Function RankString() As String
        Dim result As String = String.Empty
        result = nz("Rank", wo.Values(LongValueKey.Rank))
        Return result
    End Function

    Private Function RaceString() As String
        Dim result As String = String.Empty
        result = Plugin.GameData.HeritageStrings.GetName(wo.Values(LongValueKey.Heritage))
        If result <> String.Empty Then
            result = ", " & result
        End If
        Return result
    End Function

    Private Function CraftString() As String
        Dim result As String = String.Empty
        result = nz("Craft", wo.Values(LongValueKey.Workmanship))
        Return result
    End Function

    Private Function ProtsString() As String
        Dim result As String = String.Empty
        If wo.Values(LongValueKey.Unenchantable) <> 0 Then
            result = wo.Values(DoubleValueKey.SlashProt, 0).ToString("0.0\/;-0.0\/;")
            result &= wo.Values(DoubleValueKey.PierceProt, 0).ToString("0.0\/;-0.0\/;")
            result &= wo.Values(DoubleValueKey.BludgeonProt, 0).ToString("0.0\/;-0.0\/;")
            result &= wo.Values(DoubleValueKey.FireProt, 0).ToString("0.0\/;-0.0\/;")
            result &= wo.Values(DoubleValueKey.ColdProt, 0).ToString("0.0\/;-0.0\/;")
            result &= wo.Values(DoubleValueKey.AcidProt, 0).ToString("0.0\/;-0.0\/;")
            result &= wo.Values(DoubleValueKey.LightningProt, 0).ToString("0.0;-0.0;")
            result = ", [" & result & "]"
        End If
        Return result
    End Function

    Private Function ValueString() As String
        Dim result As String = String.Empty
        Dim value As Integer = wo.Values(LongValueKey.Value)
        If value <> 0 Then
            Return ", Value " & value.ToString("##,##0", Util.NumberFormatInfo)
        End If
        Return result
    End Function

    Private Function BurdenString() As String
        Dim result As String = String.Empty
        result = nz("BU", wo.Values(LongValueKey.Burden))
        Return result
    End Function

    Private Function TinkersString() As String
        Return nz("Tinks", wo.Values(LongValueKey.NumberTimesTinkered))
    End Function
    Public Function wieldlvl() As Integer
        Dim ReqWieldId As Integer = wo.Values(LongValueKey.WieldReqType, 0)
        If ReqWieldId = &H7 Then
            Return wo.Values(LongValueKey.WieldReqValue, 0)
        End If
    End Function
    Protected Function WieldString() As String
        Dim result As String = String.Empty

        Dim ReqWieldId As Integer = wo.Values(LongValueKey.WieldReqType, 0)

        If ReqWieldId <> 0 Then
            Dim ReqWieldvalue As Integer = wo.Values(LongValueKey.WieldReqValue, 0)
            Dim ReqWieldSkillId As Integer = wo.Values(LongValueKey.WieldReqAttribute, 0)

            If ReqWieldSkillId = &H11F Then
                'Standing ch
            ElseIf ReqWieldSkillId = &H120 Then
                'standing ew

            ElseIf ReqWieldId = &H7 Then

                result = nz("Wield Lvl", ReqWieldvalue)

            ElseIf ReqWieldvalue <> 0 Then
                result = Plugin.GameData.EquipSkill.GetName(ReqWieldSkillId)
                If result = String.Empty Then
                    result = "WieldReqAttribute 0x" & Hex(ReqWieldSkillId)
                End If

                result = ", " & result & " " & ReqWieldvalue & " to Wield"
            End If

        End If

        Return result
    End Function

    Private Function xModString(ByVal x As Double, ByVal suffix As String) As String
        If x <> 0 Then
            Return ", " & x.ToString("+0\%;-0\%;", Util.NumberFormatInfo) & suffix
        End If
        Return String.Empty
    End Function

    Private Function ElementalDmgBonusString() As String
        Dim value As Integer = wo.Values(LongValueKey.ElementalDmgBonus)
        If value <> 0 Then
            Return " +" & value
        End If
        Return String.Empty
    End Function

    Private Function MinMaxDamage() As String

        Dim WeaponVariance As Double = wo.Values(DoubleValueKey.Variance)

        Return ", " & Math.Round((WeaponMaxDamage() - (WeaponMaxDamage() * WeaponVariance)), 2).ToString(Util.NumberFormatInfo) & "-" & WeaponMaxDamage()
    End Function


    Overrides Function ToString() As String
        Dim result As String = String.Empty

        Try

            If wo IsNot Nothing Then
                If wo.ObjectClass = ObjectClass.Salvage Then
                    result = "Salvaged " & Plugin.GameData.SalvageInfo(wo.Values(LongValueKey.Material)).ToString()
                Else
                    result = wo.Name & CurrentItemLevelString()
                End If

                If wo.ObjectClass = ObjectClass.Armor Or wo.ObjectClass = ObjectClass.Clothing Then
                    result &= SetString() & ALString() & ImbueString() & TinkersString() & SpellDescriptions() & WieldString() _
                            & ActivateString() & LoreString() & RankString() & RaceString() & CraftString() & ProtsString() & ValueString() & BurdenString()

                ElseIf wo.ObjectClass = ObjectClass.Jewelry Then
                    result &= SetString() & ALString() & SpellDescriptions() & ImbueString() & TinkersString() & WieldString() _
                     & LoreString() & RankString() & RaceString() & CraftString() & ValueString() & BurdenString()

                ElseIf wo.ObjectClass = ObjectClass.MissileWeapon Then

                    result &= xModString(DamageBonusMissile, String.Empty) & ElementalDmgBonusString() &
                          ImbueString() & TinkersString() & xModString(WeaponMeleeBonus, "md") &
                          SpellDescriptions() & WieldString() & LoreString() & RankString() & RaceString() & CraftString() & ValueString() & BurdenString()

                ElseIf wo.ObjectClass = ObjectClass.MeleeWeapon Then

                    result &= ImbueString() & TinkersString() & MinMaxDamage() _
                           & xModString(WeaponAttackBonus, "a") & xModString(WeaponMeleeBonus, "md") &
                           SpellDescriptions() & WieldString() & LoreString() & RankString() & RaceString() & CraftString() & ValueString() & BurdenString()

                ElseIf wo.ObjectClass = ObjectClass.WandStaffOrb Then

                    result &= ImbueString() & TinkersString() & xModString(DamageVsMonsters, "vs. Monsters") &
                            xModString(WeaponMeleeBonus, "md") & xModString(WeaponManaCBonus, "mc") &
                        SpellDescriptions() & WieldString() & LoreString() & RankString() & RaceString() & CraftString() & ValueString() & BurdenString()

                ElseIf wo.HasIdData AndAlso (wo.ObjectClass = ObjectClass.Player OrElse wo.ObjectClass = ObjectClass.Monster) Then

                    result &= " lvl " & wo.Values(LongValueKey.CreatureLevel) & ", H" & HealthCurrent & "/" & HealthMax

                ElseIf wo.ObjectClass = ObjectClass.Salvage Then
                    result &= Me.DblValues(DoubleValueKey.SalvageWorkmanship).ToString("0.##")

                ElseIf wo.ObjectClass = Decal.Adapter.Wrappers.ObjectClass.Door Then
                    result = String.Empty
                ElseIf wo.ObjectClass = Decal.Adapter.Wrappers.ObjectClass.Gem Then
                    Dim t As Integer = Me.IntValues(LongValueKey.EquipableSlots)
                    If t = &H10000000 OrElse t = &H20000000 OrElse t = &H40000000 Then 'Aetheria
                        result &= SetString() & WieldString() & SpellDescriptions()
                    End If
                End If
            End If
        Catch ex As Exception
            Util.ErrorLogger(ex)
        End Try

        Return result
    End Function
End Class
