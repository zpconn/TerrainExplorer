float4x4 world; 
float4x4 view;
float4x4 proj;

texture textureMap;
sampler textureSampler = sampler_state 
{ 
	texture = <textureMap>; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter=LINEAR; 
	AddressU = mirror; 
	AddressV = mirror;
};


struct VS_INPUT
{
	float4 position : POSITION0;
	float3 normal : NORMAL;
	float2 texCoord : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 position : POSITION;    
    float4 color : COLOR0;
    float2 texCoord : TEXCOORD1;
};

VS_OUTPUT Transform(VS_INPUT input)
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	
	float4x4 viewProj = mul(view, proj);
	float4x4 worldViewProj = mul(world, viewProj);
	
	output.position = mul(input.position, worldViewProj);
	output.texCoord = input.texCoord;
	
	return output;
}

float4 PixelShader(VS_OUTPUT input) : COLOR0
{
	return tex2D(textureSampler, input.texCoord);
}

technique SkyboxDraw
{
	pass P0
	{   
		VertexShader = compile vs_1_1 Transform();
		PixelShader  = compile ps_1_1 PixelShader();
	}
}