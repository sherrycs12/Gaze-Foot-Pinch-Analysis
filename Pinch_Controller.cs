using System.Collections;
using UnityEngine;
using TMPro;
using System;
public class Pinch_Controller : MonoBehaviour
{
    public enum PinchType
    {
        Index,
        Middle
    }

    public SerialFingerData serialFingerData;
    public TapController tapController;
    public GameObject handUI, pinchUI; 
    public bool isIndexPinching = false;
    public bool isMiddlePinching = false;
    public float pinchThreshold = 200f;
    public TextMeshProUGUI output; 
    public event Action<PinchType> OnSelectDetected;


    private void Start()
    {
        pinchUI.SetActive(false);
        handUI.SetActive(true);
        output.gameObject.SetActive(false);
    }

    private void Update()
    {
        CheckPinchStatus();
    }

    private void CheckPinchStatus()
    {
        float indexForce = serialFingerData.GetCurrData(0);
        float middleForce = serialFingerData.GetCurrData(1);

        // index finger pinch
        if (indexForce >= pinchThreshold && !isIndexPinching)
        {
            isIndexPinching = true;
            Debug.Log("Index Pinch detected!");
            handUI.SetActive(false);
            pinchUI.SetActive(true);
            output.gameObject.SetActive(true);
            output.text = "Index Pinch!";
            OnSelectDetected?.Invoke(PinchType.Index);
        }
        else if (indexForce < pinchThreshold && isIndexPinching)
        {
            isIndexPinching = false;
            handUI.SetActive(true);
            pinchUI.SetActive(false);
            output.gameObject.SetActive(false);
            Debug.Log("Pinch released.");
        }

        // middle finger pinch
        if (middleForce >= pinchThreshold && !isMiddlePinching)
        {
            isMiddlePinching = true;
            Debug.Log("Middle Pinch detected!");
            handUI.SetActive(false);
            pinchUI.SetActive(true);
            output.gameObject.SetActive(true);
            output.text = "Middle Pinch!";
            OnSelectDetected?.Invoke(PinchType.Middle);
        }
        else if (middleForce < pinchThreshold && isMiddlePinching)
        {
            isMiddlePinching = false;
            handUI.SetActive(true);
            pinchUI.SetActive(false);
            output.gameObject.SetActive(false);
            Debug.Log("Pinch released.");
        }

    }

    private void HideCursorText()
    {
        output.gameObject.SetActive(false);
    }

}
