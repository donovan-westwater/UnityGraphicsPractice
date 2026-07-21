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
            sampler2D _LastCameraDepthTexture;
            float _JumpTime;
            float4x4 _MatrixHClipToWorld;
            inline float3 TransformUVToWorldPos(float2 uv)
            {
                float depth = tex2D(_CameraDepthTexture, uv).r;
                float lastDepth = tex2D(_LastCameraDepthTexture, uv).r;
                //depth = min(depth, lastDepth);
#ifndef SHADER_API_GLCORE
                float4 positionCS = float4(uv * 2 - 1, depth, 1) * LinearEyeDepth(depth);
#else
                float4 positionCS = float4(uv * 2 - 1, depth * 2 - 1, 1) * LinearEyeDepth(depth);
#endif
                return mul(_MatrixHClipToWorld, positionCS).xyz;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float zRaw = tex2D(_CameraDepthTexture,i.uv);
                float3 worldPos = TransformUVToWorldPos(i.uv);
                float camDist = length(_WorldSpaceCameraPos - worldPos);
                float eyeDepth = LinearEyeDepth(zRaw);
                float4 colA = tex2D(_DstATex, i.uv);
                float4 colB = tex2D(_DstBTex, i.uv);
                float4 col = colA;
                float radius = _ProjectionParams.z;
                float tRaw = abs(_JumpTime / 7.0);
                float tVal = pow(tRaw, 4.);
                camDist = clamp(camDist, 0, radius-.01);
                if (radius*tVal <= camDist) col = colB;

                float gVal = dot(float3(.3, .59, .11), col);
                float4 greyCol = float4(gVal, gVal, gVal, 1.);
                col = lerp(greyCol, col, tVal);
                return col;
            }
            ENDCG
        }
    }
}
