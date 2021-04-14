using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR;

public class HandPlayback : MonoBehaviour
{
    public GameObject leftHand, rightHand;
    public TextAsset motionFile;
    private Vector3 leftInit, rightInit;
    private List<FrameData> leftFrameData;
    private List<FrameData> rightFrameData;
    int frame = 0;
    bool playing = false;
    // Start is called before the first frame update
    
    void Start()
    {
        leftInit = new Vector3();
        rightInit = new Vector3();
        leftFrameData = new List<FrameData>();
        rightFrameData = new List<FrameData>();
        string motion_text = motionFile.text;
        string[] lines = motion_text.Split('\n');
        bool which = true;
        bool init = false;
        foreach(string line in lines)
        {
            if (line.Equals(""))
            {
                continue;
            }
            else if (line.StartsWith("left"))
            {
                which = true;
                init = true;
            }
            else if (line.StartsWith("right"))
            {
                which = false;
                init = true;
            }
            else
            {
                string[] components = line.Split(',');
                //Debug.Log(line);
                Vector3 position = new Vector3(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]));
                if (init)
                {
                    if (which)
                    {
                        leftInit = position;
                    }
                    else
                    {
                        rightInit = position;
                    }
                    init = false;
                    continue;
                }
                FrameData data = new FrameData();
                data.position = position;
                if (which)
                {
                    leftFrameData.Add(data);
                }
                else
                {
                    rightFrameData.Add(data);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playing = true;
            frame = 0;
        }
    }

    void FixedUpdate()
    {
        if (playing)
        {
            Debug.Log("attempting to play");
            if (frame >= leftFrameData.Count)
            {
                playing = false;

            }
            else {
                leftHand.transform.position = leftInit + leftFrameData[frame].position;
                rightHand.transform.position = rightInit + rightFrameData[frame].position;
                frame++;
            }
        }
    }
}
