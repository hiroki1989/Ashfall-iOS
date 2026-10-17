using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public enum MobileAction { Reload, Pickup, Dodge, Melee, SwitchWeapon, Interact, Inventory, Pause }

    /// <summary>Touch coordinates are screen pixels (bottom left); exposed layout rectangles use GUI pixels (top left).</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class MobileTouchControls : MonoBehaviour
    {
        static MobileTouchControls instance;
        public static bool Enabled => Application.isMobilePlatform || Array.IndexOf(Environment.GetCommandLineArgs(), "--mobile-controls") >= 0;
        public static Vector2 Movement { get; private set; }
        public static Vector2 Aim { get; private set; } = Vector2.right;
        public static bool Firing { get; private set; }
        public float Opacity = .5f;
        public float ControlScale = 1f;
        public bool LeftHanded;
        public Rect? ReviewSafeArea;
        public static Rect MoveRect { get; private set; }
        public static Rect AimRect { get; private set; }
        public static Rect SafeRect { get; private set; }
        readonly Dictionary<int, int> fingers = new Dictionary<int, int>();
        readonly HashSet<MobileAction> actions = new HashSet<MobileAction>();
        readonly Rect[] buttons = new Rect[8];
        readonly float[] feedback = new float[8];
        VaultGame game;
        int mode = -1, width, height;
        Rect previousSafe;
        bool combat, hub, modal;
        Texture2D disc;
        GUIStyle textStyle;
        public static bool Consume(MobileAction action) => instance != null && instance.actions.Remove(action);
        public static Rect GetControlRect(MobileAction action) => instance == null ? default : instance.buttons[(int)action];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Enabled && instance == null) new GameObject("Mobile touch controls").AddComponent<MobileTouchControls>();
        }
        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Input.simulateMouseWithTouches = true;
            Layout();
        }
        void Update()
        {
            if (!Enabled) return;
            if (game == null) game = FindAnyObjectByType<VaultGame>();
            int next = game == null ? -1 : (int)game.screen + (game.InventoryOpen ? 100 : 0) + (game.MobileModalVisible ? 1000 : 0);
            if (next != mode || width != Screen.width || height != Screen.height || previousSafe != (ReviewSafeArea ?? Screen.safeArea))
            {
                ResetInput(); mode = next; Layout();
            }
            combat = game != null && game.Active && !game.MobileModalVisible;
            hub = game != null && game.screen == VaultGame.Screen.Hub && !game.InventoryOpen && !game.MobileModalVisible;
            modal = game != null && (game.InventoryOpen || game.screen == VaultGame.Screen.Pause);
            foreach (Touch touch in Input.touches) InjectPointer(touch.fingerId, touch.position, touch.phase);
            if (!Application.isMobilePlatform && Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0)) InjectPointer(-777, Input.mousePosition, TouchPhase.Began);
                else if (Input.GetMouseButtonUp(0)) InjectPointer(-777, Input.mousePosition, TouchPhase.Ended);
                else if (Input.GetMouseButton(0)) InjectPointer(-777, Input.mousePosition, TouchPhase.Moved);
            }
        }
        void Layout()
        {
            width = Screen.width; height = Screen.height; previousSafe = ReviewSafeArea ?? Screen.safeArea;
            Rect s = previousSafe;
            if (s.width <= 0 || s.height <= 0) s = new Rect(0, 0, width, height);
            SafeRect = new Rect(s.x, height - s.yMax, s.width, s.height);
            // 44 logical points minimum, using the short axis as the device UI reference.
            float unit = Mathf.Max(1, Mathf.Min(s.width, s.height) / 390f);
            float b = Mathf.Max(44 * unit, 48 * unit * Mathf.Clamp(ControlScale, .8f, 1.35f));
            float gap = 8 * unit, stick = 112 * unit * Mathf.Clamp(ControlScale, .8f, 1.35f);
            stick = Mathf.Min(stick, s.height * .34f);
            float pad = 16 * unit, y = SafeRect.yMax - pad - stick;
            MoveRect = new Rect(SafeRect.x + pad, y, stick, stick);
            AimRect = new Rect(SafeRect.xMax - pad - stick, y, stick, stick);
            float bx = SafeRect.xMax - pad - b;
            float side = AimRect.x - gap - b;
            buttons[(int)MobileAction.Reload] = new Rect(bx, y - gap - b, b, b);
            buttons[(int)MobileAction.Pickup] = new Rect(side, y - gap - b, b, b);
            buttons[(int)MobileAction.Dodge] = new Rect(side, SafeRect.yMax - pad - b, b, b);
            buttons[(int)MobileAction.Melee] = new Rect(side, SafeRect.yMax - pad - b * 2 - gap, b, b);
            buttons[(int)MobileAction.SwitchWeapon] = new Rect(SafeRect.x + pad, y - gap - b, b, b);
            buttons[(int)MobileAction.Interact] = new Rect(SafeRect.center.x - b / 2, SafeRect.yMax - pad - b, b, b);
            buttons[(int)MobileAction.Inventory] = new Rect(SafeRect.xMax - pad - 2 * b - gap, SafeRect.y + pad, b, b);
            buttons[(int)MobileAction.Pause] = new Rect(SafeRect.xMax - pad - b, SafeRect.y + pad, b, b);
            if (LeftHanded)
            {
                MoveRect = Mirror(MoveRect); AimRect = Mirror(AimRect);
                for (int i = 0; i < buttons.Length; i++) buttons[i] = Mirror(buttons[i]);
            }
        }
        static Rect Mirror(Rect r) { r.x = SafeRect.x + SafeRect.xMax - r.xMax; return r; }
        bool Visible(int i)
        {
            if (combat) return true;
            if (hub) return i == (int)MobileAction.Interact || i >= (int)MobileAction.Inventory;
            if (modal) return game.InventoryOpen ? i == (int)MobileAction.Inventory : i == (int)MobileAction.Pause;
            return false;
        }
        public void InjectPointer(int id, Vector2 position, TouchPhase phase)
        {
            Vector2 p = new Vector2(position.x, Screen.height - position.y);
            if (phase == TouchPhase.Began)
            {
                if (fingers.ContainsKey(id)) Release(id);
                for (int i = 0; i < buttons.Length; i++)
                    if (Visible(i) && buttons[i].Contains(p))
                    {
                        fingers[id] = i; actions.Add((MobileAction)i); feedback[i] = Time.unscaledTime + .14f; return;
                    }
                if ((combat || hub) && MoveRect.Contains(p) && !fingers.ContainsValue(-1)) fingers[id] = -1;
                else if (combat && AimRect.Contains(p) && !fingers.ContainsValue(-2)) fingers[id] = -2;
            }
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled) { Release(id); return; }
            if (!fingers.TryGetValue(id, out int owner)) return;
            if (owner == -1)
            {
                Vector2 v = (p - MoveRect.center) / (MoveRect.width * .38f);
                Movement = v.magnitude < .12f ? Vector2.zero : Vector2.ClampMagnitude(new Vector2(v.x, -v.y), 1);
            }
            else if (owner == -2)
            {
                Vector2 v = (p - AimRect.center) / (AimRect.width * .38f);
                if (v.magnitude > .14f) Aim = new Vector2(v.x, -v.y).normalized;
                Firing = true;
            }
        }
        void Release(int id)
        {
            if (!fingers.TryGetValue(id, out int owner)) return;
            fingers.Remove(id);
            if (owner == -1) Movement = Vector2.zero;
            if (owner == -2) Firing = false;
        }
        public static void ResetInput()
        {
            Movement = Vector2.zero; Firing = false;
            if (instance == null) return;
            instance.fingers.Clear(); instance.actions.Clear(); Array.Clear(instance.feedback, 0, instance.feedback.Length);
        }
        void OnApplicationFocus(bool focus) { if (!focus) ResetInput(); }
        void OnApplicationPause(bool pause) { if (pause) ResetInput(); }
        void OnDisable() { ResetInput(); }
        void OnDestroy() { if (disc != null) Destroy(disc); if (instance == this) { ResetInput(); instance = null; } }
        void OnGUI()
        {
            if (!Enabled) return;
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
            GUI.matrix = Matrix4x4.identity; GUI.depth = -1000;
            try
            {
                if (disc == null)
                {
                    disc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    { float a = Mathf.Clamp01((31.5f - Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))) * 2); disc.SetPixel(x,y,new Color(1,1,1,a)); }
                    disc.Apply();
                }
                if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
                if (combat || hub) Stick(MoveRect, Movement, false);
                if (combat) Stick(AimRect, Firing ? Aim * .65f : Vector2.zero, Firing);
                string[] labels = VaultLanguage.Japanese ? new[] { "装填", "拾得", "回避", "近接", "切替", "調べる", "装備", "停止" } : new[] { "LOAD", "TAKE", "DODGE", "MELEE", "SWAP", "USE", "GEAR", "PAUSE" };
                for (int i = 0; i < buttons.Length; i++) if (Visible(i))
                {
                    Rect r = buttons[i]; bool down = fingers.ContainsValue(i) || Time.unscaledTime < feedback[i];
                    Circle(r, down); textStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(r.width * .21f, 12, 32));
                    GUI.color = new Color(.93f,.96f,1, .95f); GUI.Label(r, labels[i], textStyle);
                }
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
        }
        void Stick(Rect r, Vector2 v, bool down)
        {
            Circle(r, down); float size = r.width * .46f;
            Rect cap = new Rect(r.center.x - size / 2 + v.x * r.width * .22f, r.center.y - size / 2 - v.y * r.height * .22f, size, size);
            Circle(cap, down);
        }
        void Circle(Rect r, bool down)
        {
            float alpha = Mathf.Clamp(Opacity, .2f, 1);
            Rect shadow = r; shadow.y += r.height * .065f;
            GUI.color = new Color(0,0,0,alpha*.8f); GUI.DrawTexture(shadow, disc);
            GUI.color = new Color(.28f,.45f,.51f,alpha); GUI.DrawTexture(r,disc);
            Rect inner = new Rect(r.x + 2,r.y + (down ? 4 : 1),r.width - 4,r.height - 5);
            GUI.color = down ? new Color(.23f,.7f,.8f,alpha) : new Color(.08f,.14f,.18f,alpha);
            GUI.DrawTexture(inner,disc);
        }
    }
}


