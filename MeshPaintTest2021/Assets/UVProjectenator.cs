#define DOLATEUPDATE

using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

public class UVProjectenator : MonoBehaviour
{
    public GameObject projector; // cylinder
    public float radius = 1.0f;
    public float width = 1.0f;
    public float rotation;  //  in degrees
    public float rotation_difference;  // between lower and upper

    public UniversalRendererData URPRenderer;
    public Material DrawMaterial; // UVProjectorMaterial
    public Texture2D BaseTexture;

    private bool needsUpdate = false;
    private Transform projectorTransform;
    private bool doLateTextureUpdate = false;
    private int countdown;

    private RenderTexture renderTex;

    // this setting assumes we are rendering in Linear space (not Gamma)
    private const GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
    private const int defaultDim = 1024;

    private Vector3 lowerleft, lowerright, lowerleft_outer, lowerright_outer;
    private Vector3 upperleft, upperright, upperleft_outer, upperright_outer;

    private Material UVvisualizerMaterial;

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

    private bool _isInit = false;
    private void Init ()
    {
        if (_isInit)
            return;

        RenderFeature.SubscribeToRenderPassResultTexture(OnTextureResultRendered);
        _isInit = true;
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
        if (projector is null)
        { 
            Debug.LogError("projector gameObject is null");
            return;
        }

        projector.tag = "projector";
        projectorTransform = projector.transform;
        UVvisualizerMaterial = projector.GetComponent<Renderer>().material;

        if (RenderFeature != null) {
            if (TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinnedMeshRenderer)) {
                RenderFeature.DrawMesh = skinnedMeshRenderer.sharedMesh;
            } else {
                if (TryGetComponent<MeshFilter>(out MeshFilter meshFilter)) {
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
            

            // subscribe to result
            // apparently RenderFeature gets created multiple times, overwriting this subscription (Create() method  - Unity bug?)
            RenderFeature.SubscribeToRenderPassResultTexture(OnTextureResultRendered);
        }
    }

    private void Saveout(Texture2D tex) {
        var path = $"{Application.streamingAssetsPath}/{tex.name}.png";
        File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
    }

    private void Reset()
    {
        projector.transform.position = projectorTransform.position;
        projector.transform.rotation = projectorTransform.rotation;
        projector.transform.localScale = projectorTransform.localScale;
        DrawMaterial.SetTexture("_MainTex", BaseTexture);
        //GetComponent<Renderer>().material.SetTexture("_BaseMap", BaseTexture);
        needsUpdate = true;
    }

    void CylinderProject()
    {
        if (projector is null)
            return;

        //Reset();
        //Init();
        projector.transform.Rotate(projector.transform.up, rotation, Space.World);
        
        Vector3 center = projector.transform.position;
        float delta = 0.5f;      

        Vector3 surface_ptL = center - radius * projector.transform.forward;
        Vector3 surface_ptL_outer = center - (radius + delta) * projector.transform.forward;
        //Debug.DrawLine(center, surface_ptL_outer, Color.yellow);

        lowerleft = surface_ptL - width * projector.transform.up;
        lowerright = surface_ptL + width * projector.transform.up;
        lowerleft_outer = surface_ptL_outer - width * projector.transform.up;
        lowerright_outer = surface_ptL_outer + width * projector.transform.up;
        

        projector.transform.Rotate(projector.transform.up, -rotation_difference, Space.World);
        Vector3 surface_ptU = projector.transform.position - radius * projector.transform.forward;
        Vector3 surface_ptU_outer = projector.transform.position - (radius + delta) * projector.transform.forward;
        projector.transform.Rotate(projector.transform.up, rotation_difference, Space.World);

        upperleft = surface_ptU - width * projector.transform.up;
        upperright = surface_ptU + width * projector.transform.up;
        upperleft_outer = surface_ptU_outer - width * projector.transform.up;
        upperright_outer = surface_ptU_outer + width * projector.transform.up;
        

        float maxDist = 20.0f;
        Vector4 projectuv = new Vector4();
        Vector4 targetuv = new Vector4();
        RaycastHit[] hits;
        // min UV
        hits = Physics.RaycastAll(lowerleft_outer, lowerleft - lowerleft_outer, maxDist);
        for (int i=0; i<hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.tag == "projector")
            {
                projectuv.x = hit.textureCoord.x;
                projectuv.y = hit.textureCoord.y;
            } else
            {
                targetuv.x = hit.textureCoord.x;
                targetuv.y = hit.textureCoord.y;
            }     
        }
        // max UV
        hits = Physics.RaycastAll(upperright_outer, upperright - upperright_outer, maxDist);
        for (int i = 0; i < hits.Length; i++) {
            RaycastHit hit = hits[i];
            if (hit.transform.tag == "projector") {
                projectuv.z = hit.textureCoord.x;
                projectuv.w = hit.textureCoord.y;
            } else {
                targetuv.z = hit.textureCoord.x;
                targetuv.w = hit.textureCoord.y;
            }
        }

        if (DrawMaterial is null)
            return;

        Debug.Log("Projector" + projectuv.ToString());
        Debug.Log("Target" + targetuv.ToString());
        DrawMaterial.SetVector("_ProjectorUV", projectuv);
        DrawMaterial.SetVector("_TargetUV", targetuv);
        UVvisualizerMaterial.SetVector("_ProjectorUV", projectuv);
        RenderFeature.ShouldExecute = true;


        //Texture2D paintedTex = TextureFromRenderTexture(renderTex, "paintCapture");
        // update displayed material with latest paint strokes
        //GetComponent<Renderer>().material.SetTexture("_BaseMap", paintedTex);
        // update draw material, so we can accumulate more strokes
        //DrawMaterial.SetTexture("_MainTex", paintedTex);
        needsUpdate = false;
    } 

    public void OnTextureResultRendered(Texture2D tex)
    {
#if DOLATEUPDATE
        doLateTextureUpdate = true;
        countdown = 1;
        Debug.Log("ReadyForTextureUpdate?");
        return;
#else
        Debug.Log("Updating texture");
        // update displayed material with latest paint strokes
        GetComponent<Renderer>().material.SetTexture("_BaseMap", tex);
        // update draw material, so we can accumulate more strokes
        DrawMaterial.SetTexture("_MainTex", tex);
        RenderFeature.ShouldExecute = false;
#endif
    }

    public void UpdateFromRenderTexture()
    {
        Debug.Log("Updating texture");
        Texture2D tex = TextureFromRenderTexture(renderTex, "projectCapture");
        // update displayed material with latest paint strokes
        GetComponent<Renderer>().material.SetTexture("_BaseMap", tex);
        // update draw material, so we can accumulate more strokes
        DrawMaterial.SetTexture("_MainTex", tex);
        RenderFeature.ShouldExecute = false;
        doLateTextureUpdate = false;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            Reset();
            return;
        }

        if (needsUpdate)
            CylinderProject();


        Debug.DrawLine(lowerleft, lowerleft_outer, Color.magenta);
        Debug.DrawLine(lowerright, lowerright_outer, Color.magenta);
        Debug.DrawLine(upperleft, upperleft_outer, Color.cyan);
        Debug.DrawLine(upperright, upperright_outer, Color.cyan);
    }

    private void LateUpdate() {
        if (doLateTextureUpdate)
            //countdown--;
        //if (countdown == 0)
            UpdateFromRenderTexture();
    }

    public void OnValidate() {
        //hasProjected = false;
    }

    private void OnDestroy() {
    }
}
