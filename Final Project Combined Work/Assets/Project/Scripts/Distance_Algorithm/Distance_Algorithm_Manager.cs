using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class Distance_Algorithm_Manager : MonoBehaviour
{

    private Dictionary<string, int> inertia = new Dictionary<string, int>();

    public Database_Manager DM;
    public HandPlayback HP;

    private Dictionary<string, List<double>> likeness_score = new Dictionary<string, List<double>>();

    internal string current_classified_motion = "NA";
    public double tolerance = 10000000000;


    // Start is called before the first frame update
    void Start()
    {
        Invoke("Actual_Start", 1);
    }


    void Actual_Start() {
        fill_inertia();

        foreach (string motion_name in DM.formatters.Keys) {
            foreach (Database_Input_Formatter DIF in DM.formatters[motion_name]) {
                DIF.solve_for_energy(inertia);
                break;
            }
        }


        double live_left_hand_energy = solve_hand_energy(HP.leftFrameData, "lhand");
        double live_right_hand_energy = solve_hand_energy(HP.rightFrameData, "rhand");
        List<double> live_energy = new List<double>() { live_left_hand_energy, live_right_hand_energy };

        Debug.Log("live_left_hand_energy: " + live_left_hand_energy);
        Debug.Log("live_right_hand_energy: " + live_right_hand_energy);

        distance_algorithm(live_energy);
        current_classified_motion = classified_motion();
        Debug.Log(current_classified_motion);
    }
    
    // Getting Hand Energy
    public double solve_hand_energy(List<FrameData> frame_data, string bone_name) {

        List<Vector3> velocity_vector = get_velocity_vectors(frame_data);
        List<float> energy_vector = new List<float>();
        foreach (Vector3 v in velocity_vector) {
            energy_vector.Add(Mathf.Pow(v.magnitude, 2) * inertia[bone_name]);
        }

        float average_energy = energy_vector.Sum() / energy_vector.Count;
        return Math.Log10(average_energy + 1);
    }

    private List<Vector3> get_velocity_vectors(List<FrameData> hand_positions) {
        List<Vector3> output = new List<Vector3>();

        for (int i = 0; i < hand_positions.Count - 1; i++) {
            Vector3 x_0 = hand_positions[i].position;
            Vector3 x_1 = hand_positions[i + 1].position;

            Vector3 v = x_1 - x_0;

            // TO DO: TIME BETWEEN FRAMES
            v *= 90; // Refresh rate (Sampling rate) of system

            output.Add(v);
        }

        return output;
    }

    // Distance Algorithm
    private void distance_algorithm(List<double> live_energy) {
        foreach (string motion_name in DM.formatters.Keys) {
            foreach (Database_Input_Formatter DIF in DM.formatters[motion_name]) {

                double database_left_hand_energy = DIF.energy_for_bones["lhand"];
                double database_right_hand_energy = DIF.energy_for_bones["rhand"];


                string statement = "motion_name: " + motion_name + "\n";
                // Debug.Log("motion_name: " + motion_name + "      database_left_hand_energy: " + database_left_hand_energy);
                statement += "     database_left_hand_energy: \t\t" + database_left_hand_energy + "\n";
                statement += "     database_right_hand_energy:\t\t" + database_right_hand_energy + "\n";
                // Debug.Log("motion_name: " + motion_name + "      database_right_hand_energy: " + database_right_hand_energy);

                Debug.Log(statement);
                List<double> database_energy = new List<double>() { database_left_hand_energy, database_right_hand_energy };

                double distance = distance_equation(live_energy, database_energy);

                if (likeness_score.ContainsKey(motion_name)) {
                    likeness_score[motion_name].Add(distance);
                } else {
                    likeness_score.Add(motion_name, new List<double>() { distance });
                }
            }
        }
    }

    public string classified_motion() {
        string output = "NA";
        double lowest_score = tolerance;

        foreach (string motion_name in likeness_score.Keys) {

            List<double> scores = likeness_score[motion_name];

            string statement = "motion_name: " + motion_name + "\n";
            foreach (double score in scores) {
                statement += "     " + score + "\n";
            }

            statement += "     scores.Min(): " + scores.Min() + "\n";
            // Debug.Log(statement);


            if (scores.Min() < lowest_score) {
                output = motion_name;
                lowest_score = scores.Min();
            }
        }

        return output;
    }


    // Helper Functions
    void fill_inertia() {
        inertia.Add("root", 1);
        inertia.Add("lhipjoint", 1);
        inertia.Add("rhipjoint", 1);
        inertia.Add("lowerback", 4);
        inertia.Add("upperback", 4);
        inertia.Add("thorax", 4);
        inertia.Add("lowerneck", 3);
        inertia.Add("upperneck", 3);
        inertia.Add("head", 1);
        inertia.Add("rclavicle", 3);
        inertia.Add("rhumerus", 3);
        inertia.Add("rradius", 2);
        inertia.Add("rwrist", 1);
        inertia.Add("rhand", 1);
        inertia.Add("rfingers", 1);
        inertia.Add("rthumb", 1);
        inertia.Add("lclavicle", 3);
        inertia.Add("lhumerus", 3);
        inertia.Add("lradius", 2);
        inertia.Add("lwrist", 1);
        inertia.Add("lhand", 1);
        inertia.Add("lfingers", 1);
        inertia.Add("lthumb", 1);
        inertia.Add("rfemur", 3);
        inertia.Add("rtibia", 3);
        inertia.Add("rfoot", 1);
        inertia.Add("rtoes", 1);
        inertia.Add("lfemur", 3);
        inertia.Add("ltibia", 3);
        inertia.Add("lfoot", 1);
        inertia.Add("ltoes", 1);
    }

    double distance_equation(List<double> v1, List<double> v2) {
        double sum = 0;
        for (int i = 0; i < v1.Count; i++) {
            sum += Math.Pow(v1[i] - v2[i], 2);
        }

        return Math.Sqrt(sum);
    }
}
