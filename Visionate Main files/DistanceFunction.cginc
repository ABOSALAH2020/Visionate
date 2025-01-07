// Parameters for the SDF system
uniform float _maxDistance, _boxround, _boxSphereSmooth;
uniform float _sphereIntersectSmooth;
uniform float4 _sphere[64];
uniform float3 _SphereRotation[64];
uniform float3 _BoxRotation[64];
uniform float4 _box[64];
uniform float4 _SDFColor[128];
uniform int _SDFcounter;
uniform int _SelectedIndex;
uniform bool _IsEditingSphere;

// Sphere
//to create sphere object sd: signed distance, we need position and scale as arguments
// s: radius
float sdSphere(float3 p, float s)
{
	return length(p) - s;
}

// Box
// b: size of box in x/y/z
float sdBox(float3 p, float3 b)
{
	float3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) +
		length(max(d, 0.0));
}

// infinite plane

float sdPlane(float3 p, float4 n)
{
	// n must be normalized
	return dot(p, n.xyz) + n.w;
}

//rounded box

float sdRoundBox(in float3 p, in float3 b, in float3 r)
{
	float3 q = abs(p) - b;
	return min(max(q.x, max(q.y, q.z)), 0.0) + length(max(q, 0.0)) -r;
}

// BOOLEAN OPERATORS //

// Union
float opU(float4 d1, float4 d2)
{
	return min(d1, d2);
}

// Subtraction
float opS(float4 d1, float4 d2)
{
	return max(-d1, d2);
}

// Intersection
float opI(float4 d1, float4 d2)
{
	return max(d1, d2);
}

//smooth boolean operation

//smooth Union

float4 opUS(float4 d1, float4 d2, float k)
{
	float h = clamp(0.5 + 0.5 * (d2.w - d1.w)/k, 0.0, 1.0);
	float3 color = lerp(d2.rgb, d1.rgb, h);
	float dist = lerp(d2.w, d1.w, h) - k * h * (1.0 - h); //lerp: linear eye depth function?
    return float4(color, dist);
}

// smooth Subtraction
float4 opSS(float4 d1, float4 d2, float k)
{
	float h = clamp(0.5 - 0.5 * (d2.w + d1.w)/k, 0.0, 1.0);
	float3 color = lerp(d2.rgb, d1.rgb, h);
	float dist = lerp(d2.w, - d1.w, h) + k * h * (1.0 - h);
	return float4(color,dist);
}

// smooth Intersection
float4 opIS(float4 d1, float4 d2, float k)
{
	float h = clamp(0.5 - 0.5 * (d2.w - d1.w)/k, 0.0, 1.0);
	float3 color = lerp(d2.rgb, d1.rgb, h);
	float dist = lerp(d2.w, d1.w, h) + k * h * (1.0 - h);
	return float4(color,dist);
}

// Mod Position Axis to repeat the distance field along the axis
float pMod1 (inout float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p+halfsize)/size);
	p = fmod(p+halfsize,size)-halfsize;
	p = fmod(-p+halfsize,size)-halfsize;
	return c;
}
float3 RotatePoint(float3 p, float3 rotation)
{
	//to rotate the sdf in x,y and/or z axis
    float3x3 rotX = float3x3(
        1, 0, 0,
        0, cos(rotation.x), -sin(rotation.x),
        0, sin(rotation.x), cos(rotation.x)
    );

    float3x3 rotY = float3x3(
        cos(rotation.y), 0, sin(rotation.y),
        0, 1, 0,
        -sin(rotation.y), 0, cos(rotation.y)
    );

    float3x3 rotZ = float3x3(
        cos(rotation.z), -sin(rotation.z), 0,
        sin(rotation.z), cos(rotation.z), 0,
        0, 0, 1
    );

    return mul(rotZ, mul(rotY, mul(rotX, p)));
}
float4 distanceField(float3 p){
                
	//float modX = pMod1(p.x, modInterval.x); //to duplicate till the max distance

	//float modY = pMod1(p.y, modInterval.y); //to duplicate till the max distance

	//float modZ = pMod1(p.z, modInterval.z); //to duplicate till the max distance

	//setting ground

	//float _ground = sdPlane(p, float4(0,1,0,0)); //for float4 (x,y,z,offset)
	//float _BoxSphere1 = BoxSphere(p);
	
		float4 closest = float4(0, 0, 0, _maxDistance); // Default large distance
	
		// Iterate through the SDFs
		for (int i = 0; i < _SDFcounter; i++) {
			// Calculate the sphere distance and its rotation
			float3 rotatedSpherePoint = RotatePoint(p - _sphere[i].xyz, _SphereRotation[i]);
			float sphereDist = sdSphere(rotatedSpherePoint, _sphere[i].w);

	
			// Calculate the box distance and its rotation
			float3 rotatedBoxPoint = RotatePoint(p - _box[i].xyz, _BoxRotation[i]);
			float boxDist = sdBox(rotatedBoxPoint, _box[i].w);
	
			// Get the corresponding color indices
			int sphereColorIndex = i * 2;        // Spheres get even indices
			int boxColorIndex = i * 2 + 1;       // Boxes get odd indices

			// Determine which shape is selected
			bool isSelectedSphere = _IsEditingSphere && (i == _SelectedIndex);
			bool isSelectedBox = !_IsEditingSphere && (i == _SelectedIndex);
	
			 // Add a glow effect for the selected shape
			float glowFactor = isSelectedSphere ? 0.2 : (isSelectedBox ? 0.2 : 0.0);

			// Assign color with glow for spheres
			float4 sphereColorDist = float4(_SDFColor[sphereColorIndex].rgb - glowFactor, sphereDist);

			// Assign color with glow for boxes
			float4 boxColorDist = float4(_SDFColor[boxColorIndex].rgb - glowFactor, boxDist);
	
			// Combine sphere and box distances using the blending function
			float4 combined = opUS(sphereColorDist, boxColorDist, _boxSphereSmooth);
	
			// Keep the closest result
			closest = (combined.w < closest.w) ? combined : closest;
		}
	
		return closest;
}

