using UnityEngine;
namespace NeonVault {
 public static class PolyHavenStreetLamp {
  // Called by the existing placement adapter before its regular street props.
  public static bool Place(string category, Transform parent, Vector3 position, int seed) {
   if (category != "street" || !parent.name.StartsWith("Stream /") || (seed & 15) != 0) return false;
   var source = Resources.Load<GameObject>("ImportedWorld/PolyHaven/street_lamp_01");
   if (!source) return false;
   var bounds = ImportedWorldBatch.SourceBounds(source);
   if (bounds.size.x < .001f || bounds.size.y < .001f || bounds.size.z < .001f) return false;
   float scale = Mathf.Min(1.2f / bounds.size.x, 4f / bounds.size.y, 1.2f / bounds.size.z);
   var rotation = Quaternion.Euler(0, (seed & 3) * 90, 0);
   var offset = rotation * new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
   ImportedWorldBatch.Add(parent, source, position + offset, rotation, Vector3.one * scale);
   return true;
  }
 }
}
