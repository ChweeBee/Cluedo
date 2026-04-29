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
    Vector2Int? pendingTargetCords;
    bool moveInProgress = false;

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
            foreach (Transform p in turnManager.Players)
            {
                SnapToNearestTile(p, force: true);
                if (gridManager != null)
                    turnManager.SetLogicalTile(p, gridManager.GetCoordinatesFromPosition(p.position));
            }
        }
    }

    bool IsTileOccupied(Vector2Int cords, Transform excludeUnit)
    {
        if (turnManager == null) return false;
        if (roomManager != null && roomManager.IsRoomTile(cords)) return false;

        foreach (Transform p in turnManager.Players)
        {
            if (p == null || p == excludeUnit) continue;
            if (!turnManager.TryGetLogicalTile(p, out Vector2Int pCords)) continue;
            if (pCords == cords) return true;
        }
        return false;
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
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.Escape) && unitSelected)
        {
            unitSelected = false;
            selectedUnit = null;
            pendingTargetCords = null;
            if (gridManager != null) gridManager.ResetNodes();
            if (cameraController != null) cameraController.SetMode(CameraController.CameraMode.Default);
            return;
        }

        if (unitSelected && !moveInProgress && pendingTargetCords.HasValue && Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmPendingMove();
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
                    if (GameManager.Instance != null &&
                        GameManager.Instance.CurrentState != GameManager.GameState.WaitingForMove)
                    {
                        Debug.Log("Roll the dice first.");
                        return;
                    }

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
                    if (!unitSelected || moveInProgress) return;
                    if (diceManager.totalResult <= 0)
                    {
                        Debug.Log("Roll the dice first");
                        return;
                    }

                    Vector2Int targetCords = hit.transform.GetComponent<Tile>().cords;

                    Node targetNode = gridManager.GetNode(targetCords);
                    if (targetNode == null || !targetNode.walkable || !targetNode.explored) return;

                    if (IsTileOccupied(targetCords, selectedUnit))
                    {
                        Debug.Log("Tile is occupied by another character.");
                        return;
                    }

                    Vector2Int startCords = GetLogicalTileFor(selectedUnit);
                    pathFinder.SetNewDestination(startCords, targetCords);
                    List<Node> newPath = pathFinder.GetNewPath(startCords);

                    if (newPath == null || newPath.Count == 0) return;

                    path.Clear();
                    path = newPath;
                    pendingTargetCords = targetCords;
                    Debug.Log("Path previewed. Press Space to confirm.");
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

    void ConfirmPendingMove()
    {
        if (selectedUnit == null || diceManager == null) return;
        if (path == null || path.Count == 0) return;

        moveInProgress = true;
        diceManager.totalResult = 0;
        pendingTargetCords = null;

        StopAllCoroutines();
        StartCoroutine(FollowPath());
    }

    IEnumerator FollowPath()
    {
        // turn on walking animation on the selected unit only
        Animator animator = selectedUnit.GetComponentInChildren<Animator>();
        if (animator != null) animator.SetBool("isWalking", true);

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 startPosition = selectedUnit.position;
            Vector3 endPosition = gridManager.GetPositionFromCoordinates(path[i].cords);

            if ((new Vector2(startPosition.x, startPosition.z) - new Vector2(endPosition.x, endPosition.z)).sqrMagnitude < 0.01f)
                continue;

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

        bool parsedChar = System.Enum.TryParse<CharacterId>(selectedUnit.name, out var charId);

        if (roomManager != null)
        {
            Vector2Int? slot = roomManager.HandlePlayerMovement(selectedUnit.name, finalCords);
            if (slot.HasValue && slot.Value != finalCords)
            {
                yield return WalkToTile(slot.Value);
                finalCords = slot.Value;
            }
        }

        if (turnManager != null)
        {
            turnManager.SetLogicalTile(selectedUnit, finalCords);
            if (parsedChar) turnManager.RecordPlayerTile(charId, finalCords);
        }

        // turn off walking animation
        if (animator != null) animator.SetBool("isWalking", false);

        SnapToNearestTile(selectedUnit, force: false);

        yield return new WaitForSeconds(1f);

        if (gridManager != null) gridManager.ResetNodes();

        unitSelected = false;
        selectedUnit = null;
        moveInProgress = false;
        pendingTargetCords = null;
        if (cameraController != null) cameraController.SetMode(CameraController.CameraMode.Default);

        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerMoved();
        else if (turnManager != null)
            turnManager.NextTurn();
    }

    IEnumerator WalkToTile(Vector2Int tile)
    {
        if (selectedUnit == null || gridManager == null) yield break;

        Vector3 startPosition = selectedUnit.position;
        Vector3 endPosition = gridManager.GetPositionFromCoordinates(tile);

        if ((new Vector2(startPosition.x, startPosition.z) - new Vector2(endPosition.x, endPosition.z)).sqrMagnitude < 0.01f)
            yield break;

        selectedUnit.LookAt(endPosition);

        float travelPercent = 0f;
        while (travelPercent < 1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(endPosition - startPosition);
            selectedUnit.rotation = Quaternion.Slerp(selectedUnit.rotation, targetRotation, Time.deltaTime * 10f);

            travelPercent += Time.deltaTime * movementSpeed;
            travelPercent = Mathf.Clamp01(travelPercent);

            selectedUnit.position = Vector3.Lerp(startPosition, endPosition, travelPercent);
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator ShowReachableAfterPan()
    {
        yield return new WaitForSeconds(highlightDelay);
        if (selectedUnit == null || pathFinder == null || diceManager == null) yield break;
        if (diceManager.totalResult <= 0) yield break;

        Vector2Int origin = GetLogicalTileFor(selectedUnit);
        pathFinder.MarkReachable(origin, diceManager.totalResult);

        if (turnManager != null && gridManager != null)
        {
            foreach (Transform p in turnManager.Players)
            {
                if (p == null || p == selectedUnit) continue;
                if (!turnManager.TryGetLogicalTile(p, out Vector2Int pCords)) continue;
                if (roomManager != null && roomManager.IsRoomTile(pCords)) continue;
                Node occupied = gridManager.GetNode(pCords);
                if (occupied != null) occupied.explored = false;
            }
        }
    }

    Vector2Int GetLogicalTileFor(Transform unit)
    {
        if (turnManager != null && turnManager.TryGetLogicalTile(unit, out Vector2Int tile))
            return tile;
        return gridManager != null
            ? gridManager.GetCoordinatesFromPosition(unit.position)
            : Vector2Int.zero;
    }
}
