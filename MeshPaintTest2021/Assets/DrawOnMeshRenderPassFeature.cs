using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System;

public class DrawOnMeshRenderPassFeature : ScriptableRendererFeature
{
    // Tattooenator will set these
    [HideInInspector]
    public Material DrawMaterial { get; set; }
    [HideInInspector]
    public Mesh DrawMesh { get; set; }
    [HideInInspector]
    public RenderTexture RenderTex { get; set; }
    [HideInInspector]
    public Matrix4x4 Transform { get; set; }
    [HideInInspector]

    public bool ShouldExecute {
    get {
            if (m_ScriptablePass is null)
                return false;
            else
                return m_ScriptablePass.ShouldExecute;
        }
     set {
            if (m_ScriptablePass is null)
                return;
            m_ScriptablePass.ShouldExecute = value;
        }
    }
    private Action<Texture2D> _handler;


    class CustomRenderPass : ScriptableRenderPass
    {
        private Material _material;
        private Mesh _mesh;
        private RenderTexture _renderTex;
        private Matrix4x4 _transform;
        private bool _shouldExecute = false;

        public bool CanExecute { get; private set; }
        public event Action<Texture2D> HasExecuted; // notify that rendertex has been drawn
        public bool ShouldExecute { get { return _shouldExecute; } set { _shouldExecute = value; } }

        public CustomRenderPass(Material mat, Mesh mesh, RenderTexture rendertex, Matrix4x4 xform) {
            _material = mat;
            _mesh = mesh;
            _renderTex = rendertex;
            _transform = xform;
            Checks();
        }

        private void Checks()
        {
            CanExecute = true;
            if (_mesh is null)
            {
                Debug.LogError("Mesh set in DrawOnMeshRenderFeature is null");
                CanExecute = false;
            }
            if (_material is null)
            {
                Debug.LogError("Draw material in DrawOnMeshRenderFeature is null");
                CanExecute = false;
            }
            if (_renderTex is null)
            {
                Debug.LogError("Render texture set in DrawOnMeshRenderFeature is null");
                CanExecute = false;
            }
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var rtdesc = renderingData.cameraData.cameraTargetDescriptor;
        }

        private static Texture2D TextureFromRenderTexture(RenderTexture renderTexture, string name) {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height,
                GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            texture.name = name;

            var current = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            texture.Apply();
            RenderTexture.active = current;

            return texture;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!ShouldExecute)
                return;

            Camera camera = renderingData.cameraData.camera;
            if (camera.cameraType != CameraType.Game)
                return;
            _transform = Matrix4x4.identity;

            CommandBuffer cmd = CommandBufferPool.Get(name: "DrawPass");
            cmd.SetRenderTarget(_renderTex);
            cmd.DrawMesh(_mesh, _transform, _material);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            //context.Submit();
            Texture2D tex = TextureFromRenderTexture(_renderTex, "tiledTex");
            HasExecuted?.Invoke(tex);
            //HasExecuted?.Invoke(null);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void RemoveHasExecutedSubscribers()
        {
            var subscribers = HasExecuted?.GetInvocationList();
            if (subscribers != null) {
                for (int i = 0; i < subscribers.Length; i++) {
                    HasExecuted -= subscribers[i] as Action<Texture2D>;
                }
            }
        }
    }

    private CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        Debug.Log("Creating DrawOnMeshRenderPassFeature");
        m_ScriptablePass = new CustomRenderPass(DrawMaterial, DrawMesh, RenderTex, Transform);
        m_ScriptablePass.HasExecuted += _handler;

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ScriptablePass is null)
            return;
        if (m_ScriptablePass.CanExecute)
        {
            //Debug.Log("Enqueuing Custom RenderPass");   // guess this happens once per frame
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    public void SubscribeToRenderPassResultTexture(Action<Texture2D> handler) {
        if (!(m_ScriptablePass is null))
        {
            m_ScriptablePass.RemoveHasExecutedSubscribers();
            m_ScriptablePass.HasExecuted += handler;
        }
        _handler = handler;
    }

    protected override void Dispose(bool disposing) {
        m_ScriptablePass.HasExecuted -= _handler;
        //m_ScriptablePass.Dispose();
        base.Dispose(disposing);
    }
}


