Shader "Portal/View"
{
   Properties
   {
      _PortalTex ("Base (RGB)", 2D) = "white" { TexGen ObjectLinear }
      _Mask ("Culling Mask", 2D) = "white" {}
      _Cutoff ("Alpha cutoff", Range (0,1)) = 0.1
   }
   SubShader
   {
      Tags {"Queue"="Transparent"}
      Lighting Off
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      AlphaTest GEqual [_Cutoff]
      Pass
      {
         SetTexture [_Mask] {combine texture}
         SetTexture [_PortalTex] {matrix [_ProjMatrix] combine texture, previous}
      }
   }
} 