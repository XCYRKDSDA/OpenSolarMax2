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

uniform float2 center;
uniform float radius;
uniform float thickness;

uniform float4x4 to_ndc;

uniform bool inferior; //是否为劣角？
uniform float2 head_vector; //弧线起始角的向量
uniform float2 tail_vector; //弧线终点角的向量


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
    float4 coord_in_uv : TEXCOORD0;
    float4 coord : TEXCOORD1;
    float4 color : COLOR0;
};

VertexOutput vs_main(VertexInput v)
{
    VertexOutput o;

    o.vertex_in_ndc = mul(v.vertex, to_ndc);

    float2 vertex_vector = v.vertex.xy - center;

    //float head_vector_in_ndc = mul(float4(head_vector, 0, 0), to_ndc);
    //float tail_vector_in_ndc = mul(float4(tail_vector, 0, 0), to_ndc);

    o.coord_in_uv.x = dot(float2(-head_vector.y, head_vector.x), vertex_vector);
    o.coord_in_uv.y = dot(float2(tail_vector.y, -tail_vector.x), vertex_vector);
    o.coord_in_uv.z = o.coord_in_uv.w = 0;

    o.coord = v.vertex;
    o.color = v.color;

    return o;
}


/*************************************
 * 像素着色器
 *************************************/

struct PixelInput
{
    float4 coord_in_uv : TEXCOORD0;
    float4 coord : TEXCOORD1;
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
    float head_flag = aastep(0, p.coord_in_uv.x);
    float tail_flag = aastep(0, p.coord_in_uv.y);

    float arc_flag = inferior ? min(head_flag, tail_flag) : max(head_flag, tail_flag);

    float dist = distance(p.coord.xy, center);
    float dist_flag = aastep(radius - thickness / 2, dist)
        - aastep(radius + thickness / 2, dist);

    float flag = arc_flag * dist_flag;

    return p.color * flag;
}


/*************************************
 * 效果定义
 *************************************/

technique Ring
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL vs_main();
        PixelShader = compile PS_SHADERMODEL ps_main();
    
    
    }
}
