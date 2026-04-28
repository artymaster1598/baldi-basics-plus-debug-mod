using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
// Harmony-патчи для Debug Mode.
//
// Каждый патч помечен [HarmonyPatch] — если метод не найден в конкретной
// версии игры, Harmony выдаст предупреждение и пропустит этот патч.
// Это НЕ ломает остальные патчи и НЕ ломает меню.
// ═══════════════════════════════════════════════════════════════════════════

namespace DebugMod
{
    // ────────────────────────────────────────────────────────────────────────
    // Блокировать завершение игры в Debug Mode + God Mode
    // ────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(CoreGameManager), "EndGame")]
    internal static class Patch_CGM_EndGame
    {
        [HarmonyPrefix]
        static bool Prefix()
        {
            if (DebugState.DebugModeActive && DebugState.GodMode)
            {
                DebugModPlugin.Log.LogInfo("[Debug] EndGame заблокирован (God Mode)");
                return false;
            }
            return true;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Блокировать урон игроку в God Mode
    // ────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(PlayerManager), "DamagePlayer")]
    internal static class Patch_Player_Damage
    {
        [HarmonyPrefix]
        static bool Prefix() => !(DebugState.DebugModeActive && DebugState.GodMode);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Блокировать поимку игрока NPC в God Mode
    // ────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(PlayerManager), "CaughtBy")]
    internal static class Patch_Player_CaughtBy
    {
        [HarmonyPrefix]
        static bool Prefix() => !(DebugState.DebugModeActive && DebugState.GodMode);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Блокировать расход выносливости в Infinite Stamina
    // ────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(PlayerMovement), "DrainStam")]
    internal static class Patch_Player_DrainStam
    {
        [HarmonyPrefix]
        static bool Prefix() => !(DebugState.DebugModeActive && DebugState.InfiniteStamina);
    }
}
