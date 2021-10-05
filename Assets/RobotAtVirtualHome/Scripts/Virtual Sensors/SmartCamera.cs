using ROSUnityCore.ROSBridgeLib.sensor_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System.Collections;
using ROSUnityCore;
using UnityEngine;
using RobotAtVirtualHome;
using System;

public class SmartCamera : MonoBehaviour
{
    [Header("General")]
    [Tooltip("The log level to use")]
    public LogLevel LogLevel = LogLevel.Normal;

    public bool mouseInteractive;
    
    [Range(0.01f, 10)]
    public float CaptureFrecuency = 0.5f;
    public Vector2Int imageSize;

    [Header("General")]
    public bool sendImagesToROS;
    [Range(0.1f,10)]
    public float ROSFrecuency = 1;    

    public Texture2D ImageRGB { get; private set; }
    public Texture2D ImageDepth { get; private set; }
    public Texture2D ImageMask { get; private set; }

    private Camera cameraRgb;
    private Camera cameraDepth;
    private Camera cameraMask;
    private EnvironmentManager virtualEnvironment;

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


        ImageRGB = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
        ImageDepth = new Texture2D(imageSize.x, imageSize.y, TextureFormat.Alpha8, false);
        ImageMask = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);

        StartCoroutine(CaptureImage());
    }

    void Update() {
        if (mouseInteractive && Input.GetMouseButtonDown(0)) {
            Vector3 screenPoint = Input.mousePosition;
            Log("Data of " + screenPoint.ToString() + ": " + GetSemanticType(screenPoint),LogLevel.Developer);
            Log("Depth: " + ImageDepth.GetPixel((int)screenPoint.x, (int)screenPoint.y).ToString(),LogLevel.Developer);
        }
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
    #endregion


    #region Private Functions
    private IEnumerator CaptureImage() {
        cameraDepth.depthTextureMode = DepthTextureMode.Depth;
        Rect rect = new Rect(0, 0, imageSize.x, imageSize.y);
        RenderTexture renderTextureRGB = new RenderTexture(imageSize.x, imageSize.y, 24);

        while (Application.isPlaying) {
            yield return new WaitForEndOfFrame();
            cameraRgb.targetTexture = renderTextureRGB;
            cameraRgb.Render();
            RenderTexture.active = renderTextureRGB;
            ImageRGB.ReadPixels(rect, 0, 0);
            ImageRGB.Apply();
            cameraRgb.targetTexture = null;

            cameraDepth.targetTexture = renderTextureRGB;
            cameraDepth.Render();
            RenderTexture.active = renderTextureRGB;
            ImageDepth.ReadPixels(rect, 0, 0);
            ImageDepth.Apply();
            cameraDepth.targetTexture = null;

            cameraMask.targetTexture = renderTextureRGB;
            cameraMask.Render();
            RenderTexture.active = renderTextureRGB;
            ImageMask.ReadPixels(rect, 0, 0);
            ImageMask.Apply();
            cameraMask.targetTexture = null;

            RenderTexture.active = null; //Clean     

            Destroy(renderTextureRGB); //Free memory
            yield return new WaitForSeconds(CaptureFrecuency);
        }
    }

    IEnumerator SendImages(ROS ros) {
        Texture2D rgb = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);     

        while (Application.isPlaying) {
            Log("Sending images to ros.", LogLevel.Normal);
            if (ros.IsConnected()) {          

                Color32[] pxs = ImageRGB.GetPixels32();
                Color32[] pxsDepth = ImageDepth.GetPixels32();

                //Compose img: RGBD
                for (int i = 0; i < pxs.Length; i++) {
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
    }

    private void Log(string _msg, LogLevel lvl, bool Warning = false)
    {
        if (LogLevel <= lvl)
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
