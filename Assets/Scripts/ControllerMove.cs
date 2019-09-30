using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using System;
using System.IO;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.ML;
using System.Linq;

public class ControllerMove : MonoBehaviour
{
    //Dataset and Test Data Paths
    private string TrainingDataPath = @"dataset.csv";
    private string TestingDataPath = @"testDataset.csv";
    private string TwoTrainingDataPath = @"twoHandsDataset.csv";
    private string TwoTestingDataPath = @"twoHandsTestDataset.csv";

    //Matrix used in training of data
    Matrix<float> TrainData;
    Matrix<float> TestData;
    Matrix<int> TrainLabel;
    Matrix<int> TestLabel;
    Matrix<float> TwoTrainData;
    Matrix<float> TwoTestData;
    Matrix<int> TwoTrainLabel;
    Matrix<int> TwoTestLabel;

    //Matrix used in recognization of gestures
    Matrix<float> DataUnderObservation;
    Matrix<float> TwoDataUnderObservation;

    //SVM Objects used to train data
    SVM svm;
    SVM twoSvm;

    //Gameobject of model using in the scene
    public GameObject model3D;

    //Strings used in and after recognization of gestures
    string gestureData = "";
    string recognizedGesture;

    //Some data used after recognization after recognization
    Vector previousFrameHandPosition;
    float speed = 0.18f;
    float currentDist = 0.0f;
    float prevDist = 0.0f;

    //Intilizations for Scaling, Translation & Rotation of models;
    Quaternion modelRotation; float x, y, z;
    Vector3 modelPosition; float xx, yy, zz;

    // Start is called before the first frame update
    void Start()
    {
        LoadTrainData();
        LoadTestData();
        TwoLoadTrainData();
        TwoLoadTestData();
        TrainGesture();

        modelRotation = model3D.transform.rotation;
        x = modelRotation.x;
        y = modelRotation.y;
        z = modelRotation.z;

        modelPosition = model3D.transform.position;
        xx = modelPosition.x;
        yy = modelPosition.y;
        zz = modelPosition.z;

    }

    // Update is called once per frame
    void Update()
    {
        Controller controller = new Controller();
        if (controller.IsConnected)
        {
            generateDataForGesture(controller);

            Frame frame = controller.Frame();

            if (frame.Hands.Count == 1)
            {
                List<float[]> trainList = new List<float[]>();

                float[] data = gestureData.Split(',').Select(x => float.Parse(x)).ToArray();

                trainList.Add(data);

                DataUnderObservation = new Matrix<float>(To2D<float>(trainList.ToArray()));

                PredictGesture(controller, 1);
            }
            else if (frame.Hands.Count == 2)
            {
                List<float[]> trainList = new List<float[]>();

                float[] data = gestureData.Split(',').Select(x => float.Parse(x)).ToArray();

                trainList.Add(data);

                DataUnderObservation = new Matrix<float>(To2D<float>(trainList.ToArray()));

                PredictGesture(controller, 2);
            }
        }
        if (!controller.IsConnected)
        {
            Debug.Log("Connect Leap Motion Controller");
        }
    }

    private void LoadTrainData()
    {
        List<float[]> trainList = new List<float[]>();
        List<int> trainLabel = new List<int>();

        StreamReader reader = new StreamReader(TrainingDataPath);

        string line = "";
        if (!File.Exists(TrainingDataPath))
        {
            throw new Exception("File Not found");
        }

        while ((line = reader.ReadLine()) != null)
        {
            int firstIndex = line.IndexOf(',');
            int currentLabel = Convert.ToInt32(line.Substring(0, firstIndex));
            string currentData = line.Substring(firstIndex + 1);
            float[] data = currentData.Split(',').Select(x => float.Parse(x)).ToArray();

            trainList.Add(data);
            trainLabel.Add(currentLabel);
        }

        TrainData = new Matrix<float>(To2D<float>(trainList.ToArray()));
        TrainLabel = new Matrix<int>(trainLabel.ToArray());
    }

    private void LoadTestData()
    {
        List<float[]> trainList = new List<float[]>();
        List<int> trainLabel = new List<int>();

        StreamReader reader = new StreamReader(TestingDataPath);

        string line = "";
        if (!File.Exists(TestingDataPath))
        {
            throw new Exception("File Not found");
        }

        while ((line = reader.ReadLine()) != null)
        {
            int firstIndex = line.IndexOf(',');
            int currentLabel = Convert.ToInt32(line.Substring(0, firstIndex));
            string currentData = line.Substring(firstIndex + 1);
            float[] data = currentData.Split(',').Select(x => float.Parse(x)).ToArray();

            trainList.Add(data);
            trainLabel.Add(currentLabel);
        }

        TestData = new Matrix<float>(To2D<float>(trainList.ToArray()));
        TestLabel = new Matrix<int>(trainLabel.ToArray());
    }

    private void TwoLoadTrainData()
    {
        List<float[]> trainList = new List<float[]>();
        List<int> trainLabel = new List<int>();

        StreamReader reader = new StreamReader(TwoTrainingDataPath);

        string line = "";
        if (!File.Exists(TwoTrainingDataPath))
        {
            throw new Exception("File Not found");
        }

        while ((line = reader.ReadLine()) != null)
        {
            int firstIndex = line.IndexOf(',');
            int currentLabel = Convert.ToInt32(line.Substring(0, firstIndex));
            string currentData = line.Substring(firstIndex + 1);
            float[] data = currentData.Split(',').Select(x => float.Parse(x)).ToArray();

            trainList.Add(data);
            trainLabel.Add(currentLabel);
        }

        TwoTrainData = new Matrix<float>(To2D<float>(trainList.ToArray()));
        TwoTrainLabel = new Matrix<int>(trainLabel.ToArray());
    }

    private void TwoLoadTestData()
    {
        List<float[]> trainList = new List<float[]>();
        List<int> trainLabel = new List<int>();

        StreamReader reader = new StreamReader(TwoTestingDataPath);

        string line = "";
        if (!File.Exists(TwoTestingDataPath))
        {
            throw new Exception("File Not found");
        }

        while ((line = reader.ReadLine()) != null)
        {
            int firstIndex = line.IndexOf(',');
            int currentLabel = Convert.ToInt32(line.Substring(0, firstIndex));
            string currentData = line.Substring(firstIndex + 1);
            float[] data = currentData.Split(',').Select(x => float.Parse(x)).ToArray();

            trainList.Add(data);
            trainLabel.Add(currentLabel);
        }

        TwoTestData = new Matrix<float>(To2D<float>(trainList.ToArray()));
        TwoTestLabel = new Matrix<int>(trainLabel.ToArray());
    }

    private T[,] To2D<T>(T[][] source)
    {
        try
        {
            int FirstDim = source.Length;
            int SecondDim = source.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

            var result = new T[FirstDim, SecondDim];
            for (int i = 0; i < FirstDim; ++i)
                for (int j = 0; j < SecondDim; ++j)
                    result[i, j] = source[i][j];

            return result;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("The given jagged array is not rectangular.");
        }
    }

