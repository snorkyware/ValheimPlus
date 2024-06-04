using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.UpdateTTL))]
    public static class Se_Rested_UpdateTtl_Patch
    {
        /// <summary>
        /// Updates the time per rested comfort level.
        /// </summary>
        [UsedImplicitly]
        public static void Prefix(SE_Rested __instance)
        {
            var config = Configuration.Current.Player;
            if (!config.IsEnabled) return;
            __instance.m_TTLPerComfortLevel = config.restSecondsPerComfortLevel;
        }
    }

    /// <summary>
    /// Changes the radius in which pieces contribute to the rested bonus.
    /// </summary>
    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.GetNearbyComfortPieces))]
    public static class Se_Rested_GetNearbyComfortPieces_Transpiler
    {
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var config = Configuration.Current.Building;
            // ReSharper disable once CompareOfFloatsByEqualityOperator expecting exact constant
            if (!config.IsEnabled || config.pieceComfortRadius == 10f) return instructions;

            var il = instructions.ToList();
            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].opcode != OpCodes.Ldc_R4) continue;
                il[i].operand = Mathf.Clamp(config.pieceComfortRadius, 1f, 300f);
                return il;
            }

            ValheimPlusPlugin.Logger.LogError(
                "Couldn't transpile `SE_Rested.GetNearbyComfortPieces` for `Building.pieceComfortRadius` config!");
            return il;
        }
    }
}