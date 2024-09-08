Shader "Unlit/TriangleOnModuleFace"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.5,0.5,1)
        _OffsetFactor ("Camera Z Offset Factor", Range(-1,1)) = -1
        _OffsetUnitScale ("Camera Z Offset Unit", Range(-1,1)) = -1
        _BorderWidth ("Border Width", Float) = 0.01
        _NormalOffset ("Normal Offset", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Offset [_OffsetFactor], [_OffsetUnitScale]
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 borderDirection : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _BorderWidth;
            float _NormalOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex + v.normal * _NormalOffset + v.borderDirection * _BorderWidth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
