using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    [Header("フェーズ設定")]
    [SerializeField]
    private PhaseSettings[] phases = new PhaseSettings[5];

    [Header("生成するPrefab")]
    [SerializeField]
    private GameObject[] toyPrefabs;

    [SerializeField]
    private GameObject[] treasurePrefabs;

    [SerializeField]
    private GameObject cameraPrefab;

    [SerializeField]
    private GameObject ownerPrefab;

    [Header("共通SpawnPoint")]
    [SerializeField]
    private Transform[] toySpawnPoints;

    [SerializeField]
    private Transform[] treasureSpawnPoints;

    [SerializeField]
    private Transform[] cameraSpawnPoints;

    [SerializeField]
    private Transform[] ownerSpawnPoints;

    [Header("生成先")]
    [SerializeField]
    private Transform collectibleRoot;

    [SerializeField]
    private Transform ownerRoot;

    [Header("鳥")]
    [SerializeField]
    private BirdAttackDirector birdAttackDirector;

    [Header("UI")]
    [SerializeField]
    private Image timerFill;

    [SerializeField]
    private TMP_Text timerText;

    [SerializeField]
    private TMP_Text phaseText;

    private readonly HashSet<ToyFollower> activeToys =
        new HashSet<ToyFollower>();

    private readonly HashSet<ToyFollower> activeTreasures =
        new HashSet<ToyFollower>();

    private readonly HashSet<ToyFollower> activeCameras =
        new HashSet<ToyFollower>();

    private readonly List<GameObject> activeOwners =
        new List<GameObject>();

    private readonly HashSet<Transform> usedOwnerSpawnPoints =
        new HashSet<Transform>();

    private int currentPhaseIndex = -1;
    private float remainingTime;
    private bool isRunning;
    private bool isChangingPhase;

    private PhaseSettings CurrentSettings
    {
        get
        {
            if (phases == null ||
                currentPhaseIndex < 0 ||
                currentPhaseIndex >= phases.Length)
            {
                return null;
            }

            return phases[currentPhaseIndex];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("PhaseManagerが複数存在します。", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PrepareForCountdown()
    {
        birdAttackDirector?.SetAttackMode(false);

        if (phases == null ||
            phases.Length == 0 ||
            phases[0] == null)
        {
            return;
        }

        remainingTime = phases[0].timeLimit;
        RefreshUI(phases[0]);
    }

    public void BeginPhases()
    {
        if (isRunning)
        {
            return;
        }

        if (phases == null || phases.Length != 5)
        {
            Debug.LogError(
                "PhaseManagerのPhasesには5フェーズ設定してください。",
                this
            );
            return;
        }

        currentPhaseIndex = 0;
        isRunning = true;

        BeginCurrentPhase();
    }

    private void Update()
    {
        if (!isRunning || isChangingPhase)
        {
            return;
        }

        if (GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Max(0f, remainingTime);

        RefreshUI(CurrentSettings);

        if (remainingTime <= 0f)
        {
            FinishCurrentPhase();
        }
    }

    private void BeginCurrentPhase()
    {
        PhaseSettings settings = CurrentSettings;

        if (settings == null)
        {
            Debug.LogError(
                $"フェーズ{currentPhaseIndex + 1}の設定がありません。",
                this
            );
            return;
        }

        isChangingPhase = true;
        remainingTime = Mathf.Max(1f, settings.timeLimit);

        // 持ち越し分とは別に、新フェーズ分を出現させる
        SpawnToyBatch(settings);
        SpawnTreasureBatch(settings);

        if (settings.spawnCamera)
        {
            SpawnCamera(settings);
        }

        EnsureOwnerCount(settings);

        birdAttackDirector?.SetAttackMode(
            settings.birdAttackEnabled
        );

        RefreshUI(settings);

        isChangingPhase = false;

        Debug.Log(
            $"{settings.phaseName}開始 " +
            $"おもちゃ残数:{activeToys.Count} " +
            $"宝残数:{activeTreasures.Count}"
        );
    }

    public void NotifyDelivered(ToyFollower deliveredItem)
    {
        if (deliveredItem == null)
        {
            return;
        }

        RemoveDestroyedReferences();

        if (activeCameras.Remove(deliveredItem))
        {
            return;
        }

        if (activeTreasures.Remove(deliveredItem))
        {
            if (isRunning &&
                !isChangingPhase &&
                activeTreasures.Count == 0 &&
                CurrentSettings.refillTreasuresWhenAllDelivered)
            {
                SpawnTreasureBatch(CurrentSettings);
            }

            return;
        }

        if (!activeToys.Remove(deliveredItem))
        {
            // PhaseManager以外が生成したアイテム
            return;
        }

        if (!isRunning ||
            isChangingPhase ||
            activeToys.Count > 0)
        {
            return;
        }

        if (CurrentSettings.refillToysWhenAllDelivered)
        {
            // フェーズ5
            SpawnToyBatch(CurrentSettings);
        }
        else
        {
            // フェーズ1～4
            FinishCurrentPhase();
        }
    }

    private void FinishCurrentPhase()
    {
        if (!isRunning || isChangingPhase)
        {
            return;
        }

        isChangingPhase = true;

        // 地面に残ったおもちゃと宝だけを消す。
        // 所持中・納品演出中のものは次フェーズへ持ち越す。
        RemoveUncarriedItems(activeToys);
        RemoveUncarriedItems(activeTreasures);

        bool isFinalPhase =
            currentPhaseIndex >= phases.Length - 1;

        if (isFinalPhase)
        {
            isRunning = false;
            birdAttackDirector?.SetAttackMode(false);

            GameManager.Instance?.FinishGameFromPhaseManager();
            return;
        }

        currentPhaseIndex++;
        BeginCurrentPhase();
    }

    private void SpawnToyBatch(PhaseSettings settings)
    {
        int count = GetRandomCount(
            settings.minToyCount,
            settings.maxToyCount
        );

        SpawnBatch(
            toyPrefabs,
            toySpawnPoints,
            count,
            activeToys
        );
    }

    private void SpawnTreasureBatch(PhaseSettings settings)
    {
        int count = GetRandomCount(
            settings.minTreasureCount,
            settings.maxTreasureCount
        );

        if (count <= 0)
        {
            return;
        }

        SpawnBatch(
            treasurePrefabs,
            treasureSpawnPoints,
            count,
            activeTreasures
        );
    }

    private void SpawnCamera(PhaseSettings settings)
    {
        List<Transform> points =
            CreateShuffledPointList(cameraSpawnPoints);

        if (cameraPrefab == null || points.Count == 0)
        {
            Debug.LogError(
                $"{settings.phaseName}のカメラ設定が不足しています。",
                this
            );
            return;
        }

        ToyFollower camera =
            CreateCollectible(cameraPrefab, points[0]);

        if (camera != null)
        {
            activeCameras.Add(camera);
        }
    }

    private void SpawnBatch(
        GameObject[] prefabs,
        Transform[] spawnPoints,
        int count,
        HashSet<ToyFollower> destination)
    {
        if (count <= 0)
        {
            return;
        }

        List<GameObject> validPrefabs = new List<GameObject>();

        if (prefabs != null)
        {
            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null)
                {
                    validPrefabs.Add(prefab);
                }
            }
        }

        List<Transform> points =
            CreateShuffledPointList(spawnPoints);

        if (validPrefabs.Count == 0 || points.Count == 0)
        {
            Debug.LogError(
                "生成PrefabまたはSpawnPointが設定されていません。",
                this
            );
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = validPrefabs[
                Random.Range(0, validPrefabs.Count)
            ];

            // SpawnPoint数より生成数が多い場合は再利用する
            Transform spawnPoint = points[i % points.Count];

            ToyFollower item =
                CreateCollectible(prefab, spawnPoint);

            if (item != null)
            {
                destination.Add(item);
            }
        }
    }

    private ToyFollower CreateCollectible(
        GameObject prefab,
        Transform spawnPoint)
    {
        GameObject instance = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            collectibleRoot
        );

        ToyFollower follower =
            instance.GetComponent<ToyFollower>();

        if (follower == null)
        {
            Debug.LogError(
                $"{prefab.name}にToyFollowerがありません。",
                prefab
            );

            Destroy(instance);
            return null;
        }

        return follower;
    }

    private void EnsureOwnerCount(PhaseSettings settings)
    {
        activeOwners.RemoveAll(owner => owner == null);

        int missingCount =
            settings.ownerCount - activeOwners.Count;

        if (missingCount <= 0)
        {
            return;
        }

        if (ownerPrefab == null)
        {
            Debug.LogError("飼い主Prefabが未設定です。", this);
            return;
        }

        List<Transform> candidates =
            CreateShuffledPointList(ownerSpawnPoints);

        candidates.RemoveAll(
            point => usedOwnerSpawnPoints.Contains(point)
        );

        for (int i = 0; i < missingCount; i++)
        {
            if (candidates.Count == 0)
            {
                Debug.LogError(
                    $"{settings.phaseName}の未使用OwnerSpawnPointが不足しています。",
                    this
                );
                break;
            }

            Transform spawnPoint = candidates[0];
            candidates.RemoveAt(0);

            GameObject owner = Instantiate(
                ownerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                ownerRoot
            );

            activeOwners.Add(owner);
            usedOwnerSpawnPoints.Add(spawnPoint);
        }
    }

    private void RemoveUncarriedItems(
        HashSet<ToyFollower> items)
    {
        items.RemoveWhere(item => item == null);

        List<ToyFollower> snapshot =
            new List<ToyFollower>(items);

        foreach (ToyFollower item in snapshot)
        {
            if (item == null)
            {
                continue;
            }

            if (item.IsCarried || item.IsBeingDelivered)
            {
                continue;
            }

            items.Remove(item);
            Destroy(item.gameObject);
        }
    }

    private void RemoveDestroyedReferences()
    {
        activeToys.RemoveWhere(item => item == null);
        activeTreasures.RemoveWhere(item => item == null);
        activeCameras.RemoveWhere(item => item == null);
    }

    private static int GetRandomCount(int first, int second)
    {
        int minimum = Mathf.Max(0, Mathf.Min(first, second));
        int maximum = Mathf.Max(minimum, Mathf.Max(first, second));

        return Random.Range(minimum, maximum + 1);
    }

    private static List<Transform> CreateShuffledPointList(
        Transform[] source)
    {
        List<Transform> result = new List<Transform>();

        if (source != null)
        {
            foreach (Transform point in source)
            {
                if (point != null)
                {
                    result.Add(point);
                }
            }
        }

        for (int i = result.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Transform temporary = result[i];
            result[i] = result[randomIndex];
            result[randomIndex] = temporary;
        }

        return result;
    }

    private void RefreshUI(PhaseSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        if (timerText != null)
        {
            timerText.text =
                Mathf.CeilToInt(remainingTime).ToString();
        }

        if (timerFill != null)
        {
            timerFill.fillAmount =
                Mathf.InverseLerp(
                    0f,
                    settings.timeLimit,
                    remainingTime
                );
        }

        if (phaseText != null)
        {
            phaseText.text = string.IsNullOrWhiteSpace(
                settings.phaseName
            )
                ? $"フェーズ {currentPhaseIndex + 1}"
                : settings.phaseName;
        }
    }
}