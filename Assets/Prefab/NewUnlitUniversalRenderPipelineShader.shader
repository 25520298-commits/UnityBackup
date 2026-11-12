Shader "Custom/GhostShader"
{
    Properties
    {
        _Color ("Ghost Color", Color) = (0, 1, 1, 0.5) // Màu xanh lơ trong suốt
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Bật chế độ hòa trộn (Blending) để tạo độ trong suốt
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off // Không ghi đè chiều sâu để thấy xuyên qua
        Cull Off // Nhìn thấy cả mặt trong lẫn mặt ngoài

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Lấy màu texture gốc
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Trộn với màu Ghost (bỏ qua màu gốc, chỉ lấy độ trong suốt của _Color)
                col.rgb = _Color.rgb;
                col.a *= _Color.a;
                
                return col;
            }
            ENDCG
        }
    }
}   