using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class Database_Input_Formatter : MonoBehaviour
{    
    private Dictionary<string, Database_Bone> database_bones = new Dictionary<string, Database_Bone>();

    private Dictionary<string, double> energy_for_bones = new Dictionary<string, double>();

    internal int num_frame;
    private int current_frame = -1;
    private bool play = false;

    internal GameObject visual_point;
    internal GameObject visual_bone;

    private Queue<string> bone_names = new Queue<string>();

    // Motion:
    public void solve_for_positions() {
        current_frame = 1;
        while (current_frame < num_frame) {
            determine_bone_positions(current_frame);
            current_frame += 1;
        }
        T_Pose();
        current_frame = -1;
    }

    public void playing_animation() {
        if (Input.GetKeyDown("space")) {
            if (current_frame >= num_frame || current_frame < 1) {
                current_frame = 1;
            } else {
                current_frame += 1;
            }

            play = false;
            determine_bone_positions(current_frame);
        }

        if (Input.GetKeyDown("a")) {
            current_frame = 1;
            play = true;
        }

        if (play && current_frame < num_frame && current_frame >= 1) {
            determine_bone_positions(current_frame);
            current_frame += 1;
        }
    }

    // Parse Files:
    public void parse_skeleton_file(TextAsset skeleton_file, bool show_bones, bool enable_skeleton) {

        // TO DO: order, axis, position, orientation or root
        create_bone("root", enable_skeleton);

        List<string> file_text = new List<string>(skeleton_file.text.Split(new char[] { '\n' }));

        string bone_name = "";
        bool bonedata = false;
        bool hierarchy = false;

        for (int i = 0; i < file_text.Count; i++) {

            if (file_text[i].Contains(":bonedata")) {
                bonedata = true;
                continue;
            } else if (file_text[i].Contains(":hierarchy")) {
                hierarchy = true;
                bonedata = false;
                continue;
            }

            if (bonedata) {
                if (file_text[i].Contains("name")) {
                    string temp = file_text[i].Trim();
                    bone_name = temp.Substring(5);
                    create_bone(bone_name, enable_skeleton);

                } else if (file_text[i].Contains("direction")) {
                    string temp = file_text[i].Trim();
                    List<string> elements = new List<string>(temp.Split(new char[] { ' ' }));

                    Vector3 direction = list_to_vector3(elements, 1);
                    direction.x *= -1;
                    database_bones[bone_name].initial_direction = direction;

                } else if (file_text[i].Contains("length")) {
                    string temp = file_text[i].Trim();
                    List<string> elements = new List<string>(temp.Split(new char[] { ' ' }));

                    float length = float.Parse(elements[1]);
                    database_bones[bone_name].length = length;

                    // Makes the bones look like bones
                    if (show_bones) {
                        GameObject visual_bone_gameObject = Instantiate(visual_bone, Vector3.zero, Quaternion.identity);
                        visual_bone_gameObject.SetActive(enable_skeleton);
                        visual_bone_gameObject.name = bone_name + "_Bone";
                        visual_bone_gameObject.transform.localScale = new Vector3(1, 1, length);
                        visual_bone_gameObject.transform.SetParent(database_bones[bone_name].gameObject.transform);
                    }
                    
                } else if (file_text[i].Contains("dof")) {
                    string temp = file_text[i].Trim();
                    List<string> elements = new List<string>(temp.Split(new char[] { ' ' }));
                    elements.RemoveAt(0);
                    database_bones[bone_name].dof = elements;
                } else if (file_text[i].Contains("axis")) {
                    string temp = file_text[i].Trim();
                    List<string> elements = new List<string>(temp.Split(new char[] { ' ' }));

                    // TO DO: The axises might be flipped around (XYZ vs YZX)
                    Vector3 axis = list_to_vector3(elements, 1);
                    database_bones[bone_name].rotate_axes(axis);
                }

            } else if (hierarchy) {
                string temp = file_text[i].Trim();
                List<string> elements = new List<string>(temp.Split(new char[] { ' ' }));

                if (elements.Count <= 1) {
                    continue;
                }

                string parent_name = elements[0];
                elements.RemoveAt(0);

                database_bones[parent_name].children = elements;
                foreach (string child in elements) {
                    database_bones[child].parent = parent_name;
                    GameObject child_bone = database_bones[child].gameObject;
                    child_bone.transform.SetParent(database_bones[parent_name].transform);
                }
            }
        }
    }

    public void parse_motion_file(TextAsset file_name) {

        List<string> file_text = new List<string>(file_name.text.Split(new char[] { '\n' }));

        // TO DO: Is there always 3 lines before the actual data?
        file_text.RemoveRange(0, 3);
        int frame = 0;
        foreach (string line_untrimmed in file_text) {
            string line = line_untrimmed.Trim();
            List<string> elements = new List<string>(line.Split(new char[] { ' ' }));
            if (elements.Count == 1) {
                frame += 1;
                continue;
            } else {
                string bone_name = elements[0];
                elements.RemoveAt(0);
                database_bones[bone_name].add_to_timeline(frame, elements);
            }
        }
    }

    // Position Bones:
    public void T_Pose() {
        string statement = "";

        bone_names.Clear();
        reset_bones();
        bone_names.Enqueue("root");
        while (bone_names.Count != 0) {
            Database_Bone bone = database_bones[bone_names.Dequeue()];
            // Debug.Log("On Bone " + bone.bone_name);
            if (bone.bone_name == "root") {
                bone.T_Pose(null);
            } else {
                statement += bone.T_Pose(database_bones[bone.parent]);
            }

            foreach (string children in bone.children) {
                bone_names.Enqueue(children);
            }
        }
        // Debug.Log(statement);
    }

    void determine_bone_positions(int frame) {
        // Parent First 

        string statement = "";

        bone_names.Clear();
        T_Pose();
        bone_names.Enqueue("root");
        while (bone_names.Count != 0) {
            Database_Bone bone = database_bones[bone_names.Dequeue()];
            // Debug.Log("On Bone " + bone.bone_name);
            if (bone.bone_name == "root") {
                bone.determine_position(frame, null);
            } else {
                statement += bone.determine_position(frame, database_bones[bone.parent]);
            }

            foreach (string children in bone.children) {
                bone_names.Enqueue(children);
            }
        }

        // Debug.Log(statement);

    }
    
    // Energy Functions
    public void solve_for_energy(Dictionary<string, int> inertia) {

        // TO DO: Root Rotation

        foreach(string bone_name in database_bones.Keys) {
            List<Vector3> velocity_vector = get_velocity_vectors(bone_name);
            List<float> energy_vector = new List<float>();
            foreach (Vector3 v in velocity_vector) {
                energy_vector.Add(Mathf.Pow(v.magnitude,2) * inertia[bone_name]);
            }

            float average_energy = energy_vector.Sum() / energy_vector.Count;
            energy_for_bones.Add(bone_name, Math.Log10(average_energy + 1));
        }
    }

    private List<Vector3> get_velocity_vectors(string bone_name) {
        List<Vector3> output = new List<Vector3>();

        Database_Bone bone = database_bones[bone_name];

        foreach(int frame in bone.timeline_global_positions.Keys) {
            if (bone.timeline_global_positions.ContainsKey(frame + 1)){

                Vector3 x_0 = bone.timeline_global_positions[frame];
                Vector3 x_1 = bone.timeline_global_positions[frame + 1];

                // TO DO: consider t of each frame
                Vector3 v = x_1 - x_0;
                output.Add(v);
            }
        }
        return output;
    }

    // Helper Functions:
    void reset_bones() {
        // TO DO: RESET THE ROOT POSITION

        foreach(Database_Bone bone in database_bones.Values) {
            bone.transform.position = Vector3.zero;
            bone.transform.rotation = Quaternion.identity;
        }
    }

    Vector3 list_to_vector3(List<string> elements, int start) {
        float x = float.Parse(elements[start]);
        float y = float.Parse(elements[start + 1]);
        float z = float.Parse(elements[start + 2]);

        return new Vector3(x, y, z);
    }

    void create_bone(string name, bool enable_skeleton) {
        GameObject virtual_bone_gameObject = Instantiate(visual_point, new Vector3(0, 0, 0), Quaternion.identity);
        virtual_bone_gameObject.name = name;
        virtual_bone_gameObject.SetActive(enable_skeleton);
        Database_Bone database_bone = virtual_bone_gameObject.AddComponent<Database_Bone>();

        database_bone.bone_name = name;

        database_bones.Add(name, database_bone);
    }

}
