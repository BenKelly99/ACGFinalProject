using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR;

public class DemoSceneRecorder : MonoBehaviour
{
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject body;
    public int framesToRecord = 100;

    [HideInInspector]
    public List<FrameData> rightFrameData;
    [HideInInspector]
    public List<FrameData> leftFrameData;
    [HideInInspector]
    public bool leftInvert;
    [HideInInspector]
    public bool rightInvert;
    

    private void Start()
    {
        rightFrameData = new List<FrameData>();
        leftFrameData = new List<FrameData>();
        leftInvert = false;
        rightInvert = false;
    }

    void FixedUpdate()
    {
        FrameData rightFd = new FrameData();
        rightFd.position = rightHand.transform.position;
        FrameData leftFd = new FrameData();
        leftFd.position = leftHand.transform.position;
        rightFrameData.Add(rightFd);
        leftFrameData.Add(leftFd);
        if (rightFrameData.Count > framesToRecord)
        {
            rightFrameData.RemoveAt(0);
            leftFrameData.RemoveAt(0);
        }
        float left_dist_begin = (leftFrameData[0].position - body.transform.position).magnitude;
        float left_dist_end = (leftFrameData[leftFrameData.Count - 1].position - body.transform.position).magnitude;
        if (left_dist_end < left_dist_begin)
        {
            leftInvert = true;
        }
        else
        {
            leftInvert = false;
        }
        float right_dist_begin = (rightFrameData[0].position - body.transform.position).magnitude;
        float right_dist_end = (rightFrameData[rightFrameData.Count - 1].position - body.transform.position).magnitude;
        if (right_dist_end < right_dist_begin)
        {
            rightInvert = true;
        }
        else
        {
            rightInvert = false;
        }
    }
}
