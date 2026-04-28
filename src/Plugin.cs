using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DebugMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    public class DebugModPlugin : BaseUnityPlugin
    {
        public const string PluginGuid    = "debugmod.bbplus.cheats";
        public const string PluginName    = "Debug Mode";
        public const string PluginVersion = "1.0.0";

        internal static DebugModPlugin Instance = null!;
        internal static ManualLogSource Log      = null!;

        // ── Клавиши ──────────────────────────────────────────────────────────────
        internal static ConfigEntry<KeyCode> KeyToggleDebug = null!;
        internal static ConfigEntry<KeyCode> KeyToggleMenu  = null!;

        // ── Язык ─────────────────────────────────────────────────────────────────
        internal static ConfigEntry<string> CfgLanguage     = null!;

        // ── HUD ──────────────────────────────────────────────────────────────────
        internal static ConfigEntry<bool> CfgShowFps        = null!;
        internal static ConfigEntry<bool> CfgShowDebugLabel = null!;
        internal static ConfigEntry<bool> CfgShowMsgHud     = null!;

        // ── Геймплей ─────────────────────────────────────────────────────────────
        internal static ConfigEntry<float> CfgNoclipSpeed   = null!;
        internal static ConfigEntry<float> CfgMsgDuration   = null!;

        private Harmony _harmony = null!;

        private void Awake()
        {
            Instance = this;
            Log      = Logger;

            Log.LogInfo($"=== {PluginName} v{PluginVersion} AWAKE START ===");

            try
            {
                KeyToggleDebug = Config.Bind("Keys", "ToggleDebugMode", KeyCode.F1,
                    "Клавиша включения/выключения Debug Mode");
                KeyToggleMenu = Config.Bind("Keys", "ToggleCheatMenu", KeyCode.F2,
                    "Клавиша открытия/закрытия меню читов");

                CfgLanguage = Config.Bind("General", "Language", "EN", "UI language: RU or EN");
                L.Current   = CfgLanguage.Value == "RU" ? Language.RU : Language.EN;

                CfgShowFps        = Config.Bind("HUD", "ShowFPS",        true,  "Показывать FPS счётчик");
                CfgShowDebugLabel = Config.Bind("HUD", "ShowDebugLabel",  true,  "Показывать индикатор [DEBUG ON]");
                CfgShowMsgHud     = Config.Bind("HUD", "ShowMessages",    true,  "Показывать зелёные HUD-сообщения");

                CfgNoclipSpeed  = Config.Bind("Gameplay", "NoclipSpeed",    25f, "Скорость ноклипа (единиц/с)");
                CfgMsgDuration  = Config.Bind("Gameplay", "MessageDuration", 3f, "Длительность HUD-сообщений (секунд)");
                Log.LogInfo("Config: OK");
            }
            catch (Exception ex) { Log.LogError($"Config ОШИБКА: {ex}"); }

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("Harmony: OK");
            }
            catch (Exception ex)
            {
                // Патчи не критичны — меню работает и без них
                Log.LogWarning($"Harmony ОШИБКА (не критично): {ex.Message}");
            }

            try
            {
                var menuObj = new GameObject("DebugModMenu");
                DontDestroyOnLoad(menuObj);
                menuObj.AddComponent<DebugMenuBehaviour>();
                Log.LogInfo("DebugMenuBehaviour: OK");
            }
            catch (Exception ex) { Log.LogError($"GameObject ОШИБКА: {ex}"); }

            try
            {
                BepInEx.Logging.Logger.Listeners.Add(new DebugLogListener());
                Log.LogInfo("LogListener: OK");
            }
            catch (Exception ex) { Log.LogWarning($"LogListener ОШИБКА: {ex.Message}"); }

            Log.LogInfo($"=== {PluginName} загружен. F2 = Меню ===");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Глобальное состояние Debug Mode
    // ─────────────────────────────────────────────────────────────
    internal static class DebugState
    {
        /// Активен ли Debug Mode (ничего не считается)
        public static bool DebugModeActive { get; private set; } = false;

        /// Открыто ли меню читов (устанавливается напрямую из DebugMenuBehaviour)
        public static bool MenuVisible { get; set; } = false;

        /// Бог-мод: игрок не может получить урон / быть пойман
        public static bool GodMode { get; set; } = false;

        /// Бесконечная выносливость
        public static bool InfiniteStamina { get; set; } = false;

        /// Ноклип (передвижение сквозь стены)
        public static bool Noclip { get; set; } = false;

        /// Множитель скорости игрока (1.0 = нормальная)
        public static float SpeedMultiplier { get; set; } = 1.0f;

        /// Стелс (NPC не замечают игрока)
        public static bool StealthMode { get; set; } = false;

        /// Режим полёта
        public static bool FlyMode { get; set; } = false;

        /// Целевая высота полёта (в Unity-единицах)
        public static float FlyHeight { get; set; } = 5f;

        /// Показывать хитбоксы NPC
        public static bool ShowHitboxes { get; set; } = false;

        /// Свободная камера
        public static bool FreeCamMode { get; set; } = false;

        /// Множитель времени (Time.timeScale)
        public static float TimeScale { get; set; } = 1f;

        public static void ToggleDebugMode()
        {
            DebugModeActive = !DebugModeActive;
            if (!DebugModeActive)
            {
                GodMode         = false;
                InfiniteStamina = false;
                StealthMode     = false;
                Noclip          = false;
                FlyMode         = false;
                FreeCamMode     = false;
                ShowHitboxes    = false;
                SpeedMultiplier = 1.0f;
                TimeScale       = 1.0f;
                MenuVisible     = false;
                UnityEngine.Time.timeScale = 1f;
            }
            DebugModPlugin.Log.LogInfo($"Debug Mode: {(DebugModeActive ? "ON" : "OFF")}");
        }

        /// Включить debug mode без toggle (используется при открытии меню через F2)
        public static void EnableDebugMode()
        {
            DebugModeActive = true;
            DebugModPlugin.Log.LogInfo("Debug Mode: ON (авто)");
        }

        public static void ToggleMenu()
        {
            MenuVisible = !MenuVisible;
        }
    }
}
