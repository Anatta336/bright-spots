struct BrightQuad // 8 bytes
{
  float2 middle;    // midpoint in clipspace, only .xy
  float magnitude;  // (luminance - luminanceThreshold) / luminanceThreshold
  float3 colour;    // colour of pixel that spawned it
  float2 padding;   // pad out to a multiple of float4. holds pixel's luminance value just for fun
};