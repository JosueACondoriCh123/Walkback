// Written with Claude Code (AI), directed and tested by team MARSXR
// The curtain for the change between the room (passthrough) and Mars: a dark shell around the
// head whose opacity goes 0 -> 1 -> 0, so the world never changes in one frame.
// Unlike every other Walkback shader it DOES write alpha, on purpose: at _Alpha = 1 the room
// must be fully hidden behind it, at _Alpha = 0 it leaves color and alpha untouched.
// Drawn last and over everything (Overlay queue, no depth test).
Shader "Walkback/Fade"
{
    Properties
    {
        _Color ("Color", Color) = (0.02, 0.02, 0.025, 1)
        _Alpha ("Opacity", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_Color.rgb, saturate(_Alpha));
            }
            ENDCG
        }
    }
}
