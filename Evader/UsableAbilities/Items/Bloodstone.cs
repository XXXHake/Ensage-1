﻿namespace Evader.UsableAbilities.Items
{
    using System;
    using System.Linq;

    using Base;

    using Common;

    using Core;
    using Core.Menus;

    using Data;

    using Ensage;
    using Ensage.Common.AbilityInfo;
    using Ensage.Common.Extensions;

    using EvadableAbilities.Base;

    using AbilityType = Data.AbilityType;

    internal class Bloodstone : UsableAbility, IDisposable
    {
        #region Constructors and Destructors

        public Bloodstone(Ability ability, AbilityType type, AbilityCastTarget target = AbilityCastTarget.Self)
            : base(ability, type, target)
        {
            Game.OnUpdate += OnUpdate;
        }

        #endregion

        #region Properties

        private static UsableAbilitiesMenu Menu => Variables.Menu.UsableAbilities;

        #endregion

        #region Public Methods and Operators

        public override bool CanBeCasted(EvadableAbility ability, Unit unit)
        {
            if (Sleeper.Sleeping || !Ability.CanBeCasted() || !Hero.CanUseItems())
            {
                return false;
            }

            Debugger.WriteLine("// * Bloodstone calculations");

            var damageSource = ability.AbilityOwner;

            if (ability.IsDisable)
            {
                var totalDamage = (damageSource.MinimumDamage + damageSource.BonusDamage)
                                  * damageSource.SecondsPerAttack * 2;
                var totalMana = damageSource.Mana;

                foreach (var spell in damageSource.Spellbook.Spells.Where(x => x.Level > 0 && x.Cooldown <= 0))
                {
                    if (totalMana >= spell.ManaCost)
                    {
                        try
                        {
                            totalDamage += (int)Math.Round(AbilityDamage.CalculateDamage(spell, damageSource, unit));
                            totalMana -= spell.ManaCost;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                Debugger.WriteLine("// * Incoming damage: " + totalDamage + " from: " + damageSource.GetName());
                Debugger.WriteLine("// * HP left: " + unit.Health);

                return unit.Health <= totalDamage;
            }

            float damage;

            try
            {
                damage = (int)Math.Round(AbilityDamage.CalculateDamage(ability.Ability, ability.AbilityOwner, Hero));
            }
            catch (Exception)
            {
                return false;
            }

            if (damage > 850)
            {
                Debugger.WriteLine("// * Damage calculations probably incorrect // " + damage + " // " + ability.Name);
                damage = 350;
            }

            Debugger.WriteLine("// * Incoming damage: " + damage + " from: " + ability.Name);
            Debugger.WriteLine("// * HP left: " + unit.Health);

            return unit.Health <= damage;
        }

        public void Dispose()
        {
            Game.OnUpdate -= OnUpdate;
        }

        public override float GetRequiredTime(EvadableAbility ability, Unit unit, float remainingTime)
        {
            return CastPoint;
        }

        public override void Use(EvadableAbility ability, Unit target)
        {
            Ability.UseAbility(Hero.InFront(100));
            Sleep();
        }

        public override bool UseCustomSleep(out float sleepTime)
        {
            sleepTime = 0;
            return true;
        }

        #endregion

        #region Methods

        private void OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Sleeper.Sleeping || !Menu.BloodstoneAutoSuicide)
            {
                return;
            }

            if (!Hero.IsAlive || !Ability.CanBeCasted() || !Hero.CanUseItems())
            {
                return;
            }

            if ((float)Hero.Health / Hero.MaximumHealth * 100 <= Menu.BloodstoneHpThreshold
                && ObjectManager.GetEntitiesParallel<Hero>()
                    .Any(
                        x =>
                            x.IsValid && x.IsAlive && !x.IsIllusion && x.Team != HeroTeam
                            && x.Distance2D(Hero) < Menu.BloodstoneEnemyRange))
            {
                Debugger.WriteLine("// * Bloodstone auto suicide");
                Use(null, null);
            }
        }

        #endregion
    }
}