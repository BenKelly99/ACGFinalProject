using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Database_Bone : MonoBehaviour
{
    internal string bone_name;
    internal float length;
    // IMPORTANT: UNIT VECTOR FROM T-POSE
    internal Vector3 initial_direction;
    internal List<string> dof = new List<string>();

    internal Vector3 x_axis = Vector3.right;
    internal Vector3 y_axis = Vector3.up;
    internal Vector3 z_axis = Vector3.forward;

    internal List<string> children = new List<string>();
    internal string parent;

    internal Dictionary<int, List<float>> timeline_dof = new Dictionary<int, List<float>>();

    // IMPORTANT: THIS IS THE GLOBAL MIDDLE POINT OF THE BONE
    //  Frame = 1: T_Pose
    //  Frame > 1: The Motion
    internal Dictionary<int, Vector3> timeline_global_positions = new Dictionary<int, Vector3>();
    internal Dictionary<int, Quaternion> timeline_global_rotation = new Dictionary<int, Quaternion>();

    // File Parsing Functions:
    public void add_to_timeline(int frame, List<string> elements) {
        List<float> floats = new List<float>();
        foreach (string element in elements) {
            floats.Add(float.Parse(element));
        }
        timeline_dof.Add(frame, floats);
    }

    public void rotate_axes(Vector3 change_in_axes) {
        string statement = "bone_name: " + bone_name + "     change_in_axes: " + change_in_axes + "\n";
        x_axis = Standard_Axis_Rotation(x_axis, change_in_axes);
        y_axis = Standard_Axis_Rotation(y_axis, change_in_axes);
        z_axis = Standard_Axis_Rotation(z_axis, change_in_axes);
        statement += "   x_axis: " + x_axis + "\n";
        statement += "   y_axis: " + y_axis + "\n";
        statement += "   z_axis: " + z_axis + "\n";
        // Debug.Log(statement);
    }

    // Position Functions
    public string T_Pose(Database_Bone parent_bone) {

        // Position has already been calculated
        if (timeline_global_positions.ContainsKey(0)) {
            transform.position = timeline_global_positions[0];
            return "";
        }

        // The bone is the root
        if (bone_name == "root") {
            gameObject.transform.localPosition = Vector3.zero;
            return "";
        }

        string statement = "TPOSE: bone_name: " + bone_name + "\n";
        
        // Getting the direction of the current bone
        Vector3 current_forward_direction = Complex_Rotation_Bones_Axes(initial_direction, Vector3.zero);

        // Vector of half the current bone
        Vector3 half_bone = (length / 2) * transform.InverseTransformDirection(current_forward_direction);

        // Get local mid point and end point of current bone
        Vector3 local_mid_point = ((parent_bone.length / 2) * Vector3.forward) + half_bone;
        Vector3 local_end_point = local_mid_point + half_bone;

        // Convert the end point to a global value
        Vector3 global_end_point = transform.TransformPoint(local_end_point);

        // Position Bone
        transform.localPosition = local_mid_point;  // Move Bone
        transform.LookAt(global_end_point);         // Rotate Bone

        // statement += "     transform.position:     \t\t" + transform.position.x + "\t\t" + transform.position.y + "\t\t" + transform.position.z + "\n";
        // statement += "     transform.rotation:     \t\t" + transform.rotation.x + "\t\t" + transform.rotation.y + "\t\t" + transform.rotation.z + "\n";

        statement += "     transform.forward:\t\t" + transform.forward.x + "\t\t" + transform.forward.y + "\t\t" + transform.forward.z + "\n";
        statement += "     transform.up:     \t\t" + transform.up.x + "\t\t" + transform.up.y + "\t\t" + transform.up.z + "\n";

        return statement + "\n";
        // Debug.Log(statement);
    }

    public string determine_position(int frame, Database_Bone parent_bone) {

        // Position has already been calculated
        if (timeline_global_positions.ContainsKey(frame)) {
            transform.position = timeline_global_positions[frame];
            transform.rotation = timeline_global_rotation[frame];
            return "";
        }

        // The bone is the root
        if (parent_bone == null) {
            return determine_position_root(frame);
        }
        else {
            return determine_position_non_root(frame, parent_bone);
        }
    }

    string determine_position_root(int frame) {
        // Get Position
        Vector3 position = new Vector3(timeline_dof[frame][0], timeline_dof[frame][1], timeline_dof[frame][2]);

        // Scale to the right scale
        position *= Database_Manager.CMU_TO_METERS;

        // Change Position
        gameObject.transform.localPosition = position;

        // Get Rotation 
        Vector3 rotation_vector = dof_to_vector3(frame);

        // Find forward and up directions
        Vector3 current_forward_direction = Complex_Rotation_Bones_Axes(transform.forward, rotation_vector);
        Vector3 current_up_direction = Complex_Rotation_Bones_Axes(transform.up, rotation_vector);

        // Look at the end point of the bone in global coordinates
        Vector3 global_end_point = current_forward_direction + position;
        transform.LookAt(global_end_point, current_up_direction);

        timeline_global_positions.Add(frame, transform.position);
        timeline_global_rotation.Add(frame, transform.rotation);


        return "";
    }

    string determine_position_non_root(int frame, Database_Bone parent_bone) {
        string statement = "bone_name: " + bone_name + "     frame: " + frame + "\n";    

        // Rotation from .amc file
        Vector3 rotation_vector = dof_to_vector3(frame);

        // Getting the direction of the current bone
        Vector3 current_forward_direction = Complex_Rotation_Bones_Axes(transform.forward, rotation_vector);
        Vector3 current_up_direction = Complex_Rotation_Bones_Axes(transform.up, rotation_vector);

        statement += vector3_to_string(x_axis, "x_axis");
        statement += vector3_to_string(y_axis, "y_axis");
        statement += vector3_to_string(z_axis, "z_axis");

        statement += vector3_to_string(rotation_vector, "rotation_vector");

        statement += vector3_to_string(transform.forward,           "transform.forward            ");
        statement += vector3_to_string(transform.up,                "transform.up                   ");
        statement += vector3_to_string(current_forward_direction,   "current_forward_direction");
        statement += vector3_to_string(current_up_direction,        "current_up_direction     ");

        // Vector of half the current bone
        Vector3 local_half_bone = (length / 2) * transform.InverseTransformDirection(current_forward_direction);

        // Get local mid point and end point of current bone
        Vector3 local_mid_point = ((parent_bone.length / 2) * transform.InverseTransformDirection(parent_bone.transform.forward)) + local_half_bone;
        Vector3 global_mid_point = transform.TransformPoint(local_mid_point);
        Vector3 local_to_parent_mid_point = parent_bone.transform.InverseTransformPoint(global_mid_point);
        Vector3 new_local_mid_point = local_to_parent_mid_point - transform.localPosition;

        Vector3 global_half_bone = transform.TransformPoint(local_half_bone);
        Vector3 local_to_parent_half_bone = parent_bone.transform.InverseTransformPoint(global_half_bone);
        Vector3 new_local_half_bone = local_to_parent_half_bone - transform.localPosition;

        transform.localPosition = new_local_mid_point;

        Vector3 local_end_point = new_local_mid_point + new_local_half_bone;
        // Convert the end point to a global value
        Vector3 global_end_point = parent_bone.transform.TransformPoint(local_end_point);

        transform.LookAt(global_end_point, current_up_direction);

        timeline_global_positions.Add(frame, transform.position);
        timeline_global_rotation.Add(frame, transform.rotation);

        return statement + "\n";
    }

    // Rotation Functions:
    double cosine_rule(float a, float b, float angle) {
        double radians = (Math.PI / 180) * angle;
        double temp = Math.Pow(a, 2) + Math.Pow(b, 2) - (2*a*b* Math.Cos(radians));
        return Math.Sqrt(temp);
    }

    Vector3 Standard_Axis_Rotation(Vector3 input, Vector3 angles_degrees) {
        Vector3 angles_rad = angles_degrees * (Mathf.PI / 180);

        // THESE ARE COLUMNS. NOT ROWS!!!!!!!!
        Matrix4x4 rotate_x = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, Mathf.Cos(angles_rad.x), Mathf.Sin(angles_rad.x), 0),
            new Vector4(0, -Mathf.Sin(angles_rad.x), Mathf.Cos(angles_rad.x), 0),
            new Vector4(0, 0, 0, 0));

        Matrix4x4 rotate_y = new Matrix4x4(
            new Vector4(Mathf.Cos(angles_rad.y), 0, -Mathf.Sin(angles_rad.y), 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(Mathf.Sin(angles_rad.y), 0, Mathf.Cos(angles_rad.y), 0),
            new Vector4(0, 0, 0, 0));

        Matrix4x4 rotate_z = new Matrix4x4(
            new Vector4(Mathf.Cos(angles_rad.z), Mathf.Sin(angles_rad.z), 0, 0),
            new Vector4(-Mathf.Sin(angles_rad.z), Mathf.Cos(angles_rad.z), 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 0, 0, 0));

        Vector3 temp = rotate_x.MultiplyPoint3x4(input);
        temp = rotate_y.MultiplyPoint3x4(temp);
        temp = rotate_z.MultiplyPoint3x4(temp);

        return temp;
    }

    Vector3 Complex_Rotation_Given_Axis(Vector3 input, float angles_rad, Vector3 axis) {
        Matrix4x4 rotation_matrix = Create_Rotation_Matrices(axis, angles_rad);
        Vector3 temp = rotation_matrix.MultiplyPoint3x4(input);
        return temp;
    }

    Vector3 Complex_Rotation_Bones_Axes(Vector3 input, Vector3 angles_degrees) {
        Vector3 angles_rad = angles_degrees * (Mathf.PI / 180);

        Matrix4x4 rotate_x = Create_Rotation_Matrices(x_axis, angles_rad.x);
        Matrix4x4 rotate_y = Create_Rotation_Matrices(y_axis, angles_rad.y);
        Matrix4x4 rotate_z = Create_Rotation_Matrices(z_axis, angles_rad.z);

        Vector3 temp = rotate_z.MultiplyPoint3x4(input);
        temp = rotate_x.MultiplyPoint3x4(temp);
        temp = rotate_y.MultiplyPoint3x4(temp);

        /*
        Vector3 temp = rotate_x.MultiplyPoint3x4(input);
        temp = rotate_y.MultiplyPoint3x4(temp);
        temp = rotate_z.MultiplyPoint3x4(temp);
        */

        return temp;
    }

    Matrix4x4 Create_Rotation_Matrices(Vector3 axis, float angle) {
        Matrix4x4 mat = Matrix4x4.zero;
        // Matrix[Row, Column]
        mat[0, 0] = Mathf.Cos(angle) + Mathf.Pow(axis.x, 2) * (1 - Mathf.Cos(angle));
        mat[1, 0] = axis.y * axis.x * (1 - Mathf.Cos(angle)) + axis.z * Mathf.Sin(angle);
        mat[2, 0] = axis.z * axis.x * (1 - Mathf.Cos(angle)) - axis.y * Mathf.Sin(angle);

        mat[0, 1] = axis.x * axis.y * (1 - Mathf.Cos(angle)) - axis.z * Mathf.Sin(angle);
        mat[1, 1] = Mathf.Cos(angle) + Mathf.Pow(axis.y, 2) * (1 - Mathf.Cos(angle));
        mat[2, 1] = axis.z * axis.y * (1 - Mathf.Cos(angle)) + axis.x * Mathf.Sin(angle);

        mat[0, 2] = axis.x * axis.z * (1 - Mathf.Cos(angle)) + axis.y * Mathf.Sin(angle);
        mat[1, 2] = axis.y * axis.z * (1 - Mathf.Cos(angle)) - axis.x * Mathf.Sin(angle);
        mat[2, 2] = Mathf.Cos(angle) + Mathf.Pow(axis.z, 2) * (1 - Mathf.Cos(angle));

        return mat;
    }

    // Helper Functions
    Vector3 dof_to_vector3(int frame) {
        float rx = 0, ry = 0, rz = 0;
        for (int i = 0; i < dof.Count; i++) {
            if (dof[i].Contains("rx")) {
                rx = timeline_dof[frame][i];
            } else if (dof[i].Contains("ry")) {
                ry = -1 * timeline_dof[frame][i];
            } else if (dof[i].Contains("rz")) {
                rz = -1 * timeline_dof[frame][i];
            }
        }
        return new Vector3(rx, ry, rz);
    }

    string vector3_to_string(Vector3 vector, string name) {
        return "     " + name + ":\t\t" + vector.x + "\t\t" + vector.y + "\t\t" + vector.z + "\n";
    }


    // Print / Visualization Functions:
    public void print_bone_variables() {
        string statement = "name: " + bone_name + "\n";
        if (true) {
            statement += "   length: " + length + "\n";
        }
        if (initial_direction != null && true) {
            statement += "   direction: " + initial_direction + "\n";
        }
        if (dof.Count != 0 && true) {
            statement += "   DOF: " + String.Join(", ", dof.ToArray()) + "\n";
        }

        if (true) {
            statement += "   x_axis: " + x_axis + "\n";
        }
        if (true) {
            statement += "   y_axis: " + y_axis + "\n";
        }
        if (true) {
            statement += "   z_axis: " + z_axis + "\n";
        }

        if (children.Count != 0 && true) {
            statement += "   chilren: " + String.Join(", ", children.ToArray()) + "\n";
        }
        if (parent != null && true) {
            statement += "   parent: " + parent + "\n";
        }

        Debug.Log(statement);
    }

    public void print_bone_timeline() {
        string statement = "name: " + bone_name;
        if (dof != null) {
            statement += "     DOF: " + String.Join(", ", dof.ToArray()) + "\n";
        }else {
            statement += "     DOF: NONE\n";
        }
        foreach (int frame in timeline_dof.Keys) {
            statement += frame + ": " + String.Join(", ", timeline_dof[frame].ToArray()) + "\n";
        }


        Debug.Log(statement);
    }

}
