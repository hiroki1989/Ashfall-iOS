using UnityEngine;

namespace NeonVault
{
    // The procedural base arms are rigid children of these same rig joints.
    // All garment fitting and skin transitions derive from that one rest pose.
    static class CharacterArmFit
    {
        public static float ShoulderHeight => CharacterRigStandard.RestPositions[3].y;
        public static float ElbowHeight => CharacterRigStandard.RestPositions[4].y;
        public static float WristHeight => CharacterRigStandard.RestPositions[5].y;
        public static float Axis => Mathf.Abs(CharacterRigStandard.RestPositions[3].x);
        public const float SourceShoulderHeight = 1.62f;
        public const float SourceElbowHeight = 1.30f;
        public const float SourceWristHeight = 1.10f;
        // A gauntlet includes its cuff above the wrist. Keep its authored extent;
        // ownership and skin weights must not use different artificial joints.
        public const float SourceCuffTop = 1.14f;

        static float Remap(float value, float a, float b, float c, float d)
            => Mathf.LerpUnclamped(c, d, (value - a) / (b - a));

        public static float Height(float source)
            => source >= SourceElbowHeight
                ? Remap(source, SourceElbowHeight, SourceShoulderHeight, ElbowHeight, ShoulderHeight)
                : source >= SourceWristHeight
                    ? Remap(source, SourceWristHeight, SourceElbowHeight, WristHeight, ElbowHeight)
                    : Remap(source, .95f, SourceWristHeight, .74f, WristHeight);

        public static float CuffExtension(float sourceHeight)
        {
            // Both sides of the shared sleeve/cuff cut use the same deformation.
            // Peak at the cut; fade out before either anatomical joint is reached.
            float distance = Mathf.Abs(sourceHeight - SourceCuffTop);
            return .025f * (1f - Mathf.SmoothStep(0f, 1f, distance / .04f));
        }

        public static BoneWeight Weight(Vector3 point, int upperBone, float armInfluence)
        {
            float arm = Mathf.Clamp01(armInfluence);
            int proximal = upperBone, distal = upperBone + 1;
            float joint = ElbowHeight, radius = .055f;
            if (point.y < (ElbowHeight + WristHeight) * .5f)
            {
                proximal++; distal++;
                joint = WristHeight; radius = .045f;
            }
            float proximalWeight = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(joint - radius, joint + radius, point.y));
            float chest = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.07f, 1.23f, point.y));
            return new BoneWeight { boneIndex0 = proximal, weight0 = arm * proximalWeight,
                boneIndex1 = distal, weight1 = arm * (1f - proximalWeight),
                boneIndex2 = 0, weight2 = (1f - arm) * chest,
                boneIndex3 = 1, weight3 = (1f - arm) * (1f - chest) };
        }
    }
}
