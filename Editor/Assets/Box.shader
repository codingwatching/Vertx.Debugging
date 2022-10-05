Shader "Hidden/Vertx/Box"
{
	Properties {}
	SubShader
	{
		Tags
		{
			"RenderType"="Transparent"
		}

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Offset -1, -1
		Cull Off

		Pass // 0
		{
			ZTest Greater
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "BoxShared.cginc"
			#pragma instancing_options assumeuniformscaling nolightmap nolightprobe nolodfade
			#pragma target 4.5

			fixed4 frag(v2f i) : SV_Target
			{
				i.color.a *= Z_GREATER_FADE;
				return i.color;
			}
			ENDCG
		}
		
		Pass // 1
		{
			ZTest LEqual
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "BoxShared.cginc"
			#pragma instancing_options assumeuniformscaling nolightmap nolightprobe nolodfade
			#pragma target 4.5

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
}