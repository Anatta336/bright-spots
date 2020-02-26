Shader "Custom/Flare"
{
  Properties
  {
    _flareSprite ("Flare Image", 2D) = "" {}
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
      // #pragma enable_d3d11_debug_symbols
        
      // #include "UnityCG.cginc"
      #include "BrightQuad.hlsl"

      struct v2f
      {
        float4 position : SV_POSITION;
        float3 colour : TEXCOORD0;
        float2 uv : TEXCOORD1;
      };

      // although this was built as an AppendBuffer, we can access it like normal
      StructuredBuffer<BrightQuad> _brightQuads;

      // sampler2D _flareSprite; // DX9 style

      // DX11 style
      Texture2D<float> _flareSprite;
      SamplerState sampler_flareSprite;

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

        //TODO: make this less ugly
        float2 pos;
        float2 uv;
        if (vertexID == 0) {
          pos = quad.v0;
          uv = float2(0.0, 1.0);
        }
        else if (vertexID == 1) {
          pos = quad.v1;
          uv = float2(1.0, 1.0);
        }
        else if (vertexID == 2) {
          pos = quad.v2;
          uv = float2(0.0, 0.0);
        }
        else if (vertexID == 3) {
          pos = quad.v3;
          uv = float2(0.0, 0.0);
        }
        else if (vertexID == 4) {
          pos = quad.v4;
          uv = float2(1.0, 1.0);
        }
        else if (vertexID == 5) {
          pos = quad.v5;
          uv = float2(1.0, 0.0);
        }
        else {
          // default, shouldn't ever get used
          pos = float2(0.5, 0.5);
          uv = float2(0.5, 0.5);
        }   

        v2f o;

        // reminder: SV_POSITION coming out of vertex shader is in clip space, and will be divided through by .w
        // (so if you want it to stay still set .w to 1.0)
        // we're not doing depth testing, so just make sure .z is within clip space
        o.position = float4(pos.x, pos.y, 0.5, 1.0);
        o.uv = uv;
        o.colour = quad.colour;
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        // TODO: rather than just a rect, sample a texture
        // float3 tex = _flareSprite.Sample(sampler_flareSprite, i.uv).xxx;
        // return float4(tex, 1.0);
        return float4(i.colour, 1.0);
      }
      ENDHLSL
    }
  }
}