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

uniform float4x4 to_ndc;


/*************************************
 * 顶点着色器
 *************************************/

struct VertexInput
{
    float4 vertex : POSITION;
    float4 coord_in_uv : TEXCOORD0;
    float4 color_in_uv : COLOR0;
};

struct VertexOutput
{
    float4 vertex_in_ndc : SV_POSITION;
    float4 coord_in_uv : TEXCOORD0;
    float4 color_in_uv : COLOR0;
};

VertexOutput vs_main(VertexInput v)
{
    VertexOutput o;

    o.vertex_in_ndc = mul(v.vertex, to_ndc);

    o.coord_in_uv = v.coord_in_uv;
    o.color_in_uv = v.color_in_uv;

    return o;
}


/*************************************
 * 像素着色器
 *************************************/

struct PixelInput
{
    float4 coord_in_uv : TEXCOORD0;
    float4 color_in_uv : COLOR0;
};

float4 ps_main(PixelInput p) : SV_TARGET
{
    float4 tex_rgba = tex.Sample(tex_sampler, p.coord_in_uv.xy);

    float color_avg = (p.color_in_uv.r + p.color_in_uv.g + p.color_in_uv.b) / 3;

    float4 mixed_rgba = tex_rgba;
    mixed_rgba.rgb *= p.color_in_uv.rgb / color_avg;
    mixed_rgba *= p.color_in_uv.a;

    return mixed_rgba;
}


/*************************************
 * 效果定义
 *************************************/

technique Tint
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL vs_main();
        PixelShader = compile PS_SHADERMODEL ps_main();
    
    
    }
}
