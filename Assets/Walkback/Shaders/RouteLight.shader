// Written with Claude Code (AI), directed and tested by team MARSXR
// Light drawn on the map: the safe route (amber ribbon), the straight line (white dashes)
// and the stop pillars. Opaque and unlit, and it always writes alpha = 1, so it never opens
// a hole to the room in passthrough (trap 7).
//   uv.x = distance along the line, 0..1 (route) or 0..1 up the pillar (pillars)
//   uv.y = across the ribbon, 0..1
// _Progress cuts the line at that fraction: the route draws itself.
// _DashCount > 0 cuts it into dashes. _Border darkens the edges so amber reads on orange slope.
// _Vertical = 1 for pillars: bright at the base, fading up.
Shader "Walkback/RouteLight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.72, 0.05, 1)
        _Progress ("Progress", Range(0, 1)) = 1
        _DashCount ("Dash count (0 = solid)", Float) = 0
        _Border ("Dark border share", Range(0, 0.9)) = 0.3
        _Pulse ("Pulse", Range(0, 1)) = 0
        _Vertical ("Vertical fade (pillars)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite On
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Progress;
            float _DashCount;
            float _Border;
            float _Pulse;
            float _Vertical;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float pulse = 1.0 + _Pulse * 0.35 * sin(_Time.y * 4.0);
                if (_Vertical > 0.5)
                {
                    // Pillar: bright base, softer top; never transparent.
                    float up = saturate(i.uv.y);
                    float3 c = _Color.rgb * lerp(1.15, 0.55, up) * pulse;
                    return fixed4(saturate(c), 1);
                }

                clip(_Progress - i.uv.x);
                if (_DashCount > 0.5)
                    clip(0.55 - frac(i.uv.x * _DashCount));

                float across = abs(i.uv.y * 2.0 - 1.0);       // 0 at the center, 1 at the edges
                if (across > 1.0 - _Border)
                    return fixed4(0.06, 0.05, 0.04, 1);      // dark edge: reads on any layer
                float k = 1.0 - across / max(1.0 - _Border, 0.01);
                float3 col = _Color.rgb * (0.78 + 0.32 * k) * pulse;
                return fixed4(saturate(col), 1);
            }
            ENDCG
        }
    }
}
