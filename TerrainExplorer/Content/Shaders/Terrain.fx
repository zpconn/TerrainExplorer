float4x4 world; 
float4x4 view;
float4x4 proj;

float maxHeight;

float textureSize;

float uStart;
float uEnd;
float vStart;
float vEnd;

float normalStrength = 8.0f;

float4 lightDirection = {1, -0.7, 1, 0};

texture heightMap;
sampler heightSampler = sampler_state
{
	Texture = <heightMap>;
	
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

sampler heightPSSampler = sampler_state
{
	Texture = <heightMap>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

texture grassMap;
sampler grassSampler = sampler_state
{
	Texture = <grassMap>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Wrap;
	AddressV = Wrap;
};

texture rockMap;
sampler rockSampler = sampler_state
{
	Texture = <rockMap>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Wrap;
	AddressV = Wrap;
};

texture snowMap;
sampler snowSampler = sampler_state
{
	Texture = <snowMap>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Wrap;
	AddressV = Wrap;
};

texture normalMap;
sampler normalSampler = sampler_state
{
	Texture = <normalMap>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

// This computes a normal map using an 8-tap Sodel filter for the loaded heightmap.
float4 ComputeNormalsPS(in float2 uv : TEXCOORD0) : COLOR
{
	float texelSize = 1.0f / textureSize;
	
	// Top left
	float tl = abs(tex2D(heightPSSampler, uv + texelSize * float2(-1, 1)).x);
	
	// Left
	float l = abs(tex2D(heightPSSampler, uv + texelSize * float2(-1, 0)).x);
	
	// Bottom Left
	float bl = abs(tex2D(heightPSSampler, uv + texelSize * float2(-1, -1)).x);
	
	// Top
	float t = abs(tex2D(heightPSSampler, uv + texelSize * float2(0, 1)).x);
	
	// Bottom
	float b = abs(tex2D(heightPSSampler, uv + texelSize * float2(0, -1)).x);
	
	// Top Right
	float tr = abs(tex2D(heightPSSampler, uv + texelSize * float2(1, 1)).x);
	
	// Right
	float r = abs(tex2D(heightPSSampler, uv + texelSize * float2(1, 0)).x);
	
	// Bottom Right
	float br = abs(tex2D(heightPSSampler, uv + texelSize * float2(1, -1)).x);
	
	float dx = -tl - 2.0f * l - bl + tr + 2.0f * r + br;
	float dy = -tl - 2.0f * t - tr + bl + 2.0f * b + br;
	
	float4 normal = float4(normalize(float3(dx, 1.0f / normalStrength, dy)), 1.0f);
	
	// Convert coordinates from range (-1,1) to range (0,1)
	return normal * 0.5f + 0.5f;
}

// Looks up a height value using bilinear filtering which is not provided in the vertex shader
float4 tex2Dlod_bilinear(sampler texSam, float4 uv)
{
	float texelSize = 1.0f / textureSize;

	float4 height00 = tex2Dlod(texSam, uv);
	
	float4 height10 = tex2Dlod(texSam, uv + float4(texelSize, 0, 0, 0));
	
	float4 height01 = tex2Dlod(texSam, uv + float4(0, texelSize, 0, 0));
	
	float4 height11 = tex2Dlod(texSam, uv + float4(texelSize, texelSize, 0, 0));
	
	float2 f = frac(uv.xy * textureSize);
	
	float4 tA = lerp(height00, height10, f.x);
	float4 tB = lerp(height01, height11, f.x);
	
	return lerp(tA, tB, f.y);
}

struct VS_INPUT
{
	float4 position : POSITION0;
	float4 texCoord : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 position : POSITION;
	float4 texCoord : TEXCOORD0;
	float4 worldPos : TEXCOORD1;
	float3 textureWeights : TEXCOORD2;
};

VS_OUTPUT Transform(VS_INPUT input)
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	
	float4x4 viewProj = mul(view, proj);
	float4x4 worldViewProj = mul(world, viewProj);
	
	// We must manually compute the texture coordinates for this block of terrain by linear interpolation.
	float2 texCoord = float2(input.texCoord.x * (uEnd - uStart) + uStart, input.texCoord.y * (vEnd - vStart) + vStart);
	
	float height = tex2Dlod_bilinear(heightSampler, float4(texCoord.xy, 0, 0));
	
	// Compute the scaled height value.
	
	input.position.y += height * maxHeight;
	
	// Transform the position data through the pipeline.
	
	output.worldPos = mul(input.position, world);
	output.position = mul(input.position, worldViewProj);
	
	// Remember the global texture coordinates we computed above for use in the pixel shader.
	
	output.texCoord = float4(texCoord.xy, 0, 0);
	
	// Finally compute some texture weights used for multitexturing in the pixel shader.
	
	float3 texWeights = 0;
	
	texWeights.x = saturate(1.0f - abs(height - 0.0f) / 0.30f);
	texWeights.y = saturate(1.0f - abs(height - 0.5f) / 0.30f);
	texWeights.z = saturate(1.0f - abs(height - 1.0f) / 0.30f);
	
	float totalWeight = texWeights.x + texWeights.y + texWeights.z;
	
	texWeights /= totalWeight;
	
	output.textureWeights = texWeights;
	
	return output;
}

float4 PixelShader(in float2 texCoord : TEXCOORD0, in float3 weights : TEXCOORD2) : COLOR
{
	float4 rock = tex2D(rockSampler, texCoord * 20);
	float4 grass = tex2D(grassSampler, texCoord * 20);
	float4 snow = tex2D(snowSampler, texCoord * 20);
	
	float4 color = snow * weights.x + grass * weights.y + rock * weights.z;
	color.a = 1.0f;
	
	// Compute N dot L normal mapped lighting
	
	float4 normal = normalize(2.0f * (tex2D(normalSampler, texCoord) - 0.5f));
	
	float4 light = normalize(-lightDirection);
	
	float LdotN = dot(light, normal);
	LdotN = max(0, LdotN);
	
	color *= (0.2f + LdotN);
	color.a = 1.0f;
	
	return color;
}

technique TerrainDraw
{
	pass P0
	{
		vertexShader = compile vs_3_0 Transform();
		pixelShader = compile ps_3_0 PixelShader();
	}
}

technique ComputeNormals
{
	pass P0
	{
		pixelShader = compile ps_3_0 ComputeNormalsPS();
	}
}