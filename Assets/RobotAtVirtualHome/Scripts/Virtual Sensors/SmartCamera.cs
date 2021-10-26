using ROSUnityCore.ROSBridgeLib.sensor_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System.Collections;
using ROSUnityCore;
using UnityEngine;
using RobotAtVirtualHome;
using System;

public class SmartCamera : MonoBehaviour
{

    public enum DepthType { Approximate, Precise  }

    public enum ImageType{RGB,Depth,InstanceMask }

    [Header("General")]
    [Tooltip("The log level to use")]
    public LogLevel LogLevel = LogLevel.Normal;
    
    [Tooltip("Size of images to be captured")]
    public Vector2Int imageSize;

    [Tooltip("The approximate method uses shaders to calculate depth, but is limited in accuracy. The precise method is more expensive to calculate but uses raytracing to obtain the exact depth.")]
    public DepthType DepthMethod;

    [Tooltip("Layers that the cameras will be able to see")]
    public LayerMask layerMask;

    [Header("ROS")]
    public bool sendImagesToROS;
    [Range(0.1f,10)]
    public float ROSFrecuency = 1;

    public Action<ImageType, Texture2D> OnNewImageTaken;

    private RenderTexture renderTexture;

    private Camera cameraRgb;
    private Camera cameraDepth;
    private Camera cameraMask;
    private EnvironmentManager virtualEnvironment;
    private Rect rect;
    private Texture2D img;

    #region Unity Functions
    private void Awake() {
        virtualEnvironment = FindObjectOfType<EnvironmentManager>();
        cameraRgb = transform.Find("CameraRGB").GetComponent<Camera>();
        cameraDepth = transform.Find("CameraD").GetComponent<Camera>();
        cameraMask = transform.Find("CameraMaskInstance").GetComponent<Camera>();

        cameraRgb.cullingMask = layerMask;
        cameraDepth.cullingMask = layerMask;
        cameraMask.cullingMask = layerMask;

        Log("Sensor size: " + cameraRgb.sensorSize.ToString() + "/" +
            "Field of View: " + cameraRgb.fieldOfView.ToString() + "/" +
            "Image Size: " + imageSize.ToString() + "/" +
            "FoalLength: " + cameraRgb.focalLength.ToString() + "/" + 
            "LensShift: " + cameraRgb.lensShift + "/"+
            "Fx: " + cameraRgb.focalLength * (imageSize.x / cameraRgb.sensorSize.x) + "/"+
            "Fy: " + cameraRgb.focalLength * (imageSize.y / cameraRgb.sensorSize.y),LogLevel.Developer);

        cameraDepth.depthTextureMode = DepthTextureMode.Depth;
        rect = new Rect(0, 0, imageSize.x, imageSize.y);
        renderTexture = new RenderTexture(imageSize.x, imageSize.y, 24);
        img = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
    }

    public void Connected(ROS ros) {
        if (sendImagesToROS) {
            ros.RegisterPubPackage("CameraRGB_pub");
            ros.RegisterPubPackage("CameraDepth_pub");
            StartCoroutine(SendImages(ros));
        }
    }

    public void Disconnected(ROS ros) {
        if (sendImagesToROS) {
            StopCoroutine(SendImages(ros));
        }
    }

    public string GetSemanticType(Vector3 _screenPoint) {
        RaycastHit hit;
        Ray ray = cameraRgb.ScreenPointToRay(_screenPoint);
        if (Physics.Raycast(ray, out hit)) {
            return virtualEnvironment.FindObjectUPWithClass(typeof(VirtualObject),hit.transform).name;
        }
        return "None";
    }

    public Texture2D CaptureImage(ImageType type)
    {
        if(type == ImageType.Depth && DepthMethod == DepthType.Precise)
        {
            Ray ray;
            img = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
            for (int hPx = 0; hPx < imageSize.x; hPx++)
            {
                for (int vPx = 0; vPx < imageSize.y; vPx++)
                {
                    ray = cameraDepth.ScreenPointToRay(new Vector3(hPx,vPx,0));
                    if (Physics.Raycast(ray, out RaycastHit raycastHit, cameraDepth.farClipPlane, layerMask))
                    {
                        float value = raycastHit.distance / (cameraDepth.farClipPlane);
                        img.SetPixel(hPx, vPx, new Color(value, value, value));
                    }
                }

            }
        }
        else
        {
            switch (type)
            {
                case ImageType.RGB:
                    cameraRgb.targetTexture = renderTexture;
                    cameraRgb.Render();
                    break;

                case ImageType.Depth:
                    cameraDepth.targetTexture = renderTexture;
                    cameraDepth.Render();
                    break;


                case ImageType.InstanceMask:
                    cameraMask.targetTexture = renderTexture;
                    cameraMask.Render();
                    break;
            }

            RenderTexture.active = renderTexture;
            img.ReadPixels(rect, 0, 0);
            
            cameraRgb.targetTexture = null;
            cameraDepth.targetTexture = null;
            cameraMask.targetTexture = null;
        }
        img.Apply();
        OnNewImageTaken?.Invoke(type, img);
        return img;
    }

    #endregion


    #region Private Functions
    IEnumerator SendImages(ROS ros) {
        HeaderMsg _head;
        Texture2D img;
        while (Application.isPlaying)
        {            
            if (ros.IsConnected())
            {
                Log("Sending images to ros.", LogLevel.Developer);
                _head = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), transform.name);
                ros.Publish(CameraRGB_pub.GetMessageTopic(), new CompressedImageMsg(_head, "jpeg", CaptureImage(ImageType.RGB).EncodeToJPG()));
                img = null;
                
                //yield return new WaitForEndOfFrame();
                ros.Publish(CameraDepth_pub.GetMessageTopic(), new CompressedImageMsg(_head, "jpeg", CaptureImage(ImageType.Depth).EncodeToJPG()));
                img = null;
            }
            yield return new WaitForSeconds(ROSFrecuency);
        }
        yield return null;
    }

    private void Log(string _msg, LogLevel lvl, bool Warning = false)
    {
        if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
        {
            if (Warning)
            {
                Debug.LogWarning("[Smart Camera]: " + _msg);
            }
            else
            {
                Debug.Log("[Smart Camera]: " + _msg);
            }
        }
    }
    #endregion
}
