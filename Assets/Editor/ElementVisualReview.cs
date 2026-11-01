using System;
using System.IO;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Isolated graphics-enabled batch preview. This samples the first instant of
// each effect, not gameplay, animation timing, mobile performance or Metal.
public static class ElementVisualReview
{
    const string Output = "D:/Unity/BuildTemp/ElementVisualReview";
    public static void Run()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run only in a separate batch-mode QA project.");
        Directory.CreateDirectory(Output);
        var oldRandom = UnityEngine.Random.state;
        RenderTexture rt = null;
        Texture2D frame = null;
        Material groundMaterial = null, targetMaterial = null;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Element visual review camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.085f, .095f, .10f);
            camera.orthographic = true;
            camera.orthographicSize = 2.5f;
            camera.transform.position = new Vector3(5.5f, 5.2f, -8f);
            camera.transform.LookAt(new Vector3(0, 1.05f, 0));
            // Explicit unlit materials keep the reference surfaces dark even
            // when the editor's default ambient/light settings are very bright.
            Shader matte = Shader.Find("Unlit/Color");
            if (matte == null) throw new InvalidOperationException("Unlit/Color shader unavailable.");
            groundMaterial = new Material(matte) { color = new Color(.11f, .13f, .14f, 1) };
            targetMaterial = new Material(matte) { color = new Color(.25f, .27f, .28f, 1) };
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Neutral preview ground";
            ground.transform.localScale = Vector3.one * .7f;
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "Neutral target silhouette";
            target.transform.position = Vector3.up;
            target.GetComponent<Renderer>().sharedMaterial = targetMaterial;
            rt = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            frame = new Texture2D(768, 768, TextureFormat.RGB24, false);
            camera.targetTexture = rt;
            var types = new[]{ GunElement.Fire, GunElement.Electric, GunElement.Poison, GunElement.Explosive };
            foreach (var element in types)
            {
                UnityEngine.Random.InitState(1711);
                var root = new GameObject("Transient " + element);
                // Offset toward the camera along the viewing ray, beyond the
                // capsule surface, so the effect is not occluded by the target.
                Vector3 impact = target.transform.position - camera.transform.forward * .80f;
                CombatVisuals.ElementImpact(impact, element, root.transform);
                if (element == GunElement.Electric)
                    CombatVisuals.ElementArc(impact, impact + camera.transform.right * 1.8f + camera.transform.up * .35f, root.transform);
                camera.Render();
                var oldActive = RenderTexture.active;
                RenderTexture.active = rt;
                frame.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                frame.Apply();
                RenderTexture.active = oldActive;
                File.WriteAllBytes(Path.Combine(Output, element.ToString().ToLowerInvariant()+".png"), frame.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllText(Path.Combine(Output, "README.txt"),
                "Initial instant only. Not a gameplay, animation, iOS, or Metal validation.\n");
            Debug.Log("ELEMENT_VISUAL_REVIEW_OK " + Output);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
        finally
        {
            UnityEngine.Random.state = oldRandom;
            if (frame != null) UnityEngine.Object.DestroyImmediate(frame);
            if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            if (groundMaterial != null) UnityEngine.Object.DestroyImmediate(groundMaterial);
            if (targetMaterial != null) UnityEngine.Object.DestroyImmediate(targetMaterial);
        }
    }
}
