using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitController : MonoBehaviour
{
    [SerializeField] float movementSpeed = 3f;
    [Tooltip("Delay between selecting a unit and showing reachable tiles, so highlighting appears after the camera pan.")]
    [SerializeField] float highlightDelay = 0.5f;
    [Tooltip("If a unit ends a turn more than this far (XZ) from the nearest tile centre, they get teleported back onto it.")]
    [SerializeField] float snapTolerance = 0.3f;

    Transform selectedUnit;
    bool unitSelected = false;

    List<Node> path = new List<Node>();

    GridManager gridManager;
    Pathfinding pathFinder;
    DiceManager diceManager;
    RoomManager roomManager;
    CameraController cameraController;
    TurnManager turnManager;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomManager = FindAnyObjectByType<RoomManager>();
        gridManager = FindAnyObjectByType<GridManager>();
        pathFinder = FindAnyObjectByType<Pathfinding>();
        diceManager = FindAnyObjectByType<DiceManager>();
        cameraController = FindAnyObjectByType<CameraController>();
        turnManager = FindAnyObjectByType<TurnManager>();

        if (turnManager != null)
        {
            foreach (Transform p in turnManager.Players) SnapToNearestTile(p, force: true);
        }
    }

    void SnapToNearestTile(Transform unit, bool force)
    {
        if (unit == null || gridManager == null) return;

        Vector2Int cords = gridManager.GetCoordinatesFromPosition(unit.position);
        Vector3 tileCenter = gridManager.GetPositionFromCoordinates(cords);
        Vector3 target = new Vector3(tileCenter.x, unit.position.y, tileCenter.z);

        float planarDist = Vector2.Distance(
            new Vector2(unit.position.x, unit.position.z),
            new Vector2(target.x, target.z));

        if (force || planarDist > snapTolerance)
        {
            unit.position = target;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && unitSelected)
        {
            unitSelected = false;
            selectedUnit = null;
            if (gridManager != null) gridManager.ResetNodes();
            if (cameraController != null) cameraController.SetMode(CameraController.CameraMode.Default);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            bool hasHit = Physics.Raycast(ray, out hit);

            if (hasHit)
            {
                if (hit.transform.tag == "Unit")
                {
                    if (turnManager != null && turnManager.CurrentPlayer != null && hit.transform != turnManager.CurrentPlayer)
                    {
                        Debug.Log("Not this unit's turn.");
                        return;
                    }

                    selectedUnit = hit.transform;
                    unitSelected = true;
                    if (cameraController != null) cameraController.BeginMove(selectedUnit);
                    StartCoroutine(ShowReachableAfterPan());
                }
                if (hit.transform.tag == "Tile")
                {
                    if (!unitSelected) return;
                    if (diceManager.totalResult <= 0)
                    {
                        Debug.Log("Roll the dice first");
                        return;
                    }

                    Vector2Int targetCords = hit.transform.GetComponent<Tile>().cords;

                    // Ignore clicks on tiles that aren't currently within reach or that are blocked.
                    Node targetNode = gridManager.GetNode(targetCords);
                    if (targetNode == null || !targetNode.walkable || !targetNode.explored) return;

                    Vector2Int startCords = new Vector2Int((int) selectedUnit.transform.position.x, (int) selectedUnit.transform.position.z) / gridManager.UnityGridSize;
                    pathFinder.SetNewDestination(startCords, targetCords);
                    List<Node> newPath = pathFinder.GetNewPath(startCords);

                    if (newPath == null || newPath.Count == 0) return;

                    StopAllCoroutines();
                    path.Clear();
                    path = newPath;
                    StartCoroutine(FollowPath());

                    diceManager.totalResult = 0;
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
        // turn on walking animation on the selected unit only
        Animator animator = selectedUnit.GetComponentInChildren<Animator>();
        if (animator != null) animator.SetBool("isWalking", true);

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
        
        // TODO: fix properly
        if (roomManager != null)
        {
            roomManager.HandlePlayerMovement(selectedUnit.name, finalCords);
        }

        // turn off walking animation
        if (animator != null) animator.SetBool("isWalking", false);

        SnapToNearestTile(selectedUnit, force: false);

        yield return new WaitForSeconds(1f);

        if (gridManager != null) gridManager.ResetNodes();

        unitSelected = false;
        selectedUnit = null;
        if (cameraController != null) cameraController.SetMode(CameraController.CameraMode.Default);
        if (turnManager != null) turnManager.NextTurn();
    }

    IEnumerator ShowReachableAfterPan()
    {
        yield return new WaitForSeconds(highlightDelay);
        if (selectedUnit == null || pathFinder == null || diceManager == null) yield break;
        if (diceManager.totalResult <= 0) yield break;

        Vector2Int origin = new Vector2Int((int)selectedUnit.position.x, (int)selectedUnit.position.z) / gridManager.UnityGridSize;
        pathFinder.MarkReachable(origin, diceManager.totalResult);
    }
}
