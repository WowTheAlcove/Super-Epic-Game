using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectVisibilityManager : MonoBehaviour
{
    [Header("Objects")]
    public GameObject[] objectsToFade;

    private List<Renderer> objectRenderers = new();

    public static ObjectVisibilityManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        foreach (var obj in objectsToFade)
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            objectRenderers.Add(rend);
    }

    public void PlayerEnteredLowerDeck()
    {
        SetObjectVisibility(false);
    }

    public void PlayerExitedLowerDeck()
    {
        SetObjectVisibility(true);
    }

    private void SetObjectVisibility(bool visible)
    {
        foreach (var rend in objectRenderers)
            rend.enabled = visible;
    }
}