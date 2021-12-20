using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViMantic;
using System;
using System.Globalization;

public class DepthMeasurer : MonoBehaviour
{
    public GameObject Point;
    public Camera Camera;
    public float distance = 0.0f;

    private void Update()
    {
        if (Point != null && Camera != null){
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera);
            distance = (Math.Abs(planes[4].GetDistanceToPoint(Point.transform.position)) + Camera.nearClipPlane);
        }
    }
}