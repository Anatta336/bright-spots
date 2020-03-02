using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class BrightSpotsPass : ScriptableRenderPass
{
  // used to label this pass in Unity's Frame Debug utility
  string profilerTag;

  ComputeShader brightsCompute;
  Material flareMaterial;
  ComputeBuffer brightPoints;
  ComputeBuffer drawArgsBuffer;

  // These are needed in Execute but aren't directly available there, so store.
  // But be aware they're not guaranteed valid after the current pass is complete.
  RenderTargetIdentifier cameraColorIdent;
  RenderTextureDescriptor cameraTextureDescriptor;
  bool isUsingMSAA = false;
  
  float luminanceThreshold;
  float angle;

  // store these so we only have to look them up once
  int findBrightsKernel,
    sourceTextureID,
    brightQuadsID,
    luminanceThresholdID,
    screenSizeXID,
    screenSizeYID,
    angleID,
    widthRatioID,
    resolvedCameraColourID;
  int groupSizeX, groupSizeY;
  readonly int regionPerThread = 8;

  public BrightSpotsPass(string profilerTag,
    RenderPassEvent renderPassEvent, ComputeShader brightsCompute,
    Material flareMaterial)
  {
    this.profilerTag = profilerTag;
    this.renderPassEvent = renderPassEvent;
    this.brightsCompute = brightsCompute;
    this.flareMaterial = flareMaterial;

    findBrightsKernel = brightsCompute.FindKernel("FindBrights");
    sourceTextureID = Shader.PropertyToID("_sourceTexture");
    brightQuadsID = Shader.PropertyToID("_brightPoints");
    luminanceThresholdID = Shader.PropertyToID("_luminanceThreshold");
    screenSizeXID = Shader.PropertyToID("_screenSizeX");
    screenSizeYID = Shader.PropertyToID("_screenSizeY");
    angleID = Shader.PropertyToID("_angle");
    widthRatioID = Shader.PropertyToID("_widthRatio");
    resolvedCameraColourID = Shader.PropertyToID("_resolvedCameraColour");

    // fetch the numthreads values of the kernel (assume z is 1 so ignore it)
    brightsCompute.GetKernelThreadGroupSizes(findBrightsKernel,
      out uint sizeX, out uint sizeY, out var _);
    groupSizeX = (int)sizeX;
    groupSizeY = (int)sizeY;

    // I can't find a good place to Dispose() these ComputeBuffers. When in editor mode
    // this pass can be recreated many times, leading to them being garbage collected and
    // triggering a warning.
    // They could be recreated every frame but I suspect that'll be slower with the only
    // apparent gain being to avoid a warning from Unity.

    // buffer size here is arbitrary, if hitting the max is likely consider picking which
    // bright points are culled by something not totally arbitrary.
    brightPoints = new ComputeBuffer(1000, sizeof(float) * 8, ComputeBufferType.Append);

    // a buffer used as draw arguments for an indirect call must be created as the IndirectArguments type.
    drawArgsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

    drawArgsBuffer.SetData(new uint[] {
      6, // vertices per instance
      0, // instance count (will be set from brightPoints counter) 
      0, // byte offset of first vertex
      0, // byte offset of first instance
    });
  }

  public void Setup(
    RenderTargetIdentifier cameraColorIdent,
    float luminanceThreshold,
    float angle)
  {
    this.cameraColorIdent = cameraColorIdent;
    this.luminanceThreshold = luminanceThreshold;
    this.angle = angle;
  }

  // called each frame before Execute, use it to set up things the pass will need
  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
  {
    // reset the bright quads counter, effectively clearing the append buffer
    brightPoints.SetCounterValue(0);

    this.cameraTextureDescriptor = cameraTextureDescriptor;
    
    isUsingMSAA = cameraTextureDescriptor.msaaSamples > 1;
    if (isUsingMSAA)
    {
      // RenderTextureDescriptor is a struct, so changing resolvedDescriptor.msaa doesn't change cameraTextureDescriptor
      RenderTextureDescriptor resolvedDescriptor = cameraTextureDescriptor;
      resolvedDescriptor.msaaSamples = 1;
      cmd.GetTemporaryRT(resolvedCameraColourID, resolvedDescriptor);
    }
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
  {
    // fetch a command buffer to use
    CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
    cmd.Clear();

    // if using MSAA we should resolve before looking for bright spots
    if (isUsingMSAA)
    {
      RenderTargetIdentifier resolvedTargetIdent = new RenderTargetIdentifier(resolvedCameraColourID);
      cmd.Blit(cameraColorIdent, resolvedTargetIdent);
      cmd.SetComputeTextureParam(brightsCompute, findBrightsKernel, sourceTextureID, resolvedTargetIdent);
    }
    else
    {
      // if not using MSAA we can directly read from the camera's render target
      cmd.SetComputeTextureParam(brightsCompute, findBrightsKernel, sourceTextureID, cameraColorIdent);
    }

    // Compute shader to find brightest pixels, limited to one per group thread region.
    // When it finds a bright pixel record its details in the brightPoints buffer
    cmd.SetComputeBufferParam(brightsCompute, findBrightsKernel, brightQuadsID, brightPoints);
    cmd.SetComputeFloatParam(brightsCompute, luminanceThresholdID, luminanceThreshold);

    // calculation of thread groups ensures the whole screen is covered
    cmd.DispatchCompute(brightsCompute, findBrightsKernel,
      Mathf.CeilToInt(
        Mathf.Ceil((float)(cameraTextureDescriptor.width) / regionPerThread) / groupSizeX),
      Mathf.CeilToInt(
        Mathf.Ceil((float)(cameraTextureDescriptor.height) / regionPerThread) / groupSizeY),
      1
    );

    // put brightPoints count into instanceCount slot of drawArgsBuffer
    cmd.CopyCounterValue(brightPoints, drawArgsBuffer, sizeof(uint));
    
    // earlier resolve Blit may have changed render target, so set it back
    cmd.SetRenderTarget(cameraColorIdent);
    
    // draw the quads described by brightsCompute
    MaterialPropertyBlock properties = new MaterialPropertyBlock();
    properties.SetBuffer(brightQuadsID, brightPoints);
    properties.SetFloat(angleID, angle);
    properties.SetFloat(widthRatioID, renderingData.cameraData.camera.aspect);
    properties.SetFloat(screenSizeXID, cameraTextureDescriptor.width);
    properties.SetFloat(screenSizeYID, cameraTextureDescriptor.height);

    // it would make sense to use MeshTopology.Quads as we're drawing quads, but Unity docs say:
    // "quad topology is emulated on many platforms, so it's more efficient to use a triangular mesh."
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
