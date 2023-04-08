Shader "Hidden/TestMarch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex("Texture", 3D) = "white" {}
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
            float3 ambientLight = float3(1,1,1);

            float absorptionCoef = 0.05;
            float scatteringCoef = 0.35;

            
            float density(float3 samplePosition) {
                float _Threshold = .35;
                float _Scale = 1.0;
                float3 _Offset = float3(0,0,0);
                float _OffsetScale = 0.6;
                float _DensityMultipler = 5.0;
                float3 densityV = tex3D(_NoiseTex, samplePosition * _Scale + _Offset * _OffsetScale);
                float density = densityV.x;
                return density;
            }
            float sdfSphere(float3 p, float s)
            {
                return length(p) - s;
            }
            float4 raymarch(float3 samplePosition, float3 marchDirection, int stepCount, float stepSize) {

                float transmittance = 1.0;
                float3 illumination = float3(0.0, 0.0, 0.0);
                float extinctionCoef = absorptionCoef + scatteringCoef;
                for (int i = 0; i < stepCount; i++) {
                    samplePosition += marchDirection * stepSize;
                    float d = sdfSphere(samplePosition,7);
                    if (d <= 0) {
                        
                        float currentDensity = density(samplePosition);
                        //return float4(currentDensity, currentDensity, currentDensity, 1);
                        transmittance *= 1;// exp(-currentDensity * extinctionCoef * stepSize);

                        float inScattering = ambientLight;
                        float outScattering = scatteringCoef * currentDensity;
                        
                        float3 currentLight = inScattering * outScattering;
                        return float4(currentLight.x, currentLight.y, currentLight.z, 1);
                        illumination += transmittance * currentLight * stepSize;
                    }
                }
                return float4(illumination.x, illumination.y, illumination.z, transmittance);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 dir = float3(2 * i.uv.x - 1, 2 * i.uv.y - 1, 1);
                dir = mul(unity_CameraToWorld, float4(dir, 0));
                float3 pos = _WorldSpaceCameraPos;
                dir = normalize(dir);
                int numOfSteps = 300;
                float stepSize = 30.0 / numOfSteps;
                float4 rayCol = raymarch(pos, dir, numOfSteps, stepSize);
                return rayCol;
            }
            ENDCG
        }
    }
}
