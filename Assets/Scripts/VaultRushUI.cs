using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Called from the overworld HUD while the timed encounter is running.
        void DrawOverworldRushHUD()
        {
            if (!OverworldRushActive) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, OverworldRushSeconds));
            var box = new Rect(500, 96, 440, 98);
            Panel(box, Gold);
            WorldLabel(new Rect(box.x + 12, box.y + 8, box.width - 24, 25),
                OverworldRushObjective, Gold, 19);
            WorldLabel(new Rect(box.x + 12, box.y + 57, box.width - 24, 28),
                T("TIME LEFT  ", "残り時間  ") + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"),
                seconds <= 10 ? new Color(1f, .35f, .28f) : UiPaper, 22);
        }
    }
}
