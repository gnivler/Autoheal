using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using Kingmaker.Visual.Particles;
using UnityEngine;
using static Autoheal.Main;

// ReSharper disable InconsistentNaming

namespace Autoheal
{
    internal static class Helpers
    {
        // how many d8 are rolled
        private static Dictionary<string, float> map = new Dictionary<string, float>
        {
            {"Light", 1},
            {"Moderate", 2},
            {"Serious", 3},
            {"Critical", 4},
        };

        private static bool HealsTooMuch(ItemEntity item, UnitEntityData unit)
        {
            var matches = Regex.Match(item.Name, @"(Cure|Inflict) (Light|Moderate|Serious|Critical) Wounds");

            var severity = matches.Groups[2].ToString();
            var blueprint = item.Blueprint as BlueprintItemEquipment;
            // ReSharper disable once PossibleNullReferenceException
            var level = blueprint.CasterLevel;
            var clampedBonus =
                severity == "Light" ? Mathf.Min(level, 5) :
                severity == "Moderate" ? Mathf.Min(level, 10) :
                severity == "Serious" ? Mathf.Min(level, 15) :
                20;

            // 1 + 8 / 2 = 4.5 (average of 1d8)
            var averageHeal = map[severity] * 4.5 + clampedBonus;
            var maxHeal = map[severity] * 8 + clampedBonus;

            Log($"{unit.CharacterName}: Damage {GetMissingHP(unit)}HP, average heal {averageHeal}HP (max {maxHeal}HP)");
            if (GetMissingHP(unit) <= averageHeal)
            {
                Log($"May heal too much");
                return true;
            }

            Log("Won't heal too much");

            return false;
        }

        private static int GetMissingHP(UnitEntityData unit) => unit.MaxHP - unit.HPLeft;

        private static bool IsOnBelt(ItemEntity item)
        {
            var flag = false;
            foreach (var character in Game.Instance.Player.PartyCharacters)
            {
                var unit = character.Value;
                var slots = unit.Body.QuickSlots.Take(5);
                if (slots.Any(x => x.HasItem && x.Item == item))
                {
                    flag = true;
                    break;
                }
            }

            return flag;
        }

        private static ItemEntity FindLowestHealingConsumable(UnitEntityData unit)
        {
            try
            {
                var inventory = Game.Instance.Player.Inventory.Items;
                var usableInventory = inventory.Where(x => IsOnBelt(x) == false).ToArray();
                var lightItems = usableInventory.Where(x => IsValidConsumable(x, "Light")).ToList();
                var moderateItems = usableInventory.Where(x => IsValidConsumable(x, "Moderate")).ToList();
                var seriousItems = usableInventory.Where(x => IsValidConsumable(x, "Serious")).ToList();
                var criticalItems = usableInventory.Where(x => IsValidConsumable(x, "Critical")).ToList();
                var isUndead = unit.Descriptor.IsUndead;

                Log($"{lightItems.Count}-{moderateItems.Count}-{seriousItems.Count}");
                var verb = isUndead ? "Inflict" : "Cure";
                if (Enumerable.Any(lightItems, x => x.Name.Contains(verb)))
                {
                    Log("light");
                    return lightItems.First(x => x.Name.Contains(verb));
                }

                if (Enumerable.Any(moderateItems, x => x.Name.Contains(verb)))
                {
                    Log("moderate");
                    return moderateItems.First(x => x.Name.Contains(verb));
                }

                if (Enumerable.Any(seriousItems, x => x.Name.Contains(verb)))
                {
                    Log("serious");
                    return seriousItems.First(x => x.Name.Contains(verb));
                }

                if (Enumerable.Any(criticalItems, x => x.Name.Contains(verb)))
                {
                    Log("critical");
                    return criticalItems.First(x => x.Name.Contains(verb));
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            Log("BOMB");
            return null;
        }

        private static bool IsValidConsumable(ItemEntity itemEntity, string severity)
        {
            return (itemEntity.Name.StartsWith("Scroll of") ||
                    itemEntity.Name.StartsWith("Potion of")) &&
                   itemEntity.Name.EndsWith("Wounds") &&
                   itemEntity.Name.Contains(severity);
        }

        internal static void Heal(UnitEntityData unit)
        {
            while (GetMissingHP(unit) > 0)
            {
                var item = FindLowestHealingConsumable(unit);
                if (item == null)
                {
                    break;
                }

                Log($"Item {item}");
                if (mod.Settings.miser && HealsTooMuch(item, unit))
                {
                    break;
                }

                try
                {
                    Log("Used? " + item.TryUseFromInventory(unit, unit));
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        internal static void HealMagic()
        {
            // code copied from RestController.UseSpells()
            foreach (UnitEntityData unitEntityData in Game.Instance.Player.PartyCharacters)
            {
                if (unitEntityData.Descriptor.State.IsFinallyDead)
                {
                    if (unitEntityData.Descriptor.State.ResurrectOnRest)
                    {
                        unitEntityData.Descriptor.Resurrect(0f, true);
                        unitEntityData.Descriptor.State.LifeState = UnitLifeState.Conscious;
                        unitEntityData.Damage = unitEntityData.Stats.HitPoints - Math.Min(1, unitEntityData.Descriptor.Progression.CharacterLevel);
                        if (unitEntityData.Descriptor.IsPet)
                        {
                            Vector3 vector = unitEntityData.Descriptor.Master.Value.Position;
                            if (AstarPath.active)
                            {
                                FreePlaceSelector.PlaceSpawnPlaces(2, unitEntityData.View.Corpulence, vector);
                                vector = FreePlaceSelector.GetRelaxedPosition(1, true);
                            }

                            unitEntityData.Position = vector;
                        }
                    }
                }
            }

            if (RestController.Instance.m_ScriptedRest || Game.Instance.Player.Camping.UseSpells)
            {
                var fx = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "SpellHeal00");
                RestController.Instance.UpdateCharactersToBeHealedOnRest();
                var partyHP = new Dictionary<string, int>();
                foreach (var u in Game.Instance.Player.ControllableCharacters)
                    partyHP.Add(u.UniqueId, u.HPLeft);

                // specific healers
                if (SelectionManager.Instance.SelectedUnits.Count > 0)
                {
                    foreach (var unit in SelectionManager.Instance.SelectedUnits)
                    {
                        unit.UseSpellsOnRest();
                    }
                }
                // nobody is selected
                else
                {
                    foreach (UnitEntityData unit in RestController.Instance.CharactersToBeHealedOnRest)
                    {
                        try
                        {
                            if (unit.UseSpellsOnRest())
                            {
                                FxHelper.SpawnFxOnUnit(fx, unit.View);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                    }
                }

                foreach (var u in Game.Instance.Player.ControllableCharacters)
                {
                    if (u.HPLeft > partyHP[u.UniqueId])
                    {
                        FxHelper.SpawnFxOnUnit(fx, u.View);
                    }
                }
            }
        }

        internal static void HealAllConsumables()
        {
            foreach (var character in Game.Instance.Player.PartyCharacters)
            {
                Heal(character.Value);
            }
        }

        internal static void HealOneConsumables()
        {
            if (!SelectionManager.Instance.IsSingleSelected)
            {
                return;
            }

            var unit = UIUtility.GetCurrentCharacter();
            Heal(unit);
        }
    }
}
