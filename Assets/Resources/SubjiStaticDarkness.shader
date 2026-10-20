Shader "Subji/Static Darkness Reveal"
{
    Properties { _Color ("Darkness Color", Color) = (0,0,0,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float2 worldPos : TEXCOORD0; };

            fixed4 _Color;
            float4 _PlayerData;
            float4 _FlashlightData;
            float4 _FlashlightDirection;
            int _PlacedLightCount;
            float4 _PlacedLights[30];
            float _OcclusionEnabled;
            float _OcclusionDistances[129];
            int _VisibleWallCount;
            float4 _VisibleWallRects[64];
            float4 _OcclusionAngles;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xy;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 fromPlayer = input.worldPos - _PlayerData.xy;
                float playerDistance = length(fromPlayer);
                float darkness = _PlayerData.z < 0.0 ? 1.0 : smoothstep(_PlayerData.z,
                    _PlayerData.z + max(0.001, _PlayerData.w), playerDistance);

                if (_FlashlightData.x > 0.5 && playerDistance <= _FlashlightData.y &&
                    playerDistance > 0.001)
                {
                    float facing = dot(normalize(fromPlayer), normalize(_FlashlightDirection.xy));
                    if (facing >= _FlashlightData.z)
                    {
                        darkness = 0.0;
                        if (_OcclusionEnabled > 0.5)
                        {
                            float2 direction = normalize(fromPlayer);
                            float angle = atan2(_OcclusionAngles.z * direction.y -
                                _OcclusionAngles.w * direction.x,
                                dot(_OcclusionAngles.zw, direction));
                            float sampleIndex = clamp((angle + _OcclusionAngles.x) /
                                max(0.00001, _OcclusionAngles.y), 0.0, 128.0);
                            int left = (int)floor(sampleIndex);
                            int right = min(left + 1, 128);
                            int closest = _OcclusionDistances[left] <= _OcclusionDistances[right]
                                ? left : right;
                            if (playerDistance > _OcclusionDistances[closest] + 0.005)
                            {
                                darkness = 1.0;
                                [loop]
                                for (int w = 0; w < _VisibleWallCount; w++)
                                {
                                    float4 wall = _VisibleWallRects[w];
                                    if (all(input.worldPos >= wall.xy) && all(input.worldPos <= wall.zw))
                                    {
                                        darkness = 0.0;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                [loop]
                for (int index = 0; index < _PlacedLightCount; index++)
                {
                    float4 lightData = _PlacedLights[index];
                    float lightDistance = distance(input.worldPos, lightData.xy);
                    float lightDarkness = smoothstep(lightData.z,
                        lightData.z + max(0.001, lightData.w), lightDistance);
                    darkness = min(darkness, lightDarkness);
                }

                return fixed4(_Color.rgb, _Color.a * darkness);
            }
            ENDCG
        }
    }
}
