// The Ghost Run rider: see-through, glowing at the edges, faint scan lines drifting up. A depth pass first so the
// bike and rider don't show through themselves.
Shader "BikeRush/Ghost"
{
    Properties
    {
        _Color ("Colour", Color) = (0.25, 0.88, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.45
        _Rim ("Rim Power", Range(0.5, 8)) = 2.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            ZWrite On
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert (float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag () : SV_Target { return 0; }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Alpha, _Rim;

            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float wy : TEXCOORD2; };

            v2f vert (appdata_base i)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                float3 w = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = _WorldSpaceCameraPos - w;
                o.wy = w.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rim = pow(1 - saturate(abs(dot(normalize(i.n), normalize(i.v)))), _Rim);
                float scan = 0.75 + 0.25 * step(0.5, frac(i.wy * 9 - _Time.y * 1.5));
                float a = saturate((0.18 + rim) * scan) * _Alpha;
                return fixed4(_Color.rgb * (0.6 + rim), a);
            }
            ENDCG
        }
    }
}
