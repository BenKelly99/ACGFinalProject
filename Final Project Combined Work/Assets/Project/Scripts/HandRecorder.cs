using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Valve.VR;

public class HandRecorder : MonoBehaviour
{
    public GameObject leftHand, rightHand, body;
    [HideInInspector]
    public List<FrameData> leftFrameData, rightFrameData;
    public string type = "both";
    public bool record_raw = true;
    private bool recording;
    private string path;
    private int num;
    public string mot_name = "jab"; 

    // Start is called before the first frame update
    void Start()
    {
        string[] paths = new string[] { Application.dataPath, "Project", "Data", mot_name};
        path = Path.Combine(paths);
        leftFrameData = new List<FrameData>();
        rightFrameData = new List<FrameData>();
        recording = false;
        num = 0;
    }

    void Update()
    {
        if (SteamVR_Actions._default.RecordPlayback.stateDown)
        {
            recording = !recording;
            if (!recording)
            {
                SaveData();
                leftFrameData.Clear();
                rightFrameData.Clear();
                num += 1;
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (recording)
        {
            FrameData leftFrame = new FrameData();
            leftFrame.position = leftHand.transform.position;
            leftFrameData.Add(leftFrame);

            FrameData rightFrame = new FrameData();
            rightFrame.position = rightHand.transform.position;
            rightFrameData.Add(rightFrame);
        }
    }

    private void SaveData()
    {
        string final_path = Path.Combine(path, mot_name + "_" + num + ".txt");
        while (File.Exists(final_path))
        {
            num++;
            final_path = Path.Combine(path, mot_name + "_" + num + ".txt");
        }
        string final_raw_path = Path.Combine(path, mot_name + "_raw_" + num + ".txt");
        print("Saving data to " + final_path);
        using (StreamWriter sw = File.CreateText(final_path))
        {
            sw.WriteLine("left");
            WriteData(leftFrameData, sw);
            sw.WriteLine("right");
            WriteData(rightFrameData, sw);
        }
    }

    private void WriteData(List<FrameData> data, StreamWriter sw)
    {
        Vector3 firstPos = data[0].position;
        Vector3 lastPos = data[data.Count - 1].position;

        float dist_begin = (firstPos - body.transform.position).magnitude;
        float dist_end = (lastPos - body.transform.position).magnitude;
        bool invert;
        if (dist_end < dist_begin)
        {
            invert = true;
        }
        else
        {
            invert = false;
        }

        Vector3 difference = lastPos - firstPos;
        difference.y = 0;
        difference.Normalize();
        Vector3 xBasis = difference;
        if (invert)
        {
            xBasis = -xBasis;
        }
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
        Debug.Log("first: " + firstPos.x + "," + firstPos.y + "," + firstPos.z);
        Debug.Log("last: " + lastPos.x + "," + lastPos.y + "," + lastPos.z);
        /*
        sw.WriteLine(xBasis.x + "," + xBasis.y + "," + xBasis.z);
        sw.WriteLine(yBasis.x + "," + yBasis.y + "," + yBasis.z);
        sw.WriteLine(zBasis.x + "," + zBasis.y + "," + zBasis.z);
        */
        sw.WriteLine(firstPos.x + "," + firstPos.y + "," + firstPos.z);
        foreach (FrameData fd in data)
        {
            Vector4 position = new Vector4(fd.position.x - firstPos.x, fd.position.y - firstPos.y, fd.position.z - firstPos.z, 1);
            Vector4 newPos = changeOfBasis * position;
            sw.WriteLine(newPos.x + "," + newPos.y + "," + newPos.z);
        }
    }
}
