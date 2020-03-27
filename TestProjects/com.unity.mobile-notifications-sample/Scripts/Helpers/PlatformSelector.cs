using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformSelector : MonoBehaviour
{
    void Awake()
    {
        #if PLATFORM_ANDROID
        gameObject.GetComponent<AndroidTest>().enabled = true;
        #endif
        #if PLATFORM_IOS
        gameObject.GetComponent<iOSTest>().enabled = true;
        #endif
    }
}
