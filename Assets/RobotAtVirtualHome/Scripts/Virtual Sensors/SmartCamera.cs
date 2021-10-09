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
    }

    public void Connected(ROS ros) {
        if (sendImagesToROS) {
            ros.RegisterPubPackage("CameraRGB_pub");
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

        Texture2D result = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);

        switch (type)
        {
            case ImageType.RGB:
                cameraRgb.targetTexture = renderTexture;
                cameraRgb.Render();
                break;
            case ImageType.Depth:
                result = new Texture2D(imageSize.x, imageSize.y, TextureFormat.Alpha8, false);
                cameraDepth.targetTexture = renderTexture;
                cameraDepth.Render();
                break;

            case ImageType.InstanceMask:
                cameraMask.targetTexture = renderTexture;
                cameraMask.Render();
                break;
        }

        RenderTexture.active = renderTexture;
        result.ReadPixels(rect, 0, 0);
        result.Apply();

        //cameraRgb.targetTexture = null;
        //cameraDepth.targetTexture = null;
        //cameraMask.targetTexture = null;

        OnNewImageTaken?.Invoke(type, result);
        return result;
    }

    #endregion


    #region Private Functions
    IEnumerator SendImages(ROS ros) {
        Texture2D rgb = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
        
        while (Application.isPlaying)
        {            
            if (ros.IsConnected())
            {
                Log("Sending images to ros.", LogLevel.Developer);
                Color32[] pxs = CaptureImage(ImageType.RGB).GetPixels32();
                Color32[] pxsDepth = CaptureImage(ImageType.Depth).GetPixels32();

                //Compose img: RGBD
                for (int i = 0; i < pxs.Length; i++)
                {
                    pxs[i].a = pxsDepth[i].a;
                }

                rgb.SetPixels32(pxs);
                rgb.Apply();

                HeaderMsg _head = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), transform.name);
                CompressedImageMsg compressedImg = new CompressedImageMsg(_head, "png", rgb.EncodeToPNG());
                ros.Publish(CameraRGB_pub.GetMessageTopic(), compressedImg);
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
