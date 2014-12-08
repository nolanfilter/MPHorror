Shader "Custom/Negative" {
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
			uniform float _NegativeAmount;

			float4 frag (v2f_img i) : COLOR {
                
                float4 c = tex2D(_MainTex, i.uv);
       
	            float red = _NegativeAmount - c.r;
	            float green = _NegativeAmount - c.g;
	            float blue = _NegativeAmount - c.b;
	            
	            if( red < 0 )
	            	red = -red ;
	            if( green < 0 )
	            	green = -green ;
	            if( blue < 0 )
	            	blue = -blue ;
 
                c.rgb = float3( red, green, blue );
                
                return c;
            }
        ENDCG 
    }
}

Fallback off

}
