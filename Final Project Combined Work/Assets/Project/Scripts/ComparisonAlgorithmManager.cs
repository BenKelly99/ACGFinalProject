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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClassifyPlayback();
        }
    }

    // Update is called once per frame
    void ClassifyPlayback()
    {
        List<FrameData> normalizedExample = new List<FrameData>();
        List<FrameData> normalizedObserved = new List<FrameData>();

        ClassifingAlgorithm.FullNormalizeMotion(observed.rightFrameData, example.rightFrameData, ref normalizedObserved, ref normalizedExample);
        observed.rightFrameData = normalizedObserved;
        example.rightFrameData = normalizedExample;

        observed.play();
        example.play();
    }
}
