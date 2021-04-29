using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class DemoSceneClassifier : MonoBehaviour
{
    public List<TextAsset> leftJabs;
    public List<TextAsset> rightJabs;
    public List<TextAsset> leftUppercuts;
    public List<TextAsset> rightUppercuts;
    public List<TextAsset> centers;
    public DemoSceneRecorder recorder;

    public Dictionary<string, bool> available_motions;
    public Dictionary<string, List<(List<FrameData>, List<FrameData>)>> motion_map;

    public GameObject firebolt_prefab;
    public float firebolt_speed = 5.0f;

    public GameObject leftGrab, rightGrab;

    public HandPlayback playback;

    enum MotionType { LEFT, RIGHT, BOTH }

    void Start()
    {
        motion_map = new Dictionary<string, List<(List<FrameData>, List<FrameData>)>>();
        available_motions = new Dictionary<string, bool>();
        available_motions["left_jab"] = true;
        available_motions["right_jab"] = true;
        available_motions["left_uppercut"] = true;
        available_motions["right_uppercut"] = true;
        available_motions["center"] = true;
        motion_map["left_jab"] = new List<(List<FrameData>, List<FrameData>)>();
        motion_map["right_jab"] = new List<(List<FrameData>, List<FrameData>)>();
        motion_map["left_uppercut"] = new List<(List<FrameData>, List<FrameData>)>();
        motion_map["right_uppercut"] = new List<(List<FrameData>, List<FrameData>)>();
        motion_map["center"] = new List<(List<FrameData>, List<FrameData>)>();

        foreach (TextAsset motionFile in leftJabs)
        {
            LoadMotion(motionFile, "left_jab");
        }
        foreach (TextAsset motionFile in rightJabs)
        {
            LoadMotion(motionFile, "right_jab");
        }
        foreach (TextAsset motionFile in leftUppercuts)
        {
            LoadMotion(motionFile, "left_uppercut");
        }
        foreach (TextAsset motionFile in rightUppercuts)
        {
            LoadMotion(motionFile, "right_uppercut");
        }
        foreach (TextAsset motionFile in centers)
        {
            LoadMotion(motionFile, "center");
        }
    }

    private void Update()
    {
        if (SteamVR_Actions._default.RecordPlayback.stateDown)
        {
            List<FrameData> normalizedExample = new List<FrameData>();
            List<FrameData> normalizedObserved = new List<FrameData>();

            ClassifingAlgorithm.FullNormalizeMotion(recorder.rightFrameData, 50, motion_map["center"][0].Item1, 50, ref normalizedObserved, ref normalizedExample, recorder.rightInvert, false);
            playback.rightFrameData = normalizedObserved;
            playback.leftFrameData = normalizedExample;

            playback.rightInit = playback.leftInit;

            //playback.rightFrameData = new List<FrameData>(recorder.leftFrameData);
            //playback.leftFrameData = new List<FrameData>(motion_map["left_jab"][0].Item2);

            foreach (FrameData fd in recorder.rightFrameData)
            {
                Debug.Log(fd.position.x + "," + fd.position.y + "," + fd.position.z);
            }

            Debug.Log(recorder.leftInvert);

            Debug.Log("playback changed");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (CheckForMotion("left_jab", MotionType.LEFT) && available_motions["left_jab"])
        {
            Vector3 position = recorder.leftHand.transform.position;
            Vector3 velocity = (recorder.leftFrameData[recorder.leftFrameData.Count - 1].position - recorder.leftFrameData[0].position).normalized;
            CreateFirebolt(position, velocity, new Vector3(0.2f, 0.2f, 0.2f));
            available_motions["left_jab"] = false;
            StartCoroutine(EnableMotionAfterDelay("left_jab", 1.0f));
        }
        if(CheckForMotion("right_jab", MotionType.RIGHT) && available_motions["right_jab"])
        {
            Vector3 position = recorder.rightHand.transform.position;
            Vector3 velocity = (recorder.rightFrameData[recorder.rightFrameData.Count - 1].position - recorder.rightFrameData[0].position).normalized;
            CreateFirebolt(position, velocity, new Vector3(0.2f, 0.2f, 0.2f));
            available_motions["right_jab"] = false;
            StartCoroutine(EnableMotionAfterDelay("right_jab", 1.0f));
        }
        if (CheckForMotion("left_uppercut", MotionType.LEFT) && available_motions["left_uppercut"])
        {
            Vector3 position = leftGrab.transform.position;
            CreateFirebolt(position, Vector3.zero, new Vector3(0.04f, 0.04f, 0.04f), leftGrab.transform);
            available_motions["left_uppercut"] = false;
            StartCoroutine(EnableMotionAfterDelay("left_uppercut", 2.0f));
        }
        if (CheckForMotion("right_uppercut", MotionType.RIGHT) && available_motions["right_uppercut"])
        {
            Vector3 position = rightGrab.transform.position;
            CreateFirebolt(position, Vector3.zero, new Vector3(0.04f, 0.04f, 0.04f), rightGrab.transform);
            available_motions["right_uppercut"] = false;
            StartCoroutine(EnableMotionAfterDelay("right_uppercut", 2.0f));
        }
        if (CheckForMotion("center", MotionType.BOTH) && available_motions["center"])
        {
            GameObject firebolt = Instantiate(firebolt_prefab);
            firebolt.transform.position = (recorder.leftHand.transform.position + recorder.rightHand.transform.position) / 2.0f;
            Destroy(firebolt, 5.0f);
            available_motions["center"] = false;
            StartCoroutine(EnableMotionAfterDelay("center", 2.0f));
        }
    }

    void CreateFirebolt(Vector3 position, Vector3 velocity, Vector3 scale, Transform parent = null)
    {
        GameObject bolt = null;
        if (parent == null)
        {
            bolt = Instantiate(firebolt_prefab);
        }
        else
        {
            bolt = Instantiate(firebolt_prefab, parent);
        }
        bolt.transform.position = position;
        bolt.transform.localScale = scale;
        bolt.GetComponent<Rigidbody>().velocity = velocity * firebolt_speed;
        Destroy(bolt, 5.0f);
    }

    bool CheckForMotion(string motion, MotionType motType)
    {
        foreach ((List<FrameData>, List<FrameData>) mot in motion_map[motion])
        {
            if (motType == MotionType.LEFT)
            {
                if (ClassifingAlgorithm.DoesMotionMatchFrameData(recorder.leftFrameData, 50.0f, mot.Item1, 50.0f, recorder.leftInvert))
                {
                    return true;
                }
            }
            else if(motType == MotionType.RIGHT)
            {
                if (ClassifingAlgorithm.DoesMotionMatchFrameData(recorder.rightFrameData, 50.0f,  mot.Item2, 50.0f, recorder.rightInvert))
                {
                    return true;
                }
            }
            else
            {
                if (ClassifingAlgorithm.DoesMotionMatchFrameData(recorder.leftFrameData, 50.0f, mot.Item1, 50.0f, recorder.leftInvert) &&
                    ClassifingAlgorithm.DoesMotionMatchFrameData(recorder.rightFrameData, 50.0f, mot.Item2, 50.0f, recorder.rightInvert))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void LoadMotion(TextAsset motionFile, string motion_name)
    {
        List<FrameData> leftFrameData = new List<FrameData>();
        List<FrameData> rightFrameData = new List<FrameData>();
        string motion_text = motionFile.text;
        string[] lines = motion_text.Split('\n');
        bool which = true;
        bool init = false;
        foreach (string line in lines)
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
                Vector3 position = new Vector3(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]));
                if (!init)
                {
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
                else
                {
                    init = false;
                }
            }
        }
        motion_map[motion_name].Add((leftFrameData, rightFrameData));
    }

    IEnumerator EnableMotionAfterDelay(string motion_name, float delay)
    {
        yield return new WaitForSeconds(delay);
        available_motions[motion_name] = true;
    }
}
