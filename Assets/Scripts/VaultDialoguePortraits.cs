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
            var tex = Resources.Load<Texture2D>("Art/Portraits/portrait-" + key);
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

        // Draws the counterpart character portrait on the LEFT and Knox's portrait on the RIGHT
        // just above the given dialogue panel rect, highlighting the active speaker.
        void DrawDialoguePortraits(string leftPortraitKey, bool knoxSpeaking, Rect dialogBox, Color leftAccent)
        {
            Texture2D leftTex = LoadDialoguePortrait(leftPortraitKey);
            Texture2D knoxTex = LoadDialoguePortrait("knox");
            float size = Mathf.Min(336f, Mathf.Max(240f, dialogBox.y - 36f));
            float y = Mathf.Max(28f, dialogBox.y - size + 6f);
            Rect leftRect = new Rect(dialogBox.x + 18f, y, size, size);
            Rect rightRect = new Rect(dialogBox.xMax - 18f - size, y, size, size);
            DrawSingleDialoguePortrait(leftRect, leftTex, !knoxSpeaking, leftAccent);
            DrawSingleDialoguePortrait(rightRect, knoxTex, knoxSpeaking, Cyan);
        }

        void DrawSingleDialoguePortrait(Rect r, Texture2D tex, bool active, Color accent)
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
            GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
            GUI.color = prev;
            if (active)
            {
                Box(new Rect(r.x, r.yMax - 4, r.width, 4), accent);
            }
        }
    }
}
