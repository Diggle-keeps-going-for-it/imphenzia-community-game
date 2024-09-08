Shader "Lit/3dGridEditorSurface"
{
    Properties
    {
        _Color ("Color", color) = (1,0.5,0,1)
        _GridColor ("Grid Color", color) = (0.5,0.5,0.5,1)
        _FixedWidthGridLineWidth ("Fixed Width Grid Line Width", Range(0,20)) = 1
        _PhysicalGridLineWidth ("Physical Grid Line Width", Range(0,1)) = 0.01

        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        _Metallic ("Metallic", Range(0,1)) = 0.0

        _ShadingLitness ("Shading Lit-ness", Range(0,1)) = 0.5
        [Toggle(ENABLE_CROSS_HATCHING)] _CrossHatching ("Enable Cross Hatching", Int) = 0
        _CrossHatchColor ("Cross Hatch Color", color) = (0.5,0.5,0.5,1)
        _CrossHatchWidth ("Cross Hatch Width", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
        }
        LOD 100

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:myVertex
        #pragma target 3.0
        #pragma shader_feature ENABLE_CROSS_HATCHING
        
        #include "UnityCG.cginc"

        half _Glossiness;
        half _Metallic;
        float4 _Color;
        float4 _GridColor;
        float _FixedWidthGridLineWidth;
        float _PhysicalGridLineWidth;
        float _ShadingLitness;
        
        #ifdef ENABLE_CROSS_HATCHING
        float4 _CrossHatchColor;
        float _CrossHatchWidth;
        #endif

        struct Input
        {
            float2 local_uv;
        };

        void myVertex(inout appdata_full v, out Input data)
        {
            data.local_uv = v.texcoord;
        }

        float halfLineWidth()
        {
            return _PhysicalGridLineWidth * 0.5;
        }

        float calculateGridSpacenessOneDimension(float u, float du)
        {
            float uProximity = 0.5 - abs(u - 0.5);
            float uScreenProximity = uProximity / du;
            return saturate(uScreenProximity - _FixedWidthGridLineWidth);
        }

        float calculateDistanceConsciousGridSpacenessOneDimension(float u)
        {
            float uProximity = 0.5 - abs(u - 0.5);
            return step(_PhysicalGridLineWidth, uProximity);
        }

        float calculateGridLineness(float2 uv, float2 pixelWidth)
        {
            float fixedWidthLineness = 1 - min(calculateGridSpacenessOneDimension(uv.x, pixelWidth.x), calculateGridSpacenessOneDimension(uv.y, pixelWidth.y));
            float distanceConsciousLineness = 1 - min(
                calculateDistanceConsciousGridSpacenessOneDimension(uv.x),
                calculateDistanceConsciousGridSpacenessOneDimension(uv.y)
            );

            return max(fixedWidthLineness, distanceConsciousLineness);
        }

        #ifdef ENABLE_CROSS_HATCHING
        float calculateDiagonalCrossHatchingness(float u)
        {
            return step(frac(u * 2.f + _CrossHatchWidth/2), _CrossHatchWidth);
        }

        float calculateCrossHatchingness(float2 uv)
        {
            float positiveDiagonal = uv.x + uv.y;
            float negativeDiagonal = uv.x - uv.y;

            return 1.f - (
                  (1.f - calculateDiagonalCrossHatchingness(positiveDiagonal))
                * (1.f - calculateDiagonalCrossHatchingness(negativeDiagonal))
            );
        }
        #endif

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 pixelWidth = fwidth(IN.local_uv);
            float2 uv = frac(IN.local_uv);
            float gridLineness = calculateGridLineness(uv, pixelWidth);
            #ifdef ENABLE_CROSS_HATCHING
            float crossHatchingness = calculateCrossHatchingness(uv);
            float3 insideColor = lerp(_Color, _CrossHatchColor, crossHatchingness);
            #else
            float3 insideColor = _Color;
            #endif
            float3 color = lerp(insideColor, _GridColor, gridLineness);
            float3 zero = float3(0.f, 0.f, 0.f);
            o.Albedo = lerp(zero, color, _ShadingLitness);
            o.Emission = lerp(zero, color, 1.f - _ShadingLitness);

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
