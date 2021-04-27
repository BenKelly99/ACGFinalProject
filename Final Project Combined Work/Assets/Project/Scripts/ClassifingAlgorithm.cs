using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassifingAlgorithm
{
    public static ComparisonAlgorithmManager CAM;

    public static void SetCAM(ComparisonAlgorithmManager CAM)
    {
        ClassifingAlgorithm.CAM = CAM;
    }
    public static List<string> GetPossibleMotions(List<FrameData> frameData, Database_Manager db_manager, bool left)
    {
        List<string> possibleMotions = new List<string>();
        foreach(string motion in db_manager.formatters.Keys)
        {
            if (IsMotionPossible(frameData, db_manager.formatters[motion], left)) {
                possibleMotions.Add(motion);
            }
        }

        return possibleMotions;
    }

    private static bool IsMotionPossible(List<FrameData> frameData, List<Database_Input_Formatter> exampleMotions, bool left)
    {
        foreach (Database_Input_Formatter exampleMotion in exampleMotions)
        {
            if (DoesMotionMatchExample(frameData, exampleMotion, left))
            {
                return true;
            }
        }
        return false;
    }

    private static bool DoesMotionMatchExample(List<FrameData> frameData, Database_Input_Formatter exampleMotion, bool left)
    {
        List<FrameData> handData = GetHandDataFromMotion(exampleMotion, left);
        
        return DoesMotionMatchFrameData(frameData, 50, handData, 120);
    }

    public static void FullNormalizeMotion(List<FrameData> observed, float observedFrameRate, List<FrameData> example, float exampleFrameRate, ref List<FrameData> observedNormal, ref List<FrameData> exampleNormal)
    {
        example = ClearDeadTime(example, exampleFrameRate);
        example = NormalizeDataForRotation(example);
        example = NormalizeDataForDistance(example);
        observed = ClearDeadTime(observed, observedFrameRate);
        observed = NormalizeDataForRotation(observed);
        observed = NormalizeDataForDistance(observed);
        NormalizeOverTime(observed, observedFrameRate, example, exampleFrameRate, ref observedNormal, ref exampleNormal);
    }

    public static bool DoesMotionMatchFrameData(List<FrameData> observed, float observedFramerate, List<FrameData> example, float exampleFrameRate)
    {
        List<FrameData> finalHandData = new List<FrameData>();
        List<FrameData> finalFrameData = new List<FrameData>();
        FullNormalizeMotion(observed, observedFramerate, example, exampleFrameRate, ref finalHandData, ref finalFrameData);
        return ValidFrameDataMotionMatch(finalHandData, finalFrameData);
    }

    private static bool ValidFrameDataMotionMatch(List<FrameData> observed, List<FrameData> example)
    {
        for (int i = observed.Count - 1; i > 0; i /= 2)
        {
            if (CheckForPartialValidity(observed, example, i))
            {
                Debug.Log("Successful at step = " + i);
            }
            else
            {
                Debug.Log("Unsuccessful at step = " + i);
            }
        }
        return false;
    }

    private static bool CheckForPartialValidity(List<FrameData> observed, List<FrameData> example, int step)
    {
        Vector3 currentObservedPos = new Vector3();
        Vector3 currentExamplePos = new Vector3();
        Vector3 previousObservedPos = observed[0].position;
        Vector3 previousExamplePos = example[0].position;
        for (int i = step; i < observed.Count; i += step)
        {
            currentObservedPos = observed[i].position;
            currentExamplePos = example[i].position;
            Vector3 observedDelta = currentObservedPos - previousObservedPos;
            Vector3 expectedDelta = currentExamplePos - previousExamplePos;
            observedDelta.Normalize();
            expectedDelta.Normalize();
            if (Vector3.Dot(observedDelta, expectedDelta) < CAM.minDotThreshold)
            {
                Debug.Log("Comparison failed due to incorrect orientation");
                Debug.Log("observed delta = " + observedDelta.x + "," + observedDelta.y + "," + observedDelta.z);
                Debug.Log("expected delta = " + expectedDelta.x + "," + expectedDelta.y + "," + expectedDelta.z);
                Debug.Log("dot product = " + Vector3.Dot(observedDelta, expectedDelta));
                return false;
            }
        }
        return true;
    }

    private static void NormalizeOverTime(List<FrameData> motion_1, float framerate_1, List<FrameData> motion_2, float framerate_2, ref List<FrameData> new_motion_1, ref List<FrameData> new_motion_2)
    {
        if (((float)(motion_1.Count)) / framerate_1 > ((float)(motion_2.Count)) / framerate_2)
        {
            Debug.Log("Adjusting 2");
            Debug.Log("mot 1 time " + motion_1.Count / framerate_1);
            Debug.Log("mot 2 time " + motion_2.Count / framerate_2);
            motion_2 = AdjustDuration(motion_2, framerate_2, ((float)(motion_1.Count)) / framerate_1);
        }
        else if (((float)(motion_1.Count)) / framerate_1 < ((float)(motion_2.Count)) / framerate_2)
        {
            Debug.Log("Adjusting 1");
            motion_1 = AdjustDuration(motion_1, framerate_1, ((float)(motion_2.Count)) / framerate_2);
        }
        if (framerate_1 > framerate_2)
        {
            List<FrameData> tmp = motion_2;
            float tmp2 = framerate_2;
            motion_2 = motion_1;
            framerate_2 = framerate_1;
            motion_1 = tmp;
            framerate_1 = tmp2;
        }
        new_motion_1.Clear();
        new_motion_2.Clear();
        for (int i = 0; i < motion_2.Count - 1; i++)
        {
            float time = ((float)(i)) / framerate_2;
            float frame_frac = time * framerate_1;
            int frame_1_1 = (int)(frame_frac);
            int frame_1_2 = frame_1_1 + 1;
            float frame_1_1_weight = 1 - (frame_frac - frame_1_1);
            float frame_1_2_weight = 1 - frame_1_1_weight;
            if (frame_1_1 == motion_1.Count - 1)
            {
                break;
            }
            Debug.Log("i = " + i);
            Debug.Log("frame 1 = " + frame_1_1);
            Debug.Log("frame 2 = " + frame_1_2);
            Debug.Log("frame count 1 = " + motion_1.Count);
            Debug.Log("frame count 2 = " + motion_2.Count);
            Vector3 interp_position = motion_1[frame_1_1].position * frame_1_1_weight + motion_1[frame_1_2].position * frame_1_2_weight;
            FrameData fd = new FrameData();
            fd.position = interp_position;
            new_motion_1.Add(fd);
            new_motion_2.Add(motion_2[i]);
        }
        new_motion_1.Add(motion_1[motion_1.Count - 1]);
        new_motion_2.Add(motion_2[motion_2.Count - 1]);
    }

    private static List<FrameData> AdjustDuration(List<FrameData> motion, float framerate, float target_time)
    {
        List<FrameData> frameData = new List<FrameData>();
        float currentTime = motion.Count / framerate;
        Debug.Log("Current frame count = " + motion.Count);
        int new_frame_num = (int)(target_time * framerate) + 1;
        Debug.Log("New frame count = " + new_frame_num);
        for (int i = 0; i < new_frame_num - 1; i++)
        {
            float frame_frac = ((float)(i)) / new_frame_num * (motion.Count - 1);
            int frame_1 = (int)(frame_frac);
            int frame_2 = frame_1 + 1;
            float frame_1_weight = 1 - (frame_frac - frame_1);
            float frame_2_weight = 1 - frame_1_weight;
            Debug.Log("i = " + i);
            Debug.Log("frame 1 = " + frame_1);
            Debug.Log("frame 2 = " + frame_2);
            Debug.Log("frame frac = " + frame_frac);
            Debug.Log("frame 1 weight = " + frame_1_weight);
            Debug.Log("frame 2 weight = " + frame_2_weight);
            Vector3 interp_position = motion[frame_1].position * frame_1_weight + motion[frame_2].position * frame_2_weight;
            FrameData fd = new FrameData();
            fd.position = interp_position;
            frameData.Add(fd);
        }
        frameData.Add(motion[motion.Count - 1]);
        return frameData;
    }

    private static List<FrameData> GetHandDataFromMotion(Database_Input_Formatter exampleMotion, bool left)
    {
        if (left)
        {
            return exampleMotion.get_bone_frame_positions("lhand");
        }
        else
        {
            return exampleMotion.get_bone_frame_positions("rhand");
        }
    }

    private static List<FrameData> NormalizeDataForDistance(List<FrameData> frameData)
    {
        //return frameData;
        float maxValue = -1;
        foreach (FrameData fd in frameData)
        {
            if (Mathf.Abs(fd.position.x) > maxValue)
            {
                maxValue = Mathf.Abs(fd.position.x);
            }
            if (Mathf.Abs(fd.position.y) > maxValue)
            {
                maxValue = Mathf.Abs(fd.position.y);
            }
            if (Mathf.Abs(fd.position.z) > maxValue)
            {
                maxValue = Mathf.Abs(fd.position.z);
            }
        }
        if (maxValue < float.Epsilon)
        {
            maxValue = 1;
        }
        List<FrameData> newFrameData = new List<FrameData>();
        foreach (FrameData fd in frameData)
        {
            FrameData newFd = new FrameData();
            Vector3 newPosition = new Vector3(fd.position.x / maxValue, fd.position.y / maxValue, fd.position.z / maxValue);
            newFd.position = newPosition;
            newFrameData.Add(newFd);
        }
        return newFrameData;
    }

    private static List<FrameData> NormalizeDataForRotation(List<FrameData> frameData)
    {
        Vector3 firstPos = frameData[0].position;
        Vector3 lastPos = frameData[frameData.Count - 1].position;
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

        List<FrameData> newFrameData = new List<FrameData>();
        foreach(FrameData data in frameData)
        {
            Vector4 position = new Vector4(data.position.x - firstPos.x, data.position.y - firstPos.y, data.position.z - firstPos.z, 1);
            Vector4 newPos = changeOfBasis * position;
            FrameData newData = new FrameData();
            newData.position = new Vector3(position.x, position.y, position.z);
            newFrameData.Add(newData);
        }
        return newFrameData;
    }

    private static List<FrameData> ClearDeadTime(List<FrameData> frameData, float framerate)
    {
        List<FrameData> newFrameData = new List<FrameData>();
        int minI = 0;
        int maxI = 0;
        for (int i = 0; i < frameData.Count - 1; i++)
        {
            float delta = (frameData[i].position - frameData[i + 1].position).magnitude;
            if (delta > CAM.deadTimeMovementThreshhold)
            {
                minI = i;
                break;
            }
        }
        Debug.Log("here1");
        for (int i = frameData.Count - 1; i > 0; i--)
        {
            float delta = (frameData[i].position - frameData[i - 1].position).magnitude;
            if (delta > CAM.deadTimeMovementThreshhold)
            {
                maxI = i;
                break;
            }
        }
        Debug.Log("min i = " + minI);
        Debug.Log("max i = " + maxI);
        Debug.Log("count = " + frameData.Count);
        for (int i = minI; i <= maxI; i++)
        {
            FrameData fd = new FrameData();
            fd.position = frameData[i].position;
            newFrameData.Add(fd);
        }
        return newFrameData;
    }
}
