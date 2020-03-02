struct BrightPoint // 8 * 4 = 32 bytes
{
  int2 middle;     // midpoint in texels
  float magnitude; // (luminance - luminanceThreshold) / luminanceThreshold
  float3 colour;   // colour of pixel that spawned it
  float luminance; // 
  float padding;   // pad out to a multiple of float4
};