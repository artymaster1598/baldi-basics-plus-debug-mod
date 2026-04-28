using System.Collections.Generic;

namespace DebugMod
{
    internal enum Language { RU, EN }

    internal static class L
    {
        public static Language Current = Language.RU;

        public static string T(string key, params object[] args)
        {
            string tpl = _t.TryGetValue(key, out var d) && d.TryGetValue(Current, out var s) ? s : key;
            return args.Length > 0 ? string.Format(tpl, args) : tpl;
        }

        private static Dictionary<Language, string> D(string ru, string en) =>
            new Dictionary<Language, string> { [Language.RU] = ru, [Language.EN] = en };

        private static readonly Dictionary<string, Dictionary<Language, string>> _t =
            new Dictionary<string, Dictionary<Language, string>>
        {
            // ── Окно ────────────────────────────────────────────────────────────
            { "win.title",     D("  DEBUG CHEAT MENU  |  {0} — закрыть",  "  DEBUG CHEAT MENU  |  {0} — close") },
            { "btn.refresh",   D("↺  Обновить списки",                    "↺  Refresh lists") },

            // ── Вкладки ─────────────────────────────────────────────────────────
            { "tab.npc",      D("NPC",     "NPC") },
            { "tab.player",   D("ИГРОК",   "PLAYER") },
            { "tab.event",    D("СОБЫТИЕ", "EVENT") },
            { "tab.item",     D("ПРЕДМЕТ", "ITEM") },
            { "tab.baldi",    D("БАЛДИ",   "BALDI") },
            { "tab.time",     D("ВРЕМЯ",   "TIME") },
            { "tab.rooms",    D("КОМНАТЫ", "ROOMS") },
            { "tab.log",      D("ЛОГ",     "LOG") },
            { "tab.camera",   D("КАМЕРА",  "CAMERA") },
            { "tab.stats",    D("СТАТ",    "STATS") },
            { "tab.settings", D("НАСТР",   "SETTINGS") },

            // ── NPC ─────────────────────────────────────────────────────────────
            { "npc.spawn_tab",    D("Спавн NPC",    "Spawn NPC") },
            { "npc.live_tab",     D("Активные NPC", "Active NPCs") },
            { "npc.pfx_count",    D("Префабов: {0}  (базовая игра + моды)", "Prefabs: {0}  (base + mods)") },
            { "npc.live_count",   D("Активных на уровне: {0}",              "Active on level: {0}") },
            { "npc.spawn",        D("Спавн",   "Spawn") },
            { "npc.tp_to",        D("ТП↓",     "TP↓") },
            { "npc.freeze",       D("Заморз",  "Freeze") },
            { "npc.unfreeze",     D("Размрз",  "Unfrz") },
            { "npc.delete",       D("Удал",    "Del") },
            { "npc.freeze_all",   D("Заморозить всех",               "Freeze all") },
            { "npc.unfreeze_all", D("Разморозить всех",              "Unfreeze all") },
            { "npc.delete_all",   D("Удалить всех  (кроме Балди!)",  "Delete all  (except Baldi!)") },

            // ── Player ──────────────────────────────────────────────────────────
            { "pl.godmode",    D("  Бог-мод (нет урона, Балди не ловит)",           "  God Mode (no damage, Baldi can't catch)") },
            { "pl.stamina",    D("  Бесконечная выносливость",                      "  Infinite Stamina") },
            { "pl.stealth",    D("  Стелс (NPC тебя не замечают; Балди — нет)",     "  Stealth (NPCs ignore you; Baldi — no)") },
            { "pl.noclip",     D("  Ноклип  (WASD сквозь стены, Space↑ Ctrl↓)",    "  Noclip  (WASD through walls, Space↑ Ctrl↓)") },
            { "pl.fly",        D("  Полёт  (Space↑ Ctrl↓ или колёсико)",            "  Fly  (Space↑ Ctrl↓ or scroll)") },
            { "pl.fly_h",      D("  Высота: {0:F1}",  "  Height: {0:F1}") },
            { "pl.speed",      D("Скорость  ×{0:F1}", "Speed  ×{0:F1}") },
            { "pl.give_all",   D("Дать все предметы",             "Give all items") },
            { "pl.clear_inv",  D("Очистить инвентарь",            "Clear inventory") },
            { "pl.tp_random",  D("Телепорт в случайную комнату",  "Teleport to random room") },
            { "pl.fill_stam",  D("Восстановить выносливость",     "Restore stamina") },
            { "pl.collect_nb", D("Собрать все тетради",           "Collect all notebooks") },
            { "pl.solve_math", D("Решить все задачники",          "Solve all math machines") },
            { "pl.win",        D("★  Завершить уровень (победа)", "★  Complete level (win)") },
            { "pl.bookmarks",  D("Закладки телепорта:",           "Teleport bookmarks:") },
            { "pl.bm_save",    D("Сохр",     "Save") },
            { "pl.bm_tp",      D("ТП",       "TP") },
            { "pl.bm_empty",   D("— пусто —","— empty —") },

            // ── Event ───────────────────────────────────────────────────────────
            { "ev.count",    D("Всего событий: {0}  (базовая игра + моды)", "Total events: {0}  (base + mods)") },
            { "ev.start",    D("Старт",                  "Start") },
            { "ev.stop_all", D("Остановить все события", "Stop all events") },

            // ── Item ────────────────────────────────────────────────────────────
            { "it.count", D("Всего предметов: {0}  (базовая игра + моды)", "Total items: {0}  (base + mods)") },
            { "it.give",  D("Дать",   "Give") },
            { "it.drop",  D("На пол", "Drop") },

            // ── Baldi ───────────────────────────────────────────────────────────
            { "baldi.not_found",   D("Балди не найден на уровне.",             "Baldi not found on level.") },
            { "baldi.anger",       D("Злость: {0:F2}",                         "Anger: {0:F2}") },
            { "baldi.freeze",      D("  Заморозить Балди (отключить AI)",       "  Freeze Baldi (disable AI)") },
            { "baldi.to_player",   D("Телепортировать Балди к игроку",         "Teleport Baldi to player") },
            { "baldi.player_to",   D("Телепортировать игрока к Балди",         "Teleport player to Baldi") },
            { "baldi.remove",      D("Убрать Балди с уровня",                  "Remove Baldi from level") },
            { "baldi.anger_0",     D("Злость = 0",   "Anger = 0") },
            { "baldi.anger_max",   D("Злость = MAX", "Anger = MAX") },

            // ── Time ────────────────────────────────────────────────────────────
            { "time.scale",  D("Time.timeScale = {0:F2}x", "Time.timeScale = {0:F2}x") },
            { "time.pause",  D("Пауза  0x",     "Pause  0x") },
            { "time.slow",   D("Замедл. 0.25x", "Slow 0.25x") },
            { "time.normal", D("Норма  1x",      "Normal 1x") },
            { "time.fast",   D("Ускор. 3x",      "Fast 3x") },
            { "time.max",    D("Максимум  5x",   "Max  5x") },

            // ── Rooms ───────────────────────────────────────────────────────────
            { "rooms.count",   D("Комнат на уровне: {0}", "Rooms on level: {0}") },
            { "rooms.tp",      D("ТП",                    "TP") },
            { "rooms.refresh", D("Обновить список комнат","Refresh room list") },

            // ── Log ─────────────────────────────────────────────────────────────
            { "log.title",  D("BepInEx лог — {0} строк:", "BepInEx log — {0} lines:") },
            { "log.bottom", D("↓ В конец", "↓ Bottom") },
            { "log.clear",  D("Очистить",  "Clear") },

            // ── Camera ──────────────────────────────────────────────────────────
            { "cam.hitboxes",   D("  Показать хитбоксы NPC",                           "  Show NPC hitboxes") },
            { "cam.freecam",    D("  Свободная камера  (WASD + ПКМ поворот, Q↑ E↓)",  "  Free camera  (WASD + RMB rotate, Q↑ E↓)") },
            { "cam.speed",      D("  Скорость камеры: {0:F0}",                         "  Camera speed: {0:F0}") },
            { "cam.return",     D("Вернуть камеру к игроку", "Return camera to player") },
            { "cam.screenshot", D("Скриншот",  "Screenshot") },

            // ── Stats ───────────────────────────────────────────────────────────
            { "stat.fps",       D("FPS: {0:F1}",                                     "FPS: {0:F1}") },
            { "stat.pos",       D("Позиция: X={0:F1}  Y={1:F1}  Z={2:F1}",          "Position: X={0:F1}  Y={1:F1}  Z={2:F1}") },
            { "stat.room",      D("Комната: {0}",                                    "Room: {0}") },
            { "stat.stealth",   D("Стелс: {0}   Бессм: {1}",                        "Stealth: {0}   Invincible: {1}") },
            { "stat.no_player", D("Игрок не найден (не в уровне)",                  "Player not found (not in level)") },
            { "stat.baldi",     D("Балди: злость={0:F2}  AI={1}",                   "Baldi: anger={0:F2}  AI={1}") },
            { "stat.baldi_st",  D("  Состояние: {0}",                               "  State: {0}") },
            { "stat.notebooks", D("Тетради: собрано {0} / {1}  (осталось {2})",     "Notebooks: collected {0} / {1}  (left {2})") },
            { "stat.npcs",      D("NPC на уровне: {0}",                             "NPCs on level: {0}") },
            { "stat.frozen",    D("  Заморожено: {0}",                              "  Frozen: {0}") },
            { "stat.events",    D("Активных событий: {0}",                          "Active events: {0}") },
            { "stat.ts",        D("timeScale: {0:F2}×",                             "timeScale: {0:F2}×") },
            { "stat.ram",       D("RAM (Mono): {0:F1} MB",                          "RAM (Mono): {0:F1} MB") },
            { "stat.ytp",       D("YTP: {0}",                                       "YTP: {0}") },

            // ── Settings ────────────────────────────────────────────────────────
            { "set.keys",       D("── Клавиши ──────────────────────────", "── Key Bindings ────────────────────") },
            { "set.key_debug",  D("Debug Mode (вкл/выкл):", "Debug Mode (toggle):") },
            { "set.key_menu",   D("Открыть меню:",           "Open menu:") },
            { "set.waiting",    D("  Нажми любую клавишу...  (Esc = отмена)", "  Press any key...  (Esc = cancel)") },
            { "set.hud",        D("── HUD ──────────────────────────────", "── HUD ─────────────────────────────") },
            { "set.show_label", D("  Показывать индикатор [DEBUG ON]",  "  Show [DEBUG ON] indicator") },
            { "set.show_fps",   D("  Показывать FPS счётчик",           "  Show FPS counter") },
            { "set.show_msg",   D("  Показывать зелёные сообщения",     "  Show green messages") },
            { "set.params",     D("── Параметры ────────────────────────", "── Parameters ──────────────────────") },
            { "set.noclip_spd", D("Скорость ноклипа: {0:F0} ед/с",     "Noclip speed: {0:F0} u/s") },
            { "set.msg_dur",    D("Длительность сообщений: {0:F1} с",   "Message duration: {0:F1} s") },
            { "set.reset",      D("Сбросить всё к умолчанию",           "Reset all to default") },
            { "set.lang",       D("── Язык / Language ─────────────────", "── Language / Язык ─────────────────") },

            // ── Поиск ───────────────────────────────────────────────────────────
            { "search", D("Поиск:", "Search:") },

            // ── Сообщения ───────────────────────────────────────────────────────
            { "msg.no_level",     D("Уровень не загружен",   "Level not loaded") },
            { "msg.no_player",    D("Игрок не найден",        "Player not found") },
            { "msg.spawned",      D("Заспавнен: {0}",         "Spawned: {0}") },
            { "msg.err",          D("Ошибка: {0}",            "Error: {0}") },
            { "msg.deleted",      D("Удалён: {0}",            "Deleted: {0}") },
            { "msg.frozen",       D("Заморожен: {0}",         "Frozen: {0}") },
            { "msg.unfrozen",     D("Разморожен: {0}",        "Unfrozen: {0}") },
            { "msg.tp_to",        D("ТП к: {0}",              "TP to: {0}") },
            { "msg.given",        D("Выдан: {0}",             "Given: {0}") },
            { "msg.given_all",    D("Выдано предметов: {0}",  "Items given: {0}") },
            { "msg.inv_cleared",  D("Инвентарь очищен",       "Inventory cleared") },
            { "msg.dropped",      D("На пол: {0}",            "Dropped: {0}") },
            { "msg.no_pickup",    D("Шаблон Pickup не найден — выдан в инвентарь: {0}", "No Pickup template — added to inventory: {0}") },
            { "msg.event",        D("Событие: {0}",           "Event: {0}") },
            { "msg.events_off",   D("Все события остановлены","All events stopped") },
            { "msg.no_notebooks", D("Тетради не найдены",     "No notebooks found") },
            { "msg.nb_collected", D("Тетрадей собрано: {0}",  "Notebooks collected: {0}") },
            { "msg.math_solved",  D("Задачников решено: {0} из {1}", "Math machines solved: {0} of {1}") },
            { "msg.no_machines",  D("Задачники не найдены",   "No math machines found") },
            { "msg.win",          D("Собрано тетрадей: {0}, открыто выходов: {1}", "Notebooks: {0}, exits opened: {1}") },
            { "msg.tp_done",      D("Телепортирован",         "Teleported") },
            { "msg.no_tiles",     D("Нет тайлов",             "No tiles") },
            { "msg.b2p",          D("Балди → игрок",          "Baldi → player") },
            { "msg.p2b",          D("Игрок → Балди",          "Player → Baldi") },
            { "msg.freecam_on",   D("Свободная камера: WASD + ПКМ (поворот), Q/E вверх/вниз", "Free camera: WASD + RMB (rotate), Q/E up/down") },
            { "msg.freecam_off",  D("Камера возвращена к игроку", "Camera returned to player") },
            { "msg.no_cam",       D("Камера не найдена",      "Camera not found") },
            { "msg.screenshot",   D("Скриншот: {0}",          "Screenshot: {0}") },
            { "msg.no_cgm",       D("CoreGameManager не найден", "CoreGameManager not found") },
            { "msg.no_endseq",    D("EndSequence не найден",  "EndSequence not found") },
            { "msg.bm_saved",     D("Закладка {0} сохранена", "Bookmark {0} saved") },
            { "msg.bm_tp",        D("→ Закладка {0}",         "→ Bookmark {0}") },
            { "msg.set_reset",    D("Настройки сброшены",     "Settings reset") },
            { "msg.key_set",      D("Клавиша установлена: {0}","Key set: {0}") },
            { "msg.ytp",          D("+{0} YTP", "+{0} YTP") },
            { "msg.room_tp",      D("→ {0}", "→ {0}") },
            { "msg.refreshed",    D("Обновлено: {0} NPC · {1} предм. · {2} событий · {3} комнат",
                                    "Refreshed: {0} NPCs · {1} items · {2} events · {3} rooms") },
        };
    }
}
