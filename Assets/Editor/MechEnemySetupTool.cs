using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;
using System.IO;

public class MechEnemySetupTool
{
    private const string PREFAB_DIR = "Assets/Prefab";
    private const string MATERIAL_DIR = "Assets/Material";
    private const string MECH_MODEL_PATH = "Assets/Mech/Models/mech.fbx";
    private const string CONTROLLER_PATH = "Assets/Mech/Controller/Mech_Enemy.controller";
    private const string BULLET_PREFAB_PATH = "Assets/Prefab/Mech_Bullet.prefab";
    private const string MISSILE_PREFAB_PATH = "Assets/Prefab/Mech_Missile.prefab";
    private const string MECH_PREFAB_PATH = "Assets/Prefab/Enemy_Mech.prefab";

    private const string BIG_EXPLOSION_PATH = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/BigExplosion.prefab";
    private const string AUDIO_EXPLOSION_PATH = "Assets/Mech/Sounds/explosion.mp3";
    private const string AUDIO_BIG_CANON_PATH = "Assets/Mech/Sounds/BigCanon.wav";
    private const string AUDIO_SMALL_CANON_PATH = "Assets/Mech/Sounds/SmallCanon.wav";
    private const string AUDIO_FOOTSTEP_PATH = "Assets/Mech/Sounds/FootSteps.wav";

