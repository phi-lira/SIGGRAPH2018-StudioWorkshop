Shader "Hidden/SIGGRAPH Studio/DeferredLighting"
{
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}

        Pass
        {
            Name "DeferredLighting"

            Blend One One
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles d3d11_9x

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "LWRP/ShaderLibrary/Lighting.hlsl"

            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(0); // Albedo
            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(1); // SpecRoughness
            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(2); // Normal
            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(3); // Depth

            half BDRF(half roughness, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
            {
                half3 halfDir = SafeNormalize(lightDirectionWS + viewDirectionWS);

                half NoH = saturate(dot(normalWS, halfDir));
                half LoH = saturate(dot(lightDirectionWS, halfDir));

                // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
                // BRDFspec = (D * V * F) / 4.0
                // D = roughness² / ( NoH² * (roughness² - 1) + 1 )²
                // V * F = 1.0 / ( LoH² * (roughness + 0.5) )
                // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
                // https://community.arm.com/events/1155

                // Final BRDFspec = roughness² / ( NoH² * (roughness² - 1) + 1 )² * (LoH² * (roughness + 0.5) * 4.0)
                half roughness2 = roughness * roughness;
                half d = NoH * NoH * (roughness2 - 1.0h) + 1.0h;
                half d2 = d * d;

                half LoH2 = max(0.1h, LoH * LoH);
                half reflectance = roughness2 / (d2 * LoH2 * (roughness + 0.5) * 4.0h);
                return reflectance;
            }

            float4 Vertex(float4 vertexPosition : POSITION) : SV_POSITION
            {
                return vertexPosition;
            }

            half4 Fragment(float4 pos : SV_POSITION) : SV_Target
            {
                half3 albedo = UNITY_READ_FRAMEBUFFER_INPUT(0, pos).rgb;
                half4 specRoughness = UNITY_READ_FRAMEBUFFER_INPUT(1, pos);
                half3 normalWS = normalize((UNITY_READ_FRAMEBUFFER_INPUT(2, pos).rgb * 2.0h - 1.0h));
                float depth = UNITY_READ_FRAMEBUFFER_INPUT(3, pos).r;

                float2 positionNDC = pos.xy * _ScreenSize.zw;
                float3 positionWS = ComputeWorldSpacePosition(positionNDC, depth, UNITY_MATRIX_I_VP);

                half3 viewDirection = half3(normalize(GetCameraPositionWS() - positionWS));

                Light mainLight = GetMainLight();
                half3 specular = specRoughness.rgb;
                half roughness = specRoughness.a;

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 radiance = mainLight.color * (mainLight.attenuation * NdotL);
                half reflectance = BDRF(roughness, normalWS, mainLight.direction, viewDirection);
                half3 color = (albedo + specular * reflectance) * radiance;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
