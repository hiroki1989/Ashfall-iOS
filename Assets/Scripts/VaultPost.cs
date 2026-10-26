using UnityEngine;
namespace NeonVault
{
    [RequireComponent(typeof(Camera))]
    public class VaultPost : MonoBehaviour
    {
        Material effect;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(effect==null)effect=new Material(Resources.Load<Shader>("VaultPost"));
            var a=RenderTexture.GetTemporary(Mathf.Max(1,source.width/4),Mathf.Max(1,source.height/4),0,Application.isMobilePlatform?RenderTextureFormat.Default:RenderTextureFormat.DefaultHDR);
            var b=RenderTexture.GetTemporary(a.width,a.height,0,Application.isMobilePlatform?RenderTextureFormat.Default:RenderTextureFormat.DefaultHDR);
            Graphics.Blit(source,a,effect,0);
            for(int i=0;i<(Application.isMobilePlatform?1:2);i++)
            {
                effect.SetVector("_Direction",new Vector4((2+i)/(float)a.width,0,0,0));Graphics.Blit(a,b,effect,1);
                effect.SetVector("_Direction",new Vector4(0,(2+i)/(float)a.height,0,0));Graphics.Blit(b,a,effect,1);
            }
            effect.SetTexture("_Bloom",a);Graphics.Blit(source,destination,effect,2);
            RenderTexture.ReleaseTemporary(a);RenderTexture.ReleaseTemporary(b);
        }
        void OnDestroy(){if(effect!=null)Destroy(effect);}
    }
}
