Shader "Unlit/Outline_Mask"
{
    Properties 
    { 
        _ZTest ("ZTest", float) = 8
        //_Color ("Disable SRP Batching", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest [_ZTest] 
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            //float4 _Color;

            v2f Vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                return o;
            }

            float4 Frag (v2f i) : SV_Target
            {
                // return _Color;
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}