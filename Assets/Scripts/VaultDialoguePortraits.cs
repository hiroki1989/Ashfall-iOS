using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        static readonly Dictionary<string, Texture2D> dialoguePortraitCache = new Dictionary<string, Texture2D>();

        public static Texture2D LoadDialoguePortrait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (dialoguePortraitCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var tex = Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v11") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v10") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v9") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v8") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v7") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v6") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v5") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v4") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v3") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key + "-v2") ?? Resources.Load<Texture2D>("Art/Portraits/portrait-" + key);
            if (tex == null)
            {
                string path = Path.Combine(Application.dataPath, "Resources/Art/Portraits/portrait-" + key + ".png");
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        name = "portrait-" + key,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    tex.LoadImage(bytes, false);
                }
            }
            if (tex != null) dialoguePortraitCache[key] = tex;
            return tex;
        }

        public static readonly Rect UnifiedDialogueBox = new Rect(110f, 470f, 1220f, 340f);
        public static readonly Rect UnifiedDialoguePrimaryButton = new Rect(920f, 725f, 370f, 65f);
        public static readonly Rect UnifiedDialogueSecondaryButton = new Rect(520f, 725f, 380f, 65f);

        // Draws the counterpart character portrait on the LEFT and Knox's portrait on the RIGHT
        // at fixed, identical positions just above the unified dialogue box across all conversations.
        void DrawDialoguePortraits(string leftPortraitKey, bool knoxSpeaking, Rect dialogBox, Color leftAccent)
        {
            Texture2D leftTex = LoadDialoguePortrait(leftPortraitKey);
            Texture2D knoxTex = LoadDialoguePortrait("knox");
            const float size = 324f;
            const float y = 152f;
            Rect leftRect = new Rect(UnifiedDialogueBox.x + 18f, y, size, size);
            Rect rightRect = new Rect(UnifiedDialogueBox.xMax - 18f - size, y, size, size);
            DrawSingleDialoguePortrait(leftRect, leftTex, !knoxSpeaking, leftAccent);
            DrawSingleDialoguePortrait(rightRect, knoxTex, knoxSpeaking, Cyan, true);
        }

        void DrawUnifiedDialogueScene(string leftPortraitKey, bool knoxSpeaking, string speakerName, string bodyText, Color leftAccent, string footerNote = null)
        {
            DrawDialoguePortraits(leftPortraitKey, knoxSpeaking, UnifiedDialogueBox, leftAccent);
            Panel(UnifiedDialogueBox, knoxSpeaking ? Cyan : leftAccent);
            Color speakerColor = knoxSpeaking ? Cyan : leftAccent;
            Text(UnifiedDialogueBox.x + 35f, UnifiedDialogueBox.y + 26f, speakerName, heading, speakerColor, UnifiedDialogueBox.width - 70f);
            Text(UnifiedDialogueBox.x + 35f, UnifiedDialogueBox.y + 92f, bodyText, new GUIStyle(label) { fontSize = 33, wordWrap = true }, UiPaper, UnifiedDialogueBox.width - 70f);
            if (!string.IsNullOrEmpty(footerNote))
                Text(UnifiedDialogueBox.x + 35f, UnifiedDialogueBox.y + 272f, footerNote, new GUIStyle(label) { fontSize = 23 }, Cyan, 700f);
        }

        void DrawSingleDialoguePortrait(Rect r, Texture2D tex, bool active, Color accent, bool flipHorizontal = false)
        {
            if (tex == null) return;
            Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(.008f, .012f, .018f, active ? .96f : .82f));
            Color border = active ? accent : new Color(accent.r * .42f, accent.g * .42f, accent.b * .42f, .55f);
            Box(new Rect(r.x - 2, r.y - 2, r.width + 4, 2), border);
            Box(new Rect(r.x - 2, r.yMax, r.width + 4, 2), border);
            Box(new Rect(r.x - 2, r.y, 2, r.height), border);
            Box(new Rect(r.xMax, r.y, 2, r.height), border);
            Color prev = GUI.color;
            GUI.color = active ? Color.white : new Color(.52f, .56f, .60f, .88f);
            Matrix4x4 priorMatrix = GUI.matrix;
            if (flipHorizontal) GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), r.center);
            GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
            GUI.matrix = priorMatrix;
            GUI.color = prev;
            if (active)
            {
                Box(new Rect(r.x, r.yMax - 4, r.width, 4), accent);
            }
        }
    }
}
