using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Test_Script : MonoBehaviour
{
    public GameObject bone;
    public GameObject joint;
    public GameObject point;

    public GameObject root;
    public GameObject lowerback;
    public GameObject upperback;
    public GameObject thorax;

    private GameObject root_joint;
    private GameObject lowerback_joint;
    private GameObject upperback_joint;
    private GameObject thorax_joint;


    private Vector3 root_start = new Vector3(0,0,0);
    private Vector3 root_direction = new Vector3(0, 0, 1);
    private float root_length = 0;

    private Vector3 lowerback_original_direction = new Vector3(0, 1, 0); // new Vector3(0.997604f, 0.00952301f, -0.0685217f);
    private Vector3 lowerback_current_direction = new Vector3(1, 0, 0); // new Vector3(0.997604f, 0.00952301f, -0.0685217f);
    private float lowerback_length = 2f; //2.05943f;
    private Vector3 lowerback_cross_product = Vector3.zero;
    private float lowerback_dot_product;

    private Vector3 upperback_original_direction = new Vector3(0, 1, 0); // new Vector3(0.00494399f, 0.999577f, -0.0286701f);
    private Vector3 upperback_current_direction = new Vector3(0, 1, 0); // new Vector3(0.00494399f, 0.999577f, -0.0286701f);
    private float upperback_length = 3f; // 2.06523f;
    private Vector3 upperback_cross_product = Vector3.zero;
    private float upperback_dot_product;

    private Vector3 thorax_original_direction = new Vector3(0, 1, 0); // new Vector3(0.000729686f, 0.999992f, 0.00388603f);
    private Vector3 thorax_current_direction = new Vector3(0, 1, 0); // new Vector3(0.000729686f, 0.999992f, 0.00388603f);
    private float thorax_length = 5f; // 2.06807f;
    private Vector3 thorax_cross_product = Vector3.zero;
    private float thorax_dot_product;

    void Start() {

        root_joint = Make_Bone("Root_Bone", 1, root.transform);
        // root_joint.SetActive(false);
        lowerback_joint = Make_Bone("lowerback", lowerback_length, lowerback.transform);
        upperback_joint = Make_Bone("upperback", upperback_length, upperback.transform);
        thorax_joint = Make_Bone("thorax", thorax_length, thorax.transform);

        root.transform.localPosition = root_start;
        root.transform.forward = root_direction;

        TPose("lowerback", lowerback_joint, lowerback_original_direction, lowerback_length, root_length);
        lowerback_cross_product = Vector3.Cross(Vector3.forward, lowerback_original_direction);
        lowerback_dot_product = Vector3.Dot(Vector3.forward, lowerback_original_direction);

        TPose("upperback", upperback_joint, upperback_original_direction, upperback_length, lowerback_length);
        upperback_cross_product = Vector3.Cross(Vector3.forward, upperback_original_direction);
        Debug.Log("Vector3.forward: " + Vector3.forward);
        Debug.Log("upperback_original_direction: " + upperback_original_direction);
        Debug.Log("upperback_cross_product: " + upperback_cross_product);

        lowerback_dot_product = Vector3.Dot(Vector3.forward, upperback_original_direction);

        TPose("thorax", thorax_joint, thorax_original_direction, thorax_length, upperback_length);
        thorax_cross_product = Vector3.Cross(Vector3.forward, thorax_original_direction);
        thorax_dot_product = Vector3.Dot(Vector3.forward, thorax_original_direction);


        Place_Bones("lowerback", lowerback_length, lowerback_current_direction, lowerback, lowerback_cross_product, lowerback_dot_product);
        Place_Bones("upperback", upperback_length, upperback_current_direction, upperback, upperback_cross_product, upperback_dot_product);
        Place_Bones("thorax", thorax_length, thorax_current_direction, thorax, thorax_cross_product, thorax_dot_product);
    }

    GameObject Make_Bone(string name, float length, Transform virtual_bone) {

        GameObject temp_joint_object = Instantiate(joint, Vector3.zero, Quaternion.identity);
        temp_joint_object.name = name + "_Joint";
        if (name != "Root_Bone") {
            temp_joint_object.transform.localPosition = new Vector3(0, 0, -length/2);
        }
        temp_joint_object.transform.SetParent(virtual_bone.parent);
        virtual_bone.SetParent(temp_joint_object.transform);

        GameObject temp_bone_object = Instantiate(bone, Vector3.zero, Quaternion.identity);
        temp_bone_object.name = name + "_Bone";
        temp_bone_object.transform.localScale = new Vector3(temp_bone_object.transform.localScale.x, temp_bone_object.transform.localScale.y, length);
        temp_bone_object.transform.SetParent(virtual_bone.transform);

        return temp_joint_object;
    }

    void TPose(string bone_name, GameObject joint, Vector3 initial_direction, float bone_length, float parent_bone_length) {
        string statement = "bone_name: " + bone_name + "\n";

        Vector3 bone_vector = bone_length * joint.transform.InverseTransformDirection(initial_direction);
        statement += "     bone_vector: " + bone_vector + "\n";

        Vector3 joint_local_position = (parent_bone_length / 2) * Vector3.forward;
        statement += "     joint_local_position: " + joint_local_position + "\n";

        Vector3 local_end_point = joint_local_position + bone_vector;
        statement += "     local_end_point: " + local_end_point + "\n";

        Vector3 global_end_point = joint.transform.TransformDirection(local_end_point);
        statement += "     global_end_point: " + global_end_point + "\n";
        statement += "     joint.transform.TransformPoint(local_direction): " + joint.transform.TransformPoint(local_end_point) + "\n";
        statement += "     joint.transform.TransformDirection(local_direction): " + joint.transform.TransformDirection(local_end_point) + "\n";
        statement += "     joint.transform.TransformVector(local_direction): " + joint.transform.TransformVector(local_end_point) + "\n";

        statement += "     joint.transform.InverseTransformPoint(local_direction): " + joint.transform.InverseTransformPoint(local_end_point) + "\n";
        statement += "     joint.transform.InverseTransformDirection(local_direction): " + joint.transform.InverseTransformDirection(local_end_point) + "\n";
        statement += "     joint.transform.InverseTransformVector(local_direction): " + joint.transform.InverseTransformVector(local_end_point) + "\n";

        joint.transform.localPosition = joint_local_position;
        if (initial_direction.z >= 0) {
            joint.transform.LookAt(global_end_point, Vector3.up);
        } else {
            joint.transform.LookAt(global_end_point, Vector3.down);
        }

        // Debug.Log(statement);
    }

    void Place_Bones(string bone_name, float bone_length, Vector3 local_direction, GameObject bone, Vector3 global_cross_product, float dot_product) {

        string statement = "bone_name: " + bone_name + "\n";
        statement += "     local_direction: " + local_direction + "\n";

        Vector3 local_mid_point = (bone_length / 2) * bone.transform.InverseTransformDirection(local_direction);
        statement += "     local_mid_point: " + local_mid_point + "\n";

        Vector3 local_end_point = 2*local_mid_point;
        statement += "     local_end_point: " + local_end_point + "\n";
        Vector3 global_end_point = bone.transform.TransformDirection(local_end_point);
        statement += "     global_end_point: " + global_end_point + "\n";


        // global_cross_product;
        // Vector3 local_cross_product = bone.transform.InverseTransformVector(global_cross_product)


        statement += "     global_cross_product: " + global_cross_product + "\n";
        statement += "     bone.transform.TransformPoint(local_direction): " + bone.transform.TransformPoint(local_direction) + "\n";
        statement += "     bone.transform.TransformDirection(local_direction): " + bone.transform.TransformDirection(local_direction) + "\n";
        statement += "     bone.transform.TransformVector(local_direction): " + bone.transform.TransformVector(local_direction) + "\n";

        statement += "     bone.transform.InverseTransformPoint(local_direction): " + bone.transform.InverseTransformPoint(local_direction) + "\n";
        statement += "     bone.transform.InverseTransformDirection(local_direction): " + bone.transform.InverseTransformDirection(local_direction) + "\n";
        statement += "     bone.transform.InverseTransformVector(local_direction): " + bone.transform.InverseTransformVector(local_direction) + "\n";
        

        bone.transform.localPosition = local_mid_point;
        statement += "     bone.transform.localPosition: " + bone.transform.localPosition + "\n";

        if (local_direction.z >= 0) {
            bone.transform.LookAt(global_end_point, Vector3.up);
        } else {
            bone.transform.LookAt(global_end_point, Vector3.down);
        }


        // statement += "     transform.forward: " + bone.transform.forward + "\n";
        // statement += "     transform.up: " + bone.transform.up + "\n";
        // statement += "     transform.right: " + bone.transform.right + "\n";

        Debug.Log(statement);

    }
}
