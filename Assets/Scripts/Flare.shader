Shader "Custom/Flare"
{
  Properties
  {
    _flareSprite ("Flare Image", 2D) = "" {}
    _opacity ("Opacity", Range (0.001, 1.0)) = 0.02
    _minSize ("Minimum flare quad size", Range (0.001,0.60)) = 0.05
    _maxSize ("Maximum flare quad size", Range (0.001,0.60)) = 0.12
    _magnitudeForMax ("Magnitude of luminence for max quad size", Range (0.5, 10.0)) = 1.50
  }
  SubShader
  {
    Pass
    {
      name "FlareDraw"

      // don't cull any faces
      Cull Off

      // ignore depth test
      ZTest Always
      ZWrite Off

      // additive blend
      Blend One One
      BlendOp Add

      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 5.0
        
      // #include "UnityCG.cginc"
      #include "BrightQuad.hlsl"

      #define PI 3.1415926535

      struct v2f
      {
        float4 position : SV_POSITION;
        float3 colour : TEXCOORD0;
        float2 uv : TEXCOORD1;
      };

      // although this was built as an AppendBuffer, we can access it like normal
      StructuredBuffer<BrightQuad> _brightQuads;

      // DX9 style texture sampling
      // sampler2D _flareSprite;

      // DX11 style texture sampling
      Texture2D<float> _flareSprite;
      SamplerState sampler_flareSprite;

      float _angle;
      float _widthRatio;
      float _opacity;
      float _minSize;
      float _maxSize;
      float _magnitudeForMax;

      float2 PositionFromMiddle(BrightQuad quad, int vertexID)
      {
        float rotation = _angle;
        if (vertexID == 0)
          rotation += 0.5 * PI;
        else if (vertexID == 1 || vertexID == 4)
          rotation += 0.0;
        else if (vertexID == 2 || vertexID == 3)
          rotation += 1.0 * PI;
        else if (vertexID == 5)
          rotation += 1.5 * PI;

        float size = lerp(_minSize, _maxSize, saturate(quad.magnitude / _magnitudeForMax));

        float s = sin(rotation);
        float c = cos(rotation);
        float2x2 rMatrix = float2x2(c, -s, s, c);
        rMatrix *= 0.5;
        rMatrix += 0.5;
        rMatrix = rMatrix * 2 - 1;
        float2 offset = mul(size.xx, rMatrix);
        offset.x /= _widthRatio;

        return quad.middle + offset;
      }

      float2 UvFromQuad(int vertexID)
      {
        if (vertexID == 0) {
          return float2(0.0, 1.0);
        }
        else if (vertexID == 1) {
          return float2(1.0, 1.0);
        }
        else if (vertexID == 2) {
          return float2(0.0, 0.0);
        }
        else if (vertexID == 3) {
          return float2(0.0, 0.0);
        }
        else if (vertexID == 4) {
          return float2(1.0, 1.0);
        }
        else {
          return float2(1.0, 0.0);
        }
      }

      // use system-value semantics to know where on the _brightQuads buffer we should read from
      v2f vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
      {
        BrightQuad quad = _brightQuads[instanceID];

        /*
          v0 ──── v1,v4
          │      ╱ │
          │     ╱  │
          │    ╱   │
          │   ╱    │
          │  ╱     │
          │ ╱      │
        v2,v3 ───  v5
        */

        float2 pos = PositionFromMiddle(quad, vertexID);
        float2 uv = UvFromQuad(vertexID);

        v2f o;

        // reminder: SV_POSITION coming out of vertex shader is in clip space, and will be divided through by .w
        // (so set .w to 1.0 to make it stay still)
        // we're not doing depth testing, so just make sure .z is within clip space
        o.position = float4(pos.x, pos.y, 0.5, 1.0);
        o.uv = uv;
        o.colour = quad.colour;
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        float3 tex = _flareSprite.Sample(sampler_flareSprite, i.uv).xxx * i.colour * _opacity;
        return float4(tex, 1.0);
      }
      ENDHLSL
    }
  }
}