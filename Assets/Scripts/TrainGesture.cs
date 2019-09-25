using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Leap;
using Leap.Unity;
using System;
using System.Text;
using UnityEngine.UI;

public class TrainGesture : MonoBehaviour
{

    public GameObject startTrainingButton;
    public GameObject stopTrainingButton;
    public GameObject startTrainingText;
    public GameObject trainingStartsInText;
    public GameObject gestureNameInputField;

    Text trainingCountDowntext;
    Text startTrainingBottomText;
    InputField gestureName;
    int numberOfFingers;
    Button startTraining;
    Button stopTraining;

    string fingerPositionString = "";
    string fingerDistanceString = "";
    string featureText = "";
    string currentHand = "";
    string currentGestureName = "";

    bool startButtonPressed = false, bottomTextChanged = false, stopButtonPressed = false, writeToFile;

    const string dataFilePath = @"dataset.csv";

    // Start is called before the first frame update
    void Start()
    {
        startTraining = startTrainingButton.GetComponent<Button>();
        stopTraining = stopTrainingButton.GetComponent<Button>();
        trainingCountDowntext = trainingStartsInText.GetComponent<Text>();
        startTrainingBottomText = startTrainingText.GetComponent<Text>();
        gestureName = gestureNameInputField.GetComponent<InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        startTraining.onClick.AddListener(StartTrainingListener);
        stopTraining.onClick.AddListener(StopTrainingListener);

        if (startButtonPressed)
        {
            trainingStartsInText.SetActive(true);
            stopTrainingButton.SetActive(true);
            startTrainingText.SetActive(false);
            startTrainingButton.SetActive(false);
            gestureNameInputField.SetActive(false);
            generateFeatureSet();

        }

        if (bottomTextChanged)
        {
            startTrainingBottomText.text = "Enter Gesture Name and then press START TRAINING button.";
        }

        if (stopButtonPressed)
        {
            trainingStartsInText.SetActive(false);
            stopTrainingButton.SetActive(false);
            startTrainingText.SetActive(true);
            startTrainingButton.SetActive(true);
            gestureNameInputField.SetActive(true);
            startTrainingBottomText.text = "Press START TRAINING button to start Training.";
        }

    }

    private void StopTrainingListener()
    {
        stopButtonPressed = true;
        startButtonPressed = false;
        bottomTextChanged = false;
        gestureName.text = "";
        currentGestureName = "";
    }

    private void StartTrainingListener()
    {
        if (gestureName.text.Length != 0)
        {
            startButtonPressed = true;
            stopButtonPressed = false;
            currentGestureName = gestureName.text;
        }
        if (gestureName.text.Length == 0)
        {
            bottomTextChanged = true;
        }
    }

    private void generateFeatureSet()
    {
        Controller controller = new Controller();
        if (controller.IsConnected)
        {
            Frame frame = controller.Frame();
            numberOfFingers = 0;
            List<Hand> hands = frame.Hands;
            for (int h = 0; h < frame.Hands.Count; h++)
            {
                Hand leapHand = frame.Hands[h];

                Vector handXBasis = leapHand.PalmNormal.Cross(leapHand.Direction).Normalized;
                Vector handYBasis = -leapHand.PalmNormal;
                Vector handZBasis = -leapHand.Direction;
                Vector handOrigin = leapHand.PalmPosition;
                Matrix handTransform = new Matrix(handXBasis, handYBasis, handZBasis, handOrigin);
                handTransform = handTransform.RigidInverse();

                numberOfFingers += hands[h].Fingers.Count;

                if (leapHand.IsLeft)
                {
                    currentHand = "100";
                }
                if (leapHand.IsRight)
                {
                    currentHand = "101";
                }

                fingerPositionString += currentHand + ",";

                if (leapHand.Fingers.Count > 0)
                {
                    writeToFile = true;
                    for (int f = 0; f < leapHand.Fingers.Count; f++)
                    {
                        Finger leapFinger = leapHand.Fingers[f];
                        Vector transformedPosition = handTransform.TransformPoint(leapFinger.TipPosition);
                        Vector transformedDirection = handTransform.TransformDirection(leapFinger.Direction);
                        float fingerLength = leapFinger.Length;

                        Finger nextLeapFinger = null;
                        Vector nextFingerPosition = new Vector();

                        if (f == 0)
                        {
                            nextLeapFinger = leapHand.Fingers[1];
                            nextFingerPosition = handTransform.TransformPoint(nextLeapFinger.TipPosition);
                        }
                        else if (f == 1)
                        {
                            nextLeapFinger = leapHand.Fingers[2];
                            nextFingerPosition = handTransform.TransformPoint(nextLeapFinger.TipPosition);
                        }
                        else if (f == 2)
                        {
                            nextLeapFinger = leapHand.Fingers[3];
                            nextFingerPosition = handTransform.TransformPoint(nextLeapFinger.TipPosition);
                        }
                        else if (f == 3)
                        {
                            nextLeapFinger = leapHand.Fingers[4];
                            nextFingerPosition = handTransform.TransformPoint(nextLeapFinger.TipPosition);
                        }
                        else if (f == 4)
                        {
                            nextLeapFinger = leapHand.Fingers[0];
                            nextFingerPosition = handTransform.TransformPoint(nextLeapFinger.TipPosition);
                        }

                        float Dist = DistanceBetweenTwoPoints(UnityVectorExtension.ToVector3(transformedPosition), UnityVectorExtension.ToVector3(nextFingerPosition));

                        fingerPositionString += transformedPosition.x + "," + transformedPosition.y + "," + transformedPosition.z + ",";

                        fingerDistanceString += (int)Dist + ",";
                    }
                }
                else
                {
                    Debug.Log("No Fingers Found");
                    writeToFile = false;
                }
            }
            if (frame.Hands.Count > 0)
            {
                fingerPositionString += fingerDistanceString;
                fingerDistanceString = "";
                featureText = numberOfFingers + "," + fingerPositionString;
                Debug.Log(featureText + currentGestureName);
                AddFestureSetToFile();
                fingerPositionString = "";
            }
        }


        if (!controller.IsConnected)
        {
            Debug.Log("Connect Leap Motion Controller");
        }
    }

    private void AddFestureSetToFile()
    {
        try
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(dataFilePath, true))
            {
                StringBuilder sbOutput = new StringBuilder();
                sbOutput.Insert(0, featureText);
                file.WriteLine(sbOutput.ToString() + currentGestureName);
                Debug.Log("Written");
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Error Occured while writing data to file: " + ex.Message);
        }
    }

    private float DistanceBetweenTwoPoints(Vector3 p1, Vector3 p2)
    {

        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        float dz = p1.z - p2.z;

        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
