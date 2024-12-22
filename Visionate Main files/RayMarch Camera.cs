using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode] // to make it always update on camera
public class RayMarchCamera : SceneViewFilter
{
    [SerializeField]
    private Shader shader;
    public Material raymarchMaterial{
       
        get{
             if(!raymarchMat && shader){
            raymarchMat = new Material(shader);
             raymarchMat.hideFlags = HideFlags.HideAndDontSave; //so not to be disposed by garbage collection
             }
            return raymarchMat;
        }
    }
    private Material raymarchMat;
    public Camera camera1{
        get{
            if(!cam) {
                cam = GetComponent<Camera>();
            }
            return cam;
        }
    }
    private Camera cam;
    public float maxDistance;
    [Header("Directional Light")]
    public Transform directionLight;
    public Color LightCol;
    public float lightIntensity;

    [Header("Shadow")]
    
    [Range(0,4)]
    public float ShadowIntensity;
    public Vector2 ShadowDistance;
    [Range(1,128)]
    public float ShadowPneumbra;

    [Header("Signed Distance Field")]
    //public Color sdfColor; //to blend colors in raymarching
    public List<Color> sdfColor = new List<Color>();
    public Gradient _sdfGradient;
    public List<Vector4> spheres = new List<Vector4>();
    public List<Vector4> boxes = new List<Vector4>();
    public float boxsphereSmooth;
    public float sphereIntersectSmooth;

    public int selectedIndex = 0; // Currently selected shape index
    public bool isEditingSphere = true; // Flag to track if we're editing a sphere or a box

    //public Vector3 modInterval;
   

    //to calculate the shader based on the light element of unity

     
    
    //to communicate with the shader
    private void OnRenderImage(RenderTexture source, RenderTexture destination){
    
        if (!raymarchMaterial){ //if raymarch material hasn't been set
        
            Graphics.Blit(source, destination);
            return;
        }

        for (int i = 0; i < sdfColor.Count; i++)
        {
           float t = (float)i / Mathf.Max(1, sdfColor.Count - 1);
            sdfColor[i] = _sdfGradient.Evaluate(t);   
        }
        
        //to set values to the shader to calculate
        raymarchMaterial.SetVector("_LightDir",directionLight ? directionLight.forward : Vector3.down);
        raymarchMaterial.SetColor("_LightCol",LightCol);
        raymarchMaterial.SetFloat("_LightIntensity", lightIntensity); 
        raymarchMaterial.SetFloat("_ShadowIntensity", ShadowIntensity);
        raymarchMaterial.SetFloat("_ShadowPneumbra", ShadowPneumbra); 
        raymarchMaterial.SetVector("_ShadowDistance", ShadowDistance);
        raymarchMaterial.SetMatrix("_CamFrustum",CamFrustum(camera1));
        raymarchMaterial.SetMatrix("_CamToWorld", camera1.cameraToWorldMatrix);
        //raymarchMaterial.SetVector("CamWorldSpace", camera1.transform.position);
        raymarchMaterial.SetVectorArray("_sphere", spheres);
        raymarchMaterial.SetVectorArray("_box", boxes);
        raymarchMaterial.SetInt("_SDFcounter", Mathf.Max(spheres.Count, boxes.Count));
        raymarchMaterial.SetFloat("_maxDistance", maxDistance);
        raymarchMaterial.SetFloat("_boxSphereSmooth", boxsphereSmooth);
        raymarchMaterial.SetFloat("_sphereIntersectSmooth", sphereIntersectSmooth);

        raymarchMaterial.SetColorArray("_SDFColor", sdfColor.ToArray());
        raymarchMaterial.SetInt("_SelectedIndex", selectedIndex);
        raymarchMaterial.SetInt("_IsEditingSphere", isEditingSphere ? 1 : 0);
        //raymarchMaterial.SetInt("_SDFcounter", Mathf.Min(spheres.Count, sdfColor.Count)); // Align sizes
        //raymarchMaterial.SetVector("modInterval",modInterval);
        
        //to render the quad

        RenderTexture.active = destination;
        raymarchMaterial.SetTexture("_MainTex", source);
        GL.PushMatrix();
        GL.LoadOrtho();
        raymarchMaterial.SetPass(0);
        GL.Begin(GL.QUADS);

        //Bottom left position:

        GL.MultiTexCoord2(0,0.0f,0.0f);
        GL.Vertex3(0.0f,0.0f,3.0f); //for bottom left
       
        //Bottom Right position:

        GL.MultiTexCoord2(0,1.0f,0.0f);
        GL.Vertex3(1.0f,0.0f,2.0f); //for bottom left
        
        //Top Right position:

        GL.MultiTexCoord2(0,1.0f,1.0f);
        GL.Vertex3(1.0f,1.0f,1.0f); //for bottom left
       
        //Top left position:

        GL.MultiTexCoord2(0,0.0f,1.0f);
        GL.Vertex3(0.0f,1.0f,0.0f); //for bottom left
        GL.End();
        GL.PopMatrix();
    }

    private Matrix4x4 CamFrustum(Camera cam){
        Matrix4x4 frustum = Matrix4x4.identity; 
        //to get the tangent in the field of view
        float fov = Mathf.Tan((cam.fieldOfView * 0.5f) * Mathf.Deg2Rad);
        //to get the corners of the frustum (quad vertices)
        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;
        Vector3 TL =(-Vector3.forward - goRight + goUp); //top left point
        Vector3 TR =(-Vector3.forward + goRight + goUp); //top right point
        Vector3 BR =(-Vector3.forward + goRight - goUp); //Bottom right point
        Vector3 BL =(-Vector3.forward - goRight - goUp); //Bottom left point

        //to apply frustum factor to the matrix

        frustum.SetRow(0, TL);
        frustum.SetRow(1, TR);
        frustum.SetRow(2, BR);
        frustum.SetRow(3, BL);

        return frustum;
    }

    public float EvaluateSDF(Vector3 point)
{
    float sdf = float.MaxValue;

    // Evaluate spheres
    foreach (var sphere in spheres)
    {
        Vector3 center = new Vector3(sphere.x, sphere.y, sphere.z);
        float radius = sphere.w;
        float dist = Vector3.Distance(point, center) - radius;
        sdf = Mathf.Min(sdf, dist);
    }

    // Evaluate boxes
    foreach (var box in boxes)
    {
        Vector3 center = new Vector3(box.x, box.y, box.z);
        Vector3 halfSize = new Vector3(box.w, box.w, box.w); // Assuming uniform scale for simplicity
        Vector3 d = Vector3.Max(Vector3.zero, new Vector3(
                Mathf.Abs(point.x - center.x), 
                Mathf.Abs(point.y - center.y), 
                Mathf.Abs(point.z - center.z)) - halfSize);
        float dist = d.magnitude;
        sdf = Mathf.Min(sdf, dist);
    }

    // Return the SDF distance (add smoothing if needed)
    return sdf;
}



}
