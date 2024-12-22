Shader "Otsem/NewImageEffectShader" //to make it appear
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "DistanceFunctions.cginc" //for signed distance functions
          

            sampler2D _MainTex;
            //uniform float4 CamWorldSpace; //to make origin relate
            uniform sampler2D _CameraDepthTexture; //to calculate the depth of camera to the mesh it'll hit
            uniform float4x4 _CamFrustum, _CamToWorld; //to relate to the space in c#
            //uniform float _maxDistance, _boxround, _boxSphereSmooth;
            //uniform float _sphereIntersectSmooth; //to make it convienent for the code
            //uniform float4 _sphere[64];
            //uniform float4 _box[64];
            //uniform float3 _modInterval;
            uniform float3 _LightDir, _LightCol;
            uniform float _LightIntensity, _ShadowIntensity, _ShadowPneumbra;
            //uniform fixed4 _SDFColor[128];
            uniform float _ColorIntensity;
            uniform float2 _ShadowDistance;
            //uniform int _SDFcounter;
            //highlighted selected sdf
            //uniform int _SelectedIndex;
            //uniform bool _IsEditingSphere;
            

            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 ray: TEXCOORD1; //to sort the ray direction
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                half index = v.vertex.z;
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.ray = _CamFrustum[(int)index].xyz;
                //to normalize the array
                o.ray /= abs(o.ray.z);
                //to convert the array from i space to world space
                o.ray = mul(_CamToWorld, o.ray);
                return o;
            }

            /*float BoxSphere(float3 p){

                  //setting a 3d sphere shader

                  float Sphere1 = sdSphere(p - _sphere.xyz, _sphere.w);

                  //setting a 3d smooth shader
  
                  float Box1 = sdRoundBox(p - _box.xyz, _box.www, _boxround);

                  float combine1 = opSS(Sphere1, Box1, _boxSphereSmooth);

                  float Sphere2 = sdSphere(p - _sphere2.xyz, _sphere2.w);

                  float combine2 = opIS(Sphere2, combine1, _sphereIntersectSmooth);
                  return combine2;


            }*/
            

            /*float4 distanceField(float3 p){
                
                //float modX = pMod1(p.x, modInterval.x); //to duplicate till the max distance

                //float modY = pMod1(p.y, modInterval.y); //to duplicate till the max distance

                //float modZ = pMod1(p.z, modInterval.z); //to duplicate till the max distance

                //setting ground

                //float _ground = sdPlane(p, float4(0,1,0,0)); //for float4 (x,y,z,offset)
                //float _BoxSphere1 = BoxSphere(p);
                
                    float4 closest = float4(0, 0, 0, _maxDistance); // Default large distance
                
                    // Iterate through the SDFs
                    for (int i = 0; i < _SDFcounter; i++) {
                        // Calculate the sphere distance
                        float sphereDist = sdSphere(p - _sphere[i].xyz, _sphere[i].w);
                
                        // Calculate the box distance
                        float boxDist = sdBox(p - _box[i].xyz, _box[i].w);
                
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
                
                
                //return opU(Sphere1, Box1);   //to confirm the ray hit and union function (Yaaay!)

                //return opS(Sphere1, Box1);    // opS: operation subtraction
                
                //return opI(Sphere1,Box1);  // opI: operation intersection



            }*/

            //to get normal to shade the sphere based on the lighting

            float3 getNormal(float3 p){

                //note: it's different than mesh and polygons is that we need to recalculate 6 more times

                const float2 offset = float2(0.01, 0.0); //the offset to make the grident of the sphere
                float3 n = float3(

                    //will take the y component 3 times for y,z axis (in this case, it'll be 0)

                    distanceField(p + offset.xyy).w - distanceField(p - offset.xyy).w,
                    distanceField(p + offset.yxy).w - distanceField(p - offset.yxy).w,
                    distanceField(p + offset.yyx).w - distanceField(p - offset.yyx).w);
                    return normalize(n);  

            }

            float hardShadow(float3 ro, float3 rd, float mint, float maxt )
            { //requires ray origin, ray direction, minimum distance of shadow, max distance of shadow

                //shadows will march from the raymarching distance field
                //if the ray hits a surface within the min and max distance, it'll be 1, if not it'll be 0

                //for loop for sphere tracing:
                for(float t = mint; t < maxt;) //loop will continue
                {
                    float h = distanceField(ro + rd * t).w; 

                    //if less than 0, then it's inside distance field object

                    if (h < 0.001) 
                    {
                        return 0.0 ; //any color multiplied by 0 will be black
                    }

                    t += h; 

                    //return 1 if ray didn't hit an object

                }  

                return 1.0 ;
            }

            float softShadow(float3 ro, float3 rd, float mint, float maxt, float k )
            { 
                //k will be the pneumbra shadow parameter for soft shadow
                float result = 1.0;
                for(float t = mint; t < maxt;) //loop will continue
                {
                    float h = distanceField(ro + rd * t).w; 

                    //instead of 0, we will return the closest point to the distance object

                    if (h < 0.001) 
                    {
                        return 0.0 ; //any color multiplied by 0 will be black
                    }

                    result = min(result, k * h/t);

                    t += h; 

                    //return 1 if ray didn't hit an object

                }  

                return result ;
            }
            
                //shading function for directional light, shadows and reflection

            float3 Shading(float3 p, float3 n){

                //directional light

                float result = (_LightCol * dot(- _LightDir, n) * 0.5 + 0.5) * _LightIntensity;

                //Shadows (hard ones)

                //float shadow = hardShadow(p, - _LightDir, _ShadowDistance.x, _ShadowDistance.y) * 0.5 + 0.5;

                //Shadows (soft ones)

                float shadow = softShadow(p, - _LightDir, _ShadowDistance.x, _ShadowDistance.y, _ShadowPneumbra) * 0.5 + 0.5;

                shadow = max(0.0, pow(shadow, _ShadowIntensity)); //to ensure the shadow is at least 0

                result *= shadow;

                return result; 

            }

                 //to create the sphere tracing function (one that keeps making spheres until it hits object)
            fixed4 raymarching(float3 ro, float3 rd, float depth,  fixed3 dColor){

                //ro: ray origin, rd: ray direction

                fixed4 result = fixed4(1,1,1,1);
                const int maxIteration = 164; //to limit the tracing, might need higher for more complex objects
                float t = 0; //distance travelled along the ray direction
                for (int i = 0; i < maxIteration ; i++){

                    if( t > _maxDistance || t >= depth){

                        //Environment

                        result = fixed4(rd,0);
                        break;

                    }

                    //for under max distance

                    float3 p = ro + rd * t; //position point

                    //check for a hit in distance field

                    float4 d = distanceField(p);
                    if (d.w < 0.01) { //hit an object

                        dColor = d.rgb; // Use color from the closest shape
                        float3 n = getNormal(p);
                        float3 shading = Shading(p, n);
                        result = fixed4(dColor * shading, 1); // Apply shading
                        break;

                    }

                    //if distance is still less than max distance and above 0.01

                    t+=d.w;

                }

                return result;
            }

            fixed4 frag (v2f i) : SV_Target
            {   //to calculate the depth of mesh to the camera
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
                depth *= length(i.ray);
                fixed3 dColor;
                
                fixed3 col = tex2D(_MainTex,i.uv);
                //to set ray direction

                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                fixed4 result = raymarching(rayOrigin,rayDirection, depth, dColor);
                return fixed4(col*(1.0 - result.w) + result.xyz * result.w, 1.0); // if field distance hit something, w will be 1    

                //to blend between the raymarch and the scene colors


                /*fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                col.rgb = 1 - col.rgb;
                return col;*/

                

            }
            ENDCG
        }
    }
}
