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

    public bool live_data = false;
    public HandPlayback HP;
    public HandRecorder recorder;

    private Dictionary<string, List<double>> likeness_score = new Dictionary<string, List<double>>();

    internal string current_classified_motion = "NA";

    public int live_data_frame_rate = 90;
    public double distance_algorithm_tolerance = 10000000000;
    public float deadTimeMovementThreshhold = .025f;

    public int number_of_frames_looked_at = 100;

    public float seconds_between_live_data_checks = 1;
    private float next_live_data_check_time = 0;


    // Start is called before the first frame update
    void Start()
    {
        Invoke("Actual_Start", .1f);
    }


    void Actual_Start() {
        fill_inertia();

        foreach (string motion_name in DM.formatters.Keys) {
            foreach (Database_Input_Formatter DIF in DM.formatters[motion_name]) {
                DIF.solve_for_energy(inertia);
            }
        }

        if (!live_data) {
            double live_left_hand_energy = solve_hand_energy(HP.leftFrameData, "lhand");
            double live_right_hand_energy = solve_hand_energy(HP.rightFrameData, "rhand");
            List<double> live_energy = new List<double>() { live_left_hand_energy, live_right_hand_energy };

            Debug.Log("live_left_hand_energy: " + live_left_hand_energy);
            Debug.Log("live_right_hand_energy: " + live_right_hand_energy);

            distance_algorithm(live_energy);
            current_classified_motion = classified_motion();
            Debug.Log("Classified Motion: " + current_classified_motion);
        }
        
    }

    void FixedUpdate() {
        if (live_data && Time.time >= next_live_data_check_time) {
            Debug.Log("Check Live Data");
            next_live_data_check_time = Time.time + seconds_between_live_data_checks;


            List<FrameData> leftFrameData, rightFrameData;
            (leftFrameData, rightFrameData) = get_live_data();

            if (leftFrameData == null || rightFrameData == null) {
                Debug.Log("Invalid leftFrameData or rightFrameData data");
                return;
            }

            double live_left_hand_energy = solve_hand_energy(leftFrameData, "lhand");
            double live_right_hand_energy = solve_hand_energy(rightFrameData, "rhand");

            Debug.Log("live_left_hand_energy: " + live_left_hand_energy);
            Debug.Log("live_right_hand_energy: " + live_right_hand_energy);

            distance_algorithm(new List<double>() { live_left_hand_energy, live_right_hand_energy });
            current_classified_motion = classified_motion();
            Debug.Log("Classified Motion: " + current_classified_motion);
        }
    }

    // Get Live Data
    public (List<FrameData>, List<FrameData>) get_live_data() {
        List<FrameData> leftFrameData = recorder.leftFrameData;
        List<FrameData> rightFrameData = recorder.rightFrameData;

        if (leftFrameData.Count < number_of_frames_looked_at || rightFrameData.Count < number_of_frames_looked_at) {
            return (null, null);
        }

        leftFrameData = (List <FrameData>) leftFrameData.Skip(leftFrameData.Count() - number_of_frames_looked_at);
        rightFrameData = (List<FrameData>) rightFrameData.Skip(leftFrameData.Count() - number_of_frames_looked_at);

        leftFrameData = ClearDeadTime(leftFrameData);
        rightFrameData = ClearDeadTime(rightFrameData);

        /*
        if (leftFrameData.Count < 50 || rightFrameData.Count < 50) {
            return (null, null);
        }
        */

        return (leftFrameData, rightFrameData);
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
            v *= live_data_frame_rate; // Refresh rate (Sampling rate) of system

            output.Add(v);
        }

        return output;
    }

    // Normalize Data
    private List<FrameData> ClearDeadTime(List<FrameData> frameData) {
        List<FrameData> newFrameData = new List<FrameData>();
        int minI = 0;
        int maxI = 0;
        for (int i = 0; i < frameData.Count - 1; i++) {
            float delta = (frameData[i].position - frameData[i + 1].position).magnitude;
            if (delta > deadTimeMovementThreshhold) {
                minI = i;
                break;
            }
        }
        Debug.Log("here1");
        for (int i = frameData.Count - 1; i > 0; i--) {
            float delta = (frameData[i].position - frameData[i - 1].position).magnitude;
            if (delta > deadTimeMovementThreshhold) {
                maxI = i;
                break;
            }
        }
        Debug.Log("min i = " + minI);
        Debug.Log("max i = " + maxI);
        Debug.Log("count = " + frameData.Count);
        for (int i = minI; i <= maxI; i++) {
            FrameData fd = new FrameData();
            fd.position = frameData[i].position;
            newFrameData.Add(fd);
        }
        return newFrameData;
    }

    // Distance Algorithm
    private void distance_algorithm(List<double> live_energy) {
        foreach (string motion_name in DM.formatters.Keys) {
            foreach (Database_Input_Formatter DIF in DM.formatters[motion_name]) {

                double database_left_hand_energy = DIF.energy_for_bones["lhand"];
                double database_right_hand_energy = DIF.energy_for_bones["rhand"];


                string statement = "motion_name: " + motion_name + "\n";
                statement += "     database_left_hand_energy: \t\t" + database_left_hand_energy + "\n";
                statement += "     database_right_hand_energy:\t\t" + database_right_hand_energy + "\n";

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
        double lowest_score = distance_algorithm_tolerance;

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
