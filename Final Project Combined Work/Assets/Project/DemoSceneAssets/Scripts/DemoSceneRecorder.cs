using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR;

public class DemoSceneRecorder : MonoBehaviour
{
    public GameObject leftHand;
    public GameObject rightHand;
    public int framesToRecord = 100;

    public List<FrameData> globalRightFrameData;
    public List<FrameData> globalLeftFrameData;
    [HideInInspector]
    public List<FrameData> rightFrameData;
    [HideInInspector]
    public List<FrameData> leftFrameData;

    private void Start()
    {
        rightFrameData = new List<FrameData>();
        leftFrameData = new List<FrameData>();
        globalRightFrameData = new List<FrameData>();
        globalLeftFrameData = new List<FrameData>();
    }

    void FixedUpdate()
    {
        FrameData rightFd = new FrameData();
        rightFd.position = rightHand.transform.position;
        if (SteamVR_Actions._default.RecordPlayback.state)
        {
            //Debug.Log(rightHand.transform.position);
        }
        FrameData leftFd = new FrameData();
        leftFd.position = leftHand.transform.position;
        globalRightFrameData.Add(rightFd);
        globalLeftFrameData.Add(leftFd);
        if (globalRightFrameData.Count > framesToRecord)
        {
            globalRightFrameData.RemoveAt(0);
            globalLeftFrameData.RemoveAt(0);
        }

        leftFrameData = new List<FrameData>(globalLeftFrameData);
        rightFrameData = new List<FrameData>(globalRightFrameData);
    }
    private void WriteData(List<FrameData> data, StreamWriter sw, bool raw)
    {
        Vector3 firstPos = data[0].position;
        Vector3 lastPos = data[data.Count - 1].position;
        Vector3 difference = lastPos - firstPos;
        difference.y = 0;
        difference.Normalize();
        Vector3 xBasis = difference;
        Vector3 yBasis = new Vector3(0, 1, 0);
        Vector3 zBasis = Vector3.Cross(xBasis, yBasis);

        Vector4 xRow = new Vector4(xBasis.x, xBasis.y, xBasis.z, 0);
        Vector4 yRow = new Vector4(yBasis.x, yBasis.y, yBasis.z, 0);
        Vector4 zRow = new Vector4(zBasis.x, zBasis.y, zBasis.z, 0);

        Matrix4x4 changeOfBasis = new Matrix4x4();
        changeOfBasis.SetRow(0, xRow);
        changeOfBasis.SetRow(1, yRow);
        changeOfBasis.SetRow(2, zRow);
        changeOfBasis.SetRow(3, new Vector4(0, 0, 0, 1));
        sw.WriteLine(firstPos.x + "," + firstPos.y + "," + firstPos.z);
        foreach (FrameData fd in data)
        {
            Vector4 position = new Vector4(fd.position.x - firstPos.x, fd.position.y - firstPos.y, fd.position.z - firstPos.z, 1);
            Vector4 newPos = position;
            if (!raw)
            {
                newPos = changeOfBasis * position;
            }
            sw.WriteLine(newPos.x + "," + newPos.y + "," + newPos.z);
        }
    }
}
