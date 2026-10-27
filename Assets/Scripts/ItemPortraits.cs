using UnityEngine;

namespace NeonVault
{
    // Atlas cells represent equipment families; rolled stats remain authoritative in the card.
    public static class ItemPortraits
    {
        static Texture2D atlas;
        static bool loaded;
        static Material previewMaterial;
        static LayeredPlayer2D.WeaponRegionTable previewRegions;
        static LayeredPlayer2D.WeaponRegionTable modernPreviewRegions;
        static LayeredPlayer2D.WeaponRegionTable handgunPreviewRegions;
        static readonly Vector4[] previewHull=new Vector4[64];
        static readonly System.Collections.Generic.Dictionary<string,RenderTexture> previews=new System.Collections.Generic.Dictionary<string,RenderTexture>();
        static Texture Preview(Texture2D source,Rect crop,int weaponModel=-1)
        {
            string key=source.name+":"+crop.ToString("F5")+":"+weaponModel;if(previews.TryGetValue(key,out var cached))return cached;
            if(previewMaterial==null){var shader=Resources.Load<Shader>("PopItemPreview");if(shader==null)return source;previewMaterial=new Material(shader);}
            var result=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32);result.name="Pop item art / "+key;result.Create();
            previewMaterial.SetVector("_Crop",new Vector4(crop.x,crop.y,crop.width,crop.height));int count=0;
            if(weaponModel>=0)
            {
                if(previewRegions==null){var json=Resources.Load<TextAsset>(LayeredPlayer2D.ArtMetadataPath("WeaponRegions"));if(json!=null)previewRegions=JsonUtility.FromJson<LayeredPlayer2D.WeaponRegionTable>(json.text);}
                if(weaponModel>=12&&modernPreviewRegions==null){var json=Resources.Load<TextAsset>(LayeredPlayer2D.InkedArtReady?"Art/LayeredPlayer/InkedArt/WeaponRegions":"Art/LayeredPlayer/TacticalArt/ModernWeaponRegions");if(json!=null)modernPreviewRegions=JsonUtility.FromJson<LayeredPlayer2D.WeaponRegionTable>(json.text);}
                if(weaponModel>=14&&handgunPreviewRegions==null){var json=Resources.Load<TextAsset>("Art/LayeredPlayer/HandgunArt/HandgunWeaponRegions");if(json!=null)handgunPreviewRegions=JsonUtility.FromJson<LayeredPlayer2D.WeaponRegionTable>(json.text);}
                var regions=weaponModel>=14?handgunPreviewRegions:weaponModel>=12?modernPreviewRegions:previewRegions;
                if(regions?.regions!=null)foreach(var region in regions.regions)if(region.model==weaponModel&&region.direction==1&&region.hull!=null){count=Mathf.Min(64,region.hull.Length);for(int point=0;point<count;point++)previewHull[point]=new Vector4(region.hull[point].x,1-region.hull[point].y,0,0);break;}
            }
            previewMaterial.SetInt("_ClipCount",count);previewMaterial.SetVectorArray("_ClipPoints",previewHull);
            var previous=RenderTexture.active;try{Graphics.Blit(source,result,previewMaterial);}finally{RenderTexture.active=previous;}previews[key]=result;return result;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPreviews(){foreach(var value in previews.Values){value.Release();Object.Destroy(value);}previews.Clear();if(previewMaterial!=null)Object.Destroy(previewMaterial);previewMaterial=null;previewRegions=null;modernPreviewRegions=null;handgunPreviewRegions=null;loaded=false;atlas=null;}
        // Wasteland atlas: twelve gun designs followed by head, torso, arms and lower-body gear.
        static readonly Rect[] portraitWindows={
            new Rect(12,65,324,250),new Rect(342,65,278,250),new Rect(601,65,340,250),new Rect(913,65,329,250),
            new Rect(10,370,332,240),new Rect(335,370,287,240),new Rect(623,370,311,240),new Rect(929,370,312,240),
            new Rect(18,674,321,216),new Rect(327,674,309,216),new Rect(639,674,290,216),new Rect(938,674,305,216),
            new Rect(35,926,274,295),new Rect(335,920,297,304),new Rect(650,959,293,264),new Rect(975,906,268,317)
        };
        static Rect PortraitUV(int cell)
        {
            Rect crop=portraitWindows[Mathf.Clamp(cell,0,15)];
            return new Rect(crop.x/1254f,1-crop.yMax/1254f,crop.width/1254f,crop.height/1254f);
        }
        public static bool AtlasAvailable
        {
            get{EnsureAtlas();return atlas!=null;}
        }
        static void EnsureAtlas()
        {if(!loaded){atlas=Resources.Load<Texture2D>("Art/WastelandEquipment-v2");loaded=true;}}
        public static int GunCell(Gun item)=>VaultLanguage.DisplayModel(item);
        public static int ArmorCell(ArmorItem item)=>12+Mathf.Min((int)item.slot,3);
        public static void Draw(Rect rect,Gun item)
        {
            if(item!=null&&item.visualSet==Arsenal.TrailWardenVisualSet&&DrawApprovedPortrait(rect,"Weapon_"+GunCell(item),item.rarity)){ManufacturerBranding.PortraitBadge(rect,ManufacturerBranding.Maker(item));return;}
            if(BespokeLegendaryPortraits.Draw(rect,item))return;
            if(DistinctEquipmentPortraits.Draw(rect,item))return;
            DrawLegacy(rect,item);
            ManufacturerBranding.PortraitBadge(rect,ManufacturerBranding.Maker(item));
        }
        static void DrawLegacy(Rect rect,Gun item)
        {
            int model=GunCell(item);
            if(item.visualSet==Arsenal.TrailWardenVisualSet&&DrawApprovedPortrait(rect,"Weapon_"+model,item.rarity))return;
            var sprites=LayeredPlayer2D.LoadCells("Art/LayeredPlayer/Weapons_"+(model/2),false);
            var sprite=sprites!=null?sprites[(model%2)*8+1]:null;
            if(sprite==null){Draw(rect,model,item.color,item.rarity,0,true);return;}
            DrawSpriteCard(rect,sprite,LayeredPlayer2D.WeaponDisplayTint(model,item.color),item.rarity,true,model);
        }
        public static void Draw(Rect rect,ArmorItem item)
        {
            // The starter kit has authored art. Do not hash it into another manufacturer product.
            if(item!=null&&item.visualVariant==4&&item.designVariant==0
                &&(string.IsNullOrEmpty(item.catalogId)||item.catalogId==ArmorProductCatalog.Get(item.slot,4,0).id)
                &&DrawApprovedPortrait(rect,"Armor_"+(int)item.slot,item.rarity)){ManufacturerBranding.PortraitBadge(rect,ManufacturerBranding.Maker(item));return;}
            if(BespokeLegendaryPortraits.Draw(rect,item))return;
            if(DistinctEquipmentPortraits.Draw(rect,item))return;
            DrawLegacy(rect,item);
            ManufacturerBranding.PortraitBadge(rect,ManufacturerBranding.Maker(item));
        }
        static void DrawLegacy(Rect rect,ArmorItem item)
        {
            if(item.visualVariant==4&&DrawApprovedPortrait(rect,"Armor_"+(int)item.slot,item.rarity))return;
            var sprites=LayeredPlayer2D.LoadCells(LayeredPlayer2D.ArmorSourcePath("Front",item.visualVariant),true);
            if(sprites==null){Draw(rect,ArmorCell(item),item.color,item.rarity,item.visualVariant,false);return;}
            int[] part={0,1,2,4,6};var sprite=sprites[(item.visualVariant%2)*8+part[(int)item.slot]];
            if(sprite==null){Draw(rect,ArmorCell(item),item.color,item.rarity,item.visualVariant,false);return;}
            DrawSpriteCard(rect,sprite,Color.white,item.rarity,true,-2-(item.visualVariant%2)*5-(int)item.slot);
        }
        static bool DrawApprovedPortrait(Rect rect,string key,Rarity rarity)
        {
            var texture=Resources.Load<Texture2D>("Art/ApprovedFieldkit/Portrait_"+key);if(texture==null)return false;
            Fill(rect,new Color(.005f,.008f,.015f));Fill(new Rect(rect.x+3,rect.y+3,rect.width-6,3),Arsenal.Colors[(int)rarity]);
            GUI.color=Color.white;GUI.DrawTexture(new Rect(rect.x+5,rect.y+5,rect.width-10,rect.height-10),texture,ScaleMode.ScaleToFit,true);return true;
        }
        static void DrawSpriteCard(Rect rect,Sprite sprite,Color tint,Rarity rarity,bool directTint=false,int weaponModel=-1)
        {
            Color edge=Arsenal.Colors[(int)rarity];Fill(rect,new Color(.005f,.008f,.015f));Fill(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),new Color(.075f,.11f,.17f));Fill(new Rect(rect.x+3,rect.y+3,rect.width-6,3),edge);Fill(new Rect(rect.x+3,rect.yMax-6,rect.width-6,3),edge*.75f);
            var image=new Rect(rect.x+5,rect.y+5,rect.width-10,rect.height-10);float ratio=sprite.rect.width/sprite.rect.height;
            if(ratio>image.width/image.height){float h=image.width/ratio;image.y+=(image.height-h)*.5f;image.height=h;}
            else{float w=image.height*ratio;image.x+=(image.width-w)*.5f;image.width=w;}
            GUI.color=directTint?tint:Color.Lerp(Color.white,tint,.18f);GUI.DrawTexture(image,Preview(sprite.texture,new Rect(sprite.rect.x/sprite.texture.width,sprite.rect.y/sprite.texture.height,sprite.rect.width/sprite.texture.width,sprite.rect.height/sprite.texture.height),weaponModel),ScaleMode.StretchToFill,true);GUI.color=Color.white;
        }
        static void Fill(Rect rect,Color color)
        {GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        static void Draw(Rect rect,int cell,Color tint,Rarity rarity,int variant,bool gun)
        {
            EnsureAtlas();
            Color edge=Arsenal.Colors[(int)rarity];
            Fill(rect,new Color(.005f,.008f,.015f,1));Fill(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),new Color(.075f,.11f,.17f));
            Fill(new Rect(rect.x,rect.y,rect.width,2),edge);
            Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),edge*.75f);
            Rect image=new Rect(rect.x+5,rect.y+5,rect.width-10,rect.height-10);
            if(atlas!=null)
            {
                // Rows in the art brief are counted from the top; texture UVs from the bottom.
                GUI.color=Color.Lerp(Color.white,tint,gun?.16f:.28f);
                Rect uv=PortraitUV(cell);float ratio=uv.width*atlas.width/(uv.height*atlas.height);
                if(ratio>1){float h=image.width/ratio;image.y+=(image.height-h)*.5f;image.height=h;}
                else{float w=image.height*ratio;image.x+=(image.width-w)*.5f;image.width=w;}
                GUI.DrawTexture(image,Preview(atlas,uv),ScaleMode.StretchToFill,true);
                GUI.color=Color.white;
            }
            else
            {
                // Readable equipment silhouettes keep the cards functional if the optional art is unavailable.
                Color metal=Color.Lerp(new Color(.5f,.6f,.65f),tint,.5f);
                if(gun)
                {
                    Fill(new Rect(image.x+image.width*.12f,image.y+image.height*.36f,image.width*.65f,image.height*.18f),metal);
                    Fill(new Rect(image.x+image.width*.68f,image.y+image.height*.39f,image.width*.25f,image.height*.08f),edge);
                    Fill(new Rect(image.x+image.width*.35f,image.y+image.height*.51f,image.width*.12f,image.height*.25f),metal);
                    if(cell==1)Fill(new Rect(image.x+image.width*.62f,image.y+image.height*.52f,image.width*.28f,image.height*.07f),metal);
                }
                else
                {
                    float w=cell==8||cell==13?.5f:cell==12||cell==15?.35f:.65f;
                    Fill(new Rect(image.center.x-image.width*w*.5f,image.y+image.height*.2f,image.width*w,image.height*.6f),metal);
                    Fill(new Rect(image.center.x-2,image.y+image.height*.3f,4,image.height*.3f),edge);
                }
            }
            // Variant marks and the individual tint identify armor rolls without implying fixed numbers.
            for(int i=0;i<=Mathf.Clamp(variant,0,3);i++)
                Fill(new Rect(rect.x+5+i*7,rect.yMax-7,4,3),tint);
        }
    }
}



