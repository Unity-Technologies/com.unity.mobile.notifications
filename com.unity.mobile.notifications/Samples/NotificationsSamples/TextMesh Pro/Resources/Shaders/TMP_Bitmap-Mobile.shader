Shader "TextMeshPro/Mobile/Bitmap" {

Properties {
    _MainTex        ("Font Atlas", 2D) = "white" {}
    _Color          ("Text Color", Color) = (1,1,1,1)
    _DiffusePower   ("Diffuse Power", Range(1.0,4.0)) = 1.0

    _VertexOffsetX("Vertex OffsetX", float) = 0
    _VertexOffsetY("Vertex OffsetY", float) = 0
    _MaskSoftnessX("Mask SoftnessX", float) = 0
    _MaskSoftnessY("Mask SoftnessY", float) = 0

    _ClipRect("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

    _StencilComp("Stencil Comparison", Float) = 8
    _Stencil("Stencil ID", Float) = 0
    _StencilOp("Stencil Operation", Float) = 0
    _StencilWriteMask("Stencil Write Mask", Float) = 255
    _StencilReadMask("Stencil Read Mask", Float) = 255

    _ColorMask("Color Mask", Float) = 15
}

SubShader {

    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

    Stencil
    {
        Ref[_Stencil]
        Comp[_StencilComp]
        Pass[_StencilOp]
        ReadMask[_StencilReadMask]
        WriteMask[_StencilWriteMask]
    }


    Cull Off
    ZTest [unity_GUIZTestMode]
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha
    ColorMask[_ColorMask]

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest

        #pragma multi_compile __ UNITY_UI_CLIP_RECT
        #pragma multi_compile __ UNITY_UI_ALPHACLIP


        #include "UnityCG.cginc"

        struct appdata_t {
            float4 vertex : POSITION;
            fixed4 color : COLOR;
            float2 texcoord0 : TEXCOORD0;
            float2 texcoord1 : TEXCOORD1;
        };

        struct v2f {
            float4 vertex       : POSITION;
            fixed4 color        : COLOR;
            float2 texcoord0    : TEXCOORD0;
            float4 mask         : TEXCOORD2;
        };

        sampler2D   _MainTex;
        fixed4      _Color;
        float       _DiffusePower;

        uniform float       _VertexOffsetX;
        uniform float       _VertexOffsetY;
        uniform float4      _ClipRect;
        uniform float       _MaskSoftnessX;
        uniform float       _MaskSoftnessY;

        v2f vert (appdata_t v)
        {
            v2f OUT;
            float4 vert = v.vertex;
            vert.x += _VertexOffsetX;
            vert.y += _VertexOffsetY;

            vert.xy += (vert.w * 0.5) / _ScreenParams.xy;

            OUT.vertex = UnityPixelSnap(UnityObjectToClipPos(vert));
            OUT.color = v.color;
            OUT.color *= _Color;
            OUT.color.rgb *= _DiffusePower;
            OUT.texcoord0 = v.texcoord0;

            float2 pixelSize = OUT.vertex.w;
            //pixelSize /= abs(float2(_ScreenParams.x * UNITY_MATRIX_P[0][0], _ScreenParams.y * UNITY_MATRIX_P[1][1]));

            // Clamp _ClipRect to 16bit.
            float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
            OUT.mask = float4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));

            return OUT;
        }

        fixed4 frag (v2f IN) : COLOR
        {
            fixed4 color = fixed4(IN.color.rgb, IN.color.a * tex2D(_MainTex, IN.texcoord0).a);

            // Alternative implementation to UnityGet2DClipping with support for softness.
            #if UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color *= m.x * m.y;
            #endif

            #if UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
            #endif

            return color;
        }
        ENDCG
    }
}

SubShader {
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
    Cull Off ZTest Always ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #include "UnityCG.cginc"

        // uniforms
        float4 _MainTex_ST;

        // vertex shader input data
        struct appdata {
          float3 pos : POSITION;
          half4 color : COLOR;
          float3 uv0 : TEXCOORD0;
          UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // vertex-to-fragment interpolators
        struct v2f {
          fixed4 color : COLOR0;
          float2 uv0 : TEXCOORD0;
          float4 pos : SV_POSITION;
          UNITY_VERTEX_OUTPUT_STEREO
        };

        // vertex shader
        v2f vert (appdata IN) {
          v2f o;
          UNITY_SETUP_INSTANCE_ID(IN);
          UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
          half4 color = IN.color;
          half3 viewDir = 0.0;
          o.color = saturate(color);
          // compute texture coordinates
          o.uv0 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
          // transform position
          o.pos = UnityObjectToClipPos(IN.pos);
          return o;
        }

        // textures
        sampler2D _MainTex;
        fixed4 _Color;

        // fragment shader
        fixed4 frag (v2f IN) : SV_Target {
          fixed4 col;
          fixed4 tex, tmp0, tmp1, tmp2;
          // SetTexture #0
          tex = tex2D (_MainTex, IN.uv0.xy);
          col.rgb = _Color * IN.color;
          col.a = _Color.a * tex.a;
          return col;
        }

        // texenvs
        //! TexEnv0: 01020103 01060105 [_MainTex] [_Color]
        ENDCG
    }
}

CustomEditor "TMPro.EditorUtilities.TMP_BitmapShaderGUI"
}
