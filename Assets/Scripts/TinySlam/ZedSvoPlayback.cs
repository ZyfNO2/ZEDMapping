using UnityEngine;
using sl;
using System.Runtime.InteropServices;

public class ZedSvoPlayback : MonoBehaviour
{
    [Header("SVO Playback Settings")]
    [Tooltip("Input type for the ZED camera")]
    public sl.INPUT_TYPE inputType = sl.INPUT_TYPE.INPUT_TYPE_SVO;
    
    [Tooltip("Path to the SVO file")]
    public string svoPath = "";
    
    [Tooltip("Loop playback")]
    public bool loop = true;
    
    [Tooltip("Playback speed (1.0f = normal speed)")]
    public float playbackSpeed = 1.0f;
    
    [Header("Camera Settings")]
    [Tooltip("Resolution for the ZED camera")]
    public sl.RESOLUTION resolution = sl.RESOLUTION.HD720;
    
    [Tooltip("Depth mode for the ZED camera")]
    public sl.DEPTH_MODE depthMode = sl.DEPTH_MODE.NEURAL;
    
    // ZED camera instance
    private sl.ZEDCamera zedCamera;
    
    // Current frame and total frames
    private int currentFrame = 0;
    private int totalFrames = 0;
    
    // Playback state
    private bool isPlaying = false;
    private float lastFrameTime = 0.0f;
    
    // Rendering objects
    private GameObject renderingPlane;
    private MeshRenderer planeRenderer;
    private Material planeMaterial;
    private Camera targetCamera;
    
    // Runtime parameters
    private sl.RuntimeParameters runtimeParameters;

    void Start()
    {
        // Initialize ZED camera
        zedCamera = new sl.ZEDCamera();
        
        // Set up initialization parameters
        sl.InitParameters initParams = new sl.InitParameters();
        initParams.inputType = inputType;
        initParams.pathSVO = svoPath;
        initParams.resolution = resolution;
        initParams.depthMode = depthMode;
        initParams.svoRealTimeMode = false; // Disable real-time mode for better control
        
        // Open the ZED camera
        sl.ERROR_CODE err = zedCamera.Open(ref initParams);
        if (err != sl.ERROR_CODE.SUCCESS)
        {
            Debug.LogError("Failed to open ZED camera: " + err);
            return;
        }
        
        // Get total frames in the SVO
        totalFrames = zedCamera.GetSVONumberOfFrames();
        Debug.Log("SVO loaded with " + totalFrames + " frames");
        
        // Set up runtime parameters
        runtimeParameters = new sl.RuntimeParameters();
        runtimeParameters.confidenceThreshold = 95;
        
        // Create a plane to display the SVO video
        renderingPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeRenderer = renderingPlane.GetComponent<MeshRenderer>();
        planeMaterial = new Material(Shader.Find("Unlit/Texture"));
        planeRenderer.material = planeMaterial;
        
        // Position the plane in front of the camera
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        if (targetCamera != null)
        {
            renderingPlane.transform.position = targetCamera.transform.position + targetCamera.transform.forward * 5f;
            renderingPlane.transform.rotation = targetCamera.transform.rotation;
            renderingPlane.transform.localScale = new Vector3(5f, 1f, 5f);
        }
        
        // Create left texture before starting playback
        Texture2D leftTexture = zedCamera.CreateTextureImageType(sl.VIEW.LEFT);
        if (leftTexture != null)
        {
            Debug.Log("Left texture created: " + leftTexture.width + "x" + leftTexture.height);
        }
        else
        {
            Debug.LogError("Failed to create left texture");
        }
        
        // Start playback
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying || zedCamera == null || !zedCamera.IsOpened())
        {
            return;
        }
        
        // Calculate frame time based on playback speed
        float frameTime = 1.0f / (30.0f * playbackSpeed); // Assuming 30fps base
        
        if (Time.time - lastFrameTime >= frameTime)
        {
            // Grab a new frame
            sl.ERROR_CODE err = zedCamera.Grab(ref runtimeParameters);
            if (err == sl.ERROR_CODE.SUCCESS)
            {
                // Update current frame
                currentFrame = zedCamera.GetSVOPosition();
                
                // Update textures
                zedCamera.RetrieveTextures();
                zedCamera.UpdateTextures();
                
                // Apply texture to plane
                Texture2D leftTexture = zedCamera.GetTexture(sl.ZEDCamera.TYPE_VIEW.RETRIEVE_IMAGE, (int)sl.VIEW.LEFT); // 使用正确的类型和选项
                if (leftTexture != null)
                {
                    planeMaterial.mainTexture = leftTexture;
                    Debug.Log("Texture applied: " + leftTexture.width + "x" + leftTexture.height);
                }
                else
                {
                    Debug.LogWarning("Failed to get left texture");
                    // 尝试重新创建纹理
                    leftTexture = zedCamera.CreateTextureImageType(sl.VIEW.LEFT);
                    if (leftTexture != null)
                    {
                        Debug.Log("Left texture recreated: " + leftTexture.width + "x" + leftTexture.height);
                        planeMaterial.mainTexture = leftTexture;
                    }
                }
                
                // Check for SVO loop
                if (loop && currentFrame >= totalFrames - 2)
                {
                    zedCamera.SetSVOPosition(0);
                    currentFrame = 0;
                }
                
                // Update last frame time
                lastFrameTime = Time.time;
            }
            else if (err == sl.ERROR_CODE.END_OF_SVO_FILE_REACHED)
            {
                if (loop)
                {
                    // Reset to start
                    zedCamera.SetSVOPosition(0);
                    currentFrame = 0;
                }
                else
                {
                    // Stop playback
                    isPlaying = false;
                }
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Let ZED SDK handle the rendering
        // This is a simplified approach that relies on ZED SDK's built-in rendering
        Graphics.Blit(source, destination);
    }

    void OnDestroy()
    {
        // Clean up
        if (zedCamera != null && zedCamera.IsOpened())
        {
            zedCamera.Close();
        }
        
        if (renderingPlane != null)
        {
            Destroy(renderingPlane);
        }
        
        if (planeMaterial != null)
        {
            Destroy(planeMaterial);
        }
    }
}
