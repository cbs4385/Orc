using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Game/Create All Prefabs")]
    public static void CreateAllPrefabs()
    {
        CreateProjectilePrefab();
        CreateEnemyProjectilePrefab();
        CreateEnemyPrefab();
        CreateTreasurePrefab();
        CreateMenialPrefab();
        CreateRefugeePrefab();
        CreateEngineerPrefab();
        CreatePikemanPrefab();
        CreateCrossbowmanPrefab();
        CreateWizardPrefab();
        CreateWallSegmentPrefab();
        CreateCrossbowBoltPrefab();
        CreateFireMissilePrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All prefabs created!");
    }

    static void CreateProjectilePrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BallistaProjectile";
        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);

        // Remove default collider and add trigger
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        go.AddComponent<BallistaProjectile>();
        go.AddComponent<Rigidbody>().isKinematic = true;

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectile.mat");
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Weapons/BallistaProjectile.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateEnemyProjectilePrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EnemyProjectile";
        go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        go.AddComponent<EnemyProjectile>();
        go.AddComponent<Rigidbody>().isKinematic = true;

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/EnemyProjectile.mat");
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Weapons/EnemyProjectile.prefab");
        Object.DestroyImmediate(go);
    }

static void CreateEnemyPrefab()
    {
        var root = new GameObject("Enemy");

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 1.1f, 0);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        // Add NavMeshAgent FIRST (before EnemyMovement which requires it)
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.5f;

        root.AddComponent<Enemy>();
        root.AddComponent<EnemyMovement>();
        root.AddComponent<EnemyAttack>();

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OrcGrunt.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            head.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Enemies/Enemy.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreateTreasurePrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TreasurePickup";
        go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        go.transform.rotation = Quaternion.Euler(0, 45, 0);

        go.AddComponent<TreasurePickup>();

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Treasure.mat");
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Loot/TreasurePickup.prefab");
        Object.DestroyImmediate(go);
    }

static void CreateMenialPrefab()
    {
        var root = new GameObject("Menial");

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.3f, 0);
        body.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 0.65f, 0);
        head.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        // Add NavMeshAgent FIRST (Menial has RequireComponent)
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.2f;
        agent.height = 0.8f;
        agent.speed = 4f;

        root.AddComponent<Menial>();

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Menial.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            head.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Characters/Menial.prefab");
        Object.DestroyImmediate(root);
    }

