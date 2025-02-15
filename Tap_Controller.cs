using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TapController : MonoBehaviour
{
    public DataReceiver DataReceiver;
    public bool isClicked, isAltClicked, isEngaged; // check if the foot is clicked, alt-clicked, engaged (For application actions)
    public GameObject leftFore, leftRear, rightFore, rightRear; // icon for foot
    public GameObject leftForeGray, leftRearGray, rightForeGray, rightRearGray; // gray icon for foot
    public GameObject background; // background for the foot icon
    public Text state; // state text, for debugging
    public TextMeshProUGUI output; // output text on the right down corner of the screen
    // public TextMeshPro cursorText; // the text that shows the cursor action (e.g. click, alt-click)
    public GameObject cursorTarget; // cursor Gameobject
    // public Material OriginCursorMat, ClickCursorMat, AltClickCursorMat, FootEngageCursorMat, FootLeftForeCursorMat, FootLeftRearCursorMat , FootRightForeCursorMat, FootRightRearCursorMat; // cursor material
    private string alphaCode = ""; // use a number string to represent the tapping gestures
    private int curr = 0, last = 0; // the current and last tapping foot
    private bool LeftForeisUp = false, LeftRearisUp = false, RightForeisUp = false, RightRearisUp = false; // check if the foot is up
    private bool LeftForeisTapping = false, LeftRearisTapping = false, RightForeisTapping = false, RightRearisTapping = false; // check if the foot is tapping
    private float Timer = 0.0f, recogTimer = 0.0f, currClock = 0.0f, lastClock = 0.0f, syncThreshold = 0.1f; // timer 
    private bool isSynchronous = false; // check if the tapping is synchronous

    public enum FootType
    {
        LeftFore,
        LeftHeel,
        RightFore,
        RightHeel
    }
    
    public event Action<FootType> OnSelectDetected;
    
    
    public enum State
    {
        Idle,
        Tapping,
        Recognize
    }
    private State currState;
    // Start is called before the first frame update
    void Start()
    {
        Timer = 0.0f;
        recogTimer = 0.0f;

        isEngaged = true;
        ChangeIconAlpha(isEngaged, leftFore, leftForeGray);
        ChangeIconAlpha(isEngaged, leftRear, leftRearGray);
        ChangeIconAlpha(isEngaged, rightFore, rightForeGray);
        ChangeIconAlpha(isEngaged, rightRear, rightRearGray);
        ChangeAlpha(isEngaged, background);

        // cursorText.gameObject.SetActive(false);

        currState = State.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        DataReceiver.ReadInputToFootDataList();

        switch (currState) 
        {
            case State.Idle:
                UpdateIdleState();
                break;
            case State.Tapping:
                UpdateTappingState();
                break;
            case State.Recognize:
                UpdateRecogState();
                break;
            default:
                Debug.LogError("Unknown state!");
                break;
        }

        Timer += Time.deltaTime;
        currClock = Timer;
    }

    void UpdateIdleState() 
    {
        state.text = "Idle";
        
        // clock reset
        recogTimer = 0.0f;

        // clear all arrow
        curr = 0;
        last = 0;
        alphaCode = "";
        
        isSynchronous = false;
        isClicked = false;
        isAltClicked = false;
        
        TappingIconChecker(); // if there is a tapping, change the state to tapping
    }

    void UpdateTappingState() 
    {
        state.text = "Tapping";
        recogTimer += Time.deltaTime;

        
        TappingIconChecker(); // if there is a tapping, change the state to tapping
        
        // if there is no more tapping in 0.2 second, then change the state to recognize
        if (recogTimer >= 0.001f){
            ChangeState(State.Recognize);
        }
    }

    void UpdateRecogState()
    {
        state.text = "Recognize";

        // stay 0.03 second in this state
        recogTimer += Time.deltaTime;
        if (recogTimer >= 0f)
        {
            ChangeCmd(alphaCode); // pass the bool parameter to Application Scene
            ChangeState(State.Idle);
        }
    }

    void ChangeCmd(string alphaCode) {
        // if ((alphaCode == "3" || alphaCode == "1" || alphaCode == "2" || alphaCode == "4")) {
        //     OnSelectDetected?.Invoke();
        //     // isClicked = true;
        //     output.gameObject.SetActive(true);
        //     output.text = "Click";
        //     Debug.Log("Foot Click");
        //     Invoke("HideCursorText", 0.3f); // Hide after 0.3 seconds
        // }
        if ((alphaCode == "1")) {
            OnSelectDetected?.Invoke(FootType.LeftFore);
            // isClicked = true;
            output.gameObject.SetActive(true);
            output.text = "Left Fore Click";
            Debug.Log("Left Fore Click");
            Invoke("HideCursorText", 0.3f); // Hide after 0.3 seconds
        }
        else if ((alphaCode == "2")) {
            OnSelectDetected?.Invoke(FootType.LeftHeel);
            // isClicked = true;
            output.gameObject.SetActive(true);
            output.text = "Left Heel Click";
            Debug.Log("Left Heel Click");
            Invoke("HideCursorText", 0.3f); // Hide after 0.3 seconds
        }
        else if ((alphaCode == "3")) {
            OnSelectDetected?.Invoke(FootType.RightFore);
            // isClicked = true;
            output.gameObject.SetActive(true);
            output.text = "Right Fore Click";
            Debug.Log("Right Fore Click");
            Invoke("HideCursorText", 0.3f); // Hide after 0.3 seconds
        }
        else if ((alphaCode == "4")) {
            OnSelectDetected?.Invoke(FootType.RightHeel);
            // isClicked = true;
            output.gameObject.SetActive(true);
            output.text = "Right Heel Click";
            Debug.Log("Right Heel Click");
            Invoke("HideCursorText", 0.3f); // Hide after 0.3 seconds
        }

 
    }

    void ChangeState(State newState) 
    {
        currState = newState;
    }

    void CheckFootisUp(ref bool isUp, ref bool isTapping, int newTap,ref GameObject img, GameObject grayIcon, int threshold)
    {
        if (DataReceiver.footDataList[newTap] > threshold) // foot icon on 
        {
            isUp = true;
            img.SetActive(true);
        }
        else
        {
            isUp = false;
            img.SetActive(false);
        }

        if (isUp && !isTapping)
        {
            isTapping = true;
        }
        if (!isUp && isTapping)
        { 
            // then this moment is tapping (foot down)
            UpdateCurr(ref curr, ref last, newTap, ref currClock, ref lastClock);
            GenerateAlphaCode(curr, last);

            isTapping = false;
        }
    }

    void TappingIconChecker()
    {
        CheckFootisUp(ref LeftForeisUp, ref LeftForeisTapping, 1, ref leftFore, leftForeGray, 200);
        CheckFootisUp(ref LeftRearisUp, ref LeftRearisTapping, 2,ref leftRear, leftRearGray, 200);
        CheckFootisUp(ref RightForeisUp, ref RightForeisTapping, 3, ref rightFore, rightForeGray, 200);
        CheckFootisUp(ref RightRearisUp, ref RightRearisTapping, 4, ref rightRear, rightRearGray, 200);


        // if one of the foot is tapping, change the state to tapping
        if (LeftForeisUp || LeftRearisUp || RightForeisUp || RightRearisUp)
        {
            recogTimer = 0.0f;
            ChangeState(State.Tapping);
        }
    }

    void UpdateCurr(ref int curr, ref int last, int nowTapping, ref float currClock, ref float lastClock)
    {
        last = curr;
        curr = nowTapping;
        
        // if the last clock and curr clock is within the threshold, then it is synchronous
        CheckisSynchronous();
        lastClock = currClock;
    }

    void CheckisSynchronous()
    {
        if (Mathf.Abs(currClock - lastClock) <= syncThreshold)
        {
            isSynchronous = true;
        }
        else
        {
            isSynchronous = false;
        }
    }

    void GenerateAlphaCode(int end, int start)
    {
        if (isSynchronous)
        {
            // swap the start and end if start > end
            if (start > end)
            { 
                int temp = start;
                start = end;
                end = temp;
            }
            alphaCode = RemoveLastCharacter(alphaCode);
            alphaCode += start.ToString() + ((char)('a' + end - 1)).ToString();
            return;
        }
        else
        {
            // not synchronous
            alphaCode += end.ToString();
        }
    }
    
    string RemoveLastCharacter(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }
        else
        {
            return str.Substring(0, str.Length - 1);
        }
    }

    void ChangeIconAlpha(bool isEngaged, GameObject yellowIcon, GameObject grayIcon = null) {
        float alpha = 0.7f;
        if (isEngaged) alpha = 1.0f;

        Image footIcon = yellowIcon.GetComponent<Image>();
        if (footIcon != null) {
            Color color = footIcon.color;
            color.a = alpha;
            footIcon.color = color;
        }

        footIcon = grayIcon.GetComponent<Image>();
        if (footIcon != null) {
            Color color = footIcon.color;
            color.a = alpha;
            footIcon.color = color;
        }
    }

    void ChangeAlpha(bool isEngaged, GameObject yellowIcon) {
        float alpha = 0.5f;
        if (isEngaged) alpha = 1.0f;

        Image footIcon = yellowIcon.GetComponent<Image>();
        if (footIcon != null) {
            Color color = footIcon.color;
            color.a = alpha;
            footIcon.color = color;
        }
    }

    private void HideText()
    {
        output.gameObject.SetActive(false);
    }

    private void HideCursorText()
    {
        // cursorText.gameObject.SetActive(false);
        output.gameObject.SetActive(false);
    }

    private void ChangeMaterialAtIndex(GameObject target, Material newMaterial, int index)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            if (index >= 0 && index < materials.Length)
            {
                materials[index] = newMaterial;
                renderer.materials = materials; // Apply the updated materials array back to the renderer
            }
            else
            {
                Debug.LogWarning("Material index out of bounds!");
            }
        }
        else
        {
            Debug.LogWarning("Renderer not found on target object!");
        }
    }
}