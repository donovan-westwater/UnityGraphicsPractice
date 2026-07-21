Shader "Hidden/DiamensionJumpEffect"
{
    Properties
    {
        _DstATex ("Texture", 2D) = "white" {}
        _DstBTex("Texture", 2D) = "white" {}
        _JumpTime("Animation Time",float) = 0.0
    }
    SubShader
    {
        // No culling or depth
        //Cull Off ZWrite Off ZTest Always

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

            sampler2D _DstATex;
            sampler2D _DstBTex;
            sampler2D _CameraDepthTexture;
            float _JumpTime;

            fixed4 frag(v2f i) : SV_Target
            {
                float zRaw = tex2D(_CameraDepthTexture,i.uv);
                float eyeDepth = LinearEyeDepth(zRaw);
                float4 colA = tex2D(_DstATex, i.uv);
                float4 colB = tex2D(_DstBTex, i.uv);
                float4 col = colA;
                float radius = 10.;
                if (radius*abs(_JumpTime / 5.0) < eyeDepth) col = float4(zRaw,0,0,0);
                //col = float4(zRaw, 0, 0, 0);
                return col;
            }
            ENDCG
        }
    }
}
