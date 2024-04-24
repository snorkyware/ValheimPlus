using System;
using System.Collections.Generic;
using HarmonyLib;
using ValheimPlus.Configurations;
using System.Diagnostics;
using JetBrains.Annotations;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Disable weather damage
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
    public static class WearNTear_UpdateWear_Patch
    {
        [UsedImplicitly]
        private static void Prefix(float time, ref float ___m_rainTimer)
        {
            if (Configuration.Current.Building.IsEnabled && Configuration.Current.Building.noWeatherDamage)
            {
                // if the rain timer is perpetually set to the current time in prefix,
                // then the timer will never become large enough to trigger the weather effect.
                ___m_rainTimer = time;
            }
        }
    }

    /// <summary>
    /// Removes the integrity check for having a connected piece to the ground.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
    public static class WearNTear_HaveSupport_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (Configuration.Current.Building.IsEnabled && Configuration.Current.StructuralIntegrity.disableStructuralIntegrity)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Disable damage to player structures
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
    public static class WearNTear_ApplyDamage_Patch
    {
        private static readonly HashSet<string> UpdateWearMethodNames = new()
        {
            "UpdateWear",
            "DMD<WearNTear::UpdateWear>",
        };
        
        private static bool Prefix(ref WearNTear __instance, ref float damage)
        {
            // Gets the name of the method calling the ApplyDamage method
            StackTrace stackTrace = new StackTrace();
            string callingMethod = stackTrace.GetFrame(2).GetMethod().Name;

            if (!(Configuration.Current.StructuralIntegrity.IsEnabled && __instance.m_piece && __instance.m_piece.IsPlacedByPlayer() && !UpdateWearMethodNames.Contains(callingMethod)))
                return true;

            if (__instance.m_piece.m_name.StartsWith("$ship"))
            {
                if (Configuration.Current.StructuralIntegrity.disableDamageToPlayerBoats ||
                    (Configuration.Current.StructuralIntegrity.disableWaterDamageToPlayerBoats &&
                     stackTrace.GetFrame(15).GetMethod().Name == "UpdateWaterForce")) return false;

                return true;
            }
            if (__instance.m_piece.m_name.StartsWith("$tool_cart"))
            {
                if (Configuration.Current.StructuralIntegrity.disableDamageToPlayerCarts ||
                    (Configuration.Current.StructuralIntegrity.disableWaterDamageToPlayerCarts &&
                     stackTrace.GetFrame(15).GetMethod().Name == "UpdateWaterForce")) return false;

                return true;
            }
            return !Configuration.Current.StructuralIntegrity.disableDamageToPlayerStructures;
        }
    }

    /// <summary>
    /// Disable structural integrity
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.GetMaterialProperties))]
    public static class WearNTear_GetMaterialProperties_Patch
    {
        private static readonly Dictionary<WearNTear.MaterialType, Func<float>> Multipliers = new()
        {
            [WearNTear.MaterialType.Wood] = () => Configuration.Current.StructuralIntegrity.wood,
            [WearNTear.MaterialType.Stone] = () => Configuration.Current.StructuralIntegrity.stone,
            [WearNTear.MaterialType.Iron] = () => Configuration.Current.StructuralIntegrity.iron,
            [WearNTear.MaterialType.HardWood] = () => Configuration.Current.StructuralIntegrity.hardWood,
            [WearNTear.MaterialType.Marble] = () => Configuration.Current.StructuralIntegrity.marble,
            [WearNTear.MaterialType.Ashstone] = () => Configuration.Current.StructuralIntegrity.ashstone,
            [WearNTear.MaterialType.Ancient] = () => Configuration.Current.StructuralIntegrity.ancient,
        };

        [UsedImplicitly]
        private static void Postfix(ref WearNTear __instance, ref float horizontalLoss, ref float verticalLoss)
        {
            if (!Configuration.Current.StructuralIntegrity.IsEnabled) return;
            if (Configuration.Current.StructuralIntegrity.disableStructuralIntegrity)
            {
                verticalLoss = 0f;
                horizontalLoss = 0f;
                return;
            }

            // Unknown material type, don't modify.
            if (!Multipliers.TryGetValue(__instance.m_materialType, out var multiplier)) return;
            
            // scale the loss number between its current number and 0 based on the user config.
            float clampedMultiplier = Helper.Clamp(multiplier(), 0 , 100);
            verticalLoss -= verticalLoss / 100 * clampedMultiplier;
            horizontalLoss -= horizontalLoss / 100 * clampedMultiplier;
        }
    }
}
