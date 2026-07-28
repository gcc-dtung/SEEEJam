Shader "UI/Circle Cutout"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Float) = 0
        _Softness ("Softness", Float) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1);
                float2 uv = (i.uv - _Center.xy) * aspect;
                float dist = length(uv);

                float alpha = _Softness <= 0
                    ? (dist < _Radius ? 0 : 1)
                    : smoothstep(_Radius, _Radius + _Softness, dist);

                fixed4 col = _Color * i.color;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
