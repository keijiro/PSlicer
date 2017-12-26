Shader "PSlicer"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        [Gamma] _Metallic("Metallic", Range(0, 1)) = 0
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0)

        [Header(Backface Attributes)]
        _Color2("Color", Color) = (1, 1, 1, 1)
        [Gamma] _Metallic2("Metallic", Range(0, 1)) = 0
        _Glossiness2("Smoothness", Range(0, 1)) = 0.5

        [HideInInspector] _EffectorColor("", Color) = (0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }

        Cull Off

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float vface : VFACE;
        };

        sampler2D _MainTex;

        half4 _Color;
        half _Metallic;
        half _Glossiness;
        half3 _EmissionColor;

        half4 _Color2;
        half _Metallic2;
        half _Glossiness2;

        half _Density;
        half _Speed;

        float _EffectorRange;
        float _EffectorOffset;
        float4x4 _EffectorMatrix;
        half3 _EffectorColor;

        float _LocalTime;

        // Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
        uint Hash(uint s)
        {
            s ^= 2747636419u;
            s *= 2654435769u;
            s ^= s >> 16;
            s *= 2654435769u;
            s ^= s >> 16;
            s *= 2654435769u;
            return s;
        }

        float Random(uint seed)
        {
            return float(Hash(seed)) / 4294967295.0; // 2^32-1
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Effect coordinates
            float3 coord = mul(_EffectorMatrix, float4(IN.worldPos, 1));

            // Density
            float density1 = _Density;
            float density2 = _Density / 2; // half density
            float density3 = _Density / 3; // quarter density

            // Current slice number
            float slice1 = floor(coord.z * density1);
            float slice2 = floor(coord.z * density2); // half density
            float slice3 = floor(coord.z * density3); // quarter density

            // Random number used to select density
            float rnd2 = Random(slice2 + 10000) < 0.5;
            float rnd3 = Random(slice3 + 10000) < 0.5;

            // Actual density and current slice number
            float density = lerp(lerp(density1, density2, rnd2), density3, rnd3);
            float slice = lerp(lerp(slice1, slice2, rnd2), slice3, rnd3);

            // Random seed for the current slice
            uint seed = (uint)(slice * 199 + 10000);

            // Scrolling speed
            float speed = _Speed * (Random(seed) + 1) * 0.5;

            // Convert into polar coordinates
            float phi = atan2(coord.x, coord.y) * UNITY_INV_TWO_PI + 0.5;
            phi = frac(phi + speed * _LocalTime);

            // Threshold for the current slice
            float th = (slice / density - _EffectorOffset) / _EffectorRange;

            // Thresholding
            if (frac(phi) < th) discard;

            // Slice emission
            float em = saturate(1 - (frac(phi) - th) * 5);
            em *= 0.5 + Random(seed + 1);

            // Surface shader output
            half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            bool backface = IN.vface < 0;
            o.Albedo = backface ? _Color2.rgb : c.rgb;
            o.Metallic = backface ? _Metallic2 : _Metallic;
            o.Smoothness = backface ? _Glossiness2 : _Glossiness;
            o.Emission = (backface ? 0 : _EmissionColor) + em * _EffectorColor;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
