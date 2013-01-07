float4x4 world; 
float4x4 view;
float4x4 proj;

float maxHeight;

float textureSize;

float terrainScale;

float timer;

float4 windDirection;
float windStrength;

float3 cameraPosition;
float startFadingInDistance;
float stopFadingInDistance;

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
	float2 heightMapCoord : TEXCOORD2;
};

VS_OUTPUT Transform(VS_INPUT input)
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	
	float4x4 viewProj = mul(view, proj);

	// We must manually compute the heightmap coordinates for this grass vertex.
	
	output.worldPos = mul(input.position, world);
	
	float terrainDimension = 2.0f * terrainScale;
	float2 heightMapCoord = float2(((output.worldPos.x + terrainDimension / 2.0f) / terrainDimension - 0.25f) * 2.0f, 
	                               ((output.worldPos.z + terrainDimension / 2.0f) / terrainDimension - 0.25f) * 2.0f);
	
	float height = tex2Dlod_bilinear(heightSampler, float4(heightMapCoord.xy, 0, 0));	
	
	// Compute the scaled height value.
	
	output.worldPos.y += height * maxHeight;
	
	// Animate this vertex if it is at the top of a quad
	
	if (input.texCoord.y <= 0.1f)
	{
		output.worldPos += windDirection * windStrength * sin((timer + output.worldPos.x + output.worldPos.z) * 3);
	}
	
	// Transform the position data through the pipeline.
	
	output.position = mul(output.worldPos, viewProj);
	
	// Propagate the texture coordinates through
	
	output.texCoord = input.texCoord;
	output.heightMapCoord = heightMapCoord;
	
	return output;
}

float4 PixelShader(in float2 texCoord : TEXCOORD0, in float4 worldPos : TEXCOORD1, in float2 heightMapCoord : TEXCOORD2) : COLOR
{
	float4 color = tex2D(grassSampler, texCoord);
	
	// Compute N dot L normal mapped lighting
	
	float4 normal = normalize(2.0f * (tex2D(normalSampler, heightMapCoord) - 0.5f));
	
	float4 light = normalize(-lightDirection);
	
	float LdotN = dot(light, normal);
	LdotN = max(0, LdotN);
	
	float oldA = color.a;
	
	color *= (0.2f + LdotN);
	
	float3 displacement = worldPos - cameraPosition;
	
	color.a = oldA * saturate(1 - (length(displacement) - stopFadingInDistance) / (startFadingInDistance - stopFadingInDistance));
	
	return color;
}

technique GrassDraw
{
	// We use a two-pass technique to render alpha blended geometry with almost-correct
    // depth sorting. The only way to make blending truly proper for alpha objects is
    // to draw everything in sorted order, but manually sorting all our billboards
    // would be very expensive. Instead, we draw our billboards in two passes.
    //
    // The first pass has alpha blending turned off, alpha testing set to only accept
    // 100% opaque pixels, and the depth buffer turned on. Because this is only
    // rendering the fully solid parts of each billboard, the depth buffer works as
    // normal to give correct sorting, but obviously only part of each billboard will
    // be rendered.
    //
    // Then in the second pass we enable alpha blending, set alpha test to only accept
    // pixels with fractional alpha values, and set the depth buffer to test against
    // the existing data but not to write new depth values. This means the translucent
    // areas of each billboard will be sorted correctly against the depth buffer
    // information that was previously written while drawing the opaque parts, although
    // there can still be sorting errors between the translucent areas of different
    // billboards.
    //
    // In practice, sorting errors between translucent pixels tend not to be too
    // noticable as long as the opaque pixels are sorted correctly, so this technique
    // often looks ok, and is much faster than trying to sort everything 100%
    // correctly. It is particularly effective for organic textures like grass and
    // trees.
    
    pass RenderOpaquePixels
    {
        VertexShader = compile vs_3_0 Transform();
        PixelShader = compile ps_3_0 PixelShader();

        AlphaBlendEnable = false;
        
        AlphaTestEnable = true;
        AlphaFunc = Equal;
        AlphaRef = 255;
        
        ZEnable = true;
        ZWriteEnable = true;

        CullMode = None;
    }
    
    pass RenderAlphaBlendedFringes
    {
        VertexShader = compile vs_3_0 Transform();
        PixelShader = compile ps_3_0 PixelShader();
        
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
        AlphaTestEnable = true;
        AlphaFunc = NotEqual;
        AlphaRef = 255;

        ZEnable = true;
        ZWriteEnable = false;

        CullMode = None;
    }
}