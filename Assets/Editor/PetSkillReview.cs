using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;

public static class PetSkillReview
{
    const string Output = "D:/Unity/BuildTools/PetSkillReview";
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        RenderTexture previous = RenderTexture.active;
        try
        {
            var holder = new GameObject("Pet portrait verification");
            var preview = holder.AddComponent<PetSkillPortrait>();
            var texture = preview.Render() as RenderTexture;
            var bones=holder.GetComponentsInChildren<Transform>(true);
            var details=new System.Text.StringBuilder();
            foreach(var bone in bones) if(bone.name.StartsWith("DEF-")) details.AppendLine(bone.name+" parent="+(bone.parent?bone.parent.name:"")+" pos="+bone.position+" rot="+bone.rotation.eulerAngles);
            File.WriteAllText(Path.Combine(Output,"bones.txt"),details.ToString());
            if (!texture || !texture.IsCreated()) throw new InvalidOperationException("Pet preview render texture unavailable.");
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            var pixels = image.GetPixels32();
            int visible = 0;
            foreach (var pixel in pixels) if (pixel.a > 8) visible++;
            if (visible < pixels.Length / 100) throw new InvalidOperationException("Pet preview did not render visible pixels: " + visible);
            File.WriteAllBytes(Path.Combine(Output, "pet-preview.png"), image.EncodeToPNG());
            File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS: dog preview rendered; visible pixels=" + visible + ". Review screenshot for pose and proportions. No save data modified.\n");
            Debug.Log("PET_SKILL_REVIEW PASS visible pixels=" + visible);
            RenderTexture.active = previous;
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            RenderTexture.active = previous;
            File.WriteAllText(Path.Combine(Output, "result.txt"), "FAIL: " + e + "\n");
            Debug.LogError("PET_SKILL_REVIEW FAIL " + e);
            EditorApplication.Exit(1);
        }
    }
}
