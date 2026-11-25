using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Procedural Content Generation (PCG) untuk distribusi monster di hutan/pepohonan
/// dan resource (Jamur Heal & Ore Energy) dengan constraint & validasi NavMesh.
/// Terhubung langsung ke DDAManager.
/// </summary>
public class PCGWorldEcosystemManager : MonoBehaviour
{
    public static PCGWorldEcosystemManager Instance { get; private set; }

    [Header("Monster Prefabs")]
    public GameObject impScoutPrefab;
    public GameObject impCasterPrefab;
    public GameObject puglinBrutePrefab;
    public GameObject puglinElitePrefab;
    public RuntimeAnimatorController monsterAnimatorController;

    [Header("Resource Pickups")]
    public GameObject healMushroomPrefab;
    public GameObject energyOrePrefab;

    [Header("PCG Constraints")]
    public float minSafeDistFromHome = 26f;
    public float minSafeDistFromPlayer = 18f;
    public float minDistBetweenPacks = 22f;
    public float minDistFromEnemyPlaza = 16f;
    public float maxTerrainSlopeDeg = 38f;
    public float waterLevelY = 2.5f;

    private readonly List<Vector3> _forestTreeCandidates = new List<Vector3>();
    private readonly List<GameObject> _spawnedForestMonsters = new List<GameObject>();
    private readonly List<GameObject> _spawnedResources = new List<GameObject>();

    private Vector3 _homeCenter = new Vector3(10.5f, 4f, 69.2f);
    private readonly List<Vector3> _territoryCenters = new List<Vector3>();
    private NavMeshDataInstance _runtimeNavMesh;
    private float _respawnTimer;

    public int ActiveForestMonsterCount
    {
        get
        {
            int c = 0;
            for (int i = _spawnedForestMonsters.Count - 1; i >= 0; i--)
            {
                GameObject go = _spawnedForestMonsters[i];
                if (go == null) { _spawnedForestMonsters.RemoveAt(i); continue; }
                EnemyAIController ai = go.GetComponent<EnemyAIController>();
                if (ai != null && !ai.IsDead) c++;
            }
            return c;
        }
    }

