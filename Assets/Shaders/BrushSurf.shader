Shader "Custom/BrushSurf" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_FilterTex ("Filter", 2D) = "gray" {}
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
	    CGPROGRAM
	    #pragma surface surf Lambert
	    struct Input {
			float2 uv_MainTex;
		};

		sampler2D _MainTex;
		sampler2D _FilterTex;
		
		void surf (Input IN, inout SurfaceOutput o) {
	    		    	
	    	float2 coord = IN.uv_MainTex;
	    	float4 c = tex2D(_MainTex, coord); 
	                
	        coord = float2( coord.x, 1 - coord.y );
	        float4 filterColor = tex2D(_FilterTex, coord);
	   
	        o.Albedo = lerp(c.rgb, tex2D(_MainTex, filterColor.rg).rgb, 1);
	    }
	    ENDCG
	}
	Fallback "Diffuse"
}