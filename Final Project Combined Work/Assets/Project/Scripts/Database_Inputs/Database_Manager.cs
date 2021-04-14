using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class Database_Manager : MonoBehaviour
{
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

    private Dictionary<string, List<Database_Input_Formatter> > formatters = new Dictionary<string, List<Database_Input_Formatter>>();

    private Dictionary<string, int> inertia = new Dictionary<string, int>();

    // Start is called before the first frame update
    void Start() {

        fill_inertia();

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
            formatter.solve_for_energy(inertia);

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

    void fill_inertia() {
        inertia.Add("root", 1);
        inertia.Add("lhipjoint", 0);
        inertia.Add("rhipjoint", 0);
        inertia.Add("lowerback", 4);
        inertia.Add("upperback", 4);
        inertia.Add("thorax", 4);
        inertia.Add("lowerneck", 3);
        inertia.Add("upperneck", 3);
        inertia.Add("head", 0);
        inertia.Add("rclavicle", 3);
        inertia.Add("rhumerus", 3);
        inertia.Add("rradius", 2);
        inertia.Add("rwrist", 1);
        inertia.Add("rhand", 0);
        inertia.Add("rfingers", 0);
        inertia.Add("rthumb", 0);
        inertia.Add("lclavicle", 3);
        inertia.Add("lhumerus", 3);
        inertia.Add("lradius", 2);
        inertia.Add("lwrist", 1);
        inertia.Add("lhand", 0);
        inertia.Add("lfingers", 0);
        inertia.Add("lthumb", 0);
        inertia.Add("rfemur", 3);
        inertia.Add("rtibia", 3);
        inertia.Add("rfoot", 0);
        inertia.Add("rtoes", 0);
        inertia.Add("lfemur", 3);
        inertia.Add("ltibia", 3);
        inertia.Add("lfoot", 0);
        inertia.Add("ltoes", 0);
    }
}
