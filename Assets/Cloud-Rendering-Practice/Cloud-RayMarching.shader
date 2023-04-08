Shader "Hidden/Cloud-RayMarching"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex("Texture", 3D) = "white" {}
        _Scale("Cloud Scale",float) = 0.6
        _Offset("Cloud Offest",vector) = (0,0,0,0)
        _BoxSize("Cloud Box Diamensions",vector) = (2,2,2,0)
        _OffsetScale("Offset",float) = 0.6
        _Threshold("Density Threshold",float) = 0.75
        _DensityMultipler("Desnity Multipler",float) = 900.0
        _gFactor("g value between [-1,1] to control scatter direction for phase func",float) = 0.23
        _ScatCo("Scattering coefficent",float) = 0.06
        _AbsorbCo("Absorbsion coefficent",float) = 0.001
        _LightIntensity("Light Intensity",float) = 20
        _AmbientIntensity("Intensity of Ambient light",float) = 10
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
            #include "UnityLightingCommon.cginc"

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
            float _Scale = 0.6;
            float4 _Offset = (0, 0, 0, 0);
            float4 _BoxSize = (2, 2, 2, 0);
            float _OffsetScale = 0.6;
            float _Threshold = 0.65;
            float _DensityMultipler = 5.0;
            float3 _LightDir;
            float _gFactor;
            float _ScatCo;
            float _AbsorbCo;
            float _LightIntensity;
            float _AmbientIntensity;
            float sdBox(float3 p, float3 b)
            {
                float3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }
            float sdSphere(float3 p, float s)
            {
                return length(p) - s;
            }
            float phaseVal(float cosVal) {
                float g = _gFactor;
                float g2 = g * g;
                return (1 - g2) / (4 * 3.1415 * pow(1 + g2 - 2 * g * (cosVal), 1.5));
            }
            //Probably redudant. Ignore for now
            float luminance(float3 dir) {
                const float PI = 3.14159;
                const float num = 15;
                const float step = 2 * PI / num;
                float sum = phaseVal(cos(0));
                for (float angle = 2 * PI / num; angle < (2 * PI-step); angle += step) {
                    sum += 2 * phaseVal(cos(angle));
                }
                sum += phaseVal(cos(angle));
                sum *= step / 2;
                return sum;
            }
            //This is the shadow ray to the sun. Currently Single Scattering event. Needs to be multi scatter
            float lightMarch(float3 lightDir, float3 rayPos) {
                float3 rayDir = rayPos - _WorldSpaceCameraPos;
                normalize(rayDir);
                float4 densityV = tex3D(_NoiseTex, rayPos * _Scale + _Offset * _OffsetScale);
                float density = max(0, densityV.x - _Threshold) * _DensityMultipler;
                float totalDensity = 0;
                float stepSize = 0.02;
                float dist = 0;
                float lightIntensity = _LightIntensity;
                for (int i = 0; i < 150; i++) {
                    float3 pos = rayPos + stepSize * i * lightDir;
                    float t = sdBox(pos, _BoxSize);
                    if (t <= 0) {
                        stepSize = 0.02;
                        densityV = tex3D(_NoiseTex, pos * _Scale + _Offset * _OffsetScale);
                        density = max(0, densityV.x - _Threshold) * _DensityMultipler;
                        dist += stepSize; //luminance(rayDir)
                        totalDensity += density;
                        //att *= exp(-stepSize * density*(_ScatCo + _AbsorbCo));
                    }
                    //else {
                    //    break;
                    //}
                }
                return exp(-totalDensity * dist *(_ScatCo + _AbsorbCo))* lightIntensity;
            }
            //TODO: Scattering not working correctly. I think too much light is being atteuated. Needs to be fixed
            //Stretch goal: Add detail noise texture to improve cloud shape
            fixed4 frag(v2f i) : SV_Target
            {
                _LightDir = _WorldSpaceLightPos0.xyz;
                 //20
                fixed4 dc = fixed4(0, 0, 0, 0);//tex3D(_NoiseTex, float3(0,0,0));
                fixed4 inputColor = tex2D(_MainTex, i.uv);
                fixed4 col = fixed4(0, 0, 0, 0);
                float att = 1;
                float3 pos = float3(0, 0, 0);
                float3 dir = float3(2*i.uv.x - 1, 2 * i.uv.y -1, 1);
                //dir = mul(unity_CameraInvProjection, float4(dir.x,dir.y, 0, -1));
                dir = mul(unity_CameraToWorld, float4(dir, 0));
                float lEnergy = 0;
                pos = _WorldSpaceCameraPos;
                dir = normalize(dir);
                float stepSize = 0.1;
                //Phase function for inscattering
                float dotP = dot(-dir,-_LightDir);
                float phase = phaseVal(dotP);
                for (int j = 0; j < 300; j++) {
                    //dir = normalize(dir);
                    float t = sdBox(pos, _BoxSize);
                    if (t <= 0) {
                        stepSize = 0.01;

                        //att = att * exp(-stepSize * dc.x);
                        //col += att;
                        // dst = dst + dx*density*attenuation*luminace*phase
                        // attenuation = attenuation * exp(-step*density*att_coef)
                        dc = tex3D(_NoiseTex, pos*_Scale + _Offset*_OffsetScale);
                        lEnergy +=  stepSize * max(0, dc.x - _Threshold) * _DensityMultipler
                            * att * (lightMarch(_LightDir, pos) + _AmbientIntensity)* phase *_ScatCo;// *coefficents.y; //Not Att_Coef for now
                        att *= exp(-stepSize * max(0, dc.x - _Threshold) * _DensityMultipler *(_ScatCo + _AbsorbCo));// *(coefficents.x + coefficents.y));
                    }
                    else {
                        stepSize = t;
                    }

                    pos += dir* stepSize;
                    
                }
                //if (col.x < .01) col = att;
                //if(lEnergy < 0.2 && lEnergy > 0) col = fixed4(.2, .6, .2,1);
                //col = fixed4((2-_LightDir.x)/3, (2-_LightDir.y)/3, (2-_LightDir.z)/3, 1);
                //col = tex3D(_NoiseTex, uv3D);
                col = inputColor * att + _LightColor0 * lEnergy;
                //col.a = 0;
                return col;
            }
            ENDCG
        }
    }
}
