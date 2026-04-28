using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DebugMod
{
    public class DebugMenuBehaviour : MonoBehaviour
    {
        // ── Табы ─────────────────────────────────────────────────────────────────
        private enum Tab { NPC, Player, Event, Item, Baldi, Time, Rooms, Log, Camera, Stats, Settings }
        private Tab _tab = Tab.NPC;

        // ── Окно ─────────────────────────────────────────────────────────────────
        private Rect _win = new Rect(20f, 20f, 540f, 640f);

        // ── Поиск ────────────────────────────────────────────────────────────────
        private string _srNpc = "", _srItem = "", _srEvt = "", _srRoom = "";

        // ── Кэши ─────────────────────────────────────────────────────────────────
        private NPC[]            _npcPfx = Array.Empty<NPC>();
        private ItemObject[]     _items  = Array.Empty<ItemObject>();
        private RandomEvent[]    _events = Array.Empty<RandomEvent>();
        private RoomController[] _rooms  = Array.Empty<RoomController>();

        // ── Скролы ───────────────────────────────────────────────────────────────
        private Vector2 _scNpc, _scNpcLive, _scItem, _scEvt, _scRoom, _scLog, _scStats;

        // ── NPC sub-view ──────────────────────────────────────────────────────────
        private bool _liveNpcs = false;

        // ── Шаблон Pickup ─────────────────────────────────────────────────────────
        private Pickup? _pickupPfx;

        // ── HUD-сообщение ─────────────────────────────────────────────────────────
        private string _msg = "";
        private float  _msgT = 0f;

        // ── Стили ────────────────────────────────────────────────────────────────
        private GUISkin? _skin;
        private bool     _skinOk = false;

        // ── Курсор ───────────────────────────────────────────────────────────────
        private CursorLockMode _prevLock;
        private bool           _prevCurVis;

        // ── Baldi reflection ─────────────────────────────────────────────────────
        private Baldi?     _baldi;
        private FieldInfo? _angerFI;

        // ── Свободная камера ──────────────────────────────────────────────────────
        private Camera?    _freeCam;
        private Transform? _origParent;
        private Vector3    _origLocalPos;
        private Quaternion _origLocalRot;
        private float      _camSpeed = 30f;

        // ── FPS ───────────────────────────────────────────────────────────────────
        private float _fps      = 0f;
        private float _fpsTimer = 0f;
        private int   _fpsCnt   = 0;

        // ── Закладки (3 слота) ────────────────────────────────────────────────────
        private readonly Vector3[] _bookmarks   = new Vector3[3];
        private readonly bool[]    _bookmarkSet = new bool[3];

        // ── Rebind ────────────────────────────────────────────────────────────────
        private enum RebindTarget { None, Debug, Menu }
        private RebindTarget _rebinding = RebindTarget.None;

        // ── Cached reflection ─────────────────────────────────────────────────────
        private static FieldInfo? _invisibleFI;
        private static FieldInfo? _invisiblesFI;
        private static FieldInfo? _currentRoomFI;

        // ═══════════════════════════════════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════════════════════════════════
        private void Update()
        {
            _fpsCnt++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f) { _fps = _fpsCnt / _fpsTimer; _fpsCnt = 0; _fpsTimer = 0f; }

            // Rebind перехватывает ввод раньше всего
            if (_rebinding != RebindTarget.None)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _rebinding = RebindTarget.None;
                }
                else
                {
                    foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        if (!Input.GetKeyDown(kc)) continue;
                        if (_rebinding == RebindTarget.Debug) DebugModPlugin.KeyToggleDebug.Value = kc;
                        else                                  DebugModPlugin.KeyToggleMenu.Value  = kc;
                        ShowMsg(L.T("msg.key_set", kc));
                        _rebinding = RebindTarget.None;
                        break;
                    }
                }
                return;
            }

            if (Input.GetKeyDown(DebugModPlugin.KeyToggleDebug.Value))
                DebugState.ToggleDebugMode();

            if (Input.GetKeyDown(DebugModPlugin.KeyToggleMenu.Value))
            {
                if (!DebugState.MenuVisible) OpenMenu();
                else                         CloseMenu();
            }

            if (!DebugState.DebugModeActive) return;

            if (DebugState.InfiniteStamina) FillStamina();
            if (DebugState.Noclip)          DoNoclip();
            if (DebugState.FlyMode)         DoFly();
            if (DebugState.FreeCamMode && _freeCam != null) DoFreeCamera();

            if (!Mathf.Approximately(Time.timeScale, DebugState.TimeScale))
                Time.timeScale = DebugState.TimeScale;

            // Стелс: синхронизируем invisible (bool) и invisibles (int-счётчик)
            try
            {
                var pm = GetPlayer();
                if (pm != null)
                {
                    var t = pm.GetType();
                    _invisibleFI  ??= t.GetField("invisible",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    _invisiblesFI ??= t.GetField("invisibles",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    _invisibleFI?.SetValue(pm, DebugState.StealthMode);
                    _invisiblesFI?.SetValue(pm, DebugState.StealthMode ? 9999 : 0);
                }
            }
            catch { }

            if (_msgT > 0f) _msgT -= Time.unscaledDeltaTime;
        }

        private void LateUpdate()
        {
            if (!DebugState.DebugModeActive || !DebugState.FlyMode) return;
            try
            {
                var pm = GetPlayer();
                if (pm == null) return;
                Vector3 pos = pm.transform.position;
                pos.y = DebugState.FlyHeight;
                pm.transform.position = pos;
            }
            catch { }
        }

        private void OpenMenu()
        {
            _prevLock   = Cursor.lockState;
            _prevCurVis = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            DebugState.EnableDebugMode();
            DebugState.MenuVisible = true;
            RefreshAll();
        }

        private void CloseMenu()
        {
            Cursor.lockState = _prevLock;
            Cursor.visible   = _prevCurVis;
            DebugState.MenuVisible = false;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ONGUI
        // ═══════════════════════════════════════════════════════════════════════════
        private void OnGUI()
        {
            if (DebugModPlugin.CfgShowDebugLabel.Value)
            {
                string fpsStr = DebugModPlugin.CfgShowFps.Value ? $"  {_fps:F0} fps" : "";
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUI.Label(new Rect(Screen.width - 230f, 4f, 225f, 18f),
                    DebugState.DebugModeActive
                        ? $"[DEBUG ON] {DebugModPlugin.KeyToggleMenu.Value}=Menu{fpsStr}"
                        : $"DebugMod: {DebugModPlugin.KeyToggleMenu.Value}=Menu{fpsStr}");
                GUI.color = Color.white;

                if (DebugState.DebugModeActive)
                {
                    GUI.color = new Color(1f, 0.3f, 0.3f, 0.9f);
                    GUI.Label(new Rect(8f, 8f, 440f, 22f),
                        $"[DEBUG MODE ON]  {DebugModPlugin.KeyToggleMenu.Value}=Menu  |  {DebugModPlugin.KeyToggleDebug.Value}=Off");
                    GUI.color = Color.white;
                }
            }

            if (DebugModPlugin.CfgShowMsgHud.Value && _msgT > 0f)
            {
                GUI.color = new Color(0.25f, 1f, 0.45f, Mathf.Clamp01(_msgT));
                GUI.Label(new Rect(8f, 32f, 600f, 22f), _msg);
                GUI.color = Color.white;
            }

            if (DebugState.ShowHitboxes) DrawHitboxes();

            if (!DebugState.MenuVisible) return;

            if (!_skinOk) BuildSkin();
            GUI.skin = _skin;
            _win = GUILayout.Window(98765, _win, DrawWindow,
                L.T("win.title", DebugModPlugin.KeyToggleMenu.Value));
            GUI.skin = null;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ОКНО
        // ═══════════════════════════════════════════════════════════════════════════
        private void DrawWindow(int _)
        {
            GUILayout.BeginHorizontal();
            TabBtn(L.T("tab.npc"),    Tab.NPC);
            TabBtn(L.T("tab.player"), Tab.Player);
            TabBtn(L.T("tab.event"),  Tab.Event);
            TabBtn(L.T("tab.item"),   Tab.Item);
            TabBtn(L.T("tab.baldi"),  Tab.Baldi);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            TabBtn(L.T("tab.time"),     Tab.Time);
            TabBtn(L.T("tab.rooms"),    Tab.Rooms);
            TabBtn(L.T("tab.log"),      Tab.Log);
            TabBtn(L.T("tab.camera"),   Tab.Camera);
            TabBtn(L.T("tab.stats"),    Tab.Stats);
            TabBtn(L.T("tab.settings"), Tab.Settings);
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            switch (_tab)
            {
                case Tab.NPC:      TabNPC();      break;
                case Tab.Player:   TabPlayer();   break;
                case Tab.Event:    TabEvent();    break;
                case Tab.Item:     TabItem();     break;
                case Tab.Baldi:    TabBaldi();    break;
                case Tab.Time:     TabTime();     break;
                case Tab.Rooms:    TabRooms();    break;
                case Tab.Log:      TabLog();      break;
                case Tab.Camera:   TabCamera();   break;
                case Tab.Stats:    TabStats();    break;
                case Tab.Settings: TabSettings(); break;
            }

            GUILayout.Space(4f);
            if (GUILayout.Button(L.T("btn.refresh"), GUILayout.Height(24f))) RefreshAll();

            GUI.DragWindow(new Rect(0f, 0f, _win.width, 24f));
        }

        private void TabBtn(string label, Tab t)
        {
            GUI.color = _tab == t ? new Color(0.35f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(26f))) _tab = t;
            GUI.color = Color.white;
        }

        private float SH() => _win.height - 330f;

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: NPC
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabNPC()
        {
            GUILayout.BeginHorizontal();
            GUI.color = !_liveNpcs ? new Color(0.35f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(L.T("npc.spawn_tab"), GUILayout.Height(24f))) _liveNpcs = false;
            GUI.color = _liveNpcs ? new Color(0.35f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(L.T("npc.live_tab"),  GUILayout.Height(24f))) _liveNpcs = true;
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);
            if (_liveNpcs) TabNPCLive(); else TabNPCSpawn();
        }

        private void TabNPCSpawn()
        {
            GUILayout.Label(L.T("npc.pfx_count", _npcPfx.Length));
            SearchBar(ref _srNpc);
            _scNpc = GUILayout.BeginScrollView(_scNpc, GUILayout.Height(SH()));
            foreach (var n in Flt(_npcPfx, x => x.name, _srNpc))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(n.name, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(L.T("npc.spawn"), GUILayout.Width(68f))) SpawnNPC(n);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void TabNPCLive()
        {
            var live = FindObjectsOfType<NPC>();
            GUILayout.Label(L.T("npc.live_count", live.Length));

            _scNpcLive = GUILayout.BeginScrollView(_scNpcLive, GUILayout.Height(SH() - 32f));
            foreach (var n in live)
            {
                if (n == null) continue;
                bool frozen = !n.enabled;
                string state = "";
                try { state = n.behaviorStateMachine?.CurrentState?.GetType().Name ?? ""; } catch { }

                GUILayout.BeginHorizontal();
                GUI.color = frozen ? new Color(0.7f, 0.7f, 1f) : Color.white;
                GUILayout.Label(
                    $"{n.name}  [{Mathf.RoundToInt(n.transform.position.x)},{Mathf.RoundToInt(n.transform.position.z)}]" +
                    (state != "" ? $"  ({state})" : ""),
                    GUILayout.ExpandWidth(true));
                GUI.color = Color.white;
                if (GUILayout.Button(L.T("npc.tp_to"), GUILayout.Width(38f))) TeleportToNPC(n);
                if (GUILayout.Button(frozen ? L.T("npc.unfreeze") : L.T("npc.freeze"), GUILayout.Width(54f)))
                {
                    n.enabled = frozen;
                    ShowMsg(frozen ? L.T("msg.unfrozen", n.name) : L.T("msg.frozen", n.name));
                }
                if (GUILayout.Button(L.T("npc.delete"), GUILayout.Width(40f))) DeleteNPC(n);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(3f);
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Button(L.T("npc.freeze_all"),   GUILayout.Height(26f)))
                foreach (var n in FindObjectsOfType<NPC>()) { if (n != null) n.enabled = false; }
            GUI.color = Color.white;
            if (GUILayout.Button(L.T("npc.unfreeze_all"), GUILayout.Height(26f)))
                foreach (var n in FindObjectsOfType<NPC>()) { if (n != null) n.enabled = true; }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button(L.T("npc.delete_all"), GUILayout.Height(26f)))
                foreach (var n in FindObjectsOfType<NPC>()) { if (n is Baldi) continue; DeleteNPC(n); }
            GUI.color = Color.white;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: PLAYER
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabPlayer()
        {
            GUILayout.Space(4f);
            DebugState.GodMode         = GUILayout.Toggle(DebugState.GodMode,         L.T("pl.godmode"));
            DebugState.InfiniteStamina = GUILayout.Toggle(DebugState.InfiniteStamina, L.T("pl.stamina"));
            DebugState.StealthMode     = GUILayout.Toggle(DebugState.StealthMode,     L.T("pl.stealth"));

            bool wasNoclip = DebugState.Noclip;
            DebugState.Noclip = GUILayout.Toggle(DebugState.Noclip, L.T("pl.noclip"));
            if (DebugState.Noclip != wasNoclip) SetNoclip(DebugState.Noclip);

            bool wasFly = DebugState.FlyMode;
            DebugState.FlyMode = GUILayout.Toggle(DebugState.FlyMode, L.T("pl.fly"));
            if (DebugState.FlyMode && !wasFly) { var p = GetPlayer(); if (p != null) DebugState.FlyHeight = p.transform.position.y; }
            if (DebugState.FlyMode)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L.T("pl.fly_h", DebugState.FlyHeight), GUILayout.Width(120f));
                DebugState.FlyHeight = GUILayout.HorizontalSlider(DebugState.FlyHeight, 0f, 80f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            GUILayout.Label(L.T("pl.speed", DebugState.SpeedMultiplier));
            float prevSp = DebugState.SpeedMultiplier;
            DebugState.SpeedMultiplier = GUILayout.HorizontalSlider(DebugState.SpeedMultiplier, 0.5f, 10f);
            if (!Mathf.Approximately(DebugState.SpeedMultiplier, prevSp)) ApplySpeed(DebugState.SpeedMultiplier);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("pl.give_all"),  GUILayout.Height(26f))) GiveAll();
            if (GUILayout.Button(L.T("pl.clear_inv"), GUILayout.Height(26f))) ClearInventory();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("pl.tp_random"),  GUILayout.Height(26f))) TeleportRandom();
            if (GUILayout.Button(L.T("pl.fill_stam"),  GUILayout.Height(26f))) FillStamina();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("pl.collect_nb"), GUILayout.Height(26f))) CollectNotebooks();
            if (GUILayout.Button(L.T("pl.solve_math"), GUILayout.Height(26f))) SolveMathMachines();
            GUILayout.EndHorizontal();
            GUI.color = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button(L.T("pl.win"), GUILayout.Height(30f))) WinLevel();
            GUI.color = Color.white;

            GUILayout.Space(6f);
            GUILayout.Label(L.T("pl.bookmarks"));
            for (int i = 0; i < 3; i++)
            {
                GUILayout.BeginHorizontal();
                string lbl = _bookmarkSet[i]
                    ? $"[{i+1}]  {_bookmarks[i].x:F0},{_bookmarks[i].y:F0},{_bookmarks[i].z:F0}"
                    : $"[{i+1}]  {L.T("pl.bm_empty")}";
                GUILayout.Label(lbl, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(L.T("pl.bm_save"), GUILayout.Width(44f)))
                {
                    var pm = GetPlayer();
                    if (pm != null) { _bookmarks[i] = pm.transform.position; _bookmarkSet[i] = true; ShowMsg(L.T("msg.bm_saved", i+1)); }
                }
                GUI.enabled = _bookmarkSet[i];
                if (GUILayout.Button(L.T("pl.bm_tp"), GUILayout.Width(32f)))
                {
                    var pm = GetPlayer();
                    if (pm != null) { pm.transform.position = _bookmarks[i]; ShowMsg(L.T("msg.bm_tp", i+1)); }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: EVENT
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabEvent()
        {
            GUILayout.Label(L.T("ev.count", _events.Length));
            SearchBar(ref _srEvt);
            _scEvt = GUILayout.BeginScrollView(_scEvt, GUILayout.Height(SH() - 30f));
            foreach (var e in Flt(_events, x => x.name, _srEvt))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(e.name, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(L.T("ev.start"), GUILayout.Width(68f))) TriggerEvent(e);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Space(4f);
            if (GUILayout.Button(L.T("ev.stop_all"), GUILayout.Height(26f))) StopEvents();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: ITEM
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabItem()
        {
            GUILayout.Label(L.T("it.count", _items.Length));
            SearchBar(ref _srItem);
            _scItem = GUILayout.BeginScrollView(_scItem, GUILayout.Height(SH()));
            foreach (var item in Flt(_items, x => x.name, _srItem))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.name, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(L.T("it.give"), GUILayout.Width(52f))) GiveItem(item);
                if (GUILayout.Button(L.T("it.drop"), GUILayout.Width(58f))) DropItem(item);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: BALDI
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabBaldi()
        {
            _baldi = FindObjectOfType<Baldi>();
            if (_baldi == null) { GUILayout.Label(L.T("baldi.not_found")); return; }

            GUILayout.Label($"Baldi: {_baldi.name}");
            GUILayout.Space(6f);

            float anger = GetAnger();
            GUILayout.Label(L.T("baldi.anger", anger));
            float newAnger = GUILayout.HorizontalSlider(anger, 0f, 10f);
            if (!Mathf.Approximately(newAnger, anger)) SetAnger(newAnger);

            GUILayout.Space(6f);
            bool frozen = !_baldi.enabled;
            bool newFrz = GUILayout.Toggle(frozen, L.T("baldi.freeze"));
            if (newFrz != frozen) _baldi.enabled = !newFrz;

            GUILayout.Space(6f);
            if (GUILayout.Button(L.T("baldi.to_player"), GUILayout.Height(28f))) BaldiToPlayer();
            if (GUILayout.Button(L.T("baldi.player_to"), GUILayout.Height(28f))) PlayerToBaldi();
            if (GUILayout.Button(L.T("baldi.remove"),    GUILayout.Height(28f))) DeleteNPC(_baldi);

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("baldi.anger_0"),   GUILayout.Height(26f))) SetAnger(0f);
            if (GUILayout.Button(L.T("baldi.anger_max"), GUILayout.Height(26f))) SetAnger(10f);
            GUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: TIME
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabTime()
        {
            GUILayout.Space(6f);
            GUILayout.Label(L.T("time.scale", DebugState.TimeScale));
            DebugState.TimeScale = GUILayout.HorizontalSlider(DebugState.TimeScale, 0f, 5f);
            Time.timeScale = DebugState.TimeScale;
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            SetTS(L.T("time.pause"),  0f);
            SetTS(L.T("time.slow"),   0.25f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            SetTS(L.T("time.normal"), 1f);
            SetTS(L.T("time.fast"),   3f);
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);
            SetTS(L.T("time.max"),    5f);
        }

        private void SetTS(string label, float val)
        {
            if (GUILayout.Button(label, GUILayout.Height(30f)))
            { DebugState.TimeScale = val; Time.timeScale = val; }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: ROOMS
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabRooms()
        {
            GUILayout.Label(L.T("rooms.count", _rooms.Length));
            SearchBar(ref _srRoom);
            _scRoom = GUILayout.BeginScrollView(_scRoom, GUILayout.Height(SH() - 30f));
            foreach (var r in Flt(_rooms, x => x.name, _srRoom))
            {
                if (r == null) continue;
                string coords = $"[{r.position.x * 10},{r.position.z * 10}]";
                string size   = $"{r.size.x}×{r.size.z}";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{r.name}  {coords}  {size}  {r.category}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button(L.T("rooms.tp"), GUILayout.Width(40f))) TeleportRoom(r);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Space(4f);
            if (GUILayout.Button(L.T("rooms.refresh"), GUILayout.Height(26f)))
                _rooms = FindObjectsOfType<RoomController>().OrderBy(r => r.name).ToArray();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: LOG
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabLog()
        {
            var lines = DebugLogListener.Lines;
            GUILayout.Label(L.T("log.title", lines.Count));
            _scLog = GUILayout.BeginScrollView(_scLog, GUILayout.Height(SH() - 30f));
            lock (lines)
            {
                foreach (var line in lines)
                {
                    if      (line.Contains(":Error") || line.Contains(":Fatal")) GUI.color = new Color(1f, 0.4f, 0.4f);
                    else if (line.Contains(":Warning"))                           GUI.color = new Color(1f, 0.9f, 0.3f);
                    else                                                          GUI.color = new Color(0.8f, 0.9f, 1f);
                    GUILayout.Label(line);
                }
            }
            GUI.color = Color.white;
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("log.bottom"), GUILayout.Height(26f))) _scLog = new Vector2(0, 99999f);
            if (GUILayout.Button(L.T("log.clear"),  GUILayout.Height(26f))) { lock (lines) lines.Clear(); }
            GUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: CAMERA
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabCamera()
        {
            GUILayout.Space(4f);
            DebugState.ShowHitboxes = GUILayout.Toggle(DebugState.ShowHitboxes, L.T("cam.hitboxes"));
            GUILayout.Space(6f);
            bool wasFree = DebugState.FreeCamMode;
            DebugState.FreeCamMode = GUILayout.Toggle(DebugState.FreeCamMode, L.T("cam.freecam"));
            if (DebugState.FreeCamMode != wasFree)
            {
                if (DebugState.FreeCamMode) EnableFreeCam();
                else                        DisableFreeCam();
            }
            if (DebugState.FreeCamMode)
            {
                GUILayout.Label(L.T("cam.speed", _camSpeed));
                _camSpeed = GUILayout.HorizontalSlider(_camSpeed, 5f, 120f);
                if (GUILayout.Button(L.T("cam.return"), GUILayout.Height(28f)))
                { DebugState.FreeCamMode = false; DisableFreeCam(); }
            }
            GUILayout.Space(8f);
            if (GUILayout.Button(L.T("cam.screenshot"), GUILayout.Height(30f)))
            {
                string path = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
                ScreenCapture.CaptureScreenshot(path);
                ShowMsg(L.T("msg.screenshot", path));
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: STATS
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabStats()
        {
            _scStats = GUILayout.BeginScrollView(_scStats, GUILayout.Height(SH()));

            GUILayout.Label(L.T("stat.fps", _fps));
            GUILayout.Space(4f);

            var pm = GetPlayer();
            if (pm != null)
            {
                var pos = pm.transform.position;
                GUILayout.Label(L.T("stat.pos", pos.x, pos.y, pos.z));
                try
                {
                    _currentRoomFI ??= typeof(PlayerManager).GetField("currentRoom",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var room = _currentRoomFI?.GetValue(pm) as RoomController;
                    GUILayout.Label(L.T("stat.room", room != null ? room.name : "—"));
                }
                catch { GUILayout.Label(L.T("stat.room", "—")); }
                try
                {
                    _invisibleFI ??= typeof(PlayerManager).GetField("invisible",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    bool invis = (bool)(_invisibleFI?.GetValue(pm) ?? false);
                    GUILayout.Label(L.T("stat.stealth", invis, pm.invincible));
                }
                catch { }
            }
            else GUILayout.Label(L.T("stat.no_player"));

            GUILayout.Space(6f);
            var baldi = FindObjectOfType<Baldi>();
            if (baldi != null)
            {
                GUILayout.Label(L.T("stat.baldi", GetAnger(), baldi.enabled));
                string st = "";
                try { st = baldi.behaviorStateMachine?.CurrentState?.GetType().Name ?? "—"; } catch { }
                GUILayout.Label(L.T("stat.baldi_st", st));
            }

            GUILayout.Space(6f);
            var ec = GetEC();
            if (ec != null)
            {
                try
                {
                    int total     = GetECField<int>(ec, "notebookTotal");
                    int remaining = FindObjectsOfType<Notebook>().Length;
                    GUILayout.Label(L.T("stat.notebooks", total - remaining, total, remaining));
                }
                catch { }
            }

            GUILayout.Space(6f);
            var live = FindObjectsOfType<NPC>();
            GUILayout.Label(L.T("stat.npcs", live.Length));
            int frozen = live.Count(n => n != null && !n.enabled);
            if (frozen > 0) GUILayout.Label(L.T("stat.frozen", frozen));

            GUILayout.Space(6f);
            var evs = FindObjectsOfType<RandomEvent>();
            GUILayout.Label(L.T("stat.events", evs.Length));
            foreach (var e in evs) if (e != null) GUILayout.Label($"  • {e.name}");

            GUILayout.Space(6f);
            GUILayout.Label(L.T("stat.ts",  Time.timeScale));
            GUILayout.Label(L.T("stat.ram", System.GC.GetTotalMemory(false) / 1048576f));

            GUILayout.Space(8f);
            GUILayout.Label(L.T("stat.ytp", GetYTP()));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100",  GUILayout.Height(26f))) AddYTP(100);
            if (GUILayout.Button("+500",  GUILayout.Height(26f))) AddYTP(500);
            if (GUILayout.Button("+1000", GUILayout.Height(26f))) AddYTP(1000);
            if (GUILayout.Button("+9999", GUILayout.Height(26f))) AddYTP(9999);
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        private static T GetECField<T>(EnvironmentController ec, string name)
        {
            var fi = ec.GetType().GetField(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)(fi?.GetValue(ec) ?? default(T)!);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  TAB: НАСТРОЙКИ
        // ═══════════════════════════════════════════════════════════════════════════
        private void TabSettings()
        {
            GUILayout.Space(4f);

            // ── Язык ─────────────────────────────────────────────────────────────
            GUILayout.Label(L.T("set.lang"));
            GUILayout.BeginHorizontal();
            GUI.color = L.Current == Language.RU ? new Color(0.35f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button("RU", GUILayout.Height(28f))) SetLanguage(Language.RU);
            GUI.color = L.Current == Language.EN ? new Color(0.35f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button("EN", GUILayout.Height(28f))) SetLanguage(Language.EN);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // ── Клавиши ──────────────────────────────────────────────────────────
            GUILayout.Label(L.T("set.keys"));
            DrawKeyBind(L.T("set.key_debug"), DebugModPlugin.KeyToggleDebug, RebindTarget.Debug);
            DrawKeyBind(L.T("set.key_menu"),  DebugModPlugin.KeyToggleMenu,  RebindTarget.Menu);
            if (_rebinding != RebindTarget.None)
            {
                GUI.color = new Color(1f, 0.9f, 0.2f);
                GUILayout.Label(L.T("set.waiting"));
                GUI.color = Color.white;
            }

            GUILayout.Space(8f);

            // ── HUD ──────────────────────────────────────────────────────────────
            GUILayout.Label(L.T("set.hud"));
            ToggleCfg(DebugModPlugin.CfgShowDebugLabel, L.T("set.show_label"));
            ToggleCfg(DebugModPlugin.CfgShowFps,        L.T("set.show_fps"));
            ToggleCfg(DebugModPlugin.CfgShowMsgHud,     L.T("set.show_msg"));

            GUILayout.Space(8f);

            // ── Параметры ─────────────────────────────────────────────────────────
            GUILayout.Label(L.T("set.params"));
            GUILayout.Label(L.T("set.noclip_spd", DebugModPlugin.CfgNoclipSpeed.Value));
            DebugModPlugin.CfgNoclipSpeed.Value =
                GUILayout.HorizontalSlider(DebugModPlugin.CfgNoclipSpeed.Value, 5f, 150f);
            GUILayout.Space(4f);
            GUILayout.Label(L.T("set.msg_dur", DebugModPlugin.CfgMsgDuration.Value));
            DebugModPlugin.CfgMsgDuration.Value =
                GUILayout.HorizontalSlider(DebugModPlugin.CfgMsgDuration.Value, 0.5f, 10f);

            GUILayout.Space(10f);
            if (GUILayout.Button(L.T("set.reset"), GUILayout.Height(26f)))
            {
                DebugModPlugin.KeyToggleDebug.Value    = KeyCode.F1;
                DebugModPlugin.KeyToggleMenu.Value     = KeyCode.F2;
                DebugModPlugin.CfgShowDebugLabel.Value = true;
                DebugModPlugin.CfgShowFps.Value        = true;
                DebugModPlugin.CfgShowMsgHud.Value     = true;
                DebugModPlugin.CfgNoclipSpeed.Value    = 25f;
                DebugModPlugin.CfgMsgDuration.Value    = 3f;
                ShowMsg(L.T("msg.set_reset"));
            }
        }

        private void SetLanguage(Language lang)
        {
            L.Current = lang;
            DebugModPlugin.CfgLanguage.Value = lang.ToString();
            _skinOk = false; // перестроить стили на случай изменения текста
        }

        private void DrawKeyBind(string label, BepInEx.Configuration.ConfigEntry<KeyCode> entry, RebindTarget target)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(200f));
            bool waiting = _rebinding == target;
            GUI.color = waiting ? new Color(1f, 0.85f, 0.1f) : new Color(0.5f, 0.9f, 1f);
            if (GUILayout.Button(waiting ? L.T("set.waiting") : entry.Value.ToString(),
                GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                _rebinding = waiting ? RebindTarget.None : target;
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        private static void ToggleCfg(BepInEx.Configuration.ConfigEntry<bool> entry, string label)
            => entry.Value = GUILayout.Toggle(entry.Value, label);

        // ═══════════════════════════════════════════════════════════════════════════
        //  ХИТБОКСЫ
        // ═══════════════════════════════════════════════════════════════════════════
        private void DrawHitboxes()
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var npc in FindObjectsOfType<NPC>())
            {
                if (npc == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(npc.transform.position);
                if (sp.z < 0f) continue;
                float sx = sp.x, sy = Screen.height - sp.y;
                GUI.color = new Color(1f, 0.3f, 0.3f, 0.85f);
                GUI.Label(new Rect(sx - 60f, sy - 38f, 120f, 18f), npc.name);
                GUI.color = new Color(1f, 0f, 0f, 0.3f);
                GUI.Box(new Rect(sx - 18f, sy - 18f, 36f, 36f), "");
                GUI.color = Color.white;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДЕЙСТВИЯ — NPC
        // ═══════════════════════════════════════════════════════════════════════════
        private void SpawnNPC(NPC pfx)
        {
            try
            {
                var ec = GetEC(); var pm = GetPlayer();
                if (ec == null || pm == null) { ShowMsg(L.T("msg.no_level")); return; }
                Vector3 pos = pm.transform.position + pm.transform.forward * 10f;
                var tile = new IntVector2(Mathf.RoundToInt(pos.x / 10f), Mathf.RoundToInt(pos.z / 10f));
                ec.SpawnNPC(pfx, tile);
                ShowMsg(L.T("msg.spawned", pfx.name));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void DeleteNPC(NPC npc)
        {
            if (npc == null) return;
            try
            {
                try { GetEC()?.Npcs.Remove(npc); } catch { }
                Destroy(npc.gameObject);
                ShowMsg(L.T("msg.deleted", npc.name));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void TeleportToNPC(NPC npc)
        {
            var pm = GetPlayer();
            if (pm == null || npc == null) return;
            pm.transform.position = npc.transform.position + npc.transform.right * 3f;
            ShowMsg(L.T("msg.tp_to", npc.name));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДЕЙСТВИЯ — ПРЕДМЕТЫ
        // ═══════════════════════════════════════════════════════════════════════════
        private void GiveItem(ItemObject it)
        {
            try
            {
                var pm = GetPlayer();
                if (pm == null) { ShowMsg(L.T("msg.no_player")); return; }
                pm.itm.AddItem(it);
                ShowMsg(L.T("msg.given", it.name));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void GiveAll()
        {
            try
            {
                var pm = GetPlayer();
                if (pm == null) { ShowMsg(L.T("msg.no_player")); return; }
                foreach (var it in _items) pm.itm.AddItem(it);
                ShowMsg(L.T("msg.given_all", _items.Length));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void ClearInventory()
        {
            try
            {
                var pm = GetPlayer();
                if (pm == null) { ShowMsg(L.T("msg.no_player")); return; }
                var itm = pm.itm;
                for (int i = 0; i < itm.items.Length; i++) itm.items[i] = itm.nothing;
                ShowMsg(L.T("msg.inv_cleared"));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void DropItem(ItemObject it)
        {
            try
            {
                var pm = GetPlayer();
                if (pm == null) { ShowMsg(L.T("msg.no_player")); return; }

                if (_pickupPfx == null)
                {
                    _pickupPfx = Resources.FindObjectsOfTypeAll<Pickup>()
                                     .FirstOrDefault(p => p != null && !p.gameObject.activeInHierarchy)
                              ?? Resources.FindObjectsOfTypeAll<Pickup>()
                                     .FirstOrDefault(p => p != null);
                }

                if (_pickupPfx == null)
                {
                    pm.itm.AddItem(it);
                    ShowMsg(L.T("msg.no_pickup", it.name));
                    return;
                }

                Vector3 pos = pm.transform.position + pm.transform.forward * 3f;
                pos.y = 5f;
                var pickup = Instantiate(_pickupPfx, pos, Quaternion.identity);
                pickup.item = it;
                if (pickup.itemSprite != null) pickup.itemSprite.sprite = it.itemSpriteLarge;
                pickup.gameObject.SetActive(true);
                ShowMsg(L.T("msg.dropped", it.name));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДЕЙСТВИЯ — СОБЫТИЯ
        // ═══════════════════════════════════════════════════════════════════════════
        private void TriggerEvent(RandomEvent pfx)
        {
            try
            {
                var ec = GetEC();
                if (ec == null) { ShowMsg(L.T("msg.no_level")); return; }
                var ev = Instantiate(pfx);
                ev.Initialize(ec, new System.Random());
                ev.Begin();
                ShowMsg(L.T("msg.event", pfx.name));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void StopEvents()
        {
            foreach (var e in FindObjectsOfType<RandomEvent>())
                try { e.End(); } catch { }
            ShowMsg(L.T("msg.events_off"));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДЕЙСТВИЯ — ТЕТРАДИ / ЗАДАЧНИКИ / ПОБЕДА
        // ═══════════════════════════════════════════════════════════════════════════
        private void CollectNotebooks()
        {
            var nbs = FindObjectsOfType<Notebook>();
            if (nbs.Length == 0) { ShowMsg(L.T("msg.no_notebooks")); return; }
            int done = 0;
            foreach (var nb in nbs)
            {
                if (nb == null) continue;
                try
                {
                    var mi = nb.GetType().GetMethod("Clicked",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    mi?.Invoke(nb, new object[] { 0 });
                    done++;
                }
                catch { }
            }
            ShowMsg(L.T("msg.nb_collected", done));
        }

        private void SolveMathMachines()
        {
            var machines = FindObjectsOfType<MathMachine>();
            if (machines.Length == 0) { ShowMsg(L.T("msg.no_machines")); return; }
            int done = 0;
            foreach (var mm in machines)
            {
                if (mm == null) continue;
                bool already = false;
                try { already = (bool)(mm.GetType().GetField("completed",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(mm) ?? false); } catch { }
                if (already) continue;
                try
                {
                    var mi = mm.GetType().GetMethod("Completed",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null, new[] { typeof(int), typeof(bool) }, null);
                    mi?.Invoke(mm, new object[] { 0, true });
                    done++;
                }
                catch { }
            }
            ShowMsg(L.T("msg.math_solved", done, machines.Length));
        }

        private void WinLevel()
        {
            try
            {
                int nbCount = 0;
                foreach (var nb in FindObjectsOfType<Notebook>())
                {
                    if (nb == null) continue;
                    try
                    {
                        var mi = nb.GetType().GetMethod("Clicked",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        mi?.Invoke(nb, new object[] { 0 });
                        nbCount++;
                    }
                    catch { }
                }
                int exitCount = 0;
                foreach (var exit in FindObjectsOfType<ClassicExit>())
                {
                    if (exit == null) continue;
                    try
                    {
                        var mi = exit.GetType().GetMethod("OpenGate",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        mi?.Invoke(exit, null);
                        exitCount++;
                    }
                    catch { }
                }
                ShowMsg(L.T("msg.win", nbCount, exitCount));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private int GetYTP()
        {
            try
            {
                var cgm = Singleton<CoreGameManager>.Instance;
                if (cgm == null) return 0;
                var mi = cgm.GetType().GetMethod("GetPoints",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return Convert.ToInt32(mi?.Invoke(cgm, new object[] { 0 }) ?? 0);
            }
            catch { return 0; }
        }

        private void AddYTP(int amount)
        {
            try
            {
                var cgm = Singleton<CoreGameManager>.Instance;
                if (cgm == null) { ShowMsg(L.T("msg.no_cgm")); return; }
                var mi = cgm.GetType().GetMethod("AddPoints",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new[] { typeof(int), typeof(int), typeof(bool) }, null);
                mi?.Invoke(cgm, new object[] { amount, 0, true });
                ShowMsg(L.T("msg.ytp", amount));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДЕЙСТВИЯ — ТЕЛЕПОРТ
        // ═══════════════════════════════════════════════════════════════════════════
        private void TeleportRandom()
        {
            try
            {
                var ec = GetEC(); var pm = GetPlayer();
                if (ec == null || pm == null) { ShowMsg(L.T("msg.no_level")); return; }
                var cells = ec.AllTilesNoGarbage(false, false);
                if (cells == null || cells.Count == 0) { ShowMsg(L.T("msg.no_tiles")); return; }
                var c = cells[UnityEngine.Random.Range(0, cells.Count)];
                pm.transform.position = new Vector3(c.position.x * 10f + 5f, pm.transform.position.y, c.position.z * 10f + 5f);
                ShowMsg(L.T("msg.tp_done"));
            }
            catch (Exception ex) { ShowMsg(L.T("msg.err", ex.Message)); }
        }

        private void TeleportRoom(RoomController r)
        {
            var pm = GetPlayer();
            if (pm == null) { ShowMsg(L.T("msg.no_player")); return; }
            pm.transform.position = new Vector3(r.position.x * 10f + 5f, pm.transform.position.y, r.position.z * 10f + 5f);
            ShowMsg(L.T("msg.room_tp", r.name));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДЕЙСТВИЯ — БАЛДИ
        // ═══════════════════════════════════════════════════════════════════════════
        private FieldInfo? FindAngerFI()
        {
            if (_angerFI != null) return _angerFI;
            if (_baldi == null)   return null;
            foreach (var n in new[] { "anger", "Anger", "angryLevel", "angerLevel", "angerValue", "_anger" })
            {
                var fi = _baldi.GetType().GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(int)))
                    return _angerFI = fi;
            }
            return null;
        }

        private float GetAnger()
        {
            if (_baldi == null) _baldi = FindObjectOfType<Baldi>();
            var fi = FindAngerFI();
            return fi == null || _baldi == null ? 0f : Convert.ToSingle(fi.GetValue(_baldi));
        }

        private void SetAnger(float v)
        {
            var fi = FindAngerFI();
            if (fi != null && _baldi != null)
                fi.SetValue(_baldi, fi.FieldType == typeof(int) ? (object)(int)v : v);
        }

        private void BaldiToPlayer()
        {
            var pm = GetPlayer();
            if (_baldi == null || pm == null) return;
            _baldi.transform.position = pm.transform.position + pm.transform.right * 3f;
            ShowMsg(L.T("msg.b2p"));
        }

        private void PlayerToBaldi()
        {
            var pm = GetPlayer();
            if (_baldi == null || pm == null) return;
            pm.transform.position = _baldi.transform.position + Vector3.right * 3f;
            ShowMsg(L.T("msg.p2b"));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ДВИЖЕНИЕ
        // ═══════════════════════════════════════════════════════════════════════════
        private void SetNoclip(bool on)
        {
            var pm = GetPlayer(); if (pm == null) return;
            var cc = pm.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = !on;
        }

        private void DoNoclip()
        {
            try
            {
                var pm = GetPlayer(); if (pm == null) return;
                float spd = DebugModPlugin.CfgNoclipSpeed.Value * DebugState.SpeedMultiplier;
                float h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
                float v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
                float y = (Input.GetKey(KeyCode.Space) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftControl) ? 1f : 0f);
                Transform t = pm.transform;
                pm.transform.position += (t.right * h + t.forward * v + Vector3.up * y) * spd * Time.unscaledDeltaTime;
            }
            catch { }
        }

        private void DoFly()
        {
            float sc = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(sc) > 0.001f) DebugState.FlyHeight = Mathf.Clamp(DebugState.FlyHeight + sc * 40f, 0f, 80f);
            if (Input.GetKey(KeyCode.Space))       DebugState.FlyHeight = Mathf.Clamp(DebugState.FlyHeight + 12f * Time.unscaledDeltaTime, 0f, 80f);
            if (Input.GetKey(KeyCode.LeftControl)) DebugState.FlyHeight = Mathf.Clamp(DebugState.FlyHeight - 12f * Time.unscaledDeltaTime, 0f, 80f);
        }

        private void FillStamina()
        {
            try { var pm = GetPlayer(); if (pm != null) pm.plm.stamina = 99999f; } catch { }
        }

        private void ApplySpeed(float m)
        {
            try { var pm = GetPlayer(); if (pm != null) { pm.plm.walkSpeed = 10f * m; pm.plm.runSpeed = 22f * m; } } catch { }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  СВОБОДНАЯ КАМЕРА
        // ═══════════════════════════════════════════════════════════════════════════
        private void EnableFreeCam()
        {
            _freeCam = Camera.main;
            if (_freeCam == null) { ShowMsg(L.T("msg.no_cam")); DebugState.FreeCamMode = false; return; }
            _origParent   = _freeCam.transform.parent;
            _origLocalPos = _freeCam.transform.localPosition;
            _origLocalRot = _freeCam.transform.localRotation;
            _freeCam.transform.SetParent(null, true);
            ShowMsg(L.T("msg.freecam_on"));
        }

        private void DisableFreeCam()
        {
            if (_freeCam == null) return;
            _freeCam.transform.SetParent(_origParent, false);
            _freeCam.transform.localPosition = _origLocalPos;
            _freeCam.transform.localRotation = _origLocalRot;
            _freeCam = null;
            ShowMsg(L.T("msg.freecam_off"));
        }

        private void DoFreeCamera()
        {
            if (_freeCam == null) return;
            try
            {
                float h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
                float v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
                float y = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
                Transform t = _freeCam.transform;
                t.position += (t.right * h + t.forward * v + Vector3.up * y) * _camSpeed * Time.unscaledDeltaTime;
                if (Input.GetMouseButton(1))
                {
                    t.eulerAngles += new Vector3(-Input.GetAxis("Mouse Y") * 3f, Input.GetAxis("Mouse X") * 3f, 0f);
                }
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  КЭШИ
        // ═══════════════════════════════════════════════════════════════════════════
        private void RefreshAll()
        {
            _npcPfx = Resources.FindObjectsOfTypeAll<NPC>()
                .Where(n => n.gameObject.scene.buildIndex < 0)
                .GroupBy(n => n.name).Select(g => g.First())
                .OrderBy(n => n.name).ToArray();

            _items = Resources.FindObjectsOfTypeAll<ItemObject>()
                .GroupBy(i => i.name).Select(g => g.First())
                .OrderBy(i => i.name).ToArray();

            _events = Resources.FindObjectsOfTypeAll<RandomEvent>()
                .Where(e => e.gameObject.scene.buildIndex < 0)
                .GroupBy(e => e.name).Select(g => g.First())
                .OrderBy(e => e.name).ToArray();

            _rooms = FindObjectsOfType<RoomController>()
                .OrderBy(r => r.name).ToArray();

            ShowMsg(L.T("msg.refreshed", _npcPfx.Length, _items.Length, _events.Length, _rooms.Length));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ХЕЛПЕРЫ
        // ═══════════════════════════════════════════════════════════════════════════
        private static EnvironmentController? GetEC()     => FindObjectOfType<EnvironmentController>();
        private static PlayerManager?         GetPlayer()
        {
            try { return Singleton<CoreGameManager>.Instance?.GetPlayer(0); }
            catch { return null; }
        }

        private static T[] Flt<T>(T[] src, Func<T, string> name, string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return src;
            string ql = q.ToLowerInvariant();
            return src.Where(x => name(x).ToLowerInvariant().Contains(ql)).ToArray();
        }

        private void SearchBar(ref string val)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T("search"), GUILayout.Width(50f));
            val = GUILayout.TextField(val, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        private void ShowMsg(string msg)
        {
            _msg  = msg;
            _msgT = DebugModPlugin.CfgMsgDuration.Value;
            DebugModPlugin.Log.LogInfo(msg);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  СТИЛИ
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildSkin()
        {
            _skin = Instantiate(GUI.skin);
            var bg = MakeTex(new Color(0.07f, 0.07f, 0.12f, 0.97f));
            _skin.window.normal.background = _skin.window.onNormal.background = bg;
            _skin.window.normal.textColor  = new Color(0.7f, 0.9f, 1f);
            _skin.window.fontSize          = 13;
            _skin.button.normal.background = MakeTex(new Color(0.15f, 0.25f, 0.45f));
            _skin.button.hover.background  = MakeTex(new Color(0.23f, 0.43f, 0.70f));
            _skin.button.active.background = MakeTex(new Color(0.10f, 0.50f, 0.85f));
            _skin.button.normal.textColor  = Color.white;
            _skin.button.fontSize          = 12;
            _skin.label.normal.textColor   = new Color(0.85f, 0.92f, 1f);
            _skin.label.fontSize           = 12;
            _skin.textField.normal.background = MakeTex(new Color(0.12f, 0.12f, 0.20f));
            _skin.textField.normal.textColor  = Color.white;
            _skin.textField.fontSize          = 12;
            _skin.scrollView.normal.background = MakeTex(new Color(0.05f, 0.05f, 0.09f, 0.9f));
            _skin.toggle.normal.textColor      = new Color(0.8f, 0.9f, 1f);
            _skin.toggle.fontSize              = 12;
            _skinOk = true;
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
