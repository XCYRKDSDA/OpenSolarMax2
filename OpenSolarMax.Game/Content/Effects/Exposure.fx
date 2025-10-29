#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif


/*************************************
 * 着色器全局参数
 *************************************/

Texture2D<float4> tex;
SamplerState tex_sampler;

uniform float2 center; // 屏幕空间下中心过曝位置
uniform float half_life;    // 屏幕空间下亮度的半衰期，单位为像素
uniform float amount;  // 最亮处（中心）的亮度增量

// 该 shader 无空间变换


/*************************************
 * 顶点着色器
 *************************************/

struct VertexInput
{
    float4 vertex : POSITION;
    float4 coord_in_uv : TEXCOORD0;
};

struct VertexOutput
{
    float4 vertex_in_ndc : SV_POSITION;
    float4 coord_in_uv : TEXCOORD0;
};

VertexOutput vs_main(VertexInput v)
{
    VertexOutput o;
    o.vertex_in_ndc = v.vertex;
    o.coord_in_uv = v.coord_in_uv;
    return o;
}


/*************************************
 * 像素着色器
 *************************************/

struct PixelInput
{
    float4 coord_in_screen : SV_POSITION;
    float4 coord_in_uv : TEXCOORD0;
    float4 color_in_uv : COLOR0;
};

float4 ps_main(PixelInput p) : SV_TARGET
{
    float dist = distance(p.coord_in_screen, center);
    float exposure = amount * pow(2, - dist / half_life);
    float4 tex_rgba = tex.Sample(tex_sampler, p.coord_in_uv.xy);
    tex_rgba.rgb += exposure;
    return tex_rgba;
}


/*************************************
 * 效果定义
 *************************************/

technique Exposure
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL vs_main();
        PixelShader = compile PS_SHADERMODEL ps_main();


    }
}
