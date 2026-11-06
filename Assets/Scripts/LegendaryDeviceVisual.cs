using UnityEngine;

namespace NeonVault
{
    // A few shared primitive meshes make deployed legendary effects readable from above.
    internal static class LegendaryDeviceVisual
    {
        static readonly Material[] materials=new Material[12];
        static Material Material(int maker,bool accent)
        {
            int index=maker*2+(accent?1:0);
            if(materials[index])return materials[index];
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material=new Material(shader);
            Color tint=VaultGame.LegendaryEffectColor(maker);
            material.color=accent?tint:Color.Lerp(new Color(.12f,.15f,.18f),tint,.25f);
            material.enableInstancing=true;
            materials[index]=material;
            return material;
        }
        static void Part(Transform root,string name,PrimitiveType shape,Vector3 position,Vector3 scale,int maker,bool accent)
        {
            var part=GameObject.CreatePrimitive(shape);part.name=name;part.transform.SetParent(root,false);
            part.transform.localPosition=position;part.transform.localScale=scale;
            Object.Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial=Material(maker,accent);
        }
        public static GameObject Create(Transform world,Vector3 position,int maker)
        {
            if(!world)return null;
            maker=Mathf.Clamp(maker,0,5);
            var root=new GameObject("Legendary deployed device / "+maker);
            root.transform.SetParent(world,false);root.transform.position=position+Vector3.up*.12f;
            switch(maker)
            {
                case 0: // compact guidance beacon
                    Part(root.transform,"beacon body",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.34f,.08f,.34f),maker,false);
                    Part(root.transform,"beacon eye",PrimitiveType.Sphere,new Vector3(0,.16f,0),new Vector3(.2f,.2f,.2f),maker,true);break;
                case 1: // industrial blast mine
                    Part(root.transform,"armored mine",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.42f,.1f,.42f),maker,false);
                    for(int i=0;i<4;i++)Part(root.transform,"warning stud",PrimitiveType.Cube,new Vector3(i<2?(i==0?-.29f:.29f):0,.13f,i>=2?(i==2?-.29f:.29f):0),new Vector3(.12f,.08f,.12f),maker,true);break;
                case 2: // arc capacitor
                    Part(root.transform,"capacitor",PrimitiveType.Cube,Vector3.zero,new Vector3(.55f,.15f,.38f),maker,false);
                    Part(root.transform,"charged core",PrimitiveType.Sphere,new Vector3(0,.18f,0),new Vector3(.25f,.25f,.25f),maker,true);break;
                case 3: // spore pod
                    Part(root.transform,"pod",PrimitiveType.Sphere,Vector3.zero,new Vector3(.58f,.3f,.58f),maker,false);
                    for(int i=0;i<3;i++){float a=i*2.094f;Part(root.transform,"spore node",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.2f,.17f,Mathf.Sin(a)*.2f),new Vector3(.13f,.13f,.13f),maker,true);}break;
                case 4: // delayed fire charge
                    Part(root.transform,"charge",PrimitiveType.Cube,Vector3.zero,new Vector3(.58f,.18f,.42f),maker,false);
                    Part(root.transform,"fuse",PrimitiveType.Cylinder,new Vector3(0,.2f,0),new Vector3(.11f,.13f,.11f),maker,true);break;
                default: // shield countermeasure
                    Part(root.transform,"shield unit",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.4f,.11f,.4f),maker,false);
                    Part(root.transform,"shield lens",PrimitiveType.Cube,new Vector3(0,.14f,0),new Vector3(.42f,.07f,.16f),maker,true);break;
            }
            return root;
        }
    }
}
