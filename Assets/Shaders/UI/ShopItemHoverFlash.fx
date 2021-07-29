﻿sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

//This is here because for some reason, using uTime doesn't automatically update when used on a UIElement? Very weird
float manualUTime;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float flash = 1 + ((sin(manualUTime) + 1) / 2);
    color.rgb = color.rgb * flash;
    return color;
}

technique Technique1
{
    pass ModdersToolkitShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}