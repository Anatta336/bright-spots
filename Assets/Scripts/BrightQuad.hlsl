struct BrightQuad // 16 bytes
{
  float2 v0;
  float2 v1;
  float2 v2;
  float2 v3;
  float2 v4;
  float2 v5;
  float3 colour;
  float padding; // pad out to a multiple of float4
};