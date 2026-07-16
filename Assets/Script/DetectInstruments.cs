using UnityEngine;

public class DetectInstruments : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        { 
            Debug.Log("Player detected: " + transform.parent.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player Left: " + transform.parent.name);
        }
    }
}