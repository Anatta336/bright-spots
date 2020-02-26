Shader "Custom/Flare"
{
  Properties {}
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
        // float3 color : TEXCOORD0;
        // float2 uv : TEXCOORD1;
      };

      // although this was built as an AppendBuffer, we can access it like normal
      StructuredBuffer<BrightQuad> _brightQuads;

      // use system-value semantics to know where on the _brightQuads buffer we should read from
      v2f vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
      {
        BrightQuad quad = _brightQuads[instanceID];

        //TODO: make this less ugly
        float2 pos;
        if (vertexID == 0) {
          pos = quad.v0;
        }
        else if (vertexID == 1) {
          pos = quad.v1;
        }
        else if (vertexID == 2) {
          pos = quad.v2;
        }
        else if (vertexID == 3) {
          pos = quad.v3;
        }
        else if (vertexID == 4) {
          pos = quad.v4;
        }
        else if (vertexID == 5) {
          pos = quad.v5;
        }
        else {
          pos = float2(0.5, 0.5);
        }   

        v2f o;

        //TODO: could test by having it force positions?

        // o.position = float4(pos.x, pos.y, 0.5, 1.0);

        // blindly setting z and w to be (around) what working vertices have
        o.position = float4(pos.x, pos.y, 0.23456, 1.54321);
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        // TODO: rather than just a rect, sample a texture
        return float4(1.0, 0.5, 0.2, 1.0);
      }
      ENDHLSL
    }
  }
}