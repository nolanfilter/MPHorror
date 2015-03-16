Shader "Custom/Grayscale" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
    
		CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform float _GrayscaleAmount;

			float4 frag (v2f_img i) : COLOR {
                
                float4 c = tex2D(_MainTex, i.uv);
       
	            c.rgb = lerp(c.rgb, Luminance(c.rgb), _GrayscaleAmount);
                
                return c;
            }
        ENDCG 
    }
}

Fallback off

}
