using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    [DefaultExecutionOrder(-180)]
    public sealed class MobileMenuGestures : MonoBehaviour
    {
        sealed class Pointer { public Vector2 start, position; public bool dragged; public int region; }
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
        int Mode => game == null ? 0
                  : game.OverworldLoreArchiveVisible ? 3
                  : game.EquipmentMenuVisible ? 1
                  : game.screen == VaultGame.Screen.DungeonSelect ? 4
                  : 0;
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
            Vector2 canvasPt = Canvas(position);
            if (phase == TouchPhase.Began)
            {
                int region = Mode == 1 ? game.EquipmentGestureRegion(canvasPt)
                           : Mode == 3 ? game.LoreArchiveGestureRegion(canvasPt)
                           : Mode == 4 ? game.WorldAtlasScrollGestureRegion(canvasPt)
                           : 0;
                if (region != 0)
                    pointers[id] = new Pointer { start = position, position = position, region = region };
                return;
            }
            if (!pointers.TryGetValue(id, out Pointer pointer)) return;
            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (pointer.dragged) swallowThrough = Time.frameCount + 1;
                pointers.Remove(id); return;
            }
            Vector2 previous = pointer.position;
            pointer.position = position;
            // Measure drag threshold in 1440x900 canvas units so high-DPI mobile taps never get swallowed as drags.
            if (!pointer.dragged && Vector2.Distance(canvasPt, Canvas(pointer.start)) < 14f) return;
            Vector2 delta = canvasPt - Canvas(pointer.dragged ? previous : pointer.start);
            pointer.dragged = true;
            swallowThrough = Time.frameCount + 1;
            if (Mode == 1)
                game.DragEquipmentMenu(pointer.region, delta);
            else if (Mode == 3)
                game.DragOverworldLoreArchive(pointer.region, delta);
            else if (Mode == 4)
                game.DragWorldAtlasScroll(pointer.region, delta);
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
