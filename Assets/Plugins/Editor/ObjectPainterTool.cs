using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ObjectPainterTool : EditorWindow
{

    public static ObjectPainterTool window;

    static System.Action<SceneView> onSceneGUIDelegate;

    PointClickPlacementHelper placerHelper;

    private bool NoObjectSelected
    {
        get { return enabled && (objectSelecting == null); }
    }

    private bool NoPrefabSelected
    {
        get { return enabled && (Selection.activeGameObject != null && Selection.activeTransform != null); }
    }
    
    public GameObject objectSelecting;

    private bool enabled = false;

    public float radiusAmount = 2;

    public int objectQuantity = 1;

    public bool useNormalRotation = false;
    public bool randomYRotation = false;
    public bool randomZRotation = false;
    public bool randomYScale = false;

    public bool LockToLayer = false;
    public LayerMask layerToLock;

    public bool PreventStacking = false;
    public bool preventStackingFromAllObjects;
    public bool noPositionNoSpawn;
    public float minFreeSpaceToPlace;

    public bool RandomizeScale = false;
    public Vector2 MinMaxScale = new Vector2(0.4f, 1f);
    public Vector2 MinMaxScaleCtrl = new Vector2(0.4f, 1f);
    public int MaxRandomizeScale = 20;

    public bool customOffset = false;
    public Vector3 offSet;

    private static bool ShowNotes = false;
    public string notes;

    public bool buildingMode = false;
    private BuildingType currentBuildingType = BuildingType.None;
    private const float WALL_FLOOR_RATIO_THRESHOLD = 2f; // If width/length is 2x height, consider it a floor
    
    private enum BuildingType
    {
        None,
        Floor,
        Wall
    }

    private bool showLockToLayer
    {
        get { return LockToLayer && enabled; }
    }

    private bool showPreventStacking
    {
        get { return PreventStacking && enabled; }
    }

    private bool showRealPreventStacking
    {
        get { return objectQuantity > 1 && enabled; }
    }

    private bool showRandomizeScale
    {
        get { return RandomizeScale && enabled; }
    }

    private bool showCustomOffset
    {
        get { return customOffset && enabled; }
    }

    private Vector3 buildPos;
    private bool instantiatePrefab = false;
    private string newGroupName;
    private GameObject selectedGroup;
    private GameObject currentGameObject;
    private GameObject newSelectedGameObject;
    private int indexname = 0;


    [MenuItem("Tools/Object Painter Tool")]
    private static void OpenWindow()
    {
        window = GetWindow<ObjectPainterTool>("Object Painter");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 380, 200);
    }


    void OnEnable()
    {
        onSceneGUIDelegate = OnSceneGUI;
        SceneView.duringSceneGui += onSceneGUIDelegate;

    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= onSceneGUIDelegate;
        RemovePlacerHelper();
    }

    public void RemovePlacerHelper()
    {
        GameObject[] placedObjects = FindObjectsOfType<GameObject>() as GameObject[];
        foreach (GameObject c in placedObjects)
        {
            if (c.name == "PointClickPlacerHelper")
            {
                if (Application.isEditor)
                    Object.DestroyImmediate(c.gameObject);
                else
                    Object.Destroy(c.gameObject);
            }
        }
        placerHelper = null;
    }

    private GameObject findObject;

    public void OnSceneGUI(SceneView sceneView)
    {
        findObject = Selection.activeGameObject != null && Selection.activeTransform == null ? Selection.activeGameObject : null;
        if (findObject != null)
        {
            if ((Selection.activeGameObject != null && Selection.activeTransform == null))
            {
                objectSelecting = findObject;
            }
        }
        if (enabled)
        {
            if (placerHelper == null)
            {
                GameObject cloneObj = new GameObject("PointClickPlacerHelper");
                placerHelper = cloneObj.AddComponent<PointClickPlacementHelper>();
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (Event.current.type == EventType.MouseMove)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;
                bool DidRaycast = false;

                if (LockToLayer) DidRaycast = Physics.Raycast(ray, out hit, 500, layerToLock);
                else DidRaycast = Physics.Raycast(ray, out hit);

                if (DidRaycast && placerHelper)
                {
                    placerHelper.placementRadius = radiusAmount;
                    placerHelper.transform.position = hit.point + offSet;
                    placerHelper.MinDistance = PreventStacking;
                    placerHelper.minDistanceAmount = minFreeSpaceToPlace;
                }
            }

            if (objectSelecting != null)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    Event.current.control)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;

                    bool DidRaycast = false;

                    if (LockToLayer) DidRaycast = Physics.Raycast(ray, out hit, 500, layerToLock);
                    else DidRaycast = Physics.Raycast(ray, out hit);

                    if (DidRaycast)
                    {
                        RemoveObjectCloseTo(hit.point, Event.current.alt);
                    }
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt && !Event.current.control)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;

                    bool DidRaycast = false;

                    if (LockToLayer) DidRaycast = Physics.Raycast(ray, out hit, 500, layerToLock);
                    else DidRaycast = Physics.Raycast(ray, out hit);

                    if (DidRaycast)
                    {
                        buildPos = hit.point + offSet;

                        if (objectSelecting != null)
                        {
                            instantiatePrefab = true;
                        }
                        else
                        {
                            instantiatePrefab = false;
                        }

                        if (instantiatePrefab == true)
                        {
                            placerHelper.lastTimeClicked = Time.time;
                            if (objectQuantity > 1)
                            {
                                addedRecently.Clear();
                                if(PreventStacking) PopulateAddedRecentlyWithAll();
                                for (int i = 0; i < objectQuantity; i++)
                                {
                                    AddObjectRandomPosition(buildPos);
                                }
                                addedRecently.Clear();
                            }
                            else
                            {
                                AddSingle(buildPos, hit);
                            }
                        }

                        if (instantiatePrefab == false)
                        {
                            WarnUser();
                        }
                    }
                }
            }
        }
        else
        {
            if (placerHelper != null)
            {
                if (Application.isEditor)
                    Object.DestroyImmediate(placerHelper.gameObject);
                else
                    Object.Destroy(placerHelper.gameObject);
                placerHelper = null;
            }
        }
    }

    private void RemoveObjectCloseTo(Vector3 hitPoint, bool altDelete = false)
    {
        GameObject[] placedObjects = FindObjectsOfType<GameObject>() as GameObject[];
        foreach (GameObject c in placedObjects)
        {
            if (c.name.Contains("_PCP"))
            {
                Vector3 diff = hitPoint - c.transform.position;
                float dist = diff.sqrMagnitude;
                if (dist <= (altDelete ? radiusAmount * 10 : 4.5f))
                {
                    Undo.DestroyObjectImmediate(c);
                }
            }
        }
    }

    public void PopulateAddedRecentlyWithAll()
    {
        if (preventStackingFromAllObjects)
        {
            GameObject[] placedObjects = FindObjectsOfType<GameObject>() as GameObject[];
            foreach (GameObject c in placedObjects)
            {
                if (InRange(c.transform.position, placerHelper.transform.position, radiusAmount*1.8f))
                {
                    if (LockToLayer)
                    {
                        int objLayerMask = (1 << c.layer);
                        if ((layerToLock.value & objLayerMask) <= 0)
                        {
                            addedRecently.Add(c.transform.position);
                        }
                    }
                    else
                    {
                        addedRecently.Add(c.transform.position);
                    }
                }
            }
        }
    }

    public bool onlyAboveAmount = false;
    public float aboveAmount = 1.95f;
    public void AddObjectRandomPosition(Vector3 buildPos)
    {
        Vector3 pos = Vector3.zero;
        int count = 0;
        RaycastHit hitRaycast = new RaycastHit();
        while (pos == Vector3.zero && count < 500)
        {
            pos = RandomPositionOnRegistredSurface(out hitRaycast);
            if (PreventStacking)
            {
                for (int i = 0; i < addedRecently.Count; i++)
                {
                    if (InRange(addedRecently[i], pos, minFreeSpaceToPlace))
                    {
                        pos = Vector3.zero;
                    }
                }
            }

            if (onlyAboveAmount && pos.y < aboveAmount) pos = Vector3.zero;
            count++;
        }

        if (pos != Vector3.zero)
        {
            AddSingle(pos, hitRaycast);
        }
    }

    public bool InRange(Vector3 m, Vector3 o, float range)
    {
        return (o - m).sqrMagnitude < (range * range);
    }

    private List<Vector3> addedRecently = new List<Vector3>();

    Vector3 RandomPositionOnRegistredSurface(out RaycastHit raycastHit)
    {
        Vector3 centerPos = placerHelper.transform.position + new Vector3(0, 10f, 0);
        Vector2 randomCirclePos = Random.insideUnitCircle * radiusAmount;
        Vector3 randomPos = centerPos + new Vector3(randomCirclePos.x, 0, randomCirclePos.y);
        Vector3 returnPos = Vector3.zero;
        RaycastHit hit;
        bool DidRaycast = false;

        if (LockToLayer) DidRaycast = Physics.Raycast(randomPos, Vector3.down, out hit, centerPos.y + 500f, layerToLock);
        else DidRaycast = Physics.Raycast(randomPos, Vector3.down, out hit, centerPos.y + 500f);

        if (DidRaycast)
        {
            returnPos = hit.point;
        }
        raycastHit = hit;
        return returnPos;
    }

    private void AddSingle(Vector3 buildPos, RaycastHit clickedObject)
    {
        GameObject originalPrefab = objectSelecting;
        
        if (buildingMode)
        {
            currentBuildingType = DetermineBuildingType(originalPrefab);
            SnapObjectToSurface(originalPrefab, clickedObject);
            return;
        }

        GameObject prefab = PrefabUtility.InstantiatePrefab(originalPrefab.gameObject) as GameObject;

        Vector3 newPos = new Vector3(buildPos.x, buildPos.y, buildPos.z);

        prefab.transform.position = newPos;

        if (useNormalRotation && clickedObject.transform != null)
        {
            prefab.transform.rotation = Quaternion.FromToRotation(Vector3.up, clickedObject.normal) * prefab.transform.rotation;
        }

        if (randomYRotation)
        {
            prefab.transform.Rotate(0, Random.Range(0, 360), 0);
        }
        if (randomZRotation)
        {
            prefab.transform.Rotate(0, 0,Random.Range(-360, 360));
        }
        if (RandomizeScale)
        {
            float desiredScale = Random.Range(MinMaxScale.x, MinMaxScale.y);
            if(Event.current.shift) desiredScale = Random.Range(MinMaxScaleCtrl.x, MinMaxScaleCtrl.y);
            Vector3 currentScale = originalPrefab.transform.localScale * desiredScale;
            prefab.transform.localScale = currentScale;
        }
        if (randomYScale)
        {
            Vector3 currentScale = prefab.transform.localScale;
            currentScale.y = currentScale.y + Random.Range(-1 * (currentScale.y / 6), (currentScale.y / 6));
            prefab.transform.localScale = currentScale;
        }

        if (selectedGroup != null)
        {
            prefab.transform.parent = selectedGroup.transform;
        }

        indexname++;

        prefab.name = string.Format("{0}_{1}_PCP", prefab.name, indexname);
        addedRecently.Add(prefab.transform.position);
        Undo.RegisterCreatedObjectUndo(prefab, "Added " + prefab.name + " to Scene");
    }

    private void WarnUser()
    {
    }

    private BuildingType DetermineBuildingType(GameObject obj)
    {
        if (!obj) return BuildingType.None;
        
        Bounds bounds = obj.GetComponent<Collider>().bounds;
        float xSize = bounds.size.x;
        float ySize = bounds.size.y;
        float zSize = bounds.size.z;

        // Floor detection - XZ significantly larger than Y
        if (xSize > ySize * WALL_FLOOR_RATIO_THRESHOLD && 
            zSize > ySize * WALL_FLOOR_RATIO_THRESHOLD)
        {
            return BuildingType.Floor;
        }
        // Wall detection - Either XY or ZY significantly larger than the other dimension
        else if ((xSize > zSize * WALL_FLOOR_RATIO_THRESHOLD && ySize > zSize * WALL_FLOOR_RATIO_THRESHOLD) ||
                 (zSize > xSize * WALL_FLOOR_RATIO_THRESHOLD && ySize > xSize * WALL_FLOOR_RATIO_THRESHOLD))
        {
            return BuildingType.Wall;
        }

        return BuildingType.None;
    }

    private void SnapObjectToSurface(GameObject prefab, RaycastHit hit)
    {
        if (!buildingMode || currentBuildingType == BuildingType.None)
        {
            AddSingle(hit.point + offSet, hit);
            return;
        }

        Collider hitCollider = hit.collider;
        Bounds hitBounds = hitCollider.bounds;
        Bounds prefabBounds = prefab.GetComponent<Collider>().bounds;

        Vector3 snapPosition = hit.point;
        Quaternion snapRotation = prefab.transform.rotation;

        if (currentBuildingType == BuildingType.Floor)
        {
            HandleFloorSnapping(prefab, hit, hitBounds, prefabBounds, ref snapPosition, ref snapRotation);
        }
        else if (currentBuildingType == BuildingType.Wall)
        {
            HandleWallSnapping(prefab, hit, hitBounds, prefabBounds, ref snapPosition, ref snapRotation);
        }

        // Create the object at the snapped position
        GameObject newObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        newObj.transform.position = snapPosition;
        newObj.transform.rotation = snapRotation;
        
        if (selectedGroup != null)
        {
            newObj.transform.parent = selectedGroup.transform;
        }

        indexname++;
        newObj.name = string.Format("{0}_{1}_PCP", newObj.name, indexname);
        addedRecently.Add(newObj.transform.position);
        Undo.RegisterCreatedObjectUndo(newObj, "Added " + newObj.name + " to Scene");
    }

    private void HandleFloorSnapping(GameObject prefab, RaycastHit hit, Bounds hitBounds, Bounds prefabBounds, 
        ref Vector3 snapPosition, ref Quaternion snapRotation)
    {
        // Find closest edge of hit object
        Vector3 hitCenter = hitBounds.center;
        Vector3 hitPoint = hit.point;
        
        float distanceToNorth = Mathf.Abs(hitBounds.max.z - hitPoint.z);
        float distanceToSouth = Mathf.Abs(hitPoint.z - hitBounds.min.z);
        float distanceToEast = Mathf.Abs(hitBounds.max.x - hitPoint.x);
        float distanceToWest = Mathf.Abs(hitPoint.x - hitBounds.min.x);

        // Find minimum distance
        float minDistance = Mathf.Min(distanceToNorth, distanceToSouth, distanceToEast, distanceToWest);

        // Snap to the closest edge
        if (minDistance == distanceToNorth)
        {
            snapPosition = new Vector3(hitPoint.x, hitPoint.y, hitBounds.max.z + prefabBounds.extents.z);
        }
        else if (minDistance == distanceToSouth)
        {
            snapPosition = new Vector3(hitPoint.x, hitPoint.y, hitBounds.min.z - prefabBounds.extents.z);
        }
        else if (minDistance == distanceToEast)
        {
            snapPosition = new Vector3(hitBounds.max.x + prefabBounds.extents.x, hitPoint.y, hitPoint.z);
        }
        else // West
        {
            snapPosition = new Vector3(hitBounds.min.x - prefabBounds.extents.x, hitPoint.y, hitPoint.z);
        }

        // Allow rotation with R key
        if (Event.current.keyCode == KeyCode.R)
        {
            snapRotation *= Quaternion.Euler(0, 90, 0);
        }
    }

    private void HandleWallSnapping(GameObject prefab, RaycastHit hit, Bounds hitBounds, Bounds prefabBounds,
        ref Vector3 snapPosition, ref Quaternion snapRotation)
    {
        BuildingType hitObjectType = DetermineBuildingType(hit.collider.gameObject);

        if (hitObjectType == BuildingType.Floor)
        {
            // Snap to floor edge
            Vector3 hitPoint = hit.point;
            float distanceToNorth = Mathf.Abs(hitBounds.max.z - hitPoint.z);
            float distanceToSouth = Mathf.Abs(hitPoint.z - hitBounds.min.z);
            float distanceToEast = Mathf.Abs(hitBounds.max.x - hitPoint.x);
            float distanceToWest = Mathf.Abs(hitPoint.x - hitBounds.min.x);

            float minDistance = Mathf.Min(distanceToNorth, distanceToSouth, distanceToEast, distanceToWest);

            // Align wall perpendicular to the closest edge
            if (minDistance == distanceToNorth || minDistance == distanceToSouth)
            {
                snapRotation = Quaternion.Euler(0, 90, 0);
            }
            
            snapPosition = new Vector3(hitPoint.x, hitPoint.y + prefabBounds.extents.y, hitPoint.z);
        }
        else if (hitObjectType == BuildingType.Wall)
        {
            // Snap to wall edge
            Vector3 hitPoint = hit.point;
            Vector3 hitNormal = hit.normal;
            
            snapPosition = hitPoint + hitNormal * prefabBounds.extents.z;
            snapRotation = Quaternion.LookRotation(hitNormal);
        }

        // Allow rotation with R key
        if (Event.current.keyCode == KeyCode.R)
        {
            snapRotation *= Quaternion.Euler(0, 90, 0);
        }
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.BeginHorizontal("Box");
        
        EditorGUILayout.BeginVertical(GUILayout.Width(60));
        objectSelecting = (GameObject)EditorGUILayout.ObjectField(objectSelecting, typeof(GameObject), false, GUILayout.Width(60), GUILayout.Height(60));
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical();
        
        if (!enabled)
        {
            if (GUILayout.Button("Enable", GUILayout.Height(30)))
            {
                enabled = true;
            }
        }
        else
        {
            if (GUILayout.Button("Disable", GUILayout.Height(30)))
            {
                enabled = false;
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (NoObjectSelected)
        {
            EditorGUILayout.HelpBox("No Object selected.", MessageType.Error);
        }
        if (NoPrefabSelected)
        {
            EditorGUILayout.HelpBox("Object selected isn't a prefab. Make sure you're not selecting objects in the scene.", MessageType.Error);
        }

        if (enabled)
        {
            EditorGUILayout.Space(10);
            
            radiusAmount = EditorGUILayout.Slider("Radius Amount", radiusAmount, 1, 150);
            objectQuantity = EditorGUILayout.IntSlider("Object Quantity", objectQuantity, 1, 300);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            useNormalRotation = EditorGUILayout.ToggleLeft("Rot w/ Object", useNormalRotation, GUILayout.Width(100));
            randomYRotation = EditorGUILayout.ToggleLeft("Rand Y Rot", randomYRotation, GUILayout.Width(90));
            randomZRotation = EditorGUILayout.ToggleLeft("Rand Z Rot", randomZRotation, GUILayout.Width(90));
            randomYScale = EditorGUILayout.ToggleLeft("Rand Y Scale", randomYScale);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            LockToLayer = EditorGUILayout.ToggleLeft("Lock to Layer", LockToLayer, GUILayout.Width(100));
            if (LockToLayer)
            {
                layerToLock = EditorGUILayout.LayerField(layerToLock);
            }
            EditorGUILayout.EndHorizontal();

            if (objectQuantity > 1)
            {
                EditorGUILayout.BeginVertical("Box");
                PreventStacking = EditorGUILayout.ToggleLeft("Prevent Stacking", PreventStacking);
                
                if (PreventStacking)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    preventStackingFromAllObjects = EditorGUILayout.ToggleLeft("All", preventStackingFromAllObjects, GUILayout.Width(50));
                    noPositionNoSpawn = EditorGUILayout.ToggleLeft("Cancel", noPositionNoSpawn, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                    
                    minFreeSpaceToPlace = EditorGUILayout.Slider("Min Distance", minFreeSpaceToPlace, 0f, 30f);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginVertical("Box");
            RandomizeScale = EditorGUILayout.ToggleLeft("Randomize Scale", RandomizeScale);
            if (RandomizeScale)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.MinMaxSlider("Min Max Scale", ref MinMaxScale.x, ref MinMaxScale.y, 0.4f, MaxRandomizeScale);
                EditorGUILayout.MinMaxSlider("Min Max W Shift", ref MinMaxScaleCtrl.x, ref MinMaxScaleCtrl.y, 0.4f, MaxRandomizeScale);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            customOffset = EditorGUILayout.ToggleLeft("Custom Offset", customOffset);
            if (customOffset)
            {
                EditorGUI.indentLevel++;
                offSet = EditorGUILayout.Vector3Field("Offset", offSet);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            buildingMode = EditorGUILayout.ToggleLeft("Building Mode", buildingMode);
            if (buildingMode)
            {
                EditorGUILayout.HelpBox("Press R to rotate while placing. Building mode will automatically detect floor and wall pieces.", MessageType.Info);
            }
        }

        if (ShowNotes)
        {
            EditorGUILayout.Space(10);
            notes = EditorGUILayout.TextArea(notes, GUILayout.Height(100));
        }

        if (RandomizeScale)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min Max Scale Values", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Min: {MinMaxScale.x:F2}, Max: {MinMaxScale.y:F2}");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min Max W Shift Values", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Min: {MinMaxScaleCtrl.x:F2}, Max: {MinMaxScaleCtrl.y:F2}");
            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
    }
}
