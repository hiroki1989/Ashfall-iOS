using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    [DefaultExecutionOrder(-180)]
    public sealed class MobileMenuGestures : MonoBehaviour
    {
        sealed class Pointer { public Vector2 start, position; public bool dragged; }
        readonly Dictionary<int, Pointer> pointers = new Dictionary<int, Pointer>();
        VaultGame game;
        int mode = -1, swallowThrough = -1;
        int lastWidth, lastHeight;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (MobileTouchControls.Enabled && FindAnyObjectByType<MobileMenuGestures>() == null)
                new GameObject("Mobile menu gestures").AddComponent<MobileMenuGestures>();
        }
        void Awake() { DontDestroyOnLoad(gameObject); }
        int Mode => game == null ? 0 : game.InventoryOpen ? 1 : game.screen == VaultGame.Screen.DungeonSelect ? 2 : 0;
        void Update()
        {
            if (!MobileTouchControls.Enabled) return;
            if (game == null) game = FindAnyObjectByType<VaultGame>();
            if (mode != Mode || lastWidth != Screen.width || lastHeight != Screen.height)
            {
                ResetGestures(); mode = Mode; lastWidth = Screen.width; lastHeight = Screen.height;
            }
            foreach (Touch t in Input.touches) InjectGesturePointer(t.fingerId, t.position, t.phase);
            if (!Application.isMobilePlatform && Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0)) InjectGesturePointer(-777, Input.mousePosition, TouchPhase.Began);
                else if (Input.GetMouseButtonUp(0)) InjectGesturePointer(-777, Input.mousePosition, TouchPhase.Ended);
                else if (Input.GetMouseButton(0)) InjectGesturePointer(-777, Input.mousePosition, TouchPhase.Moved);
            }
        }
        static Vector2 Canvas(Vector2 screen) => VaultGame.MobileCanvasMatrix.inverse.MultiplyPoint3x4(new Vector3(screen.x, Screen.height - screen.y, 0));
        public void InjectGesturePointer(int id, Vector2 position, TouchPhase phase)
        {
            if (game == null) game = FindAnyObjectByType<VaultGame>();
            if (Mode == 0) { ResetGestures(); return; }
            if (mode != Mode) { ResetGestures(); mode = Mode; }
            Rect view = Mode == 1 ? game.MobileInventoryScrollRect : game.MobileCityView;
            if (phase == TouchPhase.Began)
            {
                if (view.Contains(Canvas(position))) pointers[id] = new Pointer { start = position, position = position };
                return;
            }
            if (!pointers.TryGetValue(id, out Pointer pointer)) return;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (pointer.dragged) swallowThrough = Time.frameCount + 1;
                else if(phase==TouchPhase.Ended && Mode==2) game.TrySelectCityStage(Canvas(position));
                pointers.Remove(id); return;
            }
            Vector2 previous = pointer.position;
            pointer.position = position;
            if (Mode == 2 && pointers.Count >= 2)
            {
                Pointer other = null;
                foreach (var pair in pointers) if (pair.Key != id) { other = pair.Value; break; }
                if (other != null)
                {
                    float before = Vector2.Distance(previous, other.position);
                    float after = Vector2.Distance(position, other.position);
                    if (before > 1 && after > 1)
                    {
                        Vector2 oldCenter = (Canvas(previous) + Canvas(other.position)) * .5f;
                        Vector2 newCenter = (Canvas(position) + Canvas(other.position)) * .5f;
                        game.MobileAdjustCityMap(newCenter - oldCenter, after / before, oldCenter - view.position);
                    }
                    pointer.dragged = other.dragged = true;
                    swallowThrough = Time.frameCount + 1;
                }
                return;
            }
            if (!pointer.dragged && Vector2.Distance(position, pointer.start) < 12) return;
            Vector2 delta = Canvas(position) - Canvas(pointer.dragged ? previous : pointer.start);
            pointer.dragged = true;
            swallowThrough = Time.frameCount + 1;
            if (Mode == 1)
            {
                Vector2 scroll = game.MobileInventoryScroll;
                scroll.y = Mathf.Clamp(scroll.y - delta.y, 0, Mathf.Max(0, game.MobileInventoryContentHeight - view.height));
                game.MobileInventoryScroll = scroll;
            }
            else game.MobileAdjustCityMap(delta, 1, Vector2.zero);
        }
        void OnGUI()
        {
            if (!MobileTouchControls.Enabled || Mode == 0 || Time.frameCount > swallowThrough) return;
            int depth = GUI.depth; GUI.depth = -2000;
            Event e = Event.current;
            if (e.type == EventType.MouseUp || e.type == EventType.MouseDrag)
            {
                if (e.type == EventType.MouseUp) GUIUtility.hotControl = 0;
                e.Use();
            }
            GUI.depth = depth;
        }
        public void ResetGestures() { pointers.Clear(); swallowThrough = -1; }
        void OnApplicationFocus(bool focus) { if (!focus) ResetGestures(); }
        void OnApplicationPause(bool pause) { if (pause) ResetGestures(); }
        void OnDisable() { ResetGestures(); }
    }

    public partial class VaultGame
    {
        public void MobileAdjustCityMap(Vector2 canvasPan, float zoomRatio, Vector2 anchor)
        {
            if (screen != Screen.DungeonSelect || InventoryOpen) return;
            if (!Mathf.Approximately(zoomRatio, 1)) SetCityZoom(expeditionCityZoom * zoomRatio, anchor);
            expeditionMapScroll -= canvasPan;
            Vector2 size = ExpeditionMap.CityCanvasSize * expeditionCityZoom;
            expeditionMapScroll.x = Mathf.Clamp(expeditionMapScroll.x, 0, Mathf.Max(0, size.x - MobileCityView.width + 18));
            expeditionMapScroll.y = Mathf.Clamp(expeditionMapScroll.y, 0, Mathf.Max(0, size.y - MobileCityView.height + 18));
        }
    }
}

