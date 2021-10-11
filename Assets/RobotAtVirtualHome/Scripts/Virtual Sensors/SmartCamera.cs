using ROSUnityCore.ROSBridgeLib.sensor_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System.Collections;
using ROSUnityCore;
using UnityEngine;
using RobotAtVirtualHome;
using System;

public class SmartCamera : MonoBehaviour
{
    public enum ImageType{RGB,Depth,InstanceMask }

    [Header("General")]
    [Tooltip("The log level to use")]
    public LogLevel LogLevel = LogLevel.Normal;
    
    [Tooltip("Size of images to be captured")]
    public Vector2Int imageSize;

    [Header("General")]
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
    private Texture2D img_rgb;
    private Texture2D img_d;

    #region Unity Functions
    private void Awake() {
        virtualEnvironment = FindObjectOfType<EnvironmentManager>();
        cameraRgb = transform.Find("CameraRGB").GetComponent<Camera>();
        cameraDepth = transform.Find("CameraD").GetComponent<Camera>();
        cameraMask = transform.Find("CameraMaskInstance").GetComponent<Camera>();

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
        img_d = new Texture2D(imageSize.x, imageSize.y, TextureFormat.Alpha8, false);
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
        switch (type)
        {
            case ImageType.RGB:
                cameraRgb.targetTexture = renderTexture;
                cameraRgb.Render();
                RenderTexture.active = renderTexture;
                img_rgb.ReadPixels(rect, 0, 0);
                img_rgb.Apply();
                cameraRgb.targetTexture = null;

                OnNewImageTaken?.Invoke(type, img_rgb);
                return img_rgb;
                
            case ImageType.Depth:
                cameraDepth.targetTexture = renderTexture;
                cameraDepth.Render();
                RenderTexture.active = renderTexture;
                img_d.ReadPixels(rect, 0, 0);
                img_d.Apply();
                cameraDepth.targetTexture = null;

                OnNewImageTaken?.Invoke(type, img_d);
                return img_rgb;
                

            case ImageType.InstanceMask:
                cameraMask.targetTexture = renderTexture;
                cameraMask.Render();
                RenderTexture.active = renderTexture;
                img_rgb.ReadPixels(rect, 0, 0);
                img_rgb.Apply();
                cameraMask.targetTexture = null;
                OnNewImageTaken?.Invoke(type, img_rgb);
                return img_rgb;                
        }
        return null;
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
                img = CaptureImage(ImageType.RGB);
                ros.Publish(CameraRGB_pub.GetMessageTopic(), new CompressedImageMsg(_head, "jpeg", img.EncodeToJPG()));
                img = null;
                
                yield return null;
                img = CaptureImage(ImageType.Depth);
                ros.Publish(CameraDepth_pub.GetMessageTopic(), new CompressedImageMsg(_head, "jpeg", img.EncodeToJPG()));
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
