using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RaycasterDebug : MonoBehaviour
{
    void Update()
    {
        var results = new List<RaycastResult>();
        var data = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        EventSystem.current.RaycastAll(data, results);

        if (results.Count == 0)
            Debug.Log("Raycast hit nothing");
        else
            foreach (var r in results)
                Debug.Log("Raycast hit: " + r.gameObject.name);
    }
}