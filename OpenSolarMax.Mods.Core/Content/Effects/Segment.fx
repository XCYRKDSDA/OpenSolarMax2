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

uniform float2 head;
uniform float2 tail;
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

float4 ps_main(PixelInput p) : SV_TARGET
{
    // 计算横向阈值
    
    float3 p2head, p2tail;
    p2head.z = p2tail.z = 0;
    p2head.xy = head - p.coord.xy;
    p2tail.xy = tail - p.coord.xy;
    
    float dist = abs(cross(p2head, p2tail).z / distance(head, tail));
    float flag_v = 1 - aastep(thickness / 2, dist);
    
    // 计算纵向阈值
    
    float2 head2tail = tail - head;
    float flag_h = aastep(0, dot(p2tail.xy, head2tail)) - aastep(0, dot(p2head.xy, head2tail));
    
    return p.color * flag_v * flag_h;
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