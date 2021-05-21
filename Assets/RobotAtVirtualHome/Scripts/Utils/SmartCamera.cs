using ROSUnityCore.ROSBridgeLib.sensor_msgs;
using ROSUnityCore.ROSBridgeLib.std_msgs;
using System.Collections;
using ROSUnityCore;
using UnityEngine;
using RobotAtVirtualHome;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

public class SmartCamera : MonoBehaviour
{
    public bool debug;
    public int verbose;
    public Vector2Int imageSize = new Vector2Int(640,480);
    public float ROSFrecuency = 1;
    public bool sendImagesToROS;

    public Texture2D ImageRGB { get; private set; }
    public Texture2D ImageDepth { get; private set; }
    public Texture2D imageSemanticMask { get; private set; }

    public ROS ros;
    private Camera cameraRgb;
    private Camera cameraDepth;
    private VirtualEnvironment virtualEnvironment;
    private House house;

    #region Unity Functions
    private void Awake() {
        virtualEnvironment = FindObjectOfType<VirtualEnvironment>();
        house = FindObjectOfType<House>(); 
        cameraRgb = transform.Find("CameraRGB").GetComponent<Camera>();
        cameraDepth = transform.Find("CameraD").GetComponent<Camera>();

        Log("Sensor size: " + cameraRgb.sensorSize.ToString() + "/" +
            "FoalLength: " + cameraRgb.focalLength.ToString() + "/" + 
            "LensShift: " + cameraRgb.lensShift + "/"+
            "Fx: " + cameraRgb.focalLength * (imageSize.x / cameraRgb.sensorSize.x) + "/"+
            "Fy: " + cameraRgb.focalLength * (imageSize.y / cameraRgb.sensorSize.y));


        ImageRGB = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
        ImageDepth = new Texture2D(imageSize.x, imageSize.y, TextureFormat.Alpha8, false);
        imageSemanticMask = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);
    }

    void Start() {
        if(ros == null) {
            ros = transform.root.GetComponentInChildren<ROS>();
        }
        if (ros != null && sendImagesToROS) {
            ros.RegisterPubPackage("CameraRGB_pub");
            StartCoroutine("SendImages");
        }
        StartCoroutine("UpdateImages");
    }

    void Update() {
        if (debug && Input.GetMouseButtonDown(0)) {
            Vector3 screenPoint = Input.mousePosition;
            Log("Data of " + screenPoint.ToString() + ": " + GetSemanticType(screenPoint));
            Log("Depth: " + ImageDepth.GetPixel((int)screenPoint.x, (int)screenPoint.y).ToString());
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

    public Texture2D GetImageMask() {        

        for (int i = 0; i < imageSize.x; i++) {
            for (int j = 0; j < imageSize.y; j++) {
                string name = GetSemanticType(new Vector3(i, j, 0));
                Color color = Color.black;
                if (house.semanticColors.ContainsKey(name)) {
                    color = house.semanticColors[name];
                }

                imageSemanticMask.SetPixel(i, j, color);
            }
        }
        imageSemanticMask.Apply();

        return imageSemanticMask;
    }

    #endregion


    #region Private Functions
    public IEnumerator UpdateImages() {
        while (Application.isPlaying) {
            yield return new WaitForEndOfFrame();            

            Rect rect = new Rect(0, 0, imageSize.x, imageSize.y);
            RenderTexture renderTextureRGB = new RenderTexture(imageSize.x, imageSize.y, 24);
            RenderTexture renderTextureDepth = new RenderTexture(imageSize.x, imageSize.y, 24);

            cameraRgb.targetTexture = renderTextureRGB;
            cameraRgb.Render();
            RenderTexture.active = renderTextureRGB;
            ImageRGB.ReadPixels(rect, 0, 0);
            ImageRGB.Apply();

            cameraDepth.depthTextureMode = DepthTextureMode.Depth;
            cameraDepth.targetTexture = renderTextureDepth;
            cameraDepth.Render();
            RenderTexture.active = renderTextureDepth;
            ImageDepth.ReadPixels(rect, 0, 0);
            ImageDepth.Apply();

            cameraRgb.targetTexture = null;
            cameraDepth.targetTexture = null;
            RenderTexture.active = null; //Clean
            Destroy(renderTextureRGB); //Free memory
            Destroy(renderTextureDepth); //Free memory

        }
    }

    IEnumerator SendImages() {
        while (Application.isPlaying) {
            if (ros.IsConnected()) {
                Texture2D rgb = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false);

                Color[] pxs = ImageRGB.GetPixels();
                Color[] pxsDepth = ImageDepth.GetPixels();

                //Compose img: RGBD
                for (int i = 0; i < pxs.Length; i++) {
                    pxs[i].a = pxsDepth[i].a;
                }

                rgb.SetPixels(pxs);
                rgb.Apply();

                HeaderMsg _head = new HeaderMsg(0, new TimeMsg(DateTime.Now.Second, 0), transform.name);
                CompressedImageMsg compressedImg = new CompressedImageMsg(_head, "png", rgb.EncodeToPNG());
                ros.Publish(CameraRGB_pub.GetMessageTopic(), compressedImg);
            }
            yield return new WaitForSeconds(ROSFrecuency);
        }
    }

    private void Log(string _msg) {
        if (verbose > 1)
            Debug.Log("[Smart Camera]: " + _msg);
    }

    private void LogWarning(string _msg) {
        if (verbose > 0)
            Debug.LogWarning("[Smart Camera]: " + _msg);
    }

    #endregion
}
