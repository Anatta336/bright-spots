using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class MeshPass : ScriptableRenderPass
{
  // used to label this pass in Unity's Frame Debug utility
  string profilerTag;

  Material material;
  Mesh mesh;
  
  RenderTargetIdentifier cameraColorIdent;
  RenderTargetIdentifier cameraDepthIdent;
  RenderTextureDescriptor cameraTextureDescriptor;

  public MeshPass(string profilerTag,
    RenderPassEvent renderPassEvent,
    Mesh mesh, Material material)
  {
    Debug.Log("Construct BrightSpotsPass");

    this.profilerTag = profilerTag;
    this.renderPassEvent = renderPassEvent;
    this.material = material;
    this.mesh = mesh;
  }

  public void Setup(
    RenderTargetIdentifier cameraColorIdent,
    RenderTargetIdentifier cameraDepthIdent
  )
  {
    this.cameraColorIdent = cameraColorIdent;
    this.cameraDepthIdent = cameraDepthIdent;
  }

  // called each frame before Execute, use it to set up things the pass will need
  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
  {
    this.cameraTextureDescriptor = cameraTextureDescriptor;
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
  {
    // fetch a command buffer to use
    CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
    cmd.Clear();

    // draw a mesh
    // MaterialPropertyBlock properties = new MaterialPropertyBlock();
    // properties.SetBuffer(brightQuadsID, brightsBuffer);
    cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);

    // don't forget to tell ScriptableRenderContext to actually execute the commands
    context.ExecuteCommandBuffer(cmd);

    // tidy up after ourselves
    cmd.Clear();
    CommandBufferPool.Release(cmd);
  }

  // called after Execute, use it to clean up anything allocated in Configure
  public override void FrameCleanup(CommandBuffer cmd)
  {
    
  }
}