    [MenuItem("Tools/Mech/Setup Mech Enemy (自動セットアップ)")]
    public static void SetupAll()
    {
        EnsureDirectories();

        Debug.Log("[MechSetup] 1/4: 機関銃弾プレハブを作成中...");
        GameObject bulletPrefab = CreateOrUpdateBulletPrefab();

        Debug.Log("[MechSetup] 2/4: 追尾ミサイルプレハブを作成中...");
        GameObject missilePrefab = CreateOrUpdateMissilePrefab();

        Debug.Log("[MechSetup] 3/4: アニメーターコントローラーを作成中...");
        AnimatorController animController = CreateOrUpdateAnimatorController();

        Debug.Log("[MechSetup] 4/4: Mech上位敵プレハブを作成中...");
        CreateOrUpdateMechEnemyPrefab(bulletPrefab, missilePrefab, animController);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Mech Setup", "上位敵メカ（Enemy_Mech）のセットアップが正常に完了しました！\n\n生成されたプレハブ:\nAssets/Prefab/Enemy_Mech.prefab\nAssets/Prefab/Mech_Bullet.prefab\nAssets/Prefab/Mech_Missile.prefab\n\nコントローラー:\nAssets/Mech/Controller/Mech_Enemy.controller", "OK");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
        }
        if (!Directory.Exists(MATERIAL_DIR))
        {
            Directory.CreateDirectory(MATERIAL_DIR);
        }
        if (!Directory.Exists("Assets/Mech/Controller"))
        {
            Directory.CreateDirectory("Assets/Mech/Controller");
        }
    }

    private static Material GetOrCreateBulletGlowMaterial()
    {
        string matPath = "Assets/Material/Mech_Bullet_Glow.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(1.0f, 0.75f, 0.15f, 1.0f));
                AssetDatabase.CreateAsset(mat, matPath);
            }
        }
        return mat;
    }

    private static Material GetDefaultTrailMaterial()
    {
        Material glowMat = GetOrCreateBulletGlowMaterial();
        if (glowMat != null) return glowMat;

        Material mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (s != null)
            {
                mat = new Material(s);
            }
        }
        return mat;
    }

    private static GameObject CreateOrUpdateBulletPrefab()
    {
        Material glowMat = GetOrCreateBulletGlowMaterial();

        GameObject bulletMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bulletMesh.name = "BulletMesh";
        bulletMesh.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
        bulletMesh.transform.localRotation = Quaternion.Euler(90f, 0, 0);

        if (glowMat != null)
        {
            bulletMesh.GetComponent<MeshRenderer>().material = glowMat;
        }

        GameObject rootGo = new GameObject("Mech_Bullet");
        bulletMesh.transform.SetParent(rootGo.transform, false);

        CapsuleCollider col = rootGo.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 0.35f;
        col.height = 1.6f;
        col.direction = 2; // Z軸方向

        Object.DestroyImmediate(bulletMesh.GetComponent<Collider>());

        Rigidbody rb = rootGo.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Bullet bullet = rootGo.AddComponent<Bullet>();
        bullet.speed = 26f;
        bullet.damage = 4;
        bullet.lifeTime = 3f;
        bullet.targetTag = "Player";
        bullet.safetyTime = 0.08f;

        // TrailRendererでハッキリ見えるオレンジ発光トレイルを追加
        TrailRenderer trail = rootGo.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.05f;
        if (glowMat != null) trail.material = glowMat;
        trail.startColor = new Color(1f, 0.85f, 0.2f, 1f);
        trail.endColor = new Color(1f, 0.3f, 0f, 0f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, BULLET_PREFAB_PATH);
        Object.DestroyImmediate(rootGo);
        return prefab;
    }

    private static GameObject CreateOrUpdateMissilePrefab()
    {
        GameObject missileGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missileGo.name = "Mech_Missile";
        missileGo.transform.localScale = new Vector3(0.35f, 0.7f, 0.35f);
        missileGo.transform.localRotation = Quaternion.Euler(90f, 0, 0);

        GameObject rootGo = new GameObject("Mech_Missile");
        missileGo.transform.SetParent(rootGo.transform, false);

        CapsuleCollider col = rootGo.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 0.35f;
        col.height = 1.4f;
        col.direction = 2;

        Object.DestroyImmediate(missileGo.GetComponent<Collider>());

        Rigidbody rb = rootGo.AddComponent<Rigidbody>();
        rb.useGravity = false;

        HomingMissile missile = rootGo.AddComponent<HomingMissile>();
        missile.speed = 16f;
        missile.rotateSpeed = 170f;
        missile.homingDuration = 1.0f;
        missile.safetyLaunchTime = 0.4f;
        missile.lifeTime = 6.0f;
        missile.damage = 25;
        missile.explosionRadius = 4.5f;
        missile.knockbackForce = 16f;
        missile.targetTag = "Player";

        missile.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BIG_EXPLOSION_PATH);
        missile.explosionSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_EXPLOSION_PATH);

        TrailRenderer trail = rootGo.AddComponent<TrailRenderer>();
        trail.time = 0.6f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.05f;
        Material trailMat = GetDefaultTrailMaterial();
        if (trailMat != null) trail.material = trailMat;
        trail.startColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);
        trail.endColor = new Color(0.4f, 0.4f, 0.4f, 0.0f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, MISSILE_PREFAB_PATH);
        Object.DestroyImmediate(rootGo);
        return prefab;
    }

    private static AnimatorController CreateOrUpdateAnimatorController()
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("ShootSmall", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ShootBig", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        AnimationClip clipWalk = FindClip(MECH_MODEL_PATH, "WalkInPlace");
        if (clipWalk == null) clipWalk = FindClip(MECH_MODEL_PATH, "Walk");

        AnimationClip clipShootSmall = FindClip(MECH_MODEL_PATH, "ShootSmallCanon_A");
        AnimationClip clipShootBig = FindClip(MECH_MODEL_PATH, "ShootBigCanon_A");
        AnimationClip clipHit = FindClip(MECH_MODEL_PATH, "Hit");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = clipWalk;
        idleState.speed = 0.001f;

        AnimatorState walkState = stateMachine.AddState("Walk");
        walkState.motion = clipWalk;
        walkState.speed = 1.0f;

        AnimatorState shootSmallState = stateMachine.AddState("ShootSmallCanon");
        shootSmallState.motion = clipShootSmall;

        AnimatorState shootBigState = stateMachine.AddState("ShootBigCanon");
        shootBigState.motion = clipShootBig;

        AnimatorState hitState = stateMachine.AddState("Hit");
        hitState.motion = clipHit;

        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
        idleToWalk.duration = 0.15f;
        idleToWalk.hasExitTime = false;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
        walkToIdle.duration = 0.15f;
        walkToIdle.hasExitTime = false;

        var anyToShootSmall = stateMachine.AddAnyStateTransition(shootSmallState);
        anyToShootSmall.AddCondition(AnimatorConditionMode.If, 0, "ShootSmall");
        anyToShootSmall.duration = 0.1f;
        anyToShootSmall.hasExitTime = false;

        var shootSmallToIdle = shootSmallState.AddTransition(idleState);
        shootSmallToIdle.hasExitTime = true;
        shootSmallToIdle.duration = 0.2f;

        var anyToShootBig = stateMachine.AddAnyStateTransition(shootBigState);
        anyToShootBig.AddCondition(AnimatorConditionMode.If, 0, "ShootBig");
        anyToShootBig.duration = 0.1f;
        anyToShootBig.hasExitTime = false;

        var shootBigToIdle = shootBigState.AddTransition(idleState);
        shootBigToIdle.hasExitTime = true;
        shootBigToIdle.duration = 0.2f;

        var anyToHit = stateMachine.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        anyToHit.duration = 0.05f;
        anyToHit.hasExitTime = false;

        var hitToIdle = hitState.AddTransition(idleState);
        hitToIdle.hasExitTime = true;
        hitToIdle.duration = 0.2f;

        return controller;
    }

    private static AnimationClip FindClip(string fbxPath, string clipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object obj in assets)
        {
            if (obj is AnimationClip clip && clip.name == clipName)
            {
                return clip;
            }
        }
        return null;
    }

    private static void CreateOrUpdateMechEnemyPrefab(GameObject bulletPrefab, GameObject missilePrefab, AnimatorController controller)
    {
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MECH_MODEL_PATH);
        if (fbxAsset == null)
        {
            Debug.LogError($"[MechSetup] {MECH_MODEL_PATH} が見つかりません。");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = "Enemy_Mech";
        instance.tag = "Enemy";

        Object.DestroyImmediate(instance.GetComponent<MechWalk>());
        Object.DestroyImmediate(instance.GetComponent<MechShoot>());
        Object.DestroyImmediate(instance.GetComponent<MechHit>());

        CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
        if (col == null) col = instance.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1.8f, 0);
        col.radius = 1.3f;
        col.height = 3.6f;

        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null) rb = instance.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null) agent = instance.AddComponent<NavMeshAgent>();
        agent.speed = 4.0f;
        agent.stoppingDistance = 0.5f;
        agent.radius = 1.3f;
        agent.height = 3.6f;
        agent.angularSpeed = 140f;
        agent.acceleration = 10f;

        AudioSource audioSource = instance.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = instance.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.maxDistance = 45f;

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        EnemyHealth health = instance.GetComponent<EnemyHealth>();
        if (health == null) health = instance.AddComponent<EnemyHealth>();
        health.maxHealth = 150;
        health.canBeAbsorbed = false;
        health.canBeInvestigated = true;
        health.itemDropProbability = 1.0f;
        health.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BIG_EXPLOSION_PATH);

        EnemyMech mech = instance.GetComponent<EnemyMech>();
        if (mech == null) mech = instance.AddComponent<EnemyMech>();

        mech.idealDistance = 11.0f;
        mech.minDistance = 7.0f;
        mech.maxDistance = 16.0f;
        mech.chaseSpeed = 4.0f;
        mech.retreatSpeed = 3.5f;
        mech.missileHomingDuration = 1.0f;
        mech.enableBodyAim = true; // 上半身プレイヤー照準を有効化！
        mech.maxAimYawAngle = 75.0f;
        mech.aimSpeed = 8.0f;
        mech.machineGunBulletPrefab = bulletPrefab;
        mech.homingMissilePrefab = missilePrefab;

        mech.audioSmallCanon = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_SMALL_CANON_PATH);
        mech.audioBigCanon = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_BIG_CANON_PATH);
        mech.audioFootStep = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_FOOTSTEP_PATH);

        Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "SmallCanon01_L") mech.muzzleSmallL = t;
            else if (t.name == "SmallCanon01_R") mech.muzzleSmallR = t;
            else if (t.name == "BigCanon01_L") mech.muzzleBigL = t;
            else if (t.name == "BigCanon01_R") mech.muzzleBigR = t;
            else if (t.name == "Body") mech.bodyBone = t;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(instance, MECH_PREFAB_PATH, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(instance);

        Debug.Log($"[MechSetup] {MECH_PREFAB_PATH} を正常に作成しました！");
    }
}