    private void generateDataForGesture(Controller controller)
    {
        string fingerPositionString = "";
        string fingerDistanceString = "";
        int numberOfFingers;
        string currentHand = "";

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
        }
        fingerPositionString += fingerDistanceString;
        fingerDistanceString = "";
        gestureData = (numberOfFingers + "," + fingerPositionString).TrimEnd(',');
        fingerPositionString = "";

    }

    private float DistanceBetweenTwoPoints(Vector3 p1, Vector3 p2)
    {
        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        float dz = p1.z - p2.z;

        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private void TrainGesture()
    {
        try
        {
            if (File.Exists("svm.txt"))
            {
                svm = new SVM();
                FileStorage file = new FileStorage("svm.txt", FileStorage.Mode.Read);
                svm.Read(file.GetNode("opencv_ml_svm"));
            }
            else
            {
                svm = new SVM();
                svm.C = 100;
                svm.Type = SVM.SvmType.CSvc;
                svm.Gamma = 0.005;
                svm.SetKernel(SVM.SvmKernelType.Linear);
                svm.TermCriteria = new MCvTermCriteria(1000, 1e-6);
                svm.Train(TrainData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, TrainLabel);
                svm.Save("svm.txt");
            }
            Debug.Log("SVM is trained.");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }

        try
        {
            if (File.Exists("twoooSvm.txt"))
            {
                twoSvm = new SVM();
                FileStorage file = new FileStorage("twoooSvm.txt", FileStorage.Mode.Read);
                twoSvm.Read(file.GetNode("opencv_ml_svm"));
            }
            else
            {
                twoSvm = new SVM();
                twoSvm.C = 100;
                twoSvm.Type = SVM.SvmType.CSvc;
                twoSvm.Gamma = 0.005;
                twoSvm.SetKernel(SVM.SvmKernelType.Linear);
                twoSvm.TermCriteria = new MCvTermCriteria(1000, 1e-6);
                twoSvm.Train(TwoTrainData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, TwoTrainLabel);
                //twoSvm.Save("twooSvm.txt");
            }
            Debug.Log("Two Hands SVM is trained.");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    void PredictGesture(Controller c, int h)
    {
        if (DataUnderObservation == null && TwoDataUnderObservation == null)
        {
            return;
        }

        if (svm == null && twoSvm == null)
        {
            return;
        }

        //Try Catch Block For Prediction of gesture
        try
        {
            for (int i = 0; i < DataUnderObservation.Rows; i++)
            {
                Matrix<float> row = DataUnderObservation.GetRow(i);
                float predict = 0;
                if (h == 1)
                    predict = svm.Predict(row);
                else if (h == 2)
                    predict = twoSvm.Predict(row);

                switch (predict)
                {
                    case 0:
                        recognizedGesture = "LeftHandSlide";
                        break;
                    case 1:
                        recognizedGesture = "LeftHandClose";
                        break;
                    case 2:
                        recognizedGesture = "RightHandSlide";
                        break;
                    case 3:
                        recognizedGesture = "RightHandClose";
                        break;
                    case 4:
                        recognizedGesture = "RightHandGrip";
                        break;
                    case 5:
                        recognizedGesture = "LeftHandGrip";
                        break;
                    case 6:
                        recognizedGesture = "TwoHandsSlide";
                        break;
                    case 7:
                        recognizedGesture = "TwoHandsGrip";
                        break;
                    case 8:
                        recognizedGesture = "LeftCloseRightGrip";
                        break;
                    case 9:
                        recognizedGesture = "RightCloseLeftGrip";
                        break;
                    default:
                        recognizedGesture = "No Gesture Found";
                        break;
                }
                Debug.Log("Predicted Label: " + recognizedGesture);
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            recognizedGesture = "No Gesture Found";
        }

        if (recognizedGesture.Equals("LeftHandSlide") || recognizedGesture.Equals("RightHandSlide"))
        {
            Frame frame = c.Frame();
            Hand hand = frame.Hands[0];
            Vector palmPos = hand.PalmPosition;

            if (previousFrameHandPosition.x == 0)
            {
                previousFrameHandPosition = palmPos;
                return;
            } else
            {
                float diffX = (palmPos.x - previousFrameHandPosition.x) * speed;
                float diffY = (palmPos.y - previousFrameHandPosition.y) * speed;
                float diffZ = (palmPos.z - previousFrameHandPosition.z) * speed;

                model3D.transform.position = new Vector3((xx + diffX), (yy + diffY), (zz + diffZ));
            }

        }

        if (recognizedGesture.Equals("TwoHandsSlide"))
        {
            Frame frame = c.Frame();

            for (int i = 0; i < frame.Hands.Count; i++)
            {
                Hand hand = frame.Hands[i];

                float x = hand.PalmVelocity.x;

                if (x < 0)
                {
                    x = (-1) * x;
                }

                if (x > 40)
                {
                    Vector palmPos = hand.PalmPosition;

                    if (previousFrameHandPosition == null)
                    {
                        previousFrameHandPosition = palmPos;
                        return;
                    }
                    else
                    {
                        float diffX = (palmPos.x - previousFrameHandPosition.x) * speed;
                        float diffY = (palmPos.y - previousFrameHandPosition.y) * speed;
                        float diffZ = (palmPos.z - previousFrameHandPosition.z) * speed;

                        model3D.transform.position = new Vector3((xx + diffX), (yy + diffY), (zz + diffZ));
                    }
                }

            }
        }

        if (recognizedGesture.Equals("LeftHandGrip") || recognizedGesture.Equals("RightHandGrip"))
        {
            previousFrameHandPosition = new Vector(0, 0, 0);
            Frame frame = c.Frame();
            Hand hand = frame.Hands[0];

            float hPitch = hand.Direction.Pitch * 30;
            float hYaw = hand.Direction.Yaw * 30;
            float hRoll = hand.Direction.Roll * 30;

            //model3D.transform.rotation = Quaternion.Euler((hPitch + x), (hYaw + y), (hRoll + z));
            //x += model3D.transform.rotation.x;
            //y += model3D.transform.rotation.y;
            //z += model3D.transform.rotation.z;
        }

        if (recognizedGesture.Equals("TwoHandsGrip"))
        {
            previousFrameHandPosition = new Vector(0, 0, 0);
            Frame frame = c.Frame();
            Hand hand = frame.Hands[0];
            Hand hand2 = frame.Hands[1];
            Vector palm = hand.PalmPosition;
            Vector palm2 = hand2.PalmPosition;
            float dist = DistanceBetweenTwoPoints(UnityVectorExtension.ToVector3(palm), UnityVectorExtension.ToVector3(palm2));
            currentDist = dist * 0.001f;
            if (prevDist == 0.0f)
            {
                prevDist = currentDist;
            }
            else
            {
                float n = (currentDist - prevDist) * 3f;
                Vector3 pos = model3D.transform.localScale;
                pos.x += n;
                pos.y += n;
                pos.z += n;
                model3D.transform.localScale = new Vector3(pos.x, pos.y, pos.z);
                prevDist = currentDist;
            }
        }

        if (recognizedGesture.Equals("LeftHandClose") || recognizedGesture.Equals("RightHandClose"))
        {
            previousFrameHandPosition = new Vector(0, 0, 0);
        }

        if (recognizedGesture.Equals("LeftCloseRightGrip "))
        {
            previousFrameHandPosition = new Vector(0, 0, 0);
        }

        if (recognizedGesture.Equals("RightCloseLeftGrip"))
        {
            previousFrameHandPosition = new Vector(0, 0, 0);
        }
    }

}