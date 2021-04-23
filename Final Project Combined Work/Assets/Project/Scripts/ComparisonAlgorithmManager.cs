using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
public class ComparisonAlgorithmManager: MonoBehaviour
{
    public Database_Manager DB_Manager;
    public HandPlayback observed, example;

    public float deadTimeMovementThreshhold = .025f;
    public float minDotThreshold = 0.8f;
    public float distanceFactorThreshold = 0.05f;

    void Start()
    {
        ClassifingAlgorithm.SetCAM(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            NormalizePlayback();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClassifyPlayback();
        }
    }

    // Update is called once per frame
    void NormalizePlayback()
    {
        List<FrameData> normalizedExample = new List<FrameData>();
        List<FrameData> normalizedObserved = new List<FrameData>();

        ClassifingAlgorithm.FullNormalizeMotion(observed.rightFrameData, 50, example.rightFrameData, 50, ref normalizedObserved, ref normalizedExample);
        observed.rightFrameData = normalizedObserved;
        example.rightFrameData = normalizedExample;

        observed.play();
        example.play();
    }

    void ClassifyPlayback()
    {
        ClassifingAlgorithm.DoesMotionMatchFrameData(observed.rightFrameData, 50, example.rightFrameData, 50);
    }
}
