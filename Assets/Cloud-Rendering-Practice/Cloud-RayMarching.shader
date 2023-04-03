Shader "Hidden/Cloud-RayMarching"
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
            float sdBox(float3 p, float3 b)
            {
                float3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }
            float sdSphere(float3 p, float s)
            {
                return length(p) - s;
            }
            // Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero)
            float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir) {
                // From http://jcgt.org/published/0007/03/04/
                // via https://medium.com/@bromanz/another-view-on-the-classic-ray-aabb-intersection-algorithm-for-bvh-traversal-41125138b525
                float3 t0 = (boundsMin - rayOrigin) / rayDir;
                float3 t1 = (boundsMax - rayOrigin) / rayDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
                // dstA is dst to nearest intersection, dstB dst to far intersection

                // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
                // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                // CASE 3: ray misses box (dstA > dstB)

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }
            //Add more variables to tweak shader with and setup scattering and attenuation correctly
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = fixed4(0,0,0,0);//tex2D(_MainTex, i.uv);
                float3 uv3D = float3(i.uv.x, i.uv.y, abs(_SinTime.z));
                fixed4 dc = tex3D(_NoiseTex, uv3D);
                fixed4 att = tex2D(_MainTex, i.uv);
                float3 pos = float3(0, 0, 0);
                float3 dir = float3(2*i.uv.x - 1, 2 * i.uv.y -1, 1);
                //dir = mul(unity_CameraInvProjection, float4(dir.x,dir.y, 0, -1));
                dir = mul(unity_CameraToWorld, float4(dir, 0));
                float dist = 0;
                pos = _WorldSpaceCameraPos;
                dir = normalize(dir);
                float stepSize = 0.1;
                for (int j = 0; j < 300; j++) {
                    //dir = normalize(dir);
                    float t = sdBox(pos, float3(2, 2, 2));
                    if (t <= 0) {
                        stepSize = 0.01;

                        //att = att * exp(-stepSize * dc.x);
                        //col += att;
                        dc = tex3D(_NoiseTex, pos*.6 + float3(1,0,0)*.6);
                        dist += stepSize * max(0,dc.x-.65)*5;
                    }
                    else {
                        stepSize = t;
                    }

                    pos += dir* stepSize;
                    
                }
                //if (col.x < .01) col = att;
                //col = dc;
                
                //col = tex3D(_NoiseTex, uv3D);
                //col.a = 1;
                return att* exp(-dist);
            }
            ENDCG
        }
    }
}
