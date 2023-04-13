Shader "Hidden/ComputeNoiseTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex("Texture",3D) = "white"{}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler3D _NoiseTex;

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = float3(abs(_SinTime.x),i.uv.y,i.uv.x); //float3(i.uv.x,i.uv.y,abs(_SinTime.x));
                fixed4 col = tex3D(_NoiseTex, p);
                // just invert the colors
                col.rgb = col.rgb;
                return col;
            }
            ENDCG
        }
    }
}
