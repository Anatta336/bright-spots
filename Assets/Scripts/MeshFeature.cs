using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MeshFeature : ScriptableRendererFeature
{
  [System.Serializable]
  public class MeshFeatureSettings
  {
    // we're free to put whatever we want here, public fields will be exposed in the inspector
    public bool IsEnabled = true;
    public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
    public Mesh Mesh;
    public Material Material;
  }

  // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
  public MeshFeatureSettings settings = new MeshFeatureSettings();

  RenderTargetHandle renderTextureHandle;
  MeshPass meshPass;

  public override void Create()
  {
    meshPass = new MeshPass(
      "Mesh Feature",
      settings.WhenToInsert,
      settings.Mesh,
      settings.Material
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
    meshPass.Setup(
      renderer.cameraColorTarget,
      renderer.cameraDepth
    );

    // Ask the renderer to add our pass.
    // Could queue up multiple passes and/or pick passes to use
    renderer.EnqueuePass(meshPass);
  }
}
