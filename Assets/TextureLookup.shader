Shader "Unlit/TextureLookup" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _Data    ("Data", 2D) = "white" {}
        _Lookup  ("Lookup", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _Data;
            sampler2D _Lookup;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 dataUv = tex2D(_Lookup, i.uv);
                return tex2D(_Data, dataUv).r != 0 ? 1 : 0;
            }
            ENDCG
        }
    }
}
