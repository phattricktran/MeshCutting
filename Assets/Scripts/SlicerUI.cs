using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

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
	private Vector3 startPositionScreen;

	// Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButton(0) && !isMouseDown)
		{
			HandleOnClickDown();
			
		}
		else if (Input.GetMouseButtonUp(0))
		{
			HandleOnClickUp();

			Vector3 endPositionScreen = Input.mousePosition;
			Ray ray = cam.ScreenPointToRay(endPositionScreen);
			Ray ray1 = cam.ScreenPointToRay(startPositionScreen);
			Vector3 normal = Vector3.Cross(ray.direction, ray1.direction).normalized;

			List<GameObject> objects = FindObjects(startPositionScreen, endPositionScreen);

            foreach (GameObject obj in objects)
            {
                Slicer.Cut(obj, startPosition, normal);
            }
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
		startPositionScreen = vector;
		//startPosition = Input.mousePosition;
	}

	void HandleOnClickUp()
	{
		isMouseDown = false;

		LineRenderer lineRender = GetComponent<LineRenderer>();

		// Hide the line
		lineRender.positionCount = 0;
	}

	void DrawLine()
	{
		LineRenderer lineRender = GetComponent<LineRenderer>();

		lineRender.SetPosition(0, startPosition);
		Vector3 vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
		Vector3 endPosition = cam.ScreenToWorldPoint(vector);
		lineRender.SetPosition(1, endPosition);

		/* Debug */
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		Ray ray1 = cam.ScreenPointToRay(startPositionScreen);

		Debug.DrawRay(startPosition, ray1.direction * 1000, Color.red);
		Debug.DrawRay(endPosition, ray.direction * 1000, Color.red);

        Vector3 step = (Input.mousePosition - startPositionScreen) / 10;
        Vector3 current = startPositionScreen;
        for (int i = 0; i < 10; i++)
        {
            Ray ray_i = cam.ScreenPointToRay(current);  
            Debug.DrawRay(startPosition, ray_i.direction * 1000, Color.blue);
            current = current + step;
        }
    }

	private List<GameObject> FindObjects(Vector3 start, Vector3 end)
	{
		Vector3 center = (start + end) / 2;
        center.z = cam.nearClipPlane;
		Vector3 startWorld = cam.ScreenToWorldPoint(start);
        startWorld.z = cam.nearClipPlane;
		Vector3 centerWorld = cam.ScreenToWorldPoint(center);
		Vector3 cameraDirection = cam.ScreenPointToRay(center).direction;
		float halfx = Vector3.Distance(startWorld, centerWorld);
		// TODO: Set this somewhere else
		float halfy = 0.000001f;

        List<RaycastHit> raycastHits = new List<RaycastHit>();
        HashSet<GameObject> gameObjects = new HashSet<GameObject>();

        Vector3 step = (end - start) / 10;
        Vector3 current = start;
        for (int i = 0; i < 10; i++)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(current);

            if (Physics.Raycast(ray, out hit))
            {
                GameObject objectHit = hit.transform.gameObject;
                if (objectHit.tag != "UnCuttable")
                {
                    gameObjects.Add(objectHit);
                }
            }
            current += step;
        }

        return gameObjects.ToList(); ;
	}
}
