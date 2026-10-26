using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public enum MobileAction { Reload, Pickup, Dodge, Melee, SwitchWeapon, Interact, Inventory, Pause, Fire }

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
        readonly Rect[] buttons = new Rect[9];
        readonly float[] feedback = new float[9];
        VaultGame game;
        int mode = -1, width, height;
        Rect previousSafe;
        Vector2 aimStick;
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
            actions.Clear();
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
            float fireSize = b * 1.5f;
            AimRect = new Rect(SafeRect.xMax - pad - fireSize - gap - stick, y, stick, stick);
            float bx = SafeRect.xMax - pad - b;
            float fireY = SafeRect.yMax - pad - fireSize;
            buttons[(int)MobileAction.Reload] = new Rect(bx, fireY - gap - b, b, b);
            buttons[(int)MobileAction.Pickup] = new Rect(AimRect.x+b+gap, y-gap-b, b, b);
            buttons[(int)MobileAction.Dodge] = new Rect(bx, fireY - 2*(gap+b), b, b);
            buttons[(int)MobileAction.Melee] = new Rect(AimRect.x, y-gap-b, b, b);
            buttons[(int)MobileAction.SwitchWeapon] = new Rect(SafeRect.x + pad, y - gap - b, b, b);
            buttons[(int)MobileAction.Interact] = new Rect(SafeRect.center.x - b / 2, SafeRect.yMax - pad - b, b, b);
            buttons[(int)MobileAction.Inventory] = new Rect(SafeRect.xMax - pad - 2 * b - gap, SafeRect.y + pad, b, b);
            buttons[(int)MobileAction.Pause] = new Rect(SafeRect.xMax - pad - b, SafeRect.y + pad, b, b);
            // Large, separate hold target at the natural outer-right thumb position.
            buttons[(int)MobileAction.Fire] = new Rect(SafeRect.xMax-pad-fireSize, fireY, fireSize, fireSize);
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
            if (hub) return i == (int)MobileAction.Interact || i == (int)MobileAction.Inventory || i == (int)MobileAction.Pause;
            if (modal) return false; // Full-screen menus provide their own large close/resume buttons.
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
                        fingers[id] = i; actions.Add((MobileAction)i); feedback[i] = Time.unscaledTime + .14f;
                        if (i == (int)MobileAction.Fire) Firing = true;
                        return;
                    }
                if ((combat || hub) && MoveRect.Contains(p) && !fingers.ContainsValue(-1)) fingers[id] = -1;
                else if ((combat || hub) && AimRect.Contains(p) && !fingers.ContainsValue(-2)) fingers[id] = -2;
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
                aimStick = v.magnitude < .14f ? Vector2.zero : Vector2.ClampMagnitude(new Vector2(v.x, -v.y), 1);
                if (v.magnitude > .14f) Aim = new Vector2(v.x, -v.y).normalized;
            }
            else if (owner == (int)MobileAction.Fire) Firing = true;
        }
        void Release(int id)
        {
            if (!fingers.TryGetValue(id, out int owner)) return;
            fingers.Remove(id);
            if (owner == -1) Movement = Vector2.zero;
            if (owner == -2) aimStick = Vector2.zero;
            if (owner == (int)MobileAction.Fire) Firing = fingers.ContainsValue((int)MobileAction.Fire);
        }
        public static void ResetInput()
        {
            Movement = Vector2.zero; Firing = false;
            if (instance == null) return;
            instance.aimStick = Vector2.zero;
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
                if (textStyle == null)
                {
                    textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold };
                    textStyle.normal.textColor = new Color(1f,.97f,.83f);
                }
                Font bundledFont = Resources.Load<Font>("Fonts/GenShinGothic-P-Regular");
                if(bundledFont==null)bundledFont=Resources.Load<Font>("Fonts/NotoSansJP");
                if (bundledFont != null) textStyle.font = bundledFont;
                if (combat || hub) Stick(MoveRect, Movement, false);
                if (combat || hub) Stick(AimRect, aimStick, fingers.ContainsValue(-2));
                string[] labels = VaultLanguage.Japanese ? new[] { "装填", "拾得", "回避", "近接", "切替", "調べる", "装備", "停止", "射撃" } : new[] { "LOAD", "TAKE", "DODGE", "MELEE", "SWAP", "USE", "GEAR", "PAUSE", "FIRE" };
                for (int i = 0; i < buttons.Length; i++) if (Visible(i))
                {
                    Rect r = buttons[i]; bool down = fingers.ContainsValue(i) || Time.unscaledTime < feedback[i];
                    bool primary=i==(int)MobileAction.Fire;
                    Circle(r, down, primary);
                    Color ink=primary?new Color(.08f,.11f,.14f):new Color(.96f,.96f,.91f);
                    ActionIcon(new Rect(r.x+r.width*.24f,r.y+r.height*.17f,r.width*.52f,r.height*.47f),(MobileAction)i,ink);
                    textStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(r.width * .15f, 12, 28));
                    textStyle.normal.textColor=ink;GUI.color=Color.white;
                    GUI.Label(new Rect(r.x,r.y+r.height*.65f,r.width,r.height*.24f),labels[i],textStyle);
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
        void Circle(Rect r, bool down, bool primary=false)
        {
            float alpha = Mathf.Clamp(Opacity, .2f, 1);
            Rect shadow = r; shadow.y += r.height * .065f;
            GUI.color = new Color(0,0,0,alpha*.7f); GUI.DrawTexture(shadow, disc);
            GUI.color = primary?new Color(1f,.84f,.43f,.96f):new Color(.77f,.83f,.83f,alpha*.85f); GUI.DrawTexture(r,disc);
            Rect inner = new Rect(r.x + 2,r.y + (down ? 4 : 1),r.width - 4,r.height - 5);
            GUI.color = primary?(down?new Color(1f,.94f,.69f,.98f):new Color(1f,.76f,.28f,.93f)):(down?new Color(.26f,.43f,.46f,.85f):new Color(.07f,.10f,.14f,Mathf.Min(.7f,alpha*1.3f)));
            GUI.DrawTexture(inner,disc);
        }
        // Vector icons stay crisp at phone/tablet resolutions and need no bitmap atlas.
        void IconLine(Rect r,float ax,float ay,float bx,float by,Color color,float weight=.075f)
        {
            Vector2 a=new Vector2(r.x+ax*r.width,r.y+ay*r.height), b=new Vector2(r.x+bx*r.width,r.y+by*r.height);
            Matrix4x4 old=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-r.width*weight*.5f,Vector2.Distance(a,b),r.width*weight),Texture2D.whiteTexture);GUI.matrix=old;
        }
        void ActionIcon(Rect r,MobileAction action,Color c)
        {
            int artIndex=action==MobileAction.Fire?0:action==MobileAction.Reload?1:action==MobileAction.Pickup||action==MobileAction.Interact?2:action==MobileAction.Melee?3:action==MobileAction.Inventory?4:action==MobileAction.Pause?11:-1;
            if(artIndex>=0&&FieldUIArt.Icon(r,artIndex,Color.white))return;
            switch(action)
            {
                case MobileAction.Fire:
                    IconLine(r,.05f,.32f,.95f,.32f,c,.19f);IconLine(r,.12f,.45f,.72f,.45f,c,.15f);IconLine(r,.29f,.43f,.2f,.91f,c,.19f);IconLine(r,.48f,.45f,.48f,.66f,c);IconLine(r,.27f,.67f,.49f,.67f,c);break;
                case MobileAction.Reload:
                    for(int j=0;j<15;j++){float a=(j*19+40)*Mathf.Deg2Rad,b=(j*19+59)*Mathf.Deg2Rad;IconLine(r,.5f+Mathf.Cos(a)*.36f,.5f+Mathf.Sin(a)*.36f,.5f+Mathf.Cos(b)*.36f,.5f+Mathf.Sin(b)*.36f,c);}
                    IconLine(r,.83f,.25f,.55f,.22f,c);IconLine(r,.83f,.25f,.8f,.52f,c);break;
                case MobileAction.Pickup:
                    IconLine(r,.5f,.08f,.5f,.65f,c);IconLine(r,.24f,.43f,.5f,.68f,c);IconLine(r,.76f,.43f,.5f,.68f,c);IconLine(r,.12f,.68f,.12f,.88f,c);IconLine(r,.12f,.88f,.88f,.88f,c);IconLine(r,.88f,.88f,.88f,.68f,c);break;
                case MobileAction.Dodge:
                    for(int j=0;j<2;j++){float x=.13f+j*.43f;IconLine(r,x,.12f,x+.29f,.5f,c,.1f);IconLine(r,x+.29f,.5f,x,.88f,c,.1f);}break;
                case MobileAction.Melee:
                    IconLine(r,.18f,.87f,.51f,.54f,c,.15f);IconLine(r,.33f,.46f,.64f,.77f,c);IconLine(r,.48f,.58f,.86f,.08f,c,.12f);IconLine(r,.86f,.08f,.71f,.55f,c);IconLine(r,.71f,.55f,.58f,.68f,c);break;
                case MobileAction.SwitchWeapon:
                    IconLine(r,.08f,.3f,.88f,.3f,c);IconLine(r,.68f,.1f,.9f,.3f,c);IconLine(r,.9f,.3f,.68f,.5f,c);IconLine(r,.9f,.73f,.1f,.73f,c);IconLine(r,.3f,.53f,.08f,.73f,c);IconLine(r,.08f,.73f,.3f,.93f,c);break;
                case MobileAction.Interact:
                    IconLine(r,.3f,.84f,.18f,.48f,c);IconLine(r,.18f,.48f,.35f,.56f,c);IconLine(r,.35f,.56f,.35f,.1f,c);IconLine(r,.35f,.1f,.48f,.1f,c);IconLine(r,.48f,.1f,.48f,.43f,c);IconLine(r,.48f,.43f,.8f,.48f,c);IconLine(r,.8f,.48f,.72f,.86f,c);IconLine(r,.3f,.86f,.72f,.86f,c);break;
                case MobileAction.Inventory:
                    for(int x=0;x<2;x++)for(int y=0;y<2;y++){GUI.color=c;GUI.DrawTexture(new Rect(r.x+r.width*(.1f+x*.48f),r.y+r.height*(.08f+y*.48f),r.width*.31f,r.height*.31f),Texture2D.whiteTexture);}break;
                case MobileAction.Pause:
                    IconLine(r,.29f,.1f,.29f,.9f,c,.19f);IconLine(r,.71f,.1f,.71f,.9f,c,.19f);break;
            }
        }
    }
}


