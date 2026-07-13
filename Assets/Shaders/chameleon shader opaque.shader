Shader "Custom/ChameleonSurfaceOpaque"
{
    Properties
    {
        _MainTexA ("Texture A", 2D) = "white" {}
        _ColorA ("Color A", Color) = (0, 1, 0, 1)

        _MainTexB ("Texture B", 2D) = "white" {}
        _ColorB ("Color B", Color) = (1, 0, 1, 1)

        _Sharpness ("Sharpness", Range(0.05, 10.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTexA;
        sampler2D _MainTexB;
        fixed4 _ColorA;
        fixed4 _ColorB;
        float _Sharpness;

        struct Input
        {
            float2 uv_MainTexA;
            float2 uv_MainTexB;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float facing = saturate(dot(normalize(IN.viewDir), o.Normal));
            facing = pow(facing, _Sharpness);

            fixed4 texA = tex2D(_MainTexA, IN.uv_MainTexA) * _ColorA;
            fixed4 texB = tex2D(_MainTexB, IN.uv_MainTexB) * _ColorB;

            fixed4 finalColor = lerp(texB, texA, facing);
            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
