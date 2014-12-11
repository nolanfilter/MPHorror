using UnityEngine;
using System.Collections;

// This is in fact just the Water script from Pro Standard Assets,
// just with refraction stuff removed.

[ExecuteInEditMode] // Make Portal live-update even when not in play mode
public class PortalViewController : MonoBehaviour
{
    public bool m_DisablePixelLights = true;
    public int m_TextureSize = 256;
    public float m_ClipPlaneOffset = 0.01f;
    
    public LayerMask m_ReflectLayers = -1;
        
    private Hashtable m_ReflectionCameras  = new Hashtable(); // Camera -> Camera  table
    
    private RenderTexture m_ReflectionTexture = null;
    private int m_OldReflectionTextureSize = 0;
    
    private static bool s_InsideRendering = false;
	
	public Transform SecondPortal = null;
	
    // This is called when it's known that the object will be rendered by some
    // camera. We render reflections and do other updates here.
    // Because the script executes in edit mode, reflections for the scene view
    // camera will just work!
    public void OnWillRenderObject()
    {
        if( !enabled || !renderer || !renderer.sharedMaterial || !renderer.enabled )
            return;
            
        Camera cam = Camera.current;

        if( !cam )
            return;   
              
        // Safeguard from recursive reflections.        
        if( s_InsideRendering )
            return;
        s_InsideRendering = true;
        
        Camera reflectionCamera;
        CreateMirrorObjects( cam, out reflectionCamera );
        
        // find out the reflection plane: position and normal in world space
        Vector3 normal  = transform.up;
        Vector3 right   = transform.right;
        // Optionally disable pixel lights for reflection
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if( m_DisablePixelLights )
            QualitySettings.pixelLightCount = 0;
        
        UpdateCameraModes( cam, reflectionCamera );
        
        // Render reflection
        // Reflect camera around reflection plane
        Vector4 	reflectionPlaneUp 	= new Vector4 (normal.x, normal.y, normal.z, - m_ClipPlaneOffset);
        Vector4 	reflectionPlaneRt	= new Vector4 (right.x,  right.y,  right.z,  0);
           
        Matrix4x4 	reflectionUp		= Matrix4x4.zero;
        Matrix4x4 	reflectionRt		= Matrix4x4.zero;
        Matrix4x4 	reflectionSum		= Matrix4x4.zero;          
        Quaternion 	rotate 				= transform.rotation * Quaternion.Inverse(SecondPortal.transform.rotation);
        
        CalculateReflectionMatrix(ref reflectionUp, reflectionPlaneUp);
        CalculateReflectionMatrix(ref reflectionRt, reflectionPlaneRt); 
        
        //Step1 Move to BEGIN OF COORDINATES  
        reflectionSum = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1,1,1)); 
        //Step2 reflect from Normal and Right vectors
        reflectionSum *= reflectionUp * reflectionRt;  
        //Step3 Rotate Camera on Difference Quaternion between 2 portals
        reflectionSum *= Matrix4x4.TRS(new Vector3(0,0,0), rotate, new Vector3(1,1,1));
        //Step4 Move to Other portal position         
        reflectionSum *= Matrix4x4.TRS(-SecondPortal.transform.position, Quaternion.identity, new Vector3(1,1,1));
        
        //Apply all transformations on Portal camera	
       	reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflectionSum;
		
        // Setup oblique projection matrix so that near plane is our reflection
        // plane. This way we clip everything below/above it for free.
        
        Vector4 clipPlane = CameraSpacePlane( reflectionCamera, SecondPortal.transform.position, SecondPortal.transform.up, 1.0f );
        Matrix4x4 projection = cam.projectionMatrix;
        CalculateObliqueMatrix (ref projection, clipPlane);
        reflectionCamera.projectionMatrix = projection;
        
        reflectionCamera.cullingMask = ~(1<<4) & m_ReflectLayers.value; // never render water and system layers
        reflectionCamera.targetTexture = m_ReflectionTexture;
        reflectionCamera.Render();
        
        Material[] materials = renderer.sharedMaterials;
        foreach( Material mat in materials ) {
            if( mat.HasProperty("_PortalTex") )
                mat.SetTexture( "_PortalTex", m_ReflectionTexture );
        }
        
        // Set matrix on the shader that transforms UVs from object space into screen
        // space. We want to just project reflection texture on screen.
        Matrix4x4 scaleOffset = Matrix4x4.TRS(
            new Vector3(0.5f,0.5f,0.5f), Quaternion.identity, new Vector3(0.5f,0.5f,0.5f) );
        Vector3 scale = transform.lossyScale;
        Matrix4x4 mtx = transform.localToWorldMatrix * Matrix4x4.Scale( new Vector3(1.0f/scale.x, 1.0f/scale.y, 1.0f/scale.z) );
        mtx = scaleOffset * cam.projectionMatrix * cam.worldToCameraMatrix * mtx;
        foreach( Material mat in materials ) {
            mat.SetMatrix( "_ProjMatrix", mtx );
        }
        
        // Restore pixel light count
        if( m_DisablePixelLights )
            QualitySettings.pixelLightCount = oldPixelLightCount;
        
        s_InsideRendering = false;
    }
    
    // Cleanup all the objects we possibly have created
    void OnDisable()
    {
        if( m_ReflectionTexture ) {
            DestroyImmediate( m_ReflectionTexture );
            m_ReflectionTexture = null;
        }
        foreach( DictionaryEntry kvp in m_ReflectionCameras )
            DestroyImmediate( ((Camera)kvp.Value).gameObject );
            
        m_ReflectionCameras.Clear();
    }
    
    
    private void UpdateCameraModes( Camera src, Camera dest )
    {
        if( dest == null )
            return;
        // set camera to clear the same way as current camera
        
        dest.backgroundColor = src.backgroundColor;    
		dest.clearFlags = src.clearFlags;
		if( src.clearFlags == CameraClearFlags.Skybox )
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
            if( !sky || !sky.material )
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }
        // update other values to match current camera.
        // even if we are supplying custom camera&projection matrices,
        // some of values are used elsewhere (e.g. skybox uses far plane)
        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
    }
    
    // On-demand create any objects we need
    private void CreateMirrorObjects( Camera currentCamera, out Camera reflectionCamera )
    {
        reflectionCamera = null;
        
        // Reflection render texture
        if( !m_ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize )
        {
            if( m_ReflectionTexture )
                DestroyImmediate( m_ReflectionTexture );
            m_ReflectionTexture = new RenderTexture( m_TextureSize, m_TextureSize, 16 );
            m_ReflectionTexture.name = "__PortalReflection" + GetInstanceID();
            m_ReflectionTexture.isPowerOfTwo = true;
            m_ReflectionTexture.hideFlags = HideFlags.DontSave;
            m_OldReflectionTextureSize = m_TextureSize;
        }
        
        // Camera for reflection
        reflectionCamera = m_ReflectionCameras[currentCamera] as Camera;
        if( !reflectionCamera ) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
        {
            GameObject go = new GameObject( "Portal Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox) );
            reflectionCamera = go.camera;
            reflectionCamera.enabled = false;
            reflectionCamera.transform.position = transform.position;
            reflectionCamera.transform.rotation = transform.rotation;
            reflectionCamera.gameObject.AddComponent("FlareLayer");
            go.hideFlags = HideFlags.HideAndDontSave;
            m_ReflectionCameras[currentCamera] = reflectionCamera;
        }        
    }
    
    // Extended sign: returns -1, 0 or 1 based on sign of a
    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }
    
    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint( offsetPos );
        Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
        return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
    }
    
    // Adjusts the given projection matrix so that near plane is the given clipPlane
    // clipPlane is given in camera space. See article in Game Programming Gems 5 and
    // http://aras-p.info/texts/obliqueortho.html
    private static void CalculateObliqueMatrix (ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(
            sgn(clipPlane.x),
            sgn(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot (clipPlane, q)));
        // third row = clip plane - fourth row
        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
    }

    // Calculates reflection matrix around the given plane
    private static void CalculateReflectionMatrix (ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]);
        reflectionMat.m01 = (    -2F*plane[0]*plane[1]);
        reflectionMat.m02 = (    -2F*plane[0]*plane[2]);
        reflectionMat.m03 = (    -2F*plane[3]*plane[0]);

        reflectionMat.m10 = (    -2F*plane[1]*plane[0]);
        reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]);
        reflectionMat.m12 = (    -2F*plane[1]*plane[2]);
        reflectionMat.m13 = (    -2F*plane[3]*plane[1]);
    
        reflectionMat.m20 = (    -2F*plane[2]*plane[0]);
        reflectionMat.m21 = (    -2F*plane[2]*plane[1]);
        reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]);
        reflectionMat.m23 = (    -2F*plane[3]*plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}