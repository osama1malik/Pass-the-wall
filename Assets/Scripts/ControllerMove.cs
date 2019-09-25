using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;
using System.Threading.Tasks;

public class ControllerMove : MonoBehaviour
{
    string fingerPositionString = "";
    string fingerDistanceString = "";
    int numberOfFingers;
    string currentHand = "";
    string currentGestureName = "";
    string gestureData = "";

    public GameObject model3D;

    private string TrainingDataPath = @"dataset.csv";
    private string TestingDataPath = @"testDataset.csv";

    MLContext context;
    ITransformer model;
    bool trained = false;
    string a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v;


    static readonly string _dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "dataset.csv");
    static readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "custClusteringModel.zip");

    string recognizedGesture;

    Vector previousFrameHandPosition;

    float speed = 0.18f;
    // Start is called before the first frame update
    void Start()
    {

        TrainGesture();
        //GestureData data = new GestureData()
        //{
        //    FingerCount = 5,
        //    HandType = 100,
        //    ThumbX = 20.16582F,
        //    ThumbY = -52.95452F,
        //    ThumbZ = -25.26981F,
        //    IndexX = 16.76583F,
        //    IndexY = -32.51812F,
        //    IndexZ = 10.95229F,
        //    MiddleX = 7.48679F,
        //    MiddleY = -29.70361F,
        //    MiddleZ = 19.17374F,
        //    RingX = -2.938736F,
        //    RingY = -26.24084F,
        //    RingZ = 22.8723F,
        //    PinkyX = -15.13478F,
        //    PinkyY = -23.20125F,
        //    PinkyZ = 17.12656F,
        //    ThumbIndex = 41,
        //    IndexMiddle = 12,
        //    MiddleRing = 11,
        //    RingPinky = 13,
        //    PinkyThumb = 62
        //};
        //TrainGesture(data);
    }

    // Update is called once per frame
    void Update()
    {
        Controller controller = new Controller();
        if (controller.IsConnected)
        {
            generateDataForGesture(controller);

            string[] data = gestureData.Split(',');

            a = data[0]; b = data[1]; c = data[2]; d = data[3]; e = data[4]; f = data[5]; g = data[6]; h = data[7]; i = data[8]; j = data[9]; k = data[10]; l = data[11]; m = data[12]; n = data[13]; o = data[14]; p = data[15]; q = data[16]; r = data[17]; s = data[18]; t = data[19]; u = data[20]; v = data[21];

            GestureData model = new GestureData()
            {
                FingerCount = float.Parse(a),
                HandType = float.Parse(b),
                ThumbX = float.Parse(c),
                ThumbY = float.Parse(d),
                ThumbZ = float.Parse(e),
                IndexX = float.Parse(f),
                IndexY = float.Parse(g),
                IndexZ = float.Parse(h),
                MiddleX = float.Parse(i),
                MiddleY = float.Parse(j),
                MiddleZ = float.Parse(k),
                RingX = float.Parse(l),
                RingY = float.Parse(m),
                RingZ = float.Parse(n),
                PinkyX = float.Parse(o),
                PinkyY = float.Parse(p),
                PinkyZ = float.Parse(q),
                ThumbIndex = float.Parse(r),
                IndexMiddle = float.Parse(s),
                MiddleRing = float.Parse(t),
                RingPinky = float.Parse(u),
                PinkyThumb = float.Parse(v)
            };

            PredictGesture(model, controller);
        }

        if (!controller.IsConnected)
        {
            Debug.Log("Connect Leap Motion Controller");
        }
    }

    private void generateDataForGesture(Controller controller)
    {
        //Controller controller = new Controller();

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
        gestureData = numberOfFingers + "," + fingerPositionString;
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
        var dataLocation = "./dataset.csv";

        context = new MLContext();

        var textLoader = context.Data.CreateTextLoader(new[]
        {
            new TextLoader.Column("FingerCount", DataKind.Single, 0),
            new TextLoader.Column("HandType", DataKind.Single, 1),
            new TextLoader.Column("ThumbX", DataKind.Single, 2),
            new TextLoader.Column("ThumbY", DataKind.Single, 3),
            new TextLoader.Column("ThumbZ", DataKind.Single, 4),
            new TextLoader.Column("IndexX", DataKind.Single, 5),
            new TextLoader.Column("IndexY", DataKind.Single, 6),
            new TextLoader.Column("IndexZ", DataKind.Single, 7),
            new TextLoader.Column("MiddleX", DataKind.Single, 8),
            new TextLoader.Column("MiddleY", DataKind.Single, 9),
            new TextLoader.Column("MiddleZ", DataKind.Single, 10),
            new TextLoader.Column("RingX", DataKind.Single, 11),
            new TextLoader.Column("RingY", DataKind.Single, 12),
            new TextLoader.Column("RingZ", DataKind.Single, 13),
            new TextLoader.Column("PinkyX", DataKind.Single, 14),
            new TextLoader.Column("PinkyY", DataKind.Single, 15),
            new TextLoader.Column("PinkyZ", DataKind.Single, 16),
            new TextLoader.Column("ThumbIndex", DataKind.Single, 17),
            new TextLoader.Column("IndexMiddle", DataKind.Single, 18),
            new TextLoader.Column("MiddleRing", DataKind.Single, 19),
            new TextLoader.Column("RingPinky", DataKind.Single, 20),
            new TextLoader.Column("PinkyThumb", DataKind.Single, 21),
            new TextLoader.Column("Label", DataKind.Single, 22)
            },
        hasHeader: true,
        separatorChar: ',');

        IDataView data = textLoader.Load(dataLocation);

        var trainTestData = context.Data.TrainTestSplit(data, testFraction: 0.2);

        var pipeline = context.Transforms.Concatenate("Features", "FingerCount", "HandType", "ThumbX", "ThumbY", "ThumbZ", "IndexX", "IndexY",
            "IndexZ", "MiddleX", "MiddleY", "MiddleZ", "RingX", "RingY", "RingZ", "PinkyX", "PinkyY", "PinkyZ", "ThumbIndex", "IndexMiddle",
            "MiddleRing", "RingPinky", "PinkyThumb", "Label")
            .Append(context.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: 6));

        var preview = trainTestData.TrainSet.Preview();

        model = pipeline.Fit(trainTestData.TrainSet);

        var predictions = model.Transform(trainTestData.TestSet);
        
        var metrics = context.Clustering.Evaluate(predictions, scoreColumnName: "Score", featureColumnName: "Features");

    }

    void PredictGesture(GestureData gs, Controller c)
    {
        var predictionFunc = context.Model.CreatePredictionEngine<GestureData, GesturePrediction>(model);

        var prediction = predictionFunc.Predict(gs);

        int gestureId = Convert.ToInt32(prediction.SelectedClusterId);

        switch (gestureId)
        {
            case 1:
                recognizedGesture = "LeftHandSlide";
                break;
            case 2:
                recognizedGesture = "LeftHandClose";
                break;
            case 3:
                recognizedGesture = "RightHandSlide";
                break;
            case 4:
                recognizedGesture = "RightHandClose";
                break;
            case 5:
                recognizedGesture = "LeftHandGrip";
                break;
            case 6:
                recognizedGesture = "RightHandGrip";
                break;
            default:
                recognizedGesture = "No Gesture Found";
                break;
        }

        if (recognizedGesture.Equals("LeftHandSlide") || recognizedGesture.Equals("RightHandSlide"))
        {
            Frame frame = c.Frame();
            Hand hand = frame.Hands[0];
            Vector palmPos = hand.PalmPosition;

            if (previousFrameHandPosition == null)
            {
                previousFrameHandPosition = palmPos;
            }
            else
            {
                Vector3 pos = model3D.transform.position;
                pos.x += (palmPos.x - previousFrameHandPosition.x) * speed;
                pos.y += (palmPos.y - previousFrameHandPosition.y) * speed;
                //pos.z += (palmPos.z - previousFrameHandPosition.z) * speed;
                model3D.transform.position = new Vector3(pos.x, pos.y, pos.z);
                previousFrameHandPosition = palmPos;
            }
        }
        if (recognizedGesture.Equals("LeftHandGrip") || recognizedGesture.Equals("RightHandGrip"))
        {
            Frame frame = c.Frame();
            Hand hand = frame.Hands[0];
            Vector palmPos = hand.PalmPosition;

            if (previousFrameHandPosition == null)
            {
                previousFrameHandPosition = palmPos;
            }
            else
            {
                Vector3 pos = model3D.transform.position;
                pos.x += (palmPos.x - previousFrameHandPosition.x) * speed;
                pos.y += (palmPos.y - previousFrameHandPosition.y) * speed;
                //pos.z += (palmPos.z - previousFrameHandPosition.z) * speed;
                model3D.transform.position = new Vector3(pos.x, pos.y, pos.z);
                previousFrameHandPosition = palmPos;
            }
        }
        if(recognizedGesture.Equals("LeftHandClose") || recognizedGesture.Equals("RightHandClose")){

        }

        Debug.Log($"Prediction - {recognizedGesture}");
        
    }
    
}

internal class GesturePrediction
{
    [ColumnName("PredictedLabel")]
    public uint SelectedClusterId;
    [ColumnName("Score")]
    public float[] Distance;
    //[ColumnName("Label")]
    //public uint SelectedLabelId;
}

internal class GestureData
{
    [LoadColumn(0)]
    public float FingerCount { get; set; }
    [LoadColumn(1)]
    public float HandType { get; set; }
    [LoadColumn(2)]
    public float ThumbX { get; set; }
    [LoadColumn(3)]
    public float ThumbY { get; set; }
    [LoadColumn(4)]
    public float ThumbZ { get; set; }
    [LoadColumn(5)]
    public float IndexX { get; set; }
    [LoadColumn(6)]
    public float IndexY { get; set; }
    [LoadColumn(7)]
    public float IndexZ { get; set; }
    [LoadColumn(8)]
    public float MiddleX { get; set; }
    [LoadColumn(9)]
    public float MiddleY { get; set; }
    [LoadColumn(10)]
    public float MiddleZ { get; set; }
    [LoadColumn(11)]
    public float RingX { get; set; }
    [LoadColumn(12)]
    public float RingY { get; set; }
    [LoadColumn(13)]
    public float RingZ { get; set; }
    [LoadColumn(14)]
    public float PinkyX { get; set; }
    [LoadColumn(15)]
    public float PinkyY { get; set; }
    [LoadColumn(16)]
    public float PinkyZ { get; set; }
    [LoadColumn(17)]
    public float ThumbIndex { get; set; }
    [LoadColumn(18)]
    public float IndexMiddle { get; set; }
    [LoadColumn(19)]
    public float MiddleRing { get; set; }
    [LoadColumn(20)]
    public float RingPinky { get; set; }
    [LoadColumn(21)]
    public float PinkyThumb { get; set; }
    [LoadColumn(22), ColumnName("Label")]
    public float Gesture { get; set; }

}