using UnityEngine;
using LightShaft.Scripts;
using UnityEngine.UI;
using Valve.VR;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Object_Controller : MonoBehaviour
{

    public enum SelectionType
    {
        SinglePinch,
        SingleFootFore,
        SingleFootHeel,
        MultiPinch,
        FootLeftRightFore,
        FootForeHeel
    }

    private enum MotionType
    {
        A,
        B
    }
    
    [Header("Selection Settings")]
    public SelectionType currentSelectionType = SelectionType.SinglePinch;
    private List<MotionType> targetsType = new List<MotionType>();
    private bool isFootClicked = false;
    private bool isPinchClicked = false;

    [Header("Controllers")]
    public TapController tapController;
    public Pinch_Controller pinch_Controller;
    public FocusObjectDetector focusObjectDetector;

    [Header("Target Objects")]
    public GameObject StartButton;
    public GameObject[] TargetObjects;
    public Material OriginalMaterial;  // Gray
    public Material NextSelectMaterialA; // Yellow
    public Material NextSelectMaterialB; // Blue
    public Material CorrectMaterial; // Green

    // [Header("Marker Settings")]
    // public GameObject MarkerPrefab;
    // private List<GameObject> Markers = new List<GameObject>();

    [Header("Cursor")]

    public ClickState_controller clickState_Controller;

    public GameObject Cursor;
    private string focusedObjectName;
    public int currentTargetIndex = -1;
    private HashSet<int> selectedIndices = new HashSet<int>();
    public bool isStartClicked = false;

    // record task data
    private float taskStartTime;
    private List<string> taskRawData = new List<string>();
    private List<string> taskClickData = new List<string>();
    private List<string> taskCursorData = new List<string>();
    public string outputFileName = "TaskRawData.csv", outputFileName2 = "TaskClickData.csv", outputFileName3 = "TaskCursorData.csv";
    private int taskIndex = 0;
    public GameObject Task;
    private Queue<(float, Vector3)> preClickPositions = new Queue<(float, Vector3)>();
    

    void Start()
    {
        ResetTargets();
    }

    void Update()
    {
        float currentTime = Time.time;
        Ray gazeRay = Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay;

        Vector3 userCenter = focusObjectDetector.userCenter;
        float userDistance = Vector3.Distance(userCenter, TargetObjects[0].transform.position);
        Vector3 intersection = CalculateIntersection(userCenter, userDistance, gazeRay.origin, gazeRay.direction);
        taskCursorData.Add($"Cursor,{currentTime:F3},{intersection.x:F3},{intersection.y:F3},{intersection.z:F3}");
        
    }
    void OnEnable()
    {
        FocusObjectDetector.OnFocusObjectNameDetected += UpdateFocusedObjectName;
        pinch_Controller.OnSelectDetected += HandlePinchSelect;
        tapController.OnSelectDetected += HandleFootSelect;
    }

    void OnDisable()
    {
        FocusObjectDetector.OnFocusObjectNameDetected -= UpdateFocusedObjectName;
        pinch_Controller.OnSelectDetected -= HandlePinchSelect;
        tapController.OnSelectDetected -= HandleFootSelect;
    }

    Vector3 CalculateIntersection(Vector3 center, float radius, Vector3 origin, Vector3 direction)
    {
        // 1. Calculate vector from circle center to ray origin
        Vector3 oc = origin - center;

        // 2. Calculate discriminant
        float a = Vector3.Dot(direction, direction);
        float b = 2 * Vector3.Dot(oc, direction);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float discriminant = b * b - 4 * a * c;

        // 3. Check for intersection
        if (discriminant < 0)
        {
            // No intersection
            return Vector3.zero;
        }

        // 4. Calculate t values
        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        // 5. Find intersection point(s)
        Vector3 intersection1 = origin + t1 * direction;
        Vector3 intersection2 = origin + t2 * direction;

        // 6. Check if intersection points are within the arc (optional)
        // - You would need additional information to define the arc's start and end angles.

        return intersection1; // Return the first intersection point for now
    }

    void HandlePinchSelect(Pinch_Controller.PinchType pinchType) {
        switch(currentSelectionType) {
            case SelectionType.SinglePinch:
                if(pinchType == Pinch_Controller.PinchType.Index) {
                    HandleSingleSelect();
                }
                break;
            case SelectionType.MultiPinch: 
                if(pinchType == Pinch_Controller.PinchType.Index) {
                    HandleMultiSelect(MotionType.A);
                }
                else if(pinchType == Pinch_Controller.PinchType.Middle) {
                    HandleMultiSelect(MotionType.B);
                }
                break;
            default:
                break;
        }

        // if(isStartClicked) {
        //     taskRawData.Add($"Pinch Click,{Time.time:F3}");
        //     for(int i = 0; i < TargetObjects.Length; i++)
        //     {
        //         taskRawData.Add($"Target,{i:0},{TargetObjects[i].transform.position.x:F3},{TargetObjects[i].transform.position.y:F3},{TargetObjects[i].transform.position.z:F3}");
        //     }
        //     taskRawData.Add($"Plate Position,{Task.transform.position.x:F3},{Task.transform.position.y:F3},{Task.transform.position.z:F3}");
        //     taskRawData.Add($"Gaze Ray(position,direction),{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.origin.x:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.origin.y:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.origin.z:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.direction.x:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.direction.y:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.direction.z:F3}");
        //     Ray gazeRay = Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay;

        //     Vector3 userCenter = focusObjectDetector.userCenter;
        //     float userDistance = Vector3.Distance(userCenter, TargetObjects[0].transform.position);

        //     Vector3 intersection = CalculateIntersection(userCenter, userDistance, gazeRay.origin, gazeRay.direction);
        //     Debug.Log($"Intersection point: {intersection}");
        //     Vector3 positionA = TargetObjects[currentTargetIndex].transform.position;
        //     float distance = Vector3.Distance(positionA, intersection);
        //     float TargetdistanceToOrigin = Vector3.Distance(positionA, Task.transform.position);
        //     float distanceToOrigin = Vector3.Distance(intersection, Task.transform.position);
        //     if(distanceToOrigin < TargetdistanceToOrigin)
        //     {
        //         distance *= -1;
        //         taskClickData.Add($"Pinch Click,{Time.time:F3},{currentTargetIndex:0},{distance:F3}");
        //     }
        //     else 
        //     {
        //         taskClickData.Add($"Pinch Click,{Time.time:F3},{currentTargetIndex:0},{distance:F3}");
        //     }
        // }
        // HandleClickSelect();
    }



    void HandleFootSelect(TapController.FootType footType) {
        switch(currentSelectionType) {
            case SelectionType.SingleFootFore:
                if(footType == TapController.FootType.RightFore) {
                    HandleSingleSelect();
                }
                break;
            case SelectionType.SingleFootHeel:
                if(footType == TapController.FootType.RightHeel) {
                    HandleSingleSelect();
                }
                break;
            case SelectionType.FootLeftRightFore:
                if(footType == TapController.FootType.LeftFore) {
                    HandleMultiSelect(MotionType.A);
                }
                else if(footType == TapController.FootType.RightFore) {
                    HandleMultiSelect(MotionType.B);
                }
                break;
            case SelectionType.FootForeHeel:
                if(footType == TapController.FootType.RightFore) {
                    HandleMultiSelect(MotionType.A);
                }
                else if(footType == TapController.FootType.RightHeel) {
                    HandleMultiSelect(MotionType.B);
                }
                break;
            default:
                break;
        }

        // if(isStartClicked) {
        //     taskRawData.Add($"Foot Click,{Time.time:F3}");
        //     for(int i = 0; i < TargetObjects.Length; i++)
        //     {
        //         taskRawData.Add($"Target,{i:0},{TargetObjects[i].transform.position.x:F3},{TargetObjects[i].transform.position.y:F3},{TargetObjects[i].transform.position.z:F3}");
        //     }
        //     taskRawData.Add($"Plate Position,{Task.transform.position.x:F3},{Task.transform.position.y:F3},{Task.transform.position.z:F3}");
        //     taskRawData.Add($"Gaze Ray(position,direction),{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.origin.x:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.origin.y:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.origin.z:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.direction.x:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.direction.y:F3},{Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay.direction.z:F3}");
        //     Ray gazeRay = Cursor.GetComponent<FocusObjectDetector>().FiltedGazeRay;

        //     Vector3 userCenter = focusObjectDetector.userCenter;
        //     float userDistance = Vector3.Distance(userCenter, TargetObjects[0].transform.position);

        //     Vector3 intersection = CalculateIntersection(userCenter, userDistance, gazeRay.origin, gazeRay.direction);
        //     Debug.Log($"Intersection point: {intersection}");
        //     Vector3 positionA = TargetObjects[currentTargetIndex].transform.position;
        //     float distance = Vector3.Distance(positionA, intersection);
        //     float TargetdistanceToOrigin = Vector3.Distance(positionA, Task.transform.position);
        //     float distanceToOrigin = Vector3.Distance(intersection, Task.transform.position);
        //     if(distanceToOrigin < TargetdistanceToOrigin)
        //     {
        //         distance *= -1;
        //         taskClickData.Add($"Foot Click,{Time.time:F3},{currentTargetIndex:0},{distance:F3}");
        //     }
        //     else 
        //     {
        //         taskClickData.Add($"Foot Click,{Time.time:F3},{currentTargetIndex:0},{distance:F3}");
        //     }
        // }
        // HandleClickSelect();
    }


    void HandleSingleSelect()
    {

        if (!isStartClicked)
        {
            if (focusedObjectName == StartButton.name)
            {
                StartButton.SetActive(false);
                PrepareRecording();
                isStartClicked = true;
                ActivateTargets();
                HighlightTarget(currentTargetIndex);
                Debug.Log("Start button clicked. Targets activated.");
            }
            return;
        }
        else
        {
            if(focusedObjectName == TargetObjects[currentTargetIndex].name)
            {
                MarkTargetAsCorrect(currentTargetIndex);
                SwitchToNextTarget();
                HighlightTarget(currentTargetIndex, NextSelectMaterialA);
            }
        }
    }


    void HandleMultiSelect(MotionType motionType)
    {
        if (!isStartClicked)
        {
            if (focusedObjectName == StartButton.name)
            {
                StartButton.SetActive(false);
                PrepareRecording();
                isStartClicked = true;
                ActivateTargets();
                GenerateNewTargetsType();
                if (targetsType[0] == MotionType.A)
                {
                    HighlightTarget(currentTargetIndex, NextSelectMaterialA);
                }
                else
                {
                    HighlightTarget(currentTargetIndex, NextSelectMaterialB);
                }
                Debug.Log("Start button clicked. Targets activated.");
            }
            return;
        }
        else
        {
            if(focusedObjectName == TargetObjects[currentTargetIndex].name && motionType == targetsType[0])
            {
                targetsType.RemoveAt(0);
                MarkTargetAsCorrect(currentTargetIndex);
                SwitchToNextTarget();
                if (targetsType[0] == MotionType.A)
                {
                    HighlightTarget(currentTargetIndex, NextSelectMaterialA);
                }
                else
                {
                    HighlightTarget(currentTargetIndex, NextSelectMaterialB);
                }
            }
        }
        
    }


    // void HandleClickSelect()
    // {
    //     float currentTime = Time.time;

    //     if (!isStartClicked)
    //     {
    //         if (focusedObjectName == StartButton.name)
    //         {
    //             StartButton.SetActive(false);
    //             PrepareRecording();
    //             isStartClicked = true;
    //             ActivateTargets();
    //             Debug.Log("Start button clicked. Targets activated.");
    //         }
    //         return;
    //     }

    //     if(currentSelectionType == SelectionType.Click)
    //     {
    //         if (currentTargetIndex != -1) {
    //             if (focusedObjectName == TargetObjects[currentTargetIndex].name)
    //             {
    //                 MarkTargetAsCorrect(currentTargetIndex);
    //                 SelectNextTarget();
    //             }
    //         }
    //     }

    // }

    void ResetTargets()
    {
        foreach (var target in TargetObjects)
        {
            ChangeMaterial(target, OriginalMaterial);
        }
        selectedIndices.Clear();
        targetsType.Clear();
        currentTargetIndex = -1;
    }

    void PrepareRecording()
    {
        float currentTime = Time.time;
        taskRawData.Add("Event,Time,Position");
        taskRawData.Add($"Task {taskIndex} Start,{Time.time:F3}");
        taskClickData.Add("Event,Time,currentTargetIndex,Distance to target");
        taskClickData.Add($"Task {taskIndex} Start,{Time.time:F3}");
        taskIndex++;
        for(int i = 0; i < TargetObjects.Length; i++)
        {
            taskRawData.Add($"Target,{i:0},{TargetObjects[i].transform.position.x:F3},{TargetObjects[i].transform.position.y:F3},{TargetObjects[i].transform.position.z:F3}");
        }
        taskRawData.Add($"Plate Position,{Task.transform.position.x:F3},{Task.transform.position.y:F3},{Task.transform.position.z:F3}");
    }

    void ActivateTargets()
    {
        foreach (var target in TargetObjects)
        {
            target.SetActive(true);
        }

        currentTargetIndex = Random.Range(0, TargetObjects.Length);

        Debug.Log($"First random target: {TargetObjects[currentTargetIndex].name}");
    }

    void GenerateNewTargetsType() {
        int rnd = Random.Range(0, 2);
        if(rnd == 0)
        {
            targetsType.AddRange(Enumerable.Repeat(MotionType.A, 5));
            targetsType.AddRange(Enumerable.Repeat(MotionType.B, 6));
            targetsType = targetsType.OrderBy(x => Random.value).ToList();
        }
        else
        {
            targetsType.AddRange(Enumerable.Repeat(MotionType.A, 6));
            targetsType.AddRange(Enumerable.Repeat(MotionType.B, 5));
            targetsType = targetsType.OrderBy(x => Random.value).ToList();
        }
    }

    float CalculateTotalDisplacement(List<Vector3> positions)
    {
        float totalDisplacement = 0f;

        for (int i = 1; i < positions.Count; i++)
        {
            totalDisplacement += Vector3.Distance(positions[i - 1], positions[i]);
        }

        return totalDisplacement;
    }

    void SwitchToNextTarget()
    {
        List<int> nextCandidates = new List<int>
        {
            (currentTargetIndex + 5) % 11,
            (currentTargetIndex + 6) % 11
        };

        // The reason for reversing the list is to randomize the order of the next candidates
        // So, if both candidates are valid, the next target will be selected randomly
        int rndIndex = Random.Range(0, 2);
        if(rndIndex != 0)
        {
            nextCandidates.Reverse();
        }

        foreach (int candidate in nextCandidates)
        {
            if (!selectedIndices.Contains(candidate))
            {
                currentTargetIndex = candidate;
                return;
            }
        }

        Debug.Log("All targets selected. Task complete.");
        EndTask();
    }

    void HighlightTarget(int index, Material material)
    {
        ChangeMaterial(TargetObjects[index], material);
        // if(targetsType.Count > 0)
        // {
        //     if(targetsType[0] == MotionType.A)
        //     {
        //         ChangeMaterial(TargetObjects[index], NextSelectMaterialA);
        //     }
        //     else
        //     {
        //         ChangeMaterial(TargetObjects[index], NextSelectMaterialB);
        //     }
        // }
        // else
        // {
        //     ChangeMaterial(TargetObjects[index], NextSelectMaterialA);
        //     Debug.Log($"Next target: {TargetObjects[index].name}");
        // }
    }

    void MarkTargetAsCorrect(int index)
    {
        selectedIndices.Add(index);
        ChangeMaterial(TargetObjects[index], CorrectMaterial);
        Debug.Log($"Target {TargetObjects[index].name} marked as correct.");
    }

    void EndTask()
    {
        ResetTargets();
        float taskEndTime = Time.time;
        taskRawData.Add($"Task End,{taskEndTime:F3}");
        taskClickData.Add($"Task End,{taskEndTime:F3}");
        WriteToFile();
        Debug.Log($"Task ended at {taskEndTime:F3}. Data written to file.");
        StartButton.SetActive(true);
        isStartClicked = false;
    }


    void UpdateFocusedObjectName(string name)
    {
        float currentTime = Time.time;
        if (name != focusedObjectName)
        {
            focusedObjectName = name;
            Debug.Log($"Focus recorded for {name} at {currentTime:F3}");
        }
        else if (name == null)
        {
            focusedObjectName = null;
        }

    }

    void ChangeMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            materials[0] = material;
            renderer.materials = materials;
        }
        else
        {
            Debug.LogWarning("Renderer not found on target object!");
        }
    }

    // void GenerateMarker(Vector3 position)
    // {
    //     if (MarkerPrefab != null)
    //     {
    //         GameObject marker = Instantiate(MarkerPrefab, position, Quaternion.identity);
    //         marker.transform.localScale = Vector3.one * 0.05f; // Adjust the size of the sphere
    //         marker.SetActive(false);
    //         Markers.Add(marker); 
    //         Debug.Log($"Marker created at {position}");
    //     }
    //     else
    //     {
    //         Debug.LogWarning("MarkerPrefab is not assigned!");
    //     }
    // }

    void WriteToFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, outputFileName);
        string filePath2 = Path.Combine(Application.persistentDataPath, outputFileName2);
        string filePath3 = Path.Combine(Application.persistentDataPath, outputFileName3);
        File.WriteAllLines(filePath, taskRawData);
        File.WriteAllLines(filePath2, taskClickData);
        File.WriteAllLines(filePath3, taskCursorData);
        Debug.Log($"Task data written to {filePath}");
    }
}
