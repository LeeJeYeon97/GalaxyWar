Shader "EnergyShield/EnergyShield"
{
    Properties
    {
        // Core color settings for the shield
        [Header(Main Colors)] [Space(5)]
        [HDR] _FrontColor ("Front Color", Color) = (0, 1, 1, 1)
        [HDR] _BackColor ("Back Color", Color) = (0, 0.5, 1, 1)
        [HDR] _FresnelColor ("Fresnel Color", Color) = (1, 0, 1, 1)
        
        // Base surface appearance properties
        [Space(10)]
        [Header(Surface Settings)] [Space(5)]
        _MainTexture ("Shield Texture", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Alpha ("Alpha", Range(0,1)) = 0.0
        
        // Vertex displacement animation based on time
        [Space(10)]
        [Header(Vertex Animation)] [Space(5)]
        [Vector3]_VertexOut ("Vertex Amount Out", Vector) = (0.0, 0.0, 0.0)
        [Vector3]_VertexIn ("Vertex Amount In", Vector) = (0.0, 0.0, 0.0)
        _VertexFreq ("Vertex Freq", float) = 1
        _VertexAnimAlpha ("Animation Strength", Range(0, 1)) = 0
        
        // Flowing cloud/energy texture settings
        [Space(10)]
        [Header(Cloud Settings)] [Space(5)]
        _CloudTexture ("Cloud Texture (R)", 2D) = "white" {}
        [HDR] _CloudColor ("Cloud Color", Color) = (0.8, 0.8, 0.8, 1)
        _CloudDirection ("Cloud Direction/Speed", Vector) = (0.1, 0.1, 0, 0)
        
        // Background distortion (refraction) effect
        [Space(10)]
        [Header(Distortion)] [Space(5)]
        _DistortionTexture ("Distortion Texture", 2D) = "bump" {}
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.1
        _DistortionSpeed ("Distortion Speed", float) = 0.1
        
        // Sci-fi hexagonal grid overlay
        [Space(10)]
        [Header(Hexagon Grid)] [Space(5)]
        _HexScale ("Hex Scale", float) = 10
        _HexWireWidth ("Hex Wire Width", Range(0, 0.5)) = 0.05
        [HDR] _HexColor ("Hex Color", Color) = (1, 1, 1, 1)
        _hexPulseSpeed ("Hex Pulse Speed", float) = 1.0
        _hexPulseFading ("Hex Pulse Fading", float) = 0.2
        
        // Impact/hit effect settings
        [Space(10)]
        [Header(Effects)] [Space(5)]
        [HDR] _HitColor ("Hit Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        // URP Transparent setup
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        LOD 100
        ZWrite Off
        Blend SrcAlpha One // Additive blending
        Cull Off
        
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Input from the mesh
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float2 uv2          : TEXCOORD1;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID // VR Instance ID
            };
            
            // Data passed from vertex to fragment shader
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float2 uv2          : TEXCOORD1;
                float3 worldNormal  : TEXCOORD2;
                float3 worldPos     : TEXCOORD3;
                float4 screenPos    : TEXCOORD4;
                float hitMask       : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID // VR Instance ID
                UNITY_VERTEX_OUTPUT_STEREO     // VR Stereo Output
            };

            TEXTURE2D(_MainTexture);          
            SAMPLER(sampler_MainTexture);  
            TEXTURE2D(_CloudTexture);
            SAMPLER(sampler_CloudTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FrontColor;
                float4 _BackColor;
                float4 _FresnelColor;
                float4 _MainTexture_ST;
                float _Smoothness;
                float _Metallic;
                float _Alpha;
            
                float3 _VertexOut;
                float3 _VertexIn;
                float _VertexFreq;
                float _VertexAnimAlpha;
            
                float4 _CloudTexture_ST;
                float4 _CloudDirection;
                float4 _CloudColor;
            
                float4 _DistortionTexture_ST;
                float _DistortionAmount;
                float _DistortionSpeed;
            
                float4 _HitPos;
                float _HitTime;
                float _HitRadius;
                float _HitStrength;
                float4 _HitColor;
            
                float _HexScale;
                float _HexWireWidth;
                float4 _HexColor;
                float _hexPulseSpeed;
                float _hexPulseFading;
            CBUFFER_END
            
            // Use TEXTURE2D_X for _CameraOpaqueTexture as it is a texture array in VR
            TEXTURE2D_X(_CameraOpaqueTexture);
            TEXTURE2D(_DistortionTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            
            // Pseudo-random hash function
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Distance field for hexagonal shapes
            float hexDist(float2 p) 
            {
                p = abs(p);
                float d = dot(p, normalize(float2(1, 1.73)));
                return max(d, p.x);
            }   
            
            // Generates the hexagonal grid pattern
            float getHexGrid(float2 uv) {
                float2 r = float2(1, 1.73);
                float2 h = r * 0.5;
                
                float2 a = fmod(uv, r) - h;
                float2 b = fmod(uv - h, r) - h;
                
                float2 g = dot(a, a) < dot(b, b) ? a : b;
                
                float d = hexDist(g);
                return smoothstep(0.5 - _HexWireWidth, 0.5, d);
            }            
            
            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                
                // VR Setup macros
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // Calculate impact area
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float dist = distance(_HitPos.xyz, worldPos);
                float hitMask = saturate(1.0 - (dist / _HitRadius));
                
                // Displace vertices based on hit
                float3 displacement = IN.normalOS * hitMask * _HitStrength;
                
                float3 pos = IN.positionOS.xyz;
                float3 norm = IN.normalOS;

                // Time-based vertex animation
                float currentTime = _Time.y * _VertexFreq;
                float floatDisplace = sin(currentTime + hash(IN.uv2)) * _VertexAnimAlpha;
                float3 currentVertexAmount = (floatDisplace > 0) ? _VertexOut : _VertexIn;
                
                // Scale factor of 100 applied to avoid working with tiny float values in the UI
                float3 displacedPos = pos + (norm * (currentVertexAmount.xyz / 100) * floatDisplace); 
                float3 finalPosOS = displacement + displacedPos;
                
                OUT.positionCS = TransformObjectToHClip(finalPosOS);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.uv = IN.uv;
                OUT.uv2 = IN.uv2;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos = TransformObjectToWorld(displacedPos);
                OUT.hitMask = hitMask * _HitStrength;
                
                return OUT;
            }

            float4 frag (Varyings IN, bool facing : SV_IsFrontFace) : SV_Target
            {
                // VR Fragment Setup macros
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                // Fresnel calculation
                float3 normal = normalize(IN.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos); 
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(1.0 - fresnel, _Smoothness * 2.0 + 0.00001);
                
                // Base texture sampling
                float2 uv = TRANSFORM_TEX(IN.uv, _MainTexture);
                float4 texColor = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, uv);
                
                // Background distortion (refraction)
                float2 distOffset = _DistortionSpeed * _Time.y * 0.01;
                float2 distUV = IN.uv2 * _DistortionTexture_ST.xy + _DistortionTexture_ST.zw + distOffset;
                
                float2 distortion = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_CloudTexture, distUV).rg;
                distortion *= _DistortionAmount; 
                
                float2 distortedScreenUV = (IN.screenPos.xy / IN.screenPos.w) + distortion;
                
                // Use SAMPLE_TEXTURE2D_X instead of SAMPLE_TEXTURE2D to read the background in VR
                float3 background = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortedScreenUV).rgb;

                // Reflection intensity logic
                float reflectionElement = pow(fresnel, 2);
                float relfectionIntensity = _Metallic + fresnel * (1.0 - _Metallic);
                float3 reflection = 1-(reflectionElement * relfectionIntensity);
                reflection = pow(reflection,5);
                
                // Flowing clouds setup
                float2 cloudOffset = _CloudDirection.xy * _Time.y * 0.01;
                float2 cloudUV = IN.uv * _CloudTexture_ST.xy + cloudOffset;
                float2 cloudUV2 = IN.uv * _CloudTexture_ST.xy + cloudOffset * 2; 
                
                float cloudValue = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloudUV).r;
                float cloudValue2 = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloudUV2).r;

                // Front/Back face color logic
                float4 shieldColor = facing ? _FrontColor : _BackColor;
                float faceWeight = facing ? 1.0 : 0.0;
                
                // Compose final emissions and colors
                float3 emission = _FresnelColor.rgb * texColor.rgb * reflection * faceWeight * cloudValue;
                float3 cloudEmission = reflection * faceWeight * cloudValue2 * _CloudColor.rgb;
                float3 albedo = shieldColor.rgb * texColor.rgb;
                
                float3 shieldResult = albedo + emission + cloudEmission;
                float hitEffect = IN.hitMask;
                
                // Add Hit Effect
                shieldResult = lerp(shieldResult, _HitColor.rgb, hitEffect);
                shieldResult += _HitColor.rgb * hitEffect;

                // Apply Refraction/Background Lerp
                float3 finalRGB = lerp(background, shieldResult, texColor.a); 
                
                // Hexagonal Grid Calculation
                float2 hexUV = IN.uv * _HexScale;

                float hexPulseWave = abs(sin(_Time.y - hexUV.x * 0.3));
                float hexPulse = lerp(_hexPulseFading, 1, hexPulseWave  * _hexPulseSpeed) * cloudValue;
                float hexMask = getHexGrid(hexUV * _HexScale) * hexPulse;

                float3 hexEmission = hexMask * _HexColor.rgb * faceWeight;
                finalRGB += hexEmission;

                return float4(finalRGB, _Alpha); 
            }
            ENDHLSL
        }
    }
}