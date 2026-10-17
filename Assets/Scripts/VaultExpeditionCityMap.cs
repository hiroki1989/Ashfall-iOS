using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        float expeditionCityZoom = .65f;
        bool expeditionCityDragging;
        GUIStyle expeditionCitySiteStyle;
        Texture2D expeditionCityAtlas;
        bool expeditionCityAtlasLoaded;
        static readonly Color CityRoad = new Color(.24f, .28f, .28f);
        Vector2 CityScale(Vector2 p) => p * expeditionCityZoom;
        void CityBox(Rect r, Color c) => Box(new Rect(r.x * expeditionCityZoom, r.y * expeditionCityZoom, r.width * expeditionCityZoom, r.height * expeditionCityZoom), c);
        void CityLine(Vector2 a, Vector2 b, Color c, float width = 1) => Line(CityScale(a), CityScale(b), c, Mathf.Max(1, width * expeditionCityZoom));
        void CityText(float x, float y, string value, Color color, float width = 220) => Text(x * expeditionCityZoom, y * expeditionCityZoom, value, tiny, color, width * expeditionCityZoom);
        float CityRiver(float y) => 720 + Mathf.Sin(y * .006f) * 20;

        void SetCityZoom(float next, Vector2 anchor)
        {
            next = Mathf.Clamp(next, .65f, 1.6f);
            Vector2 cityAnchor = (expeditionMapScroll + anchor) / expeditionCityZoom;
            expeditionMapScroll = cityAnchor * next - anchor;
            expeditionCityZoom = next;
        }
        void DrawExpeditionCityMap()
        {
            Box(new Rect(0, 0, 1440, 900), new Color(.018f, .027f, .032f));
            Text(42, 30, T("ASHFALL / CITY OPERATIONS", "アッシュフォール / 都市全域作戦図"), heading, Gold, 1300);
            Text(45, 93, T("100 SITES / THREATS ARE SCATTERED THROUGHOUT THE CITY", "全100地点 / 各レベルの危険地点が市内全域に点在"), small, Mist, 960);
            Panel(new Rect(1020, 150, 375, 663), Cyan);
            Rect view = new Rect(42, 150, 952, 663);
            Event e = Event.current;
            if (view.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                SetCityZoom(expeditionCityZoom * Mathf.Pow(1.12f, -e.delta.y), e.mousePosition - view.position);
                e.Use();
            }
            if (view.Contains(e.mousePosition) && e.type == EventType.MouseDown && (e.button == 1 || e.button == 2)) { expeditionCityDragging = true; e.Use(); }
            if (expeditionCityDragging && e.type == EventType.MouseDrag) { expeditionMapScroll -= e.delta; e.Use(); }
            if (e.type == EventType.MouseUp) expeditionCityDragging = false;
            Panel(view, new Color(.34f, .49f, .45f));
            Vector2 size = ExpeditionMap.CityCanvasSize * expeditionCityZoom;
            expeditionMapScroll.x = Mathf.Clamp(expeditionMapScroll.x, 0, Mathf.Max(0, size.x - view.width + 18));
            expeditionMapScroll.y = Mathf.Clamp(expeditionMapScroll.y, 0, Mathf.Max(0, size.y - view.height + 18));
            expeditionMapScroll = GUI.BeginScrollView(view, expeditionMapScroll, new Rect(0, 0, size.x, size.y));
            DrawCityAtlas();
            GUI.EndScrollView();
            DrawExpeditionBriefing();
            if (Button(new Rect(42, 835, 300, 47), T("RETURN TO LAST LIGHT", "灯火基地へ戻る"))) screen = Screen.Hub;
            if (GUI.Button(new Rect(359, 835, 46, 38), "−", centered)) SetCityZoom(expeditionCityZoom / 1.2f, view.size * .5f);
            Text(415, 845, Mathf.RoundToInt(expeditionCityZoom * 100) + "%", tiny, Mist, 65);
            if (GUI.Button(new Rect(478, 835, 46, 38), "+", centered)) SetCityZoom(expeditionCityZoom * 1.2f, view.size * .5f);
            if (GUI.Button(new Rect(534, 835, 104, 38), T("CITY FIT", "全域表示"), centered)) { expeditionCityZoom = .65f; expeditionMapScroll = Vector2.zero; }
            Text(650, 837, T("WHEEL: ZOOM / RIGHT DRAG: PAN", "ホイール: 拡縮 / 右ドラッグ: 移動"), tiny, Mist, 355);
            Text(650, 858, T("GOLD: SELECTED / CYAN: CLEARED / GREY: LOCKED", "金: 選択 / 青緑: 制圧済 / 灰: 許可不足"), tiny, Mist, 355);
        }
        void DrawCityAtlas()
        {
            if (!expeditionCityAtlasLoaded)
            {
                expeditionCityAtlas = Resources.Load<Texture2D>("Art/Environment/AshfallCityMap-v1");
                expeditionCityAtlasLoaded = true;
            }
            if (expeditionCityAtlas != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, 1440 * expeditionCityZoom, 1000 * expeditionCityZoom), expeditionCityAtlas, ScaleMode.StretchToFill);
            }
            else DrawCityAtlasFallback();
            CityLandmark(178, 122, T("CIVIC CITADEL", "市庁要塞跡"), 0);
            CityLandmark(1153, 762, T("BOTANICAL RESERVE", "植物保護区跡"), 1);
            CityLandmark(1068, 117, T("RAIL TERMINAL", "中央貨物駅"), 2);
            CityLandmark(216, 810, T("COLLAPSED STADIUM", "崩落スタジアム"), 3);
            CityText(38, 7, T("N ↑  / ASHFALL MUNICIPAL SURVEY", "北 ↑ / アッシュフォール都市測量図"), Mist, 650);
            CityText(760, 960, T("CANAL / FLOOD BARRIER", "中央運河 / 防潮堤"), new Color(.4f,.72f,.76f), 420);
            for (int id = 0; id < ExpeditionMap.StageCount; id++) DrawCitySite(id);
        }
        void DrawCityAtlasFallback()
        {
            CityBox(new Rect(0, 0, 1440, 1000), new Color(.065f, .091f, .093f));
            // A continuous municipal street network; contracts are sites, not a linear route.
            for (int row = 0; row < 10; row++)
            for (int col = 0; col < 10; col++)
            {
                float x = 22 + col * 140, y = 39 + row * 91;
                Color block = col < 3 ? new Color(.16f, .19f, .19f) : col > 7 ? new Color(.14f, .21f, .17f) : new Color(.19f, .19f, .16f);
                CityBox(new Rect(x, y, 124, 76), block);
                // Roof footprints, courtyards and broken roof edges at city scale.
                for (int n = 0; n < 5; n++)
                {
                    float bx = x + 5 + (n % 3) * 39, by = y + 5 + (n / 3) * 36;
                    float w = 27 + ((col * 7 + row * 3 + n) % 8);
                    Color roof = new Color(.25f + (n % 2) * .025f, .27f, .25f);
                    CityBox(new Rect(bx + 2, by + 3, w, 24), new Color(.025f, .04f, .04f));
                    CityBox(new Rect(bx, by, w, 24), roof);
                    CityLine(new Vector2(bx, by), new Vector2(bx + w, by), new Color(.39f, .42f, .38f));
                    CityBox(new Rect(bx + w - 8, by + 16, 8, 8), block);
                }
            }
            for (int x = 14; x < 1440; x += 140) CityLine(new Vector2(x, 26), new Vector2(x, 966), CityRoad, 11);
            for (int y = 31; y < 1000; y += 91) CityLine(new Vector2(12, y), new Vector2(1426, y), CityRoad, 11);
            // River corridor cuts through the built city; bridges join both banks.
            for (int y = 0; y < 1000; y += 8)
            {
                float rx = CityRiver(y);
                CityBox(new Rect(rx - 29, y, 58, 9), new Color(.22f, .29f, .27f));
                CityBox(new Rect(rx - 22, y, 44, 9), new Color(.08f, .24f, .29f));
                CityBox(new Rect(rx - 7, y, 3, 4), new Color(.17f, .36f, .39f));
            }
            foreach (int y in new[] { 213, 486, 759 })
            {
                CityLine(new Vector2(653, y), new Vector2(787, y), new Color(.46f, .48f, .40f), 19);
                CityLine(new Vector2(653, y), new Vector2(787, y), CityRoad, 12);
            }
            // Elevated ring road and a rail approach supply a clear city silhouette.
            Vector2[] belt = {new Vector2(32,142),new Vector2(222,18),new Vector2(1170,18),new Vector2(1408,160),new Vector2(1408,836),new Vector2(1190,972),new Vector2(202,972),new Vector2(32,832),new Vector2(32,142)};
            for (int i = 1; i < belt.Length; i++) { CityLine(belt[i-1], belt[i], new Color(.025f,.035f,.036f), 17); CityLine(belt[i-1], belt[i], new Color(.39f,.36f,.27f), 7); }
            for (int y = 90; y < 930; y += 18) CityLine(new Vector2(1260,y),new Vector2(1280,y),new Color(.43f,.37f,.28f),2);
            CityLine(new Vector2(1264, 60), new Vector2(1264, 940), new Color(.56f,.51f,.37f), 2);
            CityLine(new Vector2(1276, 60), new Vector2(1276, 940), new Color(.56f,.51f,.37f), 2);
        }
        void CityLandmark(float x, float y, string name, int kind)
        {
            Color edge = new Color(.56f,.58f,.43f);
            CityBox(new Rect(x-44,y-18,88,36),new Color(.07f,.12f,.11f));
            if (kind == 1) { for(int n=0;n<6;n++) CityBox(new Rect(x-36+n*12,y-12,9,23),new Color(.24f,.38f,.25f)); }
            else { for(int n=0;n<4;n++) CityBox(new Rect(x-38+n*21,y-11,16,23),edge); }
            CityText(x-85,y+20,name,edge,270);
        }
        void DrawCitySite(int id)
        {
            ExpeditionStage stage = ExpeditionMap.Get(id);
            Vector2 p = CityScale(ExpeditionMap.CityPoint(id));
            bool unlocked = stage.level <= DebtLevel, selected = id == expeditionMapSelection, done = clearedExpeditionStages.Contains(id);
            Color mark = selected ? Gold : done ? Cyan : unlocked ? new Color(.83f,.86f,.69f) : new Color(.43f,.47f,.47f);
            float w = expeditionCityZoom > 1 ? 51 : 39, h = expeditionCityZoom > 1 ? 31 : 25;
            Rect r = new Rect(p.x-w*.5f,p.y-h*.5f,w,h);
            Box(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),mark);
            Box(r,new Color(.025f,.045f,.05f,.98f));
            GUI.color = mark;
            if (expeditionCitySiteStyle == null) expeditionCitySiteStyle = new GUIStyle(centered) { fontSize = 10, padding = new RectOffset(0, 0, 0, 0) };
            if(GUI.Button(r,(id+1).ToString("00")+"·L"+stage.level,expeditionCitySiteStyle)) expeditionMapSelection=id;
            GUI.color = Color.white;
            if(done) Box(new Rect(r.x,r.yMax-3,r.width,3),Cyan);
        }
    }
}