static void CreateRefugeePrefab()
    {
        var root = new GameObject("Refugee");

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.3f, 0);
        body.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 0.65f, 0);
        head.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        // Add NavMeshAgent FIRST (Refugee has RequireComponent)
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.2f;
        agent.height = 0.8f;
        agent.speed = 3.5f;

        root.AddComponent<Refugee>();

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Refugee.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            head.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Characters/Refugee.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreateEngineerPrefab()
    {
        var root = new GameObject("Engineer");
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.4f, 0);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Wrench indicator (small cube)
        var wrench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wrench.name = "Wrench";
        wrench.transform.SetParent(root.transform);
        wrench.transform.localPosition = new Vector3(0.3f, 0.6f, 0);
        wrench.transform.localScale = new Vector3(0.1f, 0.3f, 0.05f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 1f;
        agent.speed = 3f;

        root.AddComponent<Engineer>();
        AssignDefenderData(root, "Assets/ScriptableObjects/Defenders/Engineer.asset");

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Menial.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            wrench.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Defenders/Engineer.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreatePikemanPrefab()
    {
        var root = new GameObject("Pikeman");
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.4f, 0);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Pike
        var pike = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pike.name = "Pike";
        pike.transform.SetParent(root.transform);
        pike.transform.localPosition = new Vector3(0, 0.9f, 0.3f);
        pike.transform.localScale = new Vector3(0.05f, 0.05f, 1.2f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 1f;
        agent.speed = 3f;

        root.AddComponent<Pikeman>();
        AssignDefenderData(root, "Assets/ScriptableObjects/Defenders/Pikeman.asset");

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Menial.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            pike.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Defenders/Pikeman.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreateCrossbowmanPrefab()
    {
        var root = new GameObject("Crossbowman");
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.4f, 0);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Bow
        var bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.name = "Bow";
        bow.transform.SetParent(root.transform);
        bow.transform.localPosition = new Vector3(0.3f, 0.5f, 0);
        bow.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 1f;
        agent.speed = 3f;

        root.AddComponent<Crossbowman>();
        AssignDefenderData(root, "Assets/ScriptableObjects/Defenders/Crossbowman.asset");

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Menial.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            bow.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Defenders/Crossbowman.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreateWizardPrefab()
    {
        var root = new GameObject("Wizard");
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.4f, 0);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 0.8f, 0);
        head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 1f;
        agent.speed = 3f;

        root.AddComponent<Wizard>();
        AssignDefenderData(root, "Assets/ScriptableObjects/Defenders/Wizard.asset");

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wizard.mat");
        if (mat != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = mat;
            head.GetComponent<Renderer>().sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Defenders/Wizard.prefab");
        Object.DestroyImmediate(root);
    }

    static void CreateWallSegmentPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "WallSegment";
        go.transform.localScale = new Vector3(2, 2, 0.5f);

        go.AddComponent<Wall>();
        go.AddComponent<WallCorners>();
        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;

        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall.mat");
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Fortress/WallSegment.prefab");
        Object.DestroyImmediate(go);
    }

    [MenuItem("Game/Rebuild Scorpio")]
    public static void RebuildScorpio()
    {
        // Find ScorpioBase in scene
        var scorpioBase = GameObject.Find("ScorpioBase");
        if (scorpioBase == null)
        {
            Debug.LogError("[PrefabCreator] ScorpioBase not found in scene!");
            return;
        }

        // Remove any existing child objects
        for (int i = scorpioBase.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(scorpioBase.transform.GetChild(i).gameObject);

        // Also remove any orphaned scorpio parts at root
        foreach (var name in new[] { "ScorpioArmLeft", "ScorpioArmRight", "ScorpioMountLeft", "ScorpioMountRight" })
        {
            var orphan = GameObject.Find(name);
            if (orphan != null && orphan.transform.parent == null)
                Object.DestroyImmediate(orphan);
        }

        var armMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ScorpioArm.mat");
        var bowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BallistaBow.mat");
        var stringMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BallistaString.mat");

        // Base scale is (0.5, 0.3, 1.5) — non-uniform, so rotated children
        // would be skewed. Use an intermediate "ArmPivot" with inverse scale
        // so arms/mounts can use world-scale coordinates without distortion.
        Vector3 baseScale = scorpioBase.transform.localScale;

        var armPivot = new GameObject("ArmPivot");
        armPivot.transform.SetParent(scorpioBase.transform, false);
        armPivot.transform.localPosition = Vector3.zero;
        armPivot.transform.localScale = new Vector3(
            1f / baseScale.x, 1f / baseScale.y, 1f / baseScale.z);
        // Now children of armPivot use real world units relative to base center

        // Mount brackets on sides near front of base
        var mountL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mountL.name = "ScorpioMountLeft";
        mountL.transform.SetParent(armPivot.transform, false);
        mountL.transform.localPosition = new Vector3(-0.2f, 0f, 0.6f);
        mountL.transform.localScale = new Vector3(0.1f, 0.15f, 0.15f);
        if (armMat != null) mountL.GetComponent<Renderer>().sharedMaterial = armMat;

        var mountR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mountR.name = "ScorpioMountRight";
        mountR.transform.SetParent(armPivot.transform, false);
        mountR.transform.localPosition = new Vector3(0.2f, 0f, 0.6f);
        mountR.transform.localScale = new Vector3(0.1f, 0.15f, 0.15f);
        if (armMat != null) mountR.GetComponent<Renderer>().sharedMaterial = armMat;

        // Torsion arms — extend forward from mounts in a V shape
        var armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armL.name = "ScorpioArmLeft";
        armL.transform.SetParent(armPivot.transform, false);
        armL.transform.localPosition = new Vector3(-0.28f, 0f, 0.65f);
        armL.transform.localEulerAngles = new Vector3(0, -20, 0);
        armL.transform.localScale = new Vector3(0.4f, 0.08f, 0.06f);
        if (bowMat != null) armL.GetComponent<Renderer>().sharedMaterial = bowMat;

        var armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armR.name = "ScorpioArmRight";
        armR.transform.SetParent(armPivot.transform, false);
        armR.transform.localPosition = new Vector3(0.28f, 0f, 0.65f);
        armR.transform.localEulerAngles = new Vector3(0, 20, 0);
        armR.transform.localScale = new Vector3(0.4f, 0.08f, 0.06f);
        if (bowMat != null) armR.GetComponent<Renderer>().sharedMaterial = bowMat;

        // Bowstring connecting arm tips
        var bowstring = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bowstring.name = "ScorpioBowstring";
        bowstring.transform.SetParent(armPivot.transform, false);
        bowstring.transform.localPosition = new Vector3(0f, 0f, 0.78f);
        bowstring.transform.localScale = new Vector3(0.5f, 0.02f, 0.02f);
        if (stringMat != null) bowstring.GetComponent<Renderer>().sharedMaterial = stringMat;

        // Trough/rail along top of base
        var trough = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trough.name = "ScorpioTrough";
        trough.transform.SetParent(armPivot.transform, false);
        trough.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        trough.transform.localScale = new Vector3(0.06f, 0.03f, 1.3f);
        if (armMat != null) trough.GetComponent<Renderer>().sharedMaterial = armMat;

        Debug.Log("[PrefabCreator] Scorpio rebuilt with arms, mounts, bowstring, and trough as children of ScorpioBase.");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    static void CreateCrossbowBoltPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "CrossbowBolt";
        go.transform.localScale = new Vector3(0.06f, 0.06f, 0.4f);

        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        go.AddComponent<DefenderProjectile>();
        go.AddComponent<Rigidbody>().isKinematic = true;

        // Brown wooden bolt color
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.45f, 0.3f, 0.15f);
        mat.SetFloat("_Smoothness", 0f);
        AssetDatabase.CreateAsset(mat, "Assets/Materials/CrossbowBolt.mat");
        go.GetComponent<Renderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Weapons/CrossbowBolt.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("[PrefabCreator] CrossbowBolt prefab created.");
    }

    static void CreateFireMissilePrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FireMissile";
        go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        go.AddComponent<DefenderProjectile>();
        go.AddComponent<Rigidbody>().isKinematic = true;

        // Bright orange-red fire color
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.4f, 0.1f);
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Surface", 0); // opaque
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 2f);
        AssetDatabase.CreateAsset(mat, "Assets/Materials/FireMissile.mat");
        go.GetComponent<Renderer>().sharedMaterial = mat;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Weapons/FireMissile.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("[PrefabCreator] FireMissile prefab created.");
    }

    static void AssignDefenderData(GameObject root, string assetPath)
    {
        var defenderData = AssetDatabase.LoadAssetAtPath<DefenderData>(assetPath);
        if (defenderData == null)
        {
            Debug.LogWarning($"[PrefabCreator] DefenderData not found at {assetPath}. Run Game/Create ScriptableObjects first.");
            return;
        }
        var defender = root.GetComponent<Defender>();
        if (defender == null) return;
        var so = new SerializedObject(defender);
        so.FindProperty("data").objectReferenceValue = defenderData;
        so.ApplyModifiedProperties();
    }
#endif
}
