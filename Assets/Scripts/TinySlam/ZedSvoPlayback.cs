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
    
    [Header("Color Correction")]
    [Tooltip("0: No correction, 1: Swap R&G, 2: Swap R&B, 3: Swap G&B")]
    public int colorCorrectMode = 2;
    
    [Header("UI Display")]
    [Tooltip("UI Image component to display the SVO video")]
    public UnityEngine.UI.Image displayImage;
    
    [Tooltip("UI Image component to display the right eye video")]
    public UnityEngine.UI.Image rightDisplayImage;
    
    // ZED camera instance
    private sl.ZEDCamera zedCamera;
    
    // Current frame and total frames
    private int currentFrame = 0;
    private int totalFrames = 0;
    
    // Playback state
    private bool isPlaying = false;
    private float lastFrameTime = 0.0f;
    
    // Rendering objects
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
        
        // Check if displayImage is assigned
        if (displayImage != null)
        {
            Debug.Log("UI Image assigned: " + displayImage.name);
        }
        else
        {
            Debug.LogWarning("No UI Image assigned. Please assign an Image component in the Inspector.");
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
        
        // Create right texture before starting playback
        Texture2D rightTexture = zedCamera.CreateTextureImageType(sl.VIEW.RIGHT);
        if (rightTexture != null)
        {
            Debug.Log("Right texture created: " + rightTexture.width + "x" + rightTexture.height);
        }
        else
        {
            Debug.LogError("Failed to create right texture");
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
                
                // Apply texture to UI Image
                Texture2D leftTexture = zedCamera.GetTexture(sl.ZEDCamera.TYPE_VIEW.RETRIEVE_IMAGE, (int)sl.VIEW.LEFT); // 使用正确的类型和选项
                if (leftTexture != null && displayImage != null)
                {
                    Debug.Log("Left texture obtained: " + leftTexture.width + "x" + leftTexture.height + ", format: " + leftTexture.format);
                    
                    // Create sprite directly from the texture
                    displayImage.sprite = Sprite.Create(leftTexture, new UnityEngine.Rect(0, 0, leftTexture.width, leftTexture.height), new Vector2(0.5f, 0.5f));
                    Debug.Log("Left sprite created and applied to UI Image");
                }
                else if (leftTexture != null)
                {
                    Debug.Log("Left texture obtained but no UI Image assigned: " + leftTexture.width + "x" + leftTexture.height);
                }
                else
                {
                    Debug.LogWarning("Failed to get left texture");
                    // 尝试重新创建纹理
                    leftTexture = zedCamera.CreateTextureImageType(sl.VIEW.LEFT);
                    if (leftTexture != null && displayImage != null)
                    {
                        Debug.Log("Left texture recreated: " + leftTexture.width + "x" + leftTexture.height);
                        displayImage.sprite = Sprite.Create(leftTexture, new UnityEngine.Rect(0, 0, leftTexture.width, leftTexture.height), new Vector2(0.5f, 0.5f));
                    }
                }
                
                // Apply right texture to UI Image
                Texture2D rightTexture = zedCamera.GetTexture(sl.ZEDCamera.TYPE_VIEW.RETRIEVE_IMAGE, (int)sl.VIEW.RIGHT); // 使用正确的类型和选项
                if (rightTexture != null && rightDisplayImage != null)
                {
                    Debug.Log("Right texture obtained: " + rightTexture.width + "x" + rightTexture.height + ", format: " + rightTexture.format);
                    
                    // Create sprite directly from the texture
                    rightDisplayImage.sprite = Sprite.Create(rightTexture, new UnityEngine.Rect(0, 0, rightTexture.width, rightTexture.height), new Vector2(0.5f, 0.5f));
                    Debug.Log("Right sprite created and applied to UI Image");
                }
                else if (rightTexture != null)
                {
                    Debug.Log("Right texture obtained but no UI Image assigned: " + rightTexture.width + "x" + rightTexture.height);
                }
                else
                {
                    Debug.LogWarning("Failed to get right texture");
                    // 尝试重新创建纹理
                    rightTexture = zedCamera.CreateTextureImageType(sl.VIEW.RIGHT);
                    if (rightTexture != null && rightDisplayImage != null)
                    {
                        Debug.Log("Right texture recreated: " + rightTexture.width + "x" + rightTexture.height);
                        rightDisplayImage.sprite = Sprite.Create(rightTexture, new UnityEngine.Rect(0, 0, rightTexture.width, rightTexture.height), new Vector2(0.5f, 0.5f));
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
                    Debug.Log("End of SVO file reached, looping back to start");
                }
                else
                {
                    // Stop playback
                    isPlaying = false;
                    Debug.Log("End of SVO file reached, playback stopped");
                }
            }
            else
            {
                Debug.LogError("Grab error: " + err);
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
            Debug.Log("ZED camera closed");
        }
        Debug.Log("ZedSvoPlayback component destroyed");
    }
}
