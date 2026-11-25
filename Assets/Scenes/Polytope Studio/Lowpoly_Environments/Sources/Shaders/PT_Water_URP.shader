Shader "Polytope Studio/PT_Water_URP"
{
    Properties
    {
        _DeepColor("Deep Color", Color) = (0.21440013, 0.6108907, 0.6226415, 0.92)
        _ShallowColor("Shallow Color", Color) = (0.21960782, 0.46475953, 0.5372549, 0.72)
        _Depth("Depth", Range(0, 1)) = 0.304
        _DepthStrength("Depth Strength", Range(0, 1)) = 0.969
        _Smootness("Smoothness", Range(0, 1)) = 0.837
        _Mettalic("Metallic", Range(0, 1)) = 0.0
        _WaveSpeed("Wave Speed", Range(0, 1)) = 0.5
        _WaveTile("Wave Tile", Range(0, 0.9)) = 0.5
        _WaveAmplitude("Wave Amplitude", Range(0, 1)) = 0.05
        [NoScaleOffset][Normal]_NormalMapTexture("Normal Map Texture", 2D) = "bump" {}
        _NormalMapWavesSpeed("Normal Map Waves Speed", Range(0, 1)) = 0.223
        _NormalMapsWavesSize("Normal Maps Waves Size", Range(0, 10)) = 6.14
        _FoamColor("Foam Color", Color) = (0.2971698, 1, 0.91264254, 1)
        _FoamAmount("Foam Amount", Range(0, 10)) = 0.35
        _FoamPower("Foam Power", Range(0.1, 5)) = 2.82
        _FoamNoiseScale("Foam Noise Scale", Range(0, 1000)) = 597
        _FlowDirection("Flow Direction (XY)", Vector) = (0.0, 1.0, 0.0, 0.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float4 screenPos   : TEXCOORD4;
                float  fogFactor   : TEXCOORD5;
                float2 uv          : TEXCOORD6;
            };

            TEXTURE2D(_NormalMapTexture);
            SAMPLER(sampler_NormalMapTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                float4 _FlowDirection;
                float _Depth;
                float _DepthStrength;
                float _Smootness;
                float _Mettalic;
                float _WaveSpeed;
                float _WaveTile;
                float _WaveAmplitude;
                float _NormalMapWavesSpeed;
                float _NormalMapsWavesSize;
                float _FoamAmount;
                float _FoamPower;
                float _FoamNoiseScale;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);

                float waveFreq = max(_WaveTile, 0.01) * 1.5;
                float waveTime = _Time.y * (_WaveSpeed * 4.0);
                float waveOffset = sin(posWS.x * waveFreq + waveTime) * cos(posWS.z * waveFreq + waveTime * 0.8);
                posWS.y += waveOffset * (_WaveAmplitude * 0.25);

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;

                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 flowDir = normalize(_FlowDirection.xy + float2(1e-5, 1e-5));
                float flowSpeed = max(_NormalMapWavesSpeed, 0.02) * 0.6;
                float worldTiling = max(_NormalMapsWavesSize, 0.1) * 0.04;

                float2 uv1 = input.positionWS.xz * worldTiling + flowDir * (_Time.y * flowSpeed);
                float2 uv2 = input.positionWS.xz * (worldTiling * 1.45) + float2(-flowDir.y * 0.35 + flowDir.x, flowDir.y * 0.85) * (_Time.y * flowSpeed * 1.25);

                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapTexture, sampler_NormalMapTexture, uv1));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapTexture, sampler_NormalMapTexture, uv2));
                half3 normalTS = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));

                half3x3 tbn = half3x3(
                    normalize(input.tangentWS),
                    normalize(input.bitangentWS),
                    normalize(input.normalWS)
                );
                half3 normalWS = normalize(mul(normalTS, tbn));

                // Depth calculation for shallow/deep color and shoreline foam
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 1e-4);
                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float waterEyeDepth = input.screenPos.w;
                float waterDepth = max(0.0, sceneEyeDepth - waterEyeDepth);

                float depthFactor = saturate(waterDepth * max(_Depth, 0.01) * (0.25 + _DepthStrength * 1.25));
                half3 waterCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);

                // Shoreline foam + flowing surface foam
                float foamNoiseScale = max(_FoamNoiseScale, 1.0) * 0.012;
                float foamNoise = ValueNoise(input.positionWS.xz * foamNoiseScale + flowDir * (_Time.y * flowSpeed * 2.0));
                float shoreMask = 1.0 - saturate(waterDepth / max(_FoamAmount, 0.01));
                float foamMask = pow(saturate(shoreMask * (0.65 + foamNoise * 0.7)), max(_FoamPower, 0.1));
                waterCol = lerp(waterCol, _FoamColor.rgb, saturate(foamMask));

                // Simple URP stylized lighting + specular reflection
                Light mainLight = GetMainLight();
                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfVec = normalize(mainLight.direction + viewDirWS);

                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half specPower = exp2(10.0 * saturate(_Smootness) + 1.0);
                half spec = pow(saturate(dot(normalWS, halfVec)), specPower) * saturate(_Smootness);

                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 4.0);
                half3 skyReflect = SampleSH(reflect(-viewDirWS, normalWS));

                half3 litColor = waterCol * (mainLight.color * (ndotl * 0.55 + 0.45) + SampleSH(normalWS) * 0.35);
                litColor += mainLight.color * spec * 0.75;
                litColor = lerp(litColor, skyReflect, fresnel * 0.35);

                litColor = MixFog(litColor, input.fogFactor);

                half alpha = lerp(0.72, 0.94, depthFactor);
                alpha = saturate(max(alpha, foamMask));

                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
