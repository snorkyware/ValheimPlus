using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(Turret), nameof(Turret.Awake))]
    public static class Turret_Awake_Patch
    {
        /// <summary>
        /// Configure the turret on wakeup
        /// </summary>
        [UsedImplicitly]
        private static void Prefix(Turret __instance)
        {
            var config = Configuration.Current.Turret;
            if (!config.IsEnabled) return;
            if (config.ignorePlayers) __instance.m_targetPlayers = false;
            __instance.m_turnRate = Helper.applyModifierValue(__instance.m_turnRate, config.turnRate);
            __instance.m_attackCooldown = Helper.applyModifierValue(__instance.m_attackCooldown, config.attackCooldown);
            __instance.m_viewDistance = Helper.applyModifierValue(__instance.m_viewDistance, config.viewDistance);
        }
    }

    [HarmonyPatch(typeof(Turret), nameof(Turret.ShootProjectile))]
    public static class Turret_ShootProjectile_Patch
    {
        private static readonly FieldInfo Field_Turret_M_MaxAmmo =
            AccessTools.Field(typeof(Turret), nameof(Turret.m_maxAmmo));

        private static readonly FieldInfo Field_Attack_M_ProjectileVel =
            AccessTools.Field(typeof(Attack), nameof(Attack.m_projectileVel));

        private static readonly FieldInfo Field_Attack_M_ProjectileAccuracy =
            AccessTools.Field(typeof(Attack), nameof(Attack.m_projectileAccuracy));

        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.Turret;
            if (!config.IsEnabled) return instructions;

            var unlimitedAmmoEnabled = config.unlimitedAmmo;
            var projectileVelocityEnabled = config.projectileVelocity != 0f;
            var projectileAccuracyEnabled = config.projectileAccuracy != 0f;
            if (!unlimitedAmmoEnabled && !projectileVelocityEnabled && !projectileAccuracyEnabled) return instructions;

            var il = instructions.ToList();
            int maxAmmoInstructionIndex = -1;
            int projectileVelocityInstructionIndex = -1;
            int projectileAccuracyInstructionIndex = -1;
            for (int i = 0; i < il.Count; ++i)
            {
                if (unlimitedAmmoEnabled &&
                    i + 2 < il.Count &&
                    il[i].LoadsField(Field_Turret_M_MaxAmmo) &&
                    il[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                    il[i + 2].Branches(out _))
                {
                    // instead of if (maxAmmo > 0) then decrement ammo, we change the 0 to max int value so that the
                    // condition is never satisfied.
                    il[i + 1] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                    maxAmmoInstructionIndex = i + 1;
                }

                if (projectileVelocityEnabled && il[i].LoadsField(Field_Attack_M_ProjectileVel))
                {
                    // apply Turret.projectileVelocity when `Attack.m_projectileVel` is loaded
                    var multiplier = Helper.applyModifierValue(1f, config.projectileVelocity);
                    il.InsertRange(i + 1, new CodeInstruction[]
                        {
                            new(OpCodes.Ldc_R4, multiplier),
                            new(OpCodes.Mul)
                        }
                    );
                    projectileVelocityInstructionIndex = i;
                }

                if (projectileAccuracyEnabled && il[i].LoadsField(Field_Attack_M_ProjectileAccuracy))
                {
                    // apply Turret.projectileVelocity when `Attack.m_projectileAccuracy` is loaded
                    // we invert projectileVelocity so that bigger number is better accuracy.
                    var multiplier = Helper.applyModifierValue(1f, -config.projectileAccuracy);
                    il.InsertRange(i + 1, new CodeInstruction[]
                        {
                            new(OpCodes.Ldc_R4, multiplier),
                            new(OpCodes.Mul)
                        }
                    );
                    projectileAccuracyInstructionIndex = i;
                }
            }

            if (unlimitedAmmoEnabled && maxAmmoInstructionIndex == -1)
                ValheimPlusPlugin.Logger.LogError(
                    "Couldn't transpile `Turret.ShootProjectile` for `Turret.unlimitedAmmo` config!");

            if (projectileVelocityEnabled && projectileVelocityInstructionIndex == -1)
                ValheimPlusPlugin.Logger.LogError(
                    "Couldn't transpile `Turret.ShootProjectile` for `Turret.projectileVelocity` config!");

            if (projectileAccuracyEnabled && projectileAccuracyInstructionIndex == -1)
                ValheimPlusPlugin.Logger.LogError(
                    "Couldn't transpile `Turret.ShootProjectile` for `Turret.projectileAccuracy` config!");

            return il.AsEnumerable();
        }
    }
}