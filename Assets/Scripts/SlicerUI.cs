using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlicerUI : MonoBehaviour
{
    [SerializeField]
    private GameObject obj;

    [SerializeField]
    private Camera cam;

    [SerializeField]
    private Material mat;

    private bool isMouseDown = false;
    private Vector3 startPosition;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && !isMouseDown)
        {
            HandleOnClickDown();
            Slicer.Cut(obj, startPosition, Vector3.up);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandleOnClickUp();
        }
        else if (Input.GetMouseButton(0) && isMouseDown)
        {
            // Draw from start position to mouse
            DrawLine();
        }
    }

    void HandleOnClickDown()
    {
        isMouseDown = true;
        LineRenderer lineRender = GetComponent<LineRenderer>();
        lineRender.positionCount = 2;
        Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
        startPosition = cam.ScreenToWorldPoint(vector);
        //startPosition = Input.mousePosition;
        Debug.Log("Pressed left click");
        Debug.Log(startPosition);
    }

    void HandleOnClickUp()
    {
        isMouseDown = false;

        LineRenderer lineRender = GetComponent<LineRenderer>();

        // Hide the line
        lineRender.positionCount = 0;

        // Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
        // Vector3 endPosition = cam.ScreenToWorldPoint(vector);
        Debug.Log("Released");
        Debug.Log(Input.mousePosition);
    }

    void DrawLine()
    {
        LineRenderer lineRender = GetComponent<LineRenderer>();

        lineRender.SetPosition(0, startPosition);
        Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
        Vector3 endPosition = cam.ScreenToWorldPoint(vector);
        lineRender.SetPosition(1, endPosition);
    }
}
