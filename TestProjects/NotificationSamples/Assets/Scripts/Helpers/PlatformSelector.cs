using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformSelector : MonoBehaviour
{
    void Awake()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                gameObject.GetComponent<AndroidTest>().enabled = true;
                break;
            case RuntimePlatform.IPhonePlayer:
                gameObject.GetComponent<iOSTest>().enabled = true;
                break;
            default:
                break;
        }
    }
}
