using System.IO;
using System.Reflection;
using UnityEngine;
using NeonVault;

public static class PopItemBaker
{
    public static void Bake()
    {
        const string output="D:/Unity/AshfallArtValidation/PopItemReview";Directory.CreateDirectory(output);
        var preview=typeof(ItemPortraits).GetMethod("Preview",BindingFlags.NonPublic|BindingFlags.Static);
        if(Arsenal.ModelCount!=18||!LayeredPlayer2D.HandgunsReady)throw new System.Exception("All18 weapon models and finished handgun source metadata required for item evidence");
        for(int model=0;model<Arsenal.ModelCount;model++)
        {
            var sprite=LayeredPlayer2D.LoadCells("Art/LayeredPlayer/Weapons_"+(model/2),false)[(model%2)*8+1];var rect=sprite.rect;var tex=sprite.texture;
            if(LayeredPlayer2D.MulticolorArtReady&&!(model>=14?tex.name.StartsWith("Handguns_"):model>=12?tex.name.StartsWith(LayeredPlayer2D.InkedArtReady?"Weapons_6":"WeaponsModern_"):tex.name.StartsWith("WeaponsV3_")))throw new System.Exception("Authored weapon source not loaded / "+model);
            Save((RenderTexture)preview.Invoke(null,new object[]{tex,new Rect(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height),model}),Path.Combine(output,"weapon-"+model+".png"),model==15||model==17?1:2);
        }
        int[] parts={0,1,2,4,6};
        for(int set=0;set<LayeredPlayer2D.ArmorStyleCount;set++)for(int slot=0;slot<5;slot++)
        {
            var armor=LayeredPlayer2D.LoadCells(LayeredPlayer2D.ArmorSourcePath("Front",set),true);var sprite=armor[(set%2)*8+parts[slot]];var rect=sprite.rect;var tex=sprite.texture;
            if(LayeredPlayer2D.MulticolorArtReady&&!(set>=2?tex.name.StartsWith("ArmorV5_"):LayeredPlayer2D.InkedArtReady?tex.name.StartsWith("Armor_"):LayeredPlayer2D.TacticalArtReady?tex.name.StartsWith("ArmorV4_"):tex.name.StartsWith("ArmorV3_")))throw new System.Exception("Authored armor source not loaded / "+set+" / "+slot);
            Save((RenderTexture)preview.Invoke(null,new object[]{tex,new Rect(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height),-2-set*5-slot}),Path.Combine(output,"armor-"+set+"-"+slot+".png"));
        }
        File.WriteAllText(Path.Combine(output,"pop-items-result.txt"),"PASS /"+(Arsenal.ModelCount+LayeredPlayer2D.ArmorStyleCount*5)+" actual GPU previews / opaque colored image with black ink pixels / "+System.DateTime.UtcNow.ToString("O"));
        Debug.Log("ASHFALL_POP_ITEM_BAKE_OK /"+(Arsenal.ModelCount+LayeredPlayer2D.ArmorStyleCount*5)+" previews");
    }
    static void Save(RenderTexture render,string path,int minimumHueFamilies=2)
    {
        var old=RenderTexture.active;var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try
        {
            RenderTexture.active=render;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply(false);int ink=0,color=0;
            int neutral=0;var hues=new int[12];
            foreach(var pixel in image.GetPixels32())if(pixel.a>128){if(pixel.r+pixel.g+pixel.b<35)ink++;else if(pixel.r+pixel.g+pixel.b>100)color++;Color.RGBToHSV(pixel,out float hue,out float saturation,out float value);if(value>.18f){if(saturation<.18f)neutral++;else hues[Mathf.Min(11,Mathf.FloorToInt(hue*12))]++;}}
            if(ink<80||color<500)throw new System.Exception("Pop item preview missing ink or color / "+path+" / "+ink+" / "+color);
            int families=0;foreach(int pixels in hues)if(pixels>=35)families++;
            // Service/quiet pistols use steel plus olive polymer/teal glass. Neutral metal is
            // a distinct material color; adding a second painted hue is not their art contract.
            if(LayeredPlayer2D.MulticolorArtReady&&(neutral<80||families<minimumHueFamilies))throw new System.Exception("Item must retain neutral metal and authored material hues / "+path+" / neutral="+neutral+" / hues="+families);
            File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
