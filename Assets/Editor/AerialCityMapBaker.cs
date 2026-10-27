using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonVault.Editor
{
    // Offline aerial cartography from the SAME footprints and vegetation as gameplay.
    // No world meshes, character assets, lighting bake, or GPU renderer are loaded.
    public static class AerialCityMapBaker
    {
        const int Size=4096;
        static string Target=>"Assets/Resources/Art/UI/FieldKit/aerial-city-detail-"+tile+".png";
        static int tile;
        static Color32[] pixels,surface,roofs;
        static int surfaceSize,roofSize,index;
        static OverworldGeography geo;
        static double started;
        public static void Run()
        {
            Directory.CreateDirectory("D:/Unity/QA-premium-ui");
            EquipmentChestReview.Run();
            NeonVault.EditorTools.RealCityGeographyReview.Run();
            started=EditorApplication.timeSinceStartup;geo=new OverworldGeography();
            pixels=new Color32[Size*Size];
            AssetDatabase.ImportAsset("Assets/Resources/Art/UI/FieldKit/aerial-surfaces-v1.png",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/Resources/Art/UI/FieldKit/aerial-roofs-v1.png",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var tex=Resources.Load<Texture2D>("Art/UI/FieldKit/aerial-surfaces-v1");
            if(!tex||!tex.isReadable)throw new InvalidOperationException("Aerial surface source must be CPU readable");
            surface=tex.GetPixels32();surfaceSize=tex.width;
            var roof=Resources.Load<Texture2D>("Art/UI/FieldKit/aerial-roofs-v1");
            if(!roof||!roof.isReadable)throw new InvalidOperationException("Aerial roof source must be CPU readable");
            roofs=roof.GetPixels32();roofSize=roof.width;
            index=0;tile=Array.IndexOf(Environment.GetCommandLineArgs(),"--aerial-resume")>=0?1:0;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                while(index<5000&&timer.ElapsedMilliseconds<24)
                {int k=index++;PaintChunk(k%2500%50+tile%2*50-50,k%2500/50+tile/2*50-50,k>=2500);}
                if(index%500==0)File.WriteAllText("D:/Unity/QA-premium-ui/aerial-progress.txt","TILE "+tile+" / "+index+" / 5000");
                if(index<5000)return;
                // Encode CPU pixels directly: avoid allocating a full uncompressed GPU copy.
                var image=new Texture2D(Size,Size,TextureFormat.RGB24,false);image.SetPixels32(pixels);
                pixels=null;GC.Collect();
                File.WriteAllBytes(Target,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);GC.Collect();
                AssetDatabase.ImportAsset(Target,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(Target);
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=4096;importer.isReadable=false;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                var ios=importer.GetPlatformTextureSettings("iPhone");ios.overridden=true;ios.maxTextureSize=4096;
                ios.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(ios);importer.SaveAndReimport();
                tile++;if(tile<4){pixels=new Color32[Size*Size];index=0;return;}
                EditorApplication.update-=Tick;surface=null;roofs=null;geo=null;GC.Collect();
                File.WriteAllText("D:/Unity/QA-premium-ui/aerial-bake.txt","AERIAL_BAKE_COMPLETED / "+(EditorApplication.timeSinceStartup-started).ToString("0.0")+"s");
                EditorApplication.Exit(0);
            }
            catch(Exception error){EditorApplication.update-=Tick;File.WriteAllText("D:/Unity/QA-premium-ui/aerial-bake.txt",error.ToString());EditorApplication.Exit(2);}
        }
        static int Pixel(float value,bool vertical=false)=>Mathf.Clamp(Mathf.FloorToInt((value+5000-(vertical?tile/2:tile%2)*5000)*Size/5000),0,Size-1);
        static float World(int value,bool vertical=false)=>(value+.5f)*5000/Size-5000+(vertical?tile/2:tile%2)*5000;
        static Color32 Shade(int r,int g,int b,int delta=0)=>new Color32((byte)Mathf.Clamp(r+delta,0,255),(byte)Mathf.Clamp(g+delta,0,255),(byte)Mathf.Clamp(b+delta,0,255),255);
        static int Noise(int x,int y){uint h=unchecked((uint)(x*374761393+y*668265263));h=(h^(h>>13))*1274126177;return (int)((h^(h>>16))&255);}
        static int Grain(int x,int y,int quadrant)
        {
            if(surface==null)return Noise(x,y)/16-8;
            int half=surfaceSize/2;
            int sx=(x%half+half)%half+(quadrant%2)*half,sy=(y%half+half)%half+(quadrant/2)*half;
            var c=surface[sy*surfaceSize+sx];return (c.r+c.g+c.b)/24-16;
        }
        static void PaintChunk(int cx,int cz,bool details)
        {
            int x0=Pixel(cx*100),x1=Pixel((cx+1)*100),y0=Pixel(cz*100,true),y1=Pixel((cz+1)*100,true);
            if(!details)for(int y=y0;y<=y1;y+=2)for(int x=x0;x<=x1;x+=2)
            {
                var p=new Vector3(World(x),0,World(y,true));float river=Mathf.Abs(p.x-geo.RiverX(p.z))-geo.RiverWidth(p.z)*.5f;
                float road=geo.RoadClearance(p);bool park=geo.Park(p);
                Color32 color;
                int broad=(int)((Mathf.PerlinNoise(p.x*.004f+33,p.z*.004f+51)-.5f)*27);
                if(river<0&&!geo.Bridge(p.z))color=Shade(34,58,61,Grain(x,y,1)/2+(int)Mathf.Clamp(river*.075f,-11,0)+(int)(3*Mathf.Sin(y*.11f+x*.04f)));
                else if(road<0||river<0&&geo.Bridge(p.z))color=Shade(80,80,74,Grain(x,y,3)/2+(Noise(x/3,y/3)%9-4));
                else if(road<2)color=Shade(137,130,108,Grain(x,y,0)/2);
                else if(river<14)color=Shade(72,77,55,Grain(x,y,0)+broad);
                else if(park)color=Shade(47,61,34,Grain(x,y,0)+broad);
                else color=Shade(91,84,68,Grain(x,y,0)+broad);
                for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)if(x+dx<Size&&y+dy<Size)pixels[(y+dy)*Size+x+dx]=color;
            }
            if(!details)return;
            var chunk=geo.GetChunk(cx,cz);int n=0;
            // Shadows first, then roof planes and parapets; never hide the road network.
            foreach(var b in chunk.Buildings)
            {
                int left=Pixel(b.xMin),right=Pixel(b.xMax),bottom=Pixel(b.yMin,true),top=Pixel(b.yMax,true);
                int hash=Noise(left,bottom),height=4+hash%9;
                for(int y=bottom-height;y<=top;y++)for(int x=left+1;x<=right+height;x++)
                    if(x>=0&&x<Size&&y>=0&&y<Size){var c=pixels[y*Size+x];pixels[y*Size+x]=Shade(c.r*64/100,c.g*64/100,c.b*64/100);}
            }
            foreach(var b in chunk.Buildings)
            {
                int left=Pixel(b.xMin),right=Pixel(b.xMax),bottom=Pixel(b.yMin,true),top=Pixel(b.yMax,true),hash=Noise(left,bottom);
                int tone=105+hash%38;
                for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++)
                {
                    bool lip=x==left||y==top;bool shadow=x==right||y==bottom;
                    int grain=Grain(x,y,2)/2+(lip?20:shadow?-23:0);
                    // Pitched roof ridge, skylights and roof service boxes at world scale.
                    if(right-left>4&&top-bottom>4&&x==(left+right)/2)grain+=12;
                    if(right-left>7&&top-bottom>7&&x>left+2&&x<left+5&&y>bottom+2&&y<bottom+5)grain-=30;
                    if(roofs!=null)
                    {
                        int cell=roofSize/4,choice=hash%16;
                        int sx=(choice%4)*cell+Mathf.Clamp((x-left)*cell/Mathf.Max(1,right-left+1),0,cell-1);
                        int sy=(choice/4)*cell+Mathf.Clamp((y-bottom)*cell/Mathf.Max(1,top-bottom+1),0,cell-1);
                        var sample=roofs[sy*roofSize+sx];
                        pixels[y*Size+x]=Shade(sample.r*83/100+20,sample.g*83/100+17,sample.b*80/100+12,grain/2);
                    }
                    else pixels[y*Size+x]=Shade(tone+6,tone+2,tone-11,grain);
                }
                n++;
            }
            foreach(var car in chunk.Cars)
            {
                int x=Pixel(car.x),y=Pixel(car.z,true);pixels[y*Size+x]=Shade(92,86,70);
                if(y+1<Size)pixels[(y+1)*Size+x]=Shade(46,51,50);
            }
            foreach(var tree in chunk.Trees)
            {
                int tx=Pixel(tree.x),ty=Pixel(tree.z,true),hash=Noise(tx,ty);float radius=(geo.Park(tree)?3.1f:2.1f)+(hash%4)*.4f;
                int reach=Mathf.CeilToInt(radius);
                for(int y=ty-reach-2;y<=ty+reach;y++)for(int x=tx-reach;x<=tx+reach+2;x++)
                {
                    if(x<0||x>=Size||y<0||y>=Size)continue;
                    float dx=x-tx,dy=y-ty;float shape=radius*(.86f+Noise(x/2,y/2)*.0011f);
                    if(dx*dx+dy*dy>shape*shape)
                    {
                        float sx=dx-2,sy=dy+2;if(sx*sx+sy*sy>radius*radius)continue;
                        var old=pixels[y*Size+x];pixels[y*Size+x]=Shade(old.r*72/100,old.g*72/100,old.b*72/100);continue;
                    }
                    float dome=Mathf.Sqrt(Mathf.Max(0,1-(dx*dx+dy*dy)/(radius*radius)));
                    int light=(int)((-dx+dy)*3+dome*20)+Noise(x,y)%15;
                    pixels[y*Size+x]=Shade(31+hash%13,43+hash%17,23+hash%10,light);
                }
            }
        }
    }
}
