using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class BrightSpotsPass : ScriptableRenderPass
{
  // used to label this pass in Unity's Frame Debug utility
  string profilerTag;

  ComputeShader brightsCompute;
  Material flareMaterial;
  ComputeBuffer brightsBuffer;
  ComputeBuffer drawArgsBuffer;

  // These are needed in Execute but aren't directly available there, so store.
  // But be aware they're not guaranteed valid after the current pass is complete.
  RenderTargetIdentifier cameraColorIdent;
  RenderTargetIdentifier cameraDepthIdent;
  RenderTextureDescriptor cameraTextureDescriptor;
  float luminanceThreshold;
  bool drawDirect;
  bool drawIndirect;

  // store these so we only have to look them up once
  int findBrightsKernel,
    colourTexID,
    textureSizeXID,
    textureSizeYID,
    brightQuadsID,
    luminanceThresholdID;
  int groupSizeX, groupSizeY;

  RenderTexture tempRT;

  public BrightSpotsPass(string profilerTag,
    RenderPassEvent renderPassEvent, ComputeShader brightsCompute,
    Material flareMaterial)
  {
    Debug.Log("Construct BrightSpotsPass");

    this.profilerTag = profilerTag;
    this.renderPassEvent = renderPassEvent;
    this.brightsCompute = brightsCompute;
    this.flareMaterial = flareMaterial;

    findBrightsKernel = brightsCompute.FindKernel("FindBrights");
    colourTexID = Shader.PropertyToID("_colourTex");
    textureSizeXID = Shader.PropertyToID("_textureSizeX");
    textureSizeYID = Shader.PropertyToID("_textureSizeY");
    brightQuadsID = Shader.PropertyToID("_brightQuads");
    luminanceThresholdID = Shader.PropertyToID("_luminanceThreshold");

    brightsCompute.GetKernelThreadGroupSizes(findBrightsKernel,
      out uint sizeX, out uint sizeY, out var _);
    groupSizeX = (int)sizeX;
    groupSizeY = (int)sizeY;

    //TODO: buffer size here is arbitrary, decide on a maximum and enforce that
    brightsBuffer = new ComputeBuffer(100000, sizeof(float) * 16, ComputeBufferType.Append);

    // a buffer used as draw arguments for an indirect call must be created as the IndirectArguments type.
    drawArgsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

    drawArgsBuffer.SetData(new uint[] {
      6, // vertices per instance
      0, // instance count (will be set from brightsBuffer counter) 
      0, // byte offset of first vertex
      0, // byte offset of first instance
    });
  }

  public void Setup(
    RenderTargetIdentifier cameraColorIdent,
    RenderTargetIdentifier cameraDepthIdent,
    float luminanceThreshold,
    bool drawDirect,
    bool drawIndirect)
  {
    this.cameraColorIdent = cameraColorIdent;
    this.cameraDepthIdent = cameraDepthIdent;
    this.luminanceThreshold = luminanceThreshold;
    this.drawDirect = drawDirect;
    this.drawIndirect = drawIndirect;
  }

  // called each frame before Execute, use it to set up things the pass will need
  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
  {
    this.cameraTextureDescriptor = cameraTextureDescriptor;
    brightsBuffer.SetCounterValue(0);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
  {
    // fetch a command buffer to use
    CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
    cmd.Clear();

    // compute shader to find bright pixels anywhere on the image
    // when it finds a bright pixel, build the vertices of a two triangle mesh in brightsBuffer

    cmd.SetComputeTextureParam(brightsCompute, findBrightsKernel, colourTexID, cameraColorIdent);
    cmd.SetComputeIntParam(brightsCompute, textureSizeXID, cameraTextureDescriptor.width);
    cmd.SetComputeIntParam(brightsCompute, textureSizeYID, cameraTextureDescriptor.height);
    cmd.SetComputeBufferParam(brightsCompute, findBrightsKernel, brightQuadsID, brightsBuffer);
    cmd.SetComputeFloatParam(brightsCompute, luminanceThresholdID, luminanceThreshold);
    cmd.DispatchCompute(brightsCompute, findBrightsKernel,
      Mathf.CeilToInt(cameraTextureDescriptor.width / groupSizeX),
      Mathf.CeilToInt(cameraTextureDescriptor.height / groupSizeY),
      1
    );

    // put brightQuads count into instanceCount slot of drawArgsBuffer
    cmd.CopyCounterValue(brightsBuffer, drawArgsBuffer, sizeof(uint));
    
    // because we were reading from cameraColorIdent, need to set it back to being render target
    cmd.SetRenderTarget(cameraColorIdent);
    
    // draw the quads described by brightsCompute
    MaterialPropertyBlock properties = new MaterialPropertyBlock();
    properties.SetBuffer(brightQuadsID, brightsBuffer);
    cmd.DrawProceduralIndirect(Matrix4x4.identity, flareMaterial, 0, MeshTopology.Triangles,
      drawArgsBuffer, 0, properties);

    // don't forget to tell ScriptableRenderContext to actually execute the commands
    context.ExecuteCommandBuffer(cmd);

    // tidy up after ourselves
    cmd.Clear();
    CommandBufferPool.Release(cmd);
  }

  // called after Execute, use it to clean up anything allocated in Configure
  public override void FrameCleanup(CommandBuffer cmd) {}
}
