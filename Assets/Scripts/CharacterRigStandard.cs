using UnityEngine;

namespace NeonVault
{
    /// <summary>Shared meter-space skeleton and attachment contract for every equipment set.</summary>
    public static class CharacterRigStandard
    {
        public const int Version=1;
        public const float HeadMeshHeight=.285f;
        public const float MaskHeight=.153f;
        public static readonly Vector3 MaskCenter=new Vector3(0,.036f,.025f);
        internal static readonly Vector3[] RestPositions=
        {
            new Vector3(0,1.25f,0),new Vector3(0,.98f,0),new Vector3(0,1.55f,0),
            new Vector3(-.18f,1.427f,0),new Vector3(-.18f,1.142f,0),new Vector3(-.18f,.877f,0),
            new Vector3(.18f,1.427f,0),new Vector3(.18f,1.142f,0),new Vector3(.18f,.877f,0),
            new Vector3(-.105f,.955f,0),new Vector3(-.105f,.517f,0),new Vector3(-.105f,.101f,0),
            new Vector3(.105f,.955f,0),new Vector3(.105f,.517f,0),new Vector3(.105f,.101f,0)
        };
        public static Transform[] GetBones(DetailedHero3D hero)=>new[]
        {
            hero.chest,hero.pelvis,hero.head,
            hero.leftUpperArm,hero.leftForearm,hero.leftHand,
            hero.rightUpperArm,hero.rightForearm,hero.rightHand,
            hero.leftThigh,hero.leftShin,hero.leftFoot,
            hero.rightThigh,hero.rightShin,hero.rightFoot
        };
    }
}

