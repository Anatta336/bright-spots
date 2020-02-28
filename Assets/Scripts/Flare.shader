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
        
      #include "BrightPoint.hlsl"

      #define PI 3.1415926535

      struct v2f
      {
        float4 position : SV_POSITION;
        float3 colour : TEXCOORD0;
        float2 uv : TEXCOORD1;
      };

      // although this was built as an AppendBuffer, we can still bind and access it like normal
      StructuredBuffer<BrightPoint> _brightPoints;

      // DX11 style texture sampling
      Texture2D<float> _flareSprite;
      SamplerState sampler_flareSprite;

      float _screenSizeX;
      float _screenSizeY;
      float _angle;
      float _widthRatio;
      float _opacity;
      float _minSize;
      float _maxSize;
      float _magnitudeForMax;

      static float2 uvByVertexID[6] =
      {
        float2(0.0, 1.0),
        float2(1.0, 1.0),
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        float2(1.0, 1.0),
        float2(1.0, 0.0)
      };
      static float angleByVertexID[6] =
      {
        0.5 * PI,
        0.0 * PI,
        1.0 * PI,
        1.0 * PI,
        0.0 * PI,
        1.5 * PI
      };

      float2 PositionFromBrightPoint(BrightPoint brightPoint, int vertexID)
      {
        float rotation = angleByVertexID[vertexID] + _angle;
        float size = lerp(_minSize, _maxSize, saturate(brightPoint.magnitude / _magnitudeForMax));

        // create offset by rotating size
        float s = sin(rotation);
        float c = cos(rotation);
        float2x2 rMatrix = float2x2(c, -s, s, c);
        rMatrix *= 0.5;
        rMatrix += 0.5;
        rMatrix = rMatrix * 2 - 1;
        float2 offset = mul(size.xx, rMatrix);

        // correct for aspect ratio so quad is square when displayed
        offset.x /= _widthRatio;

        float2 middle = float2(
          (float(brightPoint.middle.x) / _screenSizeX) * 2.0 - 1.0,
          (1.0 - (float(brightPoint.middle.y) / _screenSizeY)) * 2.0 - 1.0
        );

        return middle + offset;
      }

      // use system-value semantics to know where on the _brightPoints buffer we should read from
      v2f vert (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
      {
        v2f o;

        BrightPoint brightPoint = _brightPoints[instanceID];
        float2 pos = PositionFromBrightPoint(brightPoint, vertexID);

        // reminder: SV_POSITION coming out of vertex shader is in clip space, and will be divided through by .w
        // (so set .w to 1.0 to make it stay still)
        // we're not doing depth testing, so just make sure .z is within clip space
        o.position = float4(pos.x, pos.y, 0.5, 1.0);
        o.uv = uvByVertexID[vertexID];
        o.colour = brightPoint.colour;
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