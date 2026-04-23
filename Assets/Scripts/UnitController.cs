using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitController : MonoBehaviour
{
    [SerializeField] float movementSpeed = 3f;

    Transform selectedUnit;
    bool unitSelected = false;

    List<Node> path = new List<Node>();

    GridManager gridManager;
    Pathfinding pathFinder;
    DiceManager diceManager;
    RoomManager roomManager;

    public Animator animator;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomManager = FindAnyObjectByType<RoomManager>();
        gridManager = FindAnyObjectByType<GridManager>();
        pathFinder = FindAnyObjectByType<Pathfinding>();
        diceManager = FindAnyObjectByType<DiceManager>();   
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {

                

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            bool hasHit = Physics.Raycast(ray, out hit);

            if (hasHit)
            {
                if (hit.transform.tag == "Unit")
                {
                    selectedUnit = hit.transform;
                    unitSelected = true;
                }
                if (hit.transform.tag == "Tile")
                {
                    if (unitSelected)
                    {
                        if(diceManager.totalResult <= 0)
                        {
                            Debug.Log("Roll the dice first");
                            return;
                        }
                        Vector2Int targetCords = hit.transform.GetComponent<Tile>().cords;
                        Vector2Int startCords = new Vector2Int((int) selectedUnit.transform.position.x, (int) selectedUnit.transform.position.z) / gridManager.UnityGridSize;
                        pathFinder.SetNewDestination(startCords, targetCords);
                        List<Node> newPath = pathFinder.GetNewPath(startCords);
                        
                        if (newPath == null || newPath.Count == 0)
                        {
                            Debug.Log("No valid path.");
                            Debug.Log("No valid path. totalResult is: " + diceManager.totalResult);
                            return;
                        }
                        int stepsRequired = newPath.Count - 1;

                        if(stepsRequired > diceManager.totalResult)
                        {
                            Debug.Log("Destination greater than dice roll");
                            return;
                        }
                        StopAllCoroutines();
                        path.Clear();
                        path = newPath;
                        StartCoroutine(FollowPath());

                        diceManager.totalResult = 0;
                    }
                }


            }
        }
    }


/*
    void RecalculatePath(bool resetPath)
    {
        Vector2Int coordinates = new Vector2Int();
        
        if (resetPath)
        {
            coordinates = pathFinder.StartCords;
        }
        else
        {
            coordinates = gridManager.GetCoordinatesFromPosition(transform.position);
        }

        StopAllCoroutines();
        path.Clear();
        path = pathFinder.GetNewPath(coordinates);
        StartCoroutine(FollowPath());
    }
    */

    IEnumerator FollowPath()
    {
        // turn on walking animation
        animator.SetBool("isWalking", true);

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 startPosition = selectedUnit.position;
            Vector3 endPosition = gridManager.GetPositionFromCoordinates(path[i].cords);
            float travelPercent = 0f;

            selectedUnit.LookAt(endPosition);

            while (travelPercent < 1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(endPosition - startPosition);
                selectedUnit.rotation = Quaternion.Slerp(selectedUnit.rotation, targetRotation, Time.deltaTime * 10f);

                travelPercent += Time.deltaTime * movementSpeed;

                // Clamp to avoid overshooting
                travelPercent = Mathf.Clamp01(travelPercent);

                selectedUnit.position = Vector3.Lerp(startPosition, endPosition, travelPercent);
                yield return new WaitForEndOfFrame();
            }
        }


        // Check room entry/exit
        Vector2Int finalCords = path[path.Count - 1].cords;
        roomManager.HandlePlayerMovement(selectedUnit.name, finalCords);

        // turn off walking animation
        animator.SetBool("isWalking", false);
        
    }
}
