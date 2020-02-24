using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MyBlitFeature : ScriptableRendererFeature
{
  [System.Serializable]
  public class MyFeatureSettings
  {
    // we're free to put whatever we want here, public fields will be exposed in the inspector
    public bool IsEnabled = true;
    public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
    public Material MaterialToBlit;
  }

  // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
  public MyFeatureSettings settings = new MyFeatureSettings();

  RenderTargetHandle renderTextureHandle;
  MyBlitRenderPass myRenderPass;

  public override void Create()
  {
    myRenderPass = new MyBlitRenderPass(
      "My custom pass",
      settings.WhenToInsert,
      settings.MaterialToBlit
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
    
    // Gather up and pass any extra information our pass will need.
    // In this case we're getting the camera's color buffer target
    var cameraColorTargetIdent = renderer.cameraColorTarget;
    myRenderPass.Setup(cameraColorTargetIdent);

    // Ask the renderer to add our pass.
    // Could queue up multiple passes and/or pick passes to use
    renderer.EnqueuePass(myRenderPass);
  }
}