    public int ActiveResourceCount
    {
        get
        {
            int c = 0;
            for (int i = _spawnedResources.Count - 1; i >= 0; i--)
            {
                if (_spawnedResources[i] == null) _spawnedResources.RemoveAt(i);
                else c++;
            }
            return c;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        DiscoverAnchors();
        EnsureRuntimeNavMesh();
        CollectForestTreeCandidates();
        GenerateEcosystem(clearExisting: true);
        _respawnTimer = DDAManager.Instance != null ? DDAManager.Instance.RespawnInterval : 26f;
    }

    private void Update()
    {
        _respawnTimer -= Time.deltaTime;
        if (_respawnTimer <= 0f)
        {
            DDAManager dda = DDAManager.Instance;
            _respawnTimer = dda != null ? dda.RespawnInterval : 26f;
            TopUpForestMonsters();
        }
    }

    private void DiscoverAnchors()
    {
        GameObject plane = GameObject.Find("Plane");
        if (plane != null) waterLevelY = plane.transform.position.y;

        VillageBuildRecord record = FindAnyObjectByType<VillageBuildRecord>();
        if (record != null) _homeCenter = record.villageCenter;

        _territoryCenters.Clear();
        TerritoryCaptureZone[] zones = FindObjectsByType<TerritoryCaptureZone>();
        foreach (TerritoryCaptureZone z in zones)
        {
            _territoryCenters.Add(z.transform.position);
        }
    }

    /// <summary>
    /// Membangun NavMesh runtime mencakup seluruh 9 terrain & bangunan agar
    /// NavMeshAgent monster dan NPC dapat bernavigasi di seluruh world 300x300m.
    /// </summary>
    public void EnsureRuntimeNavMesh()
    {
        // Cek apakah scene sudah punya NavMesh valid di sekitar home
        if (NavMesh.SamplePosition(_homeCenter, out _, 4f, NavMesh.AllAreas)) return;

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
        Bounds worldBounds = new Bounds(_homeCenter, new Vector3(360f, 140f, 360f));
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            Vector3 sz = t.terrainData.size;
            Bounds tb = new Bounds(t.transform.position + sz * 0.5f, sz + Vector3.up * 80f);
            worldBounds.Encapsulate(tb);
        }
        for (int i = 0; i < _territoryCenters.Count; i++)
        {
            worldBounds.Encapsulate(new Bounds(_territoryCenters[i], new Vector3(80f, 60f, 80f)));
        }

        NavMeshBuilder.CollectSources(
            worldBounds,
            ~0,
            NavMeshCollectGeometry.PhysicsColliders,
            0,
            markups,
            sources);

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = 0.45f;
        settings.agentHeight = 1.8f;
        settings.agentSlope = 44f;
        settings.agentClimb = 0.55f;

        NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, worldBounds, Vector3.zero, Quaternion.identity);
        if (data != null)
        {
            _runtimeNavMesh = NavMesh.AddNavMeshData(data);
            Debug.Log($"[PCG NavMesh] Runtime NavMesh berhasil dibangun ({sources.Count} sources).");
        }
    }

    private void OnDestroy()
    {
        if (_runtimeNavMesh.valid) NavMesh.RemoveNavMeshData(_runtimeNavMesh);
    }

    private void CollectForestTreeCandidates()
    {
        _forestTreeCandidates.Clear();
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            TerrainData data = t.terrainData;
            Vector3 tPos = t.transform.position;
            Vector3 tSize = data.size;

            foreach (TreeInstance tree in data.treeInstances)
            {
                Vector3 wp = new Vector3(
                    tPos.x + tree.position.x * tSize.x,
                    0f,
                    tPos.z + tree.position.z * tSize.z);
                wp.y = t.SampleHeight(wp) + tPos.y;

                if (wp.y < waterLevelY + 0.7f) continue;

                // Evaluasi slope permukaan terrain
                Vector3 normal = data.GetInterpolatedNormal(tree.position.x, tree.position.z);
                if (Vector3.Angle(Vector3.up, normal) > maxTerrainSlopeDeg) continue;

                // Constraint: harus di luar zona aman Home Village & pusat plaza desa target
                if (Vector2.Distance(new Vector2(wp.x, wp.z), new Vector2(_homeCenter.x, _homeCenter.z)) < minSafeDistFromHome)
                    continue;

                bool insidePlaza = false;
                foreach (Vector3 tc in _territoryCenters)
                {
                    if (Vector2.Distance(new Vector2(wp.x, wp.z), new Vector2(tc.x, tc.z)) < minDistFromEnemyPlaza)
                    {
                        insidePlaza = true;
                        break;
                    }
                }
                if (insidePlaza) continue;

                _forestTreeCandidates.Add(wp);
            }
        }
    }

    public void OnDifficultyUpdated()
    {
        AdaptEcosystemToDifficulty();
    }

    /// <summary>
    /// Menyesuaikan populasi monster & resource saat DDA berubah secara inkremental
    /// tanpa menghapus monster yang sedang bertarung atau berada dekat pemain.
    /// </summary>
    public void AdaptEcosystemToDifficulty()
    {
        DDAManager dda = DDAManager.Instance;
        int packTarget = dda != null ? dda.ForestPackCount : 7;
        int maxPerPack = dda != null ? dda.MaxMonstersPerPack : 3;
        int desiredTotal = packTarget * Mathf.Max(2, maxPerPack - 1);

        Vector3 playerPos = ThirdPersonPlayerController.Instance != null
            ? ThirdPersonPlayerController.Instance.transform.position
            : _homeCenter;

        // 1. Bersihkan entri null/mati dan pangkas kelebihan hanya pada monster patrol jauh yang belum tersentuh
        int livingCount = 0;
        for (int i = _spawnedForestMonsters.Count - 1; i >= 0; i--)
        {
            GameObject go = _spawnedForestMonsters[i];
            if (go == null)
            {
                _spawnedForestMonsters.RemoveAt(i);
                continue;
            }
            EnemyAIController ai = go.GetComponent<EnemyAIController>();
            if (ai == null || ai.IsDead) continue;
            livingCount++;
        }

        if (livingCount > desiredTotal)
        {
            for (int i = _spawnedForestMonsters.Count - 1; i >= 0 && livingCount > desiredTotal; i--)
            {
                GameObject go = _spawnedForestMonsters[i];
                if (go == null) continue;
                EnemyAIController ai = go.GetComponent<EnemyAIController>();
                if (ai == null || ai.IsDead || ai.IsEngagedOrDamaged) continue;

                if (Vector3.Distance(go.transform.position, playerPos) > 36f)
                {
                    _spawnedForestMonsters.RemoveAt(i);
                    Destroy(go);
                    livingCount--;
                }
            }
        }
        else if (livingCount < desiredTotal)
        {
            int deficit = desiredTotal - livingCount;
            int packsNeeded = Mathf.Clamp(Mathf.CeilToInt(deficit / (float)Mathf.Max(1, maxPerPack)), 1, 5);
            List<Vector3> pts = SampleValidForestPoints(packsNeeded, minDistBetweenPacks);
            foreach (Vector3 packCenter in pts)
            {
                int packSize = Mathf.Min(maxPerPack, desiredTotal - livingCount);
                for (int m = 0; m < packSize; m++)
                {
                    Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.8f, 4.5f);
                    Vector3 cand = packCenter + new Vector3(offset.x, 0f, offset.y);
                    if (ValidateSpawnPoint(cand, out Vector3 validPos))
                    {
                        EnemyAIController spawned = SpawnAdaptedEnemyAt(validPos, forceElite: false);
                        if (spawned != null)
                        {
                            _spawnedForestMonsters.Add(spawned.gameObject);
                            livingCount++;
                        }
                    }
                }
                if (livingCount >= desiredTotal) break;
            }
        }

        // 2. Sesuaikan resource pickup tanpa mengganggu item tepat di dekat pemain
        int desiredResources = (dda != null ? dda.HealMushroomCount + dda.EnergyOreCount : 18);
        int activeRes = ActiveResourceCount;
        if (activeRes > desiredResources)
        {
            for (int i = _spawnedResources.Count - 1; i >= 0 && activeRes > desiredResources; i--)
            {
                GameObject r = _spawnedResources[i];
                if (r != null && Vector3.Distance(r.transform.position, playerPos) > 18f)
                {
                    _spawnedResources.RemoveAt(i);
                    Destroy(r);
                    activeRes--;
                }
            }
        }
        else if (activeRes < desiredResources)
        {
            int addEach = Mathf.Max(1, (desiredResources - activeRes) / 2);
            SpawnResourceNodes(healMushroomPrefab, PickupItem.PickupType.HealMushroom, 35f, addEach);
            SpawnResourceNodes(energyOrePrefab, PickupItem.PickupType.EnergyOre, 35f, addEach);
        }
    }

    public void GenerateEcosystem(bool clearExisting)
    {
        if (clearExisting) ClearSpawnedEcosystem();

        DDAManager dda = DDAManager.Instance;
        int packTarget = dda != null ? dda.ForestPackCount : 7;
        int maxPerPack = dda != null ? dda.MaxMonstersPerPack : 3;
        int healCount = dda != null ? dda.HealMushroomCount : 10;
        int oreCount = dda != null ? dda.EnergyOreCount : 8;

        List<Vector3> chosenPackCenters = SampleValidForestPoints(packTarget, minDistBetweenPacks);
        foreach (Vector3 packCenter in chosenPackCenters)
        {
            int packSize = Random.Range(Mathf.Max(1, maxPerPack - 1), maxPerPack + 1);
            for (int m = 0; m < packSize; m++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.8f, 4.5f);
                Vector3 cand = packCenter + new Vector3(offset.x, 0f, offset.y);
                if (ValidateSpawnPoint(cand, out Vector3 validPos))
                {
                    EnemyAIController spawned = SpawnAdaptedEnemyAt(validPos, forceElite: false);
                    if (spawned != null) _spawnedForestMonsters.Add(spawned.gameObject);
                }
            }
        }

        // Spawn Resource Pickups (Heal Mushrooms & Energy Ores) sesuai parameter DDA
        SpawnResourceNodes(healMushroomPrefab, PickupItem.PickupType.HealMushroom, 35f, healCount);
        SpawnResourceNodes(energyOrePrefab, PickupItem.PickupType.EnergyOre, 35f, oreCount);
    }

    private void TopUpForestMonsters()
    {
        DDAManager dda = DDAManager.Instance;
        int desiredTotal = dda != null ? dda.ForestPackCount * Mathf.Max(2, dda.MaxMonstersPerPack - 1) : 14;
        if (ActiveForestMonsterCount >= desiredTotal) return;

        List<Vector3> pts = SampleValidForestPoints(2, minDistBetweenPacks);
        foreach (Vector3 pt in pts)
        {
            if (ValidateSpawnPoint(pt, out Vector3 valid))
            {
                EnemyAIController e = SpawnAdaptedEnemyAt(valid, false);
                if (e != null) _spawnedForestMonsters.Add(e.gameObject);
            }
        }
    }

    private List<Vector3> SampleValidForestPoints(int desiredCount, float minSpacing)
    {
        List<Vector3> selected = new List<Vector3>();
        if (_forestTreeCandidates.Count == 0) return selected;

        Vector3 playerPos = ThirdPersonPlayerController.Instance != null
            ? ThirdPersonPlayerController.Instance.transform.position
            : _homeCenter;

        int attempts = 0;
        int maxAttempts = Mathf.Max(120, desiredCount * 35);
        while (selected.Count < desiredCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 treePos = _forestTreeCandidates[Random.Range(0, _forestTreeCandidates.Count)];
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector3 candidate = treePos + new Vector3(dir.x, 0f, dir.y) * Random.Range(2.4f, 5.2f);

            if (Vector3.Distance(candidate, playerPos) < minSafeDistFromPlayer) continue;

            bool tooClose = false;
            for (int i = 0; i < selected.Count; i++)
            {
                if (Vector2.Distance(new Vector2(candidate.x, candidate.z), new Vector2(selected[i].x, selected[i].z)) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            if (ValidateSpawnPoint(candidate, out Vector3 navPos))
            {
                selected.Add(navPos);
            }
        }
        return selected;
    }

    private bool ValidateSpawnPoint(Vector3 candidate, out Vector3 validNavPos)
    {
        validNavPos = candidate;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            return false;
        }
        if (hit.position.y < waterLevelY + 0.55f) return false;

        // Pastikan titik terhubung oleh NavMesh (bisa berjalan menuju area sekitar)
        NavMeshPath path = new NavMeshPath();
        Vector3 checkTarget = _territoryCenters.Count > 0 ? _territoryCenters[0] : _homeCenter;
        if (NavMesh.SamplePosition(checkTarget, out NavMeshHit targetHit, 8f, NavMesh.AllAreas))
        {
            if (NavMesh.CalculatePath(hit.position, targetHit.position, NavMesh.AllAreas, path))
            {
                if (path.status == NavMeshPathStatus.PathInvalid) return false;
            }
        }

        validNavPos = hit.position;
        return true;
    }

    public EnemyAIController SpawnAdaptedEnemyAt(Vector3 position, bool forceElite)
    {
        DDAManager dda = DDAManager.Instance;
        float bruteRatio = dda != null ? dda.BruteRatio : 0.25f;
        float rangedRatio = dda != null ? dda.RangedRatio : 0.25f;

        GameObject chosenPrefab = impScoutPrefab;
        if (forceElite && puglinElitePrefab != null)
        {
            chosenPrefab = puglinElitePrefab;
        }
        else
        {
            float roll = Random.value;
            if (roll < bruteRatio && puglinBrutePrefab != null) chosenPrefab = puglinBrutePrefab;
            else if (roll < bruteRatio + rangedRatio && impCasterPrefab != null) chosenPrefab = impCasterPrefab;
        }

        if (chosenPrefab == null) return null;

        GameObject inst = Instantiate(chosenPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        EnemyAIController ai = inst.GetComponent<EnemyAIController>();
        if (ai != null && monsterAnimatorController != null)
        {
            ai.EnsureAnimatorController(monsterAnimatorController);
        }
        return ai;
    }

    private void SpawnResourceNodes(GameObject prefab, PickupItem.PickupType type, float amount, int count)
    {
        if (prefab == null || count <= 0) return;

        List<Vector3> pts = SampleValidForestPoints(count, 10f);
        foreach (Vector3 pt in pts)
        {
            GameObject res = Instantiate(prefab, pt + Vector3.up * 0.05f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            res.transform.localScale = type == PickupItem.PickupType.HealMushroom ? Vector3.one * 1.35f : Vector3.one * 0.75f;
            foreach (Collider col in res.GetComponentsInChildren<Collider>()) col.enabled = false;

            PickupItem p = res.AddComponent<PickupItem>();
            p.pickupType = type;
            p.amount = amount;
            _spawnedResources.Add(res);
        }
    }

    private void ClearSpawnedEcosystem()
    {
        for (int i = 0; i < _spawnedForestMonsters.Count; i++)
        {
            if (_spawnedForestMonsters[i] != null) Destroy(_spawnedForestMonsters[i]);
        }
        _spawnedForestMonsters.Clear();

        for (int i = 0; i < _spawnedResources.Count; i++)
        {
            if (_spawnedResources[i] != null) Destroy(_spawnedResources[i]);
        }
        _spawnedResources.Clear();
    }
}
