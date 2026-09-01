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
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float2 worldPos : TEXCOORD0; };

            fixed4 _Color;
            float4 _PlayerData;
            float4 _FlashlightData;
            float4 _FlashlightDirection;
            int _PlacedLightCount;
            float4 _PlacedLights[30];

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
                float darkness = smoothstep(_PlayerData.z,
                    _PlayerData.z + max(0.001, _PlayerData.w), playerDistance);

                if (_FlashlightData.x > 0.5 && playerDistance <= _FlashlightData.y &&
                    playerDistance > 0.001)
                {
                    float facing = dot(normalize(fromPlayer), normalize(_FlashlightDirection.xy));
                    if (facing >= _FlashlightData.z)
                        darkness = 0.0;
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
