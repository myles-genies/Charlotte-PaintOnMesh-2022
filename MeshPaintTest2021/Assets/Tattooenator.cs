using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

public class Tattooenator : MonoBehaviour
{
    // Use base class for URPRenderer - ScriptableRendererData will be ForwardRendererData
    // at runtime in URP 10.4.0 and UniversalRendererData in later versions (ie, URP 12.X)
    public ScriptableRendererData URPRenderer;
    public Material DrawMaterial;
    public Texture2D BaseTexture;

    private RenderTexture renderTex;

    // this setting assumes we are rendering in Linear space (not Gamma)
    private const GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
    private const int defaultDim = 1024;

    private DrawOnMeshRenderPassFeature _renderFeature = null;
    private DrawOnMeshRenderPassFeature RenderFeature {
        get {
            if (_renderFeature == null) {
                if (URPRenderer == null) {
                    Debug.LogError("URPRenderer is null, select the renderer that has the DrawOnMesh Render Feature.");
                } else {
                    _renderFeature = URPRenderer.rendererFeatures.OfType<DrawOnMeshRenderPassFeature>().FirstOrDefault();
                    if (_renderFeature == default(DrawOnMeshRenderPassFeature))
                        Debug.LogError("DrawOnMesh Render Feature not found"); 
                }
            }
            return _renderFeature;
        }
    }

    private static Texture2D TextureFromRenderTexture(RenderTexture renderTexture, string name) {
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height,
            graphicsFormat, TextureCreationFlags.None);
        texture.name = name;

        var current = RenderTexture.active;
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
        texture.Apply();
        RenderTexture.active = current;

        return texture;
    }


    void Start()
    {
        if (RenderFeature != null)
        {
            if (TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                RenderFeature.DrawMesh = skinnedMeshRenderer.sharedMesh;
            } else {
                if (TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    RenderFeature.DrawMesh = meshFilter.sharedMesh;
                }
            }
            
            RenderFeature.DrawMaterial = DrawMaterial;

            if (BaseTexture != null) {
                renderTex = RenderTexture.GetTemporary(BaseTexture.width, BaseTexture.height, 0, graphicsFormat);
                // accumulate paint strokes atop a base texture
                DrawMaterial.SetTexture("_MainTex", BaseTexture);
            } else {
                renderTex = RenderTexture.GetTemporary(defaultDim, defaultDim, 0, graphicsFormat);
            }
            RenderFeature.RenderTex = renderTex;

            RenderFeature.ShouldExecute = true;
        }

    }

    private void Saveout(Texture2D tex) {
        var path = $"{Application.streamingAssetsPath}/{tex.name}.png";
        File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
    }

    void Update()
    {

        if (Input.GetMouseButton(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;
            RaycastHit[] hits;
            Debug.DrawRay(ray.origin, ray.direction * 10);
            //if (Physics.Raycast(ray, out hit)) {
            hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; i++) {
                RaycastHit hit = hits[i];
                Debug.Log(" hit " + hit.point.ToString());
                if (hit.transform.tag == "projector") {
                    DrawMaterial.SetVector("_Coordinate", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));
                    Debug.Log(" proj uv " + hit.textureCoord.ToString());
                } else {
                    //DrawMaterial.SetVector("_Coordinate", new Vector4(hit.point.x, hit.point.y, hit.point.z, 0));
                    DrawMaterial.SetVector("_HitUV", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));
                    Debug.Log(" obj uv " + hit.textureCoord.ToString());         
                }
                
            }
            RenderFeature.ShouldExecute = true;
            Texture2D paintedTex = TextureFromRenderTexture(renderTex, "paintCapture");
            Saveout(paintedTex);  // for debugging

            // update displayed material with latest paint strokes
            GetComponent<Renderer>().material.SetTexture("_BaseMap", paintedTex);
            // update draw material, so we can accumulate more strokes
            DrawMaterial.SetTexture("_MainTex", paintedTex);
        }
    }

    private void OnDestroy() {
        RenderTexture.ReleaseTemporary(renderTex);
    }
}
