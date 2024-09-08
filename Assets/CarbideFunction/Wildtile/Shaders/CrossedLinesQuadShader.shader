Shader "Unlit/CrossedLinesQuadShader"
{
    Properties
    {
        _InnerColor ("Inner Color", color) = (1,0.5,0,1)
        _BorderColor ("Border Color", color) = (1,0.5,0,1)
        _LineWidth ("Line width", Range(0,10)) = 2
        _BorderProportion ("Border Proportion", Range(0,1)) = 0.1
        _MinimumScaleFactor ("Minimum scale factor", Range(0,100)) = 1
        [Toggle(DEBUG_MODE)] _DebugMode ("Debug Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature DEBUG_MODE

            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float3 pseudoLineCoordinates : TEXCOORD0;
                float3 lineDirection : TEXCOORD1;
            };
             
            struct v2f{
                float4 worldPos : SV_POSITION;
                float2 endCapUv : TEXCOORD0;
        #if DEBUG_MODE
                float3 color : COLOR;
        #endif
            };

            float4 _BorderColor;
            float4 _InnerColor;
            float _LineWidth;
            float _BorderProportion;
            float _MinimumScaleFactor;

            float4 scaleToScreen(float4 offset)
            {
                offset.x /= _ScreenParams.x;
                offset.y /= _ScreenParams.y;
                return offset;
            }

            float fromUvToNegOneToOne(float uv)
            {
                return uv * 2.f - 1.f;
            }

            v2f vert (appdata v)
            {
                // shift outwards by screen-fixed distance 

                float4 clipVert = UnityObjectToClipPos(v.vertex);

                float4 clipVertFurtherAlongLine = UnityObjectToClipPos(v.vertex + v.lineDirection);

                float offsetVertIsBehindCamera = clipVertFurtherAlongLine.w < 0.f ? 1.f : 0.f;
                float vertIsBehindCamera = clipVert.w < 0.f ? 1.f : 0.f;

                // mathematical == operation
                float shouldUsePositiveLineDirection = offsetVertIsBehindCamera * vertIsBehindCamera + (1.f - offsetVertIsBehindCamera) * (1.f - vertIsBehindCamera);

                float2 direction = clipVertFurtherAlongLine.xy / clipVertFurtherAlongLine.w - clipVert.xy / clipVert.w;
                direction *= lerp(-1.f, 1.f, shouldUsePositiveLineDirection);
                direction *= _ScreenParams;
                direction = normalize(direction);
                float2 normal = float2(-direction.y, direction.x);

                float foreScalar = 0.5;
                float pointScalar = max(clipVert.w, _MinimumScaleFactor);
                // float pointScalar = clipVert.w;
                float4 foreOffset = scaleToScreen(float4(direction * pointScalar * _LineWidth / 2.f, 0.f, 0.f)) * foreScalar;
                float4 sideOffset = scaleToScreen(float4(normal * pointScalar * _LineWidth / 2.f, 0.f, 0.f));

                v2f o;
        #if DEBUG_MODE
                o.color = v.pseudoLineCoordinates;
                o.color = float3(v.pseudoLineCoordinates.yz, shouldUsePositiveLineDirection);
        #endif
                o.worldPos = clipVert + foreOffset * fromUvToNegOneToOne(v.pseudoLineCoordinates.y) + sideOffset * fromUvToNegOneToOne(v.pseudoLineCoordinates.z);
                float progressThroughLine = (v.pseudoLineCoordinates.x + v.pseudoLineCoordinates.y) - 1.f;
                o.endCapUv = float2(progressThroughLine, fromUvToNegOneToOne(v.pseudoLineCoordinates.z));

                return o;
            }

            float calculateDistanceFromLine(float2 endCapCoordinates)
            {
                return endCapCoordinates.x * endCapCoordinates.x + endCapCoordinates.y * endCapCoordinates.y;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float distanceFromLine = calculateDistanceFromLine(i.endCapUv);
                clip(1 - distanceFromLine);
        #if !DEBUG_MODE
                float borderThreshold = 1.f - _BorderProportion;
                borderThreshold *= borderThreshold;
                return lerp(_InnerColor, _BorderColor, step(borderThreshold, distanceFromLine));
        #else
                fixed4 endCapUv = fixed4((i.endCapUv + 1.0) * 0.5, 0, 1);
                fixed4 triColor = float4(i.color, 1);
                return lerp(endCapUv, triColor, 0.9f);
        #endif
            }
            ENDCG
        }
    }
}
