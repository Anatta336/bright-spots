using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BrightSpotsFeature : ScriptableRendererFeature
{
  [System.Serializable]
  public class BrightSpotsSettings
  {
    // we're free to put whatever we want here, public fields will be exposed in the inspector
    public bool IsEnabled = true;
    public RenderPassEvent WhenToInsert = RenderPassEvent.BeforeRenderingSkybox;
    public ComputeShader BrightsCompute;
    public Material FlareMaterial;

    [Range(0f, 50f)]
    public float RotationSpeed = 2f;

    [Range(0f, 1f)]
    public float RotationRange = 0.25f;

    [Range(0f, 2f)]
    public float LuminanceThreshold = 0.9f;
  }

  // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
  public BrightSpotsSettings settings = new BrightSpotsSettings();

  BrightSpotsPass brightSpotsPass;

  public override void Create()
  {
    brightSpotsPass = new BrightSpotsPass(
      "Bright Spots",
      settings.WhenToInsert,
      settings.BrightsCompute,
      settings.FlareMaterial
    );
  }
  
  // called every frame once per camera
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
    if (!settings.IsEnabled)
    {
      return;
    }
    
    float angle = Mathf.PI * settings.RotationRange *
      Mathf.Sin(Time.time * settings.RotationSpeed);

    // Gather up any extra information our pass will need.
    brightSpotsPass.Setup(
      renderer.cameraColorTarget,
      settings.LuminanceThreshold,
      angle
    );

    // Ask the renderer to add our pass.
    renderer.EnqueuePass(brightSpotsPass);
  }
}
