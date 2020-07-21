// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "uGUI/GUI-Blurred" 
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_TexelSize ("Texel Size", Vector) = (0,0,0)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_ColorMask ("Color Mask", Float) = 15
		_DebugColor ("Debug Color", Color) = (1,1,0)

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
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;
			half2 _TexelSize;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
				
				half4 color1 = tex2D(_MainTex, IN.texcoord + half2(-_TexelSize.x,0));
				half4 color2 = tex2D(_MainTex, IN.texcoord + half2(+_TexelSize.x,0));
				half4 color3 = tex2D(_MainTex, IN.texcoord + half2(0,-_TexelSize.y));
				half4 color4 = tex2D(_MainTex, IN.texcoord + half2(0,+_TexelSize.y));
				
				color = (color1 + color2 + color3 + color4 + color) / 5;
				
				clip (color.a - 0.01);
				
				return color;
			}
		ENDCG
		}
	}
}
