using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BrightSpotsFeature : ScriptableRendererFeature
{
  [System.Serializable]
  public class MyFeatureSettings
  {
    // we're free to put whatever we want here, public fields will be exposed in the inspector
    public bool IsEnabled = true;
    public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
    public ComputeShader BrightsCompute;
    public Material FlareMaterial;
    public RenderTexture RenderTexture;

    [Range(0f, 2f)]
    public float LuminanceThreshold = 0.9f;
  }

  // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
  public MyFeatureSettings settings = new MyFeatureSettings();

  RenderTargetHandle renderTextureHandle;
  BrightSpotsPass myRenderPass;

  void OnEnable()
  {
    Debug.Log("ENABLE BrightSpotsFeature");
  }

  public override void Create()
  {
    Debug.Log("Create() BrightSpotsFeature");

    myRenderPass = new BrightSpotsPass(
      "Bright Spots",
      settings.WhenToInsert,
      settings.BrightsCompute,
      settings.FlareMaterial,
      settings.RenderTexture
    );
  }
  
  // called every frame once per camera
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
    if (!settings.IsEnabled)
    {
      // we can do nothing this frame if we want
      return;
    }
    
    // Gather up any extra information our pass will need.
    myRenderPass.Setup(
      renderer.cameraColorTarget,
      renderer.cameraDepth,
      settings.LuminanceThreshold);

    // Ask the renderer to add our pass.
    // Could queue up multiple passes and/or pick passes to use
    renderer.EnqueuePass(myRenderPass);
  }
}
