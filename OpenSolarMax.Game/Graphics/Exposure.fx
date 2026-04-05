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
};

struct VertexOutput
{
    float4 vertex_in_ndc : SV_POSITION;
};

VertexOutput vs_main(VertexInput v)
{
    VertexOutput o;
    o.vertex_in_ndc = v.vertex;
    return o;
}


/*************************************
 * 像素着色器
 *************************************/

struct PixelInput
{
    float4 coord_in_screen : SV_POSITION;
};

float4 ps_main(PixelInput p) : SV_TARGET
{
    float dist = distance(p.coord_in_screen, center);
    float exposure = amount * pow(2, - dist / half_life);
    return float4(exposure, exposure, exposure, 1.0);
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
