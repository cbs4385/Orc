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

        root.AddComponent<Engineer>();
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 1f;
        agent.speed = 3f;

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

        root.AddComponent<Pikeman>();

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

        root.AddComponent<Crossbowman>();

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

        root.AddComponent<Wizard>();

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
#endif
}
