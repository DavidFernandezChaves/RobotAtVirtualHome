using ROSUnityCore.ROSBridgeLib.sensor_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System.Collections;
using ROSUnityCore;
using UnityEngine;
using RobotAtVirtualHome;
using System;
using System.Globalization;

public class SmartCamera : MonoBehaviour
{

    public enum ImageType{RGB,Depth,InstanceMask }
    public enum PrecisionMode {Fast, Accurate}

    [Header("General")]
    [Tooltip("The log level to use")]
    public LogLevel LogLevel = LogLevel.Normal;
    
    [Tooltip("Size of images to be captured")]
    public Vector2Int imageSize;

    [Tooltip("With the fast method the precision per pixel is 8 bits while with the accurate mode each pixel is represented by 16 bits")]
    public PrecisionMode depthAccuracy;

    [Tooltip("Layers that the cameras will be able to see")]
    public LayerMask layerMask;

    [Header("ROS")]
    public bool sendImagesToROS;
    [Tooltip("Frequency at which a new image is sent to ROS in Hz")]
    [Range(0.1f,5)]
    public float ROSFrecuency = 1;    

    public Action<ImageType, Texture2D> OnNewImageTaken;

    private RenderTexture renderTexture;

    private Camera cameraRgb;
    private Camera cameraDepth;
    private Camera cameraMask;
    private EnvironmentManager virtualEnvironment;
    private Rect rect;
    private Texture2D img_rgb;
    private Texture2D img_depth;

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
        img_rgb = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
        img_depth = new Texture2D(imageSize.x, imageSize.y, TextureFormat.R16, false);
    }
    #endregion

    #region Public Functions
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
        switch (type)
        {
            case ImageType.RGB:
                
                cameraRgb.targetTexture = renderTexture;
                cameraRgb.Render();
                RenderTexture.active = renderTexture;
                img_rgb.ReadPixels(rect, 0, 0);
                cameraRgb.targetTexture = null;
                img_rgb.Apply();
                OnNewImageTaken?.Invoke(type, img_rgb);
                return img_rgb;

            case ImageType.Depth:
                switch (depthAccuracy)
                {
                    case PrecisionMode.Accurate:
                        float distance;
                        for (int hPx = 0; hPx < imageSize.x; hPx++)
                        {
                            for (int vPx = 0; vPx < imageSize.y; vPx++)
                            { 

                                if (Physics.Raycast(cameraDepth.ScreenPointToRay(new Vector3(hPx, vPx, 0)), out RaycastHit raycastHit, cameraDepth.farClipPlane, layerMask))
                                {
                                    distance = raycastHit.distance / cameraDepth.farClipPlane;
                                    img_depth.SetPixel(hPx, vPx, new Color(distance, distance, distance, 1f));
                                }
                            }
                        }
                        break;

                    case PrecisionMode.Fast:
                        cameraDepth.targetTexture = renderTexture;
                        cameraDepth.Render();
                        RenderTexture.active = renderTexture;
                        img_depth.ReadPixels(rect, 0, 0);
                        cameraDepth.targetTexture = null;
                        img_depth.Apply();
                        break;
                }

                OnNewImageTaken?.Invoke(type, img_depth);
                return img_depth;

            case ImageType.InstanceMask:
                cameraMask.targetTexture = renderTexture;
                cameraMask.Render();
                RenderTexture.active = renderTexture;
                img_rgb.ReadPixels(rect, 0, 0);
                cameraMask.targetTexture = null;
                img_rgb.Apply();
                OnNewImageTaken?.Invoke(type, img_rgb);
                return img_rgb;
        }

        return null;
    }

    public string GetTransformString()
    {
        return ((double)transform.position.x).ToString("F15", CultureInfo.InvariantCulture) + "," +
                ((double)transform.position.y).ToString("F15", CultureInfo.InvariantCulture) + "," +
                ((double)transform.position.z).ToString("F15", CultureInfo.InvariantCulture) + "," +
                ((double)transform.rotation.eulerAngles.x).ToString("F15", CultureInfo.InvariantCulture) + "," +
                ((double)transform.rotation.eulerAngles.y).ToString("F15", CultureInfo.InvariantCulture) + "," +
                ((double)transform.rotation.eulerAngles.z).ToString("F15", CultureInfo.InvariantCulture);
    }

    #endregion

    #region Private Functions
    IEnumerator SendImages(ROS ros) {
        HeaderMsg _head;
        Log("Sending images to ros.", LogLevel.Developer);
        while (Application.isPlaying)
        {            
            if (ros.IsConnected())
            {                
                _head = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), transform.name);
                ros.Publish(CameraRGB_pub.GetMessageTopic(), new CompressedImageMsg(_head, "jpeg", CaptureImage(ImageType.RGB).EncodeToJPG()));
                yield return null;
                ros.Publish(CameraDepth_pub.GetMessageTopic(), new CompressedImageMsg(_head, "png", CaptureImage(ImageType.Depth).EncodeToPNG()));
            }
            yield return new WaitForSeconds(1/ROSFrecuency);
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
