using UnityEngine;
using System;

public class DebugDude : MonoBehaviour
{

    IDebugFunction[] debugFunctions;
    void Start()
    {
        debugFunctions = GetComponents<IDebugFunction>();
        GameEvents.DebugLog($"DebugDude initialized with {debugFunctions.Length} debug functions.", DebugCategory.General);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            ExecuteAllDebugFunctions();
        }
    }

    void ExecuteAllDebugFunctions()
    {
        foreach (IDebugFunction debugFunction in debugFunctions)
        {
            debugFunction.ExecuteDebug();
        }
    }
}