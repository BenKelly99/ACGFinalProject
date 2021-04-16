using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;




public class Database_Manager : MonoBehaviour
{
    
    internal static float CMU_TO_METERS = 0.056444f;

    [Serializable]
    public struct Motion_Files
    {
        public string motion_name;
        public TextAsset file_name;
        public TextAsset skeleton_file;
        public int num_frame;
    }

    public int current_motion_file;
    private string current_motion_file_name;
    private int current_motion_file_index;

    public Motion_Files[] motion_files;

    public GameObject visual_point;
    public GameObject visual_bone;
    public bool show_bones = true;

    internal Dictionary<string, List<Database_Input_Formatter> > formatters = new Dictionary<string, List<Database_Input_Formatter>>();

    // Start is called before the first frame update
    void Start() {


        for (int i = 0; i < motion_files.Length; i++) {

            Motion_Files MF = motion_files[i];

            Database_Input_Formatter formatter = gameObject.AddComponent<Database_Input_Formatter>();

            formatter.num_frame = MF.num_frame;
            formatter.visual_point = visual_point;
            formatter.visual_bone = visual_bone;

            if (i == current_motion_file) {
                formatter.parse_skeleton_file(MF.skeleton_file, show_bones, true);
            } else {
                formatter.parse_skeleton_file(MF.skeleton_file, show_bones, false);
            }

            formatter.parse_motion_file(MF.file_name);

            formatter.solve_for_positions();
            

            if (formatters.ContainsKey(MF.motion_name)){
                formatters[MF.motion_name].Add(formatter);
            } else {
                formatters.Add(MF.motion_name, new List<Database_Input_Formatter>() { formatter });
            }

            if (i == current_motion_file) {
                formatter.T_Pose();
                current_motion_file_name = MF.motion_name;
                current_motion_file_index = formatters[MF.motion_name].Count - 1;
            }


        }
    }

    void FixedUpdate() {
        formatters[current_motion_file_name][current_motion_file_index].playing_animation();
    }
}
