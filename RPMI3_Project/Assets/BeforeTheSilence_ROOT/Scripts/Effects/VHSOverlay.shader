Shader "Hidden/VHSEffect"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"
    
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    float _Noise;
    float _Scanlines;
    float _Distortion;
    
    // Función de ruido
    float rand(float2 co)
    {
        return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
    }
    
    float4 frag(v2f_img i) : SV_Target
    {
        float2 uv = i.uv;
        
        // 1. DISTORSIÓN ONDULANTE
        float wave = sin(uv.y * 20.0 + _Time.y * 5.0) * _Distortion;
        uv.x += wave;
        
        // 2. COLOR BASE
        float4 color = tex2D(_MainTex, uv);
        
        // 3. RUIDO TEMPORAL (esto es lo que faltaba en el tuyo)
        float noise = rand(uv + frac(_Time.y)) * _Noise;
        color.rgb += noise - (_Noise * 0.5);
        
        // 4. SCANLINES ANIMADAS
        float scanline = sin(uv.y * 600.0 + _Time.y * 10.0);
        color.rgb -= scanline * _Scanlines * 0.1;
        
        // 5. VIGNETTE
        float2 center = uv - 0.5;
        float vignette = 1.0 - length(center) * 1.5;
        color.rgb *= vignette;
        
        return color;
    }
    ENDCG
    
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}