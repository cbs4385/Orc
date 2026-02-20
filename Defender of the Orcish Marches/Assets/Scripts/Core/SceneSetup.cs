using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Game/Setup Walls")]
    public static void SetupWalls()
    {
        var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall.mat");
        var wallManager = GameObject.Find("WallManager");
        if (wallManager == null) { Debug.LogError("No WallManager found"); return; }

        int count = 0;
        foreach (Transform child in wallManager.transform)
        {
            if (!child.name.StartsWith("Wall_")) continue;

            // Add Wall component
            if (child.GetComponent<Wall>() == null)
                child.gameObject.AddComponent<Wall>();

            // Add WallCorners component
            if (child.GetComponent<WallCorners>() == null)
                child.gameObject.AddComponent<WallCorners>();

            // Add NavMeshObstacle
            var obstacle = child.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
                obstacle = child.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;

            // Assign material
            var rend = child.GetComponent<Renderer>();
            if (rend != null && wallMat != null)
                rend.sharedMaterial = wallMat;

            count++;
        }
        Debug.Log($"Setup {count} wall segments.");
    }

    [MenuItem("Game/Rebuild Courtyard (Half Size)")]
    public static void RebuildCourtyardHalf()
    {
        var wallManager = GameObject.Find("WallManager");
        if (wallManager == null) { Debug.LogError("No WallManager found"); return; }

        var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall.mat");

        // Delete all existing Wall_ children
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in wallManager.transform)
        {
            if (child.name.StartsWith("Wall_"))
                toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete)
            DestroyImmediate(go);

        float radius = 4f;
        float step = 2f;
        int wallIndex = 0;

        // North side (z = radius)
        for (float x = -radius; x <= radius; x += step)
        {
            CreateWallSegment(wallManager.transform, $"Wall_N{++wallIndex}",
                new Vector3(x, 1, radius), new Vector3(step, 2, 0.5f), wallMat);
        }

        // South side (z = -radius)
        wallIndex = 0;
        for (float x = -radius; x <= radius; x += step)
        {
            CreateWallSegment(wallManager.transform, $"Wall_S{++wallIndex}",
                new Vector3(x, 1, -radius), new Vector3(step, 2, 0.5f), wallMat);
        }

        // East side (x = radius), skip corners
        wallIndex = 0;
        for (float z = -radius + step; z < radius; z += step)
        {
            CreateWallSegment(wallManager.transform, $"Wall_E{++wallIndex}",
                new Vector3(radius, 1, z), new Vector3(0.5f, 2, step), wallMat);
        }

        // West side (x = -radius), skip corners
        wallIndex = 0;
        for (float z = -radius + step; z < radius; z += step)
        {
            CreateWallSegment(wallManager.transform, $"Wall_W{++wallIndex}",
                new Vector3(-radius, 1, z), new Vector3(0.5f, 2, step), wallMat);
        }

        Debug.Log("Courtyard rebuilt at half size (±4).");
        EditorUtility.SetDirty(wallManager);
    }

    /// <summary>Rotate mesh vertices/normals in-place. Used to bake FBX Z-up to Unity Y-up.</summary>
    static Mesh RotateMeshZToY(Mesh source)
    {
        var mesh = Instantiate(source);
        mesh.name = source.name + "_YUp";
        var rot = Quaternion.Euler(-90f, 0f, 0f);

        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
            verts[i] = rot * verts[i];
        mesh.vertices = verts;

        var norms = mesh.normals;
        for (int i = 0; i < norms.Length; i++)
            norms[i] = rot * norms[i];
        mesh.normals = norms;

        if (mesh.tangents != null && mesh.tangents.Length > 0)
        {
            var tans = mesh.tangents;
            for (int i = 0; i < tans.Length; i++)
            {
                var t = rot * new Vector3(tans[i].x, tans[i].y, tans[i].z);
                tans[i] = new Vector4(t.x, t.y, t.z, tans[i].w);
            }
            mesh.tangents = tans;
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    static void CreateWallSegment(Transform parent, string name, Vector3 position, Vector3 scale, Material fallbackMat)
    {
        // Try to use WallSegment.fbx mesh (unit cube with stone detail)
        var wallFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/WallSegment.fbx");
        GameObject go;

        if (wallFbx != null)
        {
            var srcMf = wallFbx.GetComponentInChildren<MeshFilter>();
            var srcMr = wallFbx.GetComponentInChildren<MeshRenderer>();
            go = new GameObject(name);
            // FBX mesh data is Z-up; bake -90° X rotation into vertices
            // so non-uniform parent scale doesn't cause shearing.
            go.AddComponent<MeshFilter>().sharedMesh = RotateMeshZToY(srcMf.sharedMesh);
            var rend = go.AddComponent<MeshRenderer>();
            if (srcMr != null)
                rend.sharedMaterials = srcMr.sharedMaterials;
            go.AddComponent<BoxCollider>();
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (fallbackMat != null)
                go.GetComponent<Renderer>().sharedMaterial = fallbackMat;
        }

        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        go.AddComponent<Wall>();
        go.AddComponent<WallCorners>();

        var modifier = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
        modifier.ignoreFromBuild = true;

        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = Vector3.one;
        obstacle.center = Vector3.zero;
    }

    /// <summary>Update the WallSegment prefab with the FBX mesh and materials.</summary>
    static void UpdateWallPrefab()
    {
        var wallFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/WallSegment.fbx");
        if (wallFbx == null) return;

        var srcMf = wallFbx.GetComponentInChildren<MeshFilter>();
        var srcMr = wallFbx.GetComponentInChildren<MeshRenderer>();
        if (srcMf == null) return;

        string path = "Assets/Prefabs/Fortress/WallSegment.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);

        // Remove any child Mesh objects from previous approach
        Transform oldChild = contents.transform.Find("Mesh");
        if (oldChild != null) DestroyImmediate(oldChild.gameObject);

        // Bake rotation into mesh vertices (FBX Z-up → Unity Y-up)
        var rotatedMesh = RotateMeshZToY(srcMf.sharedMesh);

        var mf = contents.GetComponent<MeshFilter>();
        if (mf == null) mf = contents.AddComponent<MeshFilter>();
        mf.sharedMesh = rotatedMesh;

        var mr = contents.GetComponent<MeshRenderer>();
        if (mr == null) mr = contents.AddComponent<MeshRenderer>();
        if (srcMr != null) mr.sharedMaterials = srcMr.sharedMaterials;

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("[SceneSetup] Updated WallSegment prefab with FBX mesh.");
    }

    [MenuItem("Game/Rebuild Walls With Gates")]
    public static void RebuildWallsWithGates()
    {
        var wallManager = GameObject.Find("WallManager");
        if (wallManager == null) { Debug.LogError("No WallManager found"); return; }

        var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall.mat");

        // Delete ALL children of WallManager (walls + old gates)
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in wallManager.transform)
            toDelete.Add(child.gameObject);
        foreach (var go in toDelete)
            DestroyImmediate(go);

        // Walls centered at ±3.75 (moved inward by 0.25 = half wall thickness)
        // E/W walls extend to full ±4 on Z to wrap corners cleanly
        float wallCenter = 3.75f; // 4 - 0.25 (half of 0.5 thickness)
        float wallThick = 0.5f;

        // Create or load gate materials
        Material gateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gate.mat");
        if (gateMat == null)
        {
            gateMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            gateMat.color = new Color(0.45f, 0.28f, 0.15f);
            gateMat.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(gateMat, "Assets/Materials/Gate.mat");
        }

        Material postMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GatePost.mat");
        if (postMat == null)
        {
            postMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            postMat.color = new Color(0.35f, 0.22f, 0.1f);
            postMat.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(postMat, "Assets/Materials/GatePost.mat");
        }

        // N/S walls: stop at ±3.75 on X (E/W walls wrap corners)
        // Each section: from x=±1 to x=±3.75, width=2.75, center at ±2.375
        float nsHalfLen = (wallCenter - 1f) / 2f;  // 1.375
        float nsCenterX = 1f + nsHalfLen;           // 2.375
        float nsWidth = wallCenter - 1f;             // 2.75

        // North wall (z=3.75)
        CreateWallSegment(wallManager.transform, "Wall_NW", new Vector3(-nsCenterX, 1, wallCenter), new Vector3(nsWidth, 2, wallThick), wallMat);
        CreateWallSegment(wallManager.transform, "Wall_NE", new Vector3(nsCenterX, 1, wallCenter), new Vector3(nsWidth, 2, wallThick), wallMat);
        // South wall (z=-3.75)
        CreateWallSegment(wallManager.transform, "Wall_SW", new Vector3(-nsCenterX, 1, -wallCenter), new Vector3(nsWidth, 2, wallThick), wallMat);
        CreateWallSegment(wallManager.transform, "Wall_SE", new Vector3(nsCenterX, 1, -wallCenter), new Vector3(nsWidth, 2, wallThick), wallMat);

        // E/W walls: extend z from ±1 to ±4 (full corner wrap), length=3, center at ±2.5
        float ewLen = 3f;
        float ewCenterZ = 2.5f;

        // East wall (x=3.75)
        CreateWallSegment(wallManager.transform, "Wall_EN", new Vector3(wallCenter, 1, ewCenterZ), new Vector3(wallThick, 2, ewLen), wallMat);
        CreateWallSegment(wallManager.transform, "Wall_ES", new Vector3(wallCenter, 1, -ewCenterZ), new Vector3(wallThick, 2, ewLen), wallMat);
        // West wall (x=-3.75)
        CreateWallSegment(wallManager.transform, "Wall_WN", new Vector3(-wallCenter, 1, ewCenterZ), new Vector3(wallThick, 2, ewLen), wallMat);
        CreateWallSegment(wallManager.transform, "Wall_WS", new Vector3(-wallCenter, 1, -ewCenterZ), new Vector3(wallThick, 2, ewLen), wallMat);

        // 1 gate (East only) — enemies spawn from the west
        CreateGate(wallManager.transform, "Gate_E", new Vector3(wallCenter, 0, 0), 90f, gateMat, postMat);

        // Fill wall segments where the removed gates were (N, S, W)
        CreateWallSegment(wallManager.transform, "Wall_NC", new Vector3(0, 1, wallCenter), new Vector3(2, 2, wallThick), wallMat);
        CreateWallSegment(wallManager.transform, "Wall_SC", new Vector3(0, 1, -wallCenter), new Vector3(2, 2, wallThick), wallMat);
        CreateWallSegment(wallManager.transform, "Wall_WC", new Vector3(-wallCenter, 1, 0), new Vector3(wallThick, 2, 2), wallMat);

        // Update the WallSegment prefab so newly placed walls also use the FBX mesh
        UpdateWallPrefab();

        Debug.Log("Walls rebuilt with 1 gate (East).");
        EditorUtility.SetDirty(wallManager);

        // Bake NavMesh so agents can path through gates
        BakeNavMesh();
    }

    static void CreateGate(Transform parent, string name, Vector3 position, float yRotation, Material doorMat, Material postMat)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0, yRotation, 0);
        var gate = root.AddComponent<Gate>();

        var link = root.AddComponent<Unity.AI.Navigation.NavMeshLink>();
        link.startPoint = new Vector3(0, 0, -1.5f);
        link.endPoint = new Vector3(0, 0, 1.5f);
        link.width = 2f;
        link.bidirectional = true;
        link.area = 3;

        // Try to use Gate.fbx model (armature with door pivots)
        var gateFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Gate.fbx");
        if (gateFbx != null)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(gateFbx);
            model.name = "GateModel";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Find door pivot bones in the FBX hierarchy
            Transform leftPivot = null, rightPivot = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "LeftDoorPivot") leftPivot = t;
                else if (t.name == "RightDoorPivot") rightPivot = t;
            }

            var so = new SerializedObject(gate);
            so.FindProperty("leftDoorPivot").objectReferenceValue = leftPivot;
            so.FindProperty("rightDoorPivot").objectReferenceValue = rightPivot;
            so.ApplyModifiedProperties();

            Debug.Log($"[SceneSetup] Gate FBX wired: LeftPivot={leftPivot != null}, RightPivot={rightPivot != null}");
        }
        else
        {
            // Fallback: build gate from primitive cubes
            Debug.LogWarning("[SceneSetup] Gate.fbx not found, using primitive cubes.");
            BuildGatePrimitives(root, gate, doorMat, postMat);
        }
    }

    static void BuildGatePrimitives(GameObject root, Gate gate, Material doorMat, Material postMat)
    {
        var leftPivot = new GameObject("LeftDoorPivot");
        leftPivot.transform.SetParent(root.transform, false);
        leftPivot.transform.localPosition = new Vector3(-1, 0, 0);

        var leftDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftDoor.name = "LeftDoor";
        DestroyImmediate(leftDoor.GetComponent<BoxCollider>());
        leftDoor.transform.SetParent(leftPivot.transform, false);
        leftDoor.transform.localPosition = new Vector3(0.5f, 1, 0);
        leftDoor.transform.localScale = new Vector3(1, 2, 0.15f);
        if (doorMat != null) leftDoor.GetComponent<Renderer>().sharedMaterial = doorMat;

        var rightPivot = new GameObject("RightDoorPivot");
        rightPivot.transform.SetParent(root.transform, false);
        rightPivot.transform.localPosition = new Vector3(1, 0, 0);

        var rightDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightDoor.name = "RightDoor";
        DestroyImmediate(rightDoor.GetComponent<BoxCollider>());
        rightDoor.transform.SetParent(rightPivot.transform, false);
        rightDoor.transform.localPosition = new Vector3(-0.5f, 1, 0);
        rightDoor.transform.localScale = new Vector3(1, 2, 0.15f);
        if (doorMat != null) rightDoor.GetComponent<Renderer>().sharedMaterial = doorMat;

        var leftPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPost.name = "LeftPost";
        DestroyImmediate(leftPost.GetComponent<BoxCollider>());
        leftPost.transform.SetParent(root.transform, false);
        leftPost.transform.localPosition = new Vector3(-1.1f, 1.25f, 0);
        leftPost.transform.localScale = new Vector3(0.2f, 2.5f, 0.3f);
        if (postMat != null) leftPost.GetComponent<Renderer>().sharedMaterial = postMat;

        var rightPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPost.name = "RightPost";
        DestroyImmediate(rightPost.GetComponent<BoxCollider>());
        rightPost.transform.SetParent(root.transform, false);
        rightPost.transform.localPosition = new Vector3(1.1f, 1.25f, 0);
        rightPost.transform.localScale = new Vector3(0.2f, 2.5f, 0.3f);
        if (postMat != null) rightPost.GetComponent<Renderer>().sharedMaterial = postMat;

        var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.name = "Lintel";
        DestroyImmediate(lintel.GetComponent<BoxCollider>());
        lintel.transform.SetParent(root.transform, false);
        lintel.transform.localPosition = new Vector3(0, 2.6f, 0);
        lintel.transform.localScale = new Vector3(2.4f, 0.2f, 0.3f);
        if (postMat != null) lintel.GetComponent<Renderer>().sharedMaterial = postMat;

        var so = new SerializedObject(gate);
        so.FindProperty("leftDoorPivot").objectReferenceValue = leftPivot.transform;
        so.FindProperty("rightDoorPivot").objectReferenceValue = rightPivot.transform;
        so.ApplyModifiedProperties();
    }

    [MenuItem("Game/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        var surface = FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("NavMesh baked.");
        }
        else
        {
            Debug.LogError("No NavMeshSurface found.");
        }
    }
#endif
}
