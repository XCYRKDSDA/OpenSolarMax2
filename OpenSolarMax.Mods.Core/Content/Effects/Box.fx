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

uniform float2 origin; // 左下角
uniform float2 size; // 尺寸
uniform float thickness;

uniform float4x4 to_ndc;


/*************************************
 * 顶点着色器
 *************************************/

struct VertexInput
{
    float4 vertex : POSITION;
    float4 color : COLOR0;
};

struct VertexOutput
{
    float4 vertex_in_ndc : SV_POSITION;
    float4 coord : TEXCOORD0;
    float4 color : COLOR0;
};

VertexOutput vs_main(VertexInput v)
{
    VertexOutput o;
    
    o.vertex_in_ndc = mul(v.vertex, to_ndc);
    o.color = v.color;
    
    o.coord = v.vertex;
    
    return o;
}


/*************************************
 * 像素着色器
 *************************************/

struct PixelInput
{
    float4 coord : TEXCOORD0;
    float4 color : COLOR0;
};

float aastep(float threshold, float value)
{
    float epsilon = length(float2(ddx(value), ddy(value))) * 0.70710678118654757;
    //float epsilon = fwidth(value) / 2;
    return smoothstep(threshold - epsilon, threshold + epsilon, value);
}

float inside(float min, float max, float value)
{
    return clamp(aastep(min, value) - aastep(max, value), 0, 1);
}

float4 ps_main(PixelInput p) : SV_TARGET
{
    float half_thickness = thickness / 2;
    
    float flag = inside(origin.x - half_thickness, origin.x + size.x + half_thickness, p.coord.x)
                 * inside(origin.y - half_thickness, origin.y + size.y + half_thickness, p.coord.y)
                 - inside(origin.x + half_thickness, origin.x + size.x - half_thickness, p.coord.x)
                   * inside(origin.y + half_thickness, origin.y + size.y - half_thickness, p.coord.y);
    
    return p.color * flag;
}


/*************************************
 * 效果定义
 *************************************/

technique Circle
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL vs_main();
        PixelShader = compile PS_SHADERMODEL ps_main();
    }
}