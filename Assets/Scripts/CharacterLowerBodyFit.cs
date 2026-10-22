using UnityEngine;

namespace NeonVault
{
    // Fit measured rest-space garment centres, not camera projections. Measurements
    // come from common/armor sharedMesh vertices in part-slot-fit/rest-vertices.json.
    static class CharacterLowerBodyFit
    {
        // Warden's source boot rim .43 maps between source .25/.65 to .101/.517.
        public const float FeetSeamHeight = .2882f;
        static readonly float[] Heights = { .20f, .25f, .2882f, .35f, .43f, .517f, .65f, .75f, .80f, .95f };
        static readonly float[] Inward = { .075f, .0586f, .06145f, .04467f, .04893f, .03732f, .03339f, .015f, 0f, 0f };
        static readonly float[] Forward = { .045f, .04373f, .05962f, .05182f, .04399f, .05013f, .06229f, .05353f, .040f, 0f };

        static void Offset(float height, out float inward, out float forward, out float inwardSlope, out float forwardSlope)
        {
            inward = Inward[0]; forward = Forward[0]; inwardSlope = forwardSlope = 0f;
            if (height <= Heights[0]) return;
            for (int i = 1; i < Heights.Length; i++)
            {
                if (height > Heights[i]) continue;
                float span = Heights[i] - Heights[i - 1];
                float t = Mathf.Clamp01((height - Heights[i - 1]) / span);
                float smooth = t * t * (3f - 2f * t);
                float derivative = 6f * t * (1f - t) / span;
                inward = Mathf.Lerp(Inward[i - 1], Inward[i], smooth);
                forward = Mathf.Lerp(Forward[i - 1], Forward[i], smooth);
                inwardSlope = (Inward[i] - Inward[i - 1]) * derivative;
                forwardSlope = (Forward[i] - Forward[i - 1]) * derivative;
                return;
            }
            inward = forward = 0f;
        }

        public static Vector3 Position(Vector3 source)
        {
            if (source.y >= .95f) return source;
            Offset(source.y, out var inward, out var forward, out _, out _);
            // Fade across the joined crotch so left/right halves cannot separate.
            float lateral = Mathf.Clamp(source.x / .07f, -1f, 1f);
            return source + new Vector3(-inward * lateral, 0f, forward);
        }

        public static Vector3 Normal(Vector3 source, Vector3 normal)
        {
            if (source.y >= .95f) return normal;
            Offset(source.y, out var inward, out _, out var dx, out var dz);
            float lateral = Mathf.Clamp(source.x / .07f, -1f, 1f);
            float xScale = Mathf.Abs(source.x) < .07f ? 1f - inward / .07f : 1f;
            float nx = normal.x / Mathf.Max(.15f, xScale);
            return new Vector3(nx, normal.y + lateral * dx * nx - dz * normal.z, normal.z).normalized;
        }

        static float SourceHeight(float height)
        {
            if (height < .101f) return .10f + (height - .03f) / (.101f - .03f) * .15f;
            if (height < .517f) return .25f + (height - .101f) / (.517f - .101f) * .40f;
            if (height < .89f) return .65f + (height - .517f) / (.89f - .517f) * .35f;
            return 1f + (height - .89f) / .15f * .25f;
        }

        public static BoneWeight Weight(Vector3 point)
        {
            int a = point.x >= 0f ? 12 : 9, b = a + 1;
            float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.45f, .59f, point.y));
            if (point.y < .30f)
            {
                a++; b++;
                t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.09f, .23f, point.y));
            }
            float sourceHeight = SourceHeight(point.y);
            float leg = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.88f, 1.08f, sourceHeight));
            leg *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .07f, Mathf.Abs(point.x)));
            return new BoneWeight { boneIndex0 = a, weight0 = leg * (1f - t), boneIndex1 = b,
                weight1 = leg * t, boneIndex2 = 1, weight2 = 1f - leg };
        }
    }
}
