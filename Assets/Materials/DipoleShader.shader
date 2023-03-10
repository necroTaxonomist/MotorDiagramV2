Shader "Unlit/DipoleShader"
{
    Properties
    {
        [PerRendererData]
        _PosColor ("Positive Color", Color) = (1,0,0,1)

        [PerRendererData]
        _NegColor ("Negative Color", Color) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _PosColor;
            fixed4 _NegColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return lerp( _NegColor, _PosColor, i.uv.y );
            }
            ENDCG
        }
    }
}
