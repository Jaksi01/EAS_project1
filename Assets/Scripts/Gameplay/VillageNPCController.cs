using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC penduduk desa (Home Village) yang dapat berpatroli/bekerja di desa
/// dan diajak bergabung oleh player sebagai sekutu untuk bertarung menaklukkan monster bersama.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class VillageNPCController : MonoBehaviour, IDamageable
{
    public static readonly List<VillageNPCController> AllNpcs = new List<VillageNPCController>();

    [Header("NPC Identity & Village Post")]
    public string displayName = "Villager";
    public float patrolRadius = 8f;
    public bool stationaryWorker;
    public int workMode; // 0 = Idle_FoldArms, 1 = Farm_Harvest

    [Header("Companion & Combat")]
    public float maxHp = 135f;
    public float attackDamage = 16f;
    public float attackRange = 2.4f;
    public float attackCooldown = 1.15f;
    public float followRunSpeed = 7.4f;
    public float sprintSpeed = 10.6f;
    public float aggroRange = 19f;
    public float recruitPromptRadius = 3.8f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _weaponGrip;
    private TextMesh _statusLabel;

    private Vector3 _origin;
    private Quaternion _originRot;
    private float _waitTimer;
    private bool _warpedToNavMesh;

    private bool _isRecruited;
    private bool _isDowned;
    private float _downedTimer;
    private float _currentHp;
    private float _attackTimer;
    private float _repathTimer;
    private float _cycleOffset;
    private int _attackVariant;
    private float _pendingHitTimer = -1f;
    private EnemyAIController _pendingHitTarget;
    private EnemyAIController _combatTarget;

    public bool IsRecruited => _isRecruited;
    public bool IsDowned => _isDowned;
    public bool IsDead => !_isRecruited || _isDowned;
    public Transform Transform => transform;
    public float CurrentHp => _currentHp;
    public string CurrentActivityText
    {
        get
        {
            if (!_isRecruited) return "In Village";
            if (_isDowned) return $"Recovering ({_downedTimer:F0}s)";
            if (_combatTarget != null && !_combatTarget.IsDead) return $"Fighting {_combatTarget.archetype}";
            return "Following";
        }
    }

    public static int RecruitedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < AllNpcs.Count; i++)
            {
                if (AllNpcs[i] != null && AllNpcs[i].IsRecruited) count++;
            }
            return count;
        }
    }

    public static VillageNPCController FindNearestPromptTarget(Vector3 playerPos, float maxDist)
    {
        VillageNPCController bestNew = null;
        float bestNewDist = maxDist;
        VillageNPCController bestRecruited = null;
        float bestRecruitedDist = maxDist;

        for (int i = 0; i < AllNpcs.Count; i++)
        {
            VillageNPCController npc = AllNpcs[i];
            if (npc == null) continue;
            float d = Vector3.Distance(playerPos, npc.transform.position);
            if (d > maxDist) continue;

            if (!npc.IsRecruited && d < bestNewDist)
            {
                bestNew = npc;
                bestNewDist = d;
            }
            else if (npc.IsRecruited && d < bestRecruitedDist)
            {
                bestRecruited = npc;
                bestRecruitedDist = d;
            }
        }

        return bestNew != null ? bestNew : bestRecruited;
    }

    public static void RecallAllCompanions(Vector3 playerPos)
    {
        int recalled = 0;
        for (int i = 0; i < AllNpcs.Count; i++)
        {
            VillageNPCController npc = AllNpcs[i];
            if (npc == null || !npc.IsRecruited) continue;
            if (Vector3.Distance(playerPos, npc.transform.position) > 5.5f)
            {
                npc.WarpNearPosition(playerPos + RandomPointOffset(recalled));
                recalled++;
            }
        }

        if (recalled > 0)
        {
            CombatFXHelper.SpawnFloatingText(playerPos + Vector3.up * 1.1f, $"MEMANGGIL {recalled} SEKUTU!", new Color(0.4f, 0.9f, 1f), 1.1f);
        }
    }

    private void OnEnable()
    {
        if (!AllNpcs.Contains(this)) AllNpcs.Add(this);
    }

    private void OnDisable()
    {
        AllNpcs.Remove(this);
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _weaponGrip = FindChildRecursive(transform, "WeaponGrip_RightHand");
        _currentHp = maxHp;
        _cycleOffset = Random.value;
    }

    private void Start()
    {
        _origin = transform.position;
        _originRot = transform.rotation;
        SetWeaponDrawn(false);
        CreateStatusLabel();

        if (_agent != null) _agent.speed = 1.85f;
        SyncAnimatorBaseParams();

        if (stationaryWorker)
        {
            if (_agent != null) _agent.enabled = false;
            SnapToGround();
        }
        else
        {
            TryBindToNavMesh();
        }
    }

    private void Update()
    {
        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
        if (_repathTimer > 0f) _repathTimer -= Time.deltaTime;
        if (_pendingHitTimer >= 0f)
        {
            _pendingHitTimer -= Time.deltaTime;
            if (_pendingHitTimer <= 0f)
            {
                _pendingHitTimer = -1f;
                ExecutePendingNpcHit();
            }
        }

        if (_isRecruited)
        {
            UpdateCompanionMode();
        }
        else
        {
            UpdateVillageMode();
        }

        UpdateStatusLabel();
    }

    public void Recruit()
    {
        if (_isRecruited) return;

        _isRecruited = true;
        _isDowned = false;
        _currentHp = maxHp;
        _combatTarget = null;
        SetWeaponDrawn(true);

        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.stoppingDistance = 1.1f;
            _agent.speed = followRunSpeed;
            _warpedToNavMesh = _agent.isOnNavMesh;
            if (!_warpedToNavMesh) TryBindToNavMesh();
        }

        if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null)
        {
            _animator.SetBool("IsCompanion", true);
            _animator.SetInteger("WorkMode", 0);
            SetAnimBoolSafe(_animator, "IsDowned", false);
            SetAnimTriggerSafe(_animator, "Cheer");
        }

        CombatFXHelper.SpawnFloatingText(transform.position + Vector3.up * 0.9f, $"{displayName} BERGABUNG!", new Color(0.35f, 1f, 0.55f), 1.15f);
    }

    public void Dismiss()
    {
        if (!_isRecruited) return;

        _isRecruited = false;
        _isDowned = false;
        _combatTarget = null;
        _currentHp = maxHp;
        SetWeaponDrawn(false);

        if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null)
        {
            _animator.SetBool("IsCompanion", false);
            _animator.SetInteger("WorkMode", workMode);
            SetAnimBoolSafe(_animator, "IsDowned", false);
        }

        if (_agent != null && _agent.enabled)
        {
            _agent.speed = 1.85f;
            _agent.stoppingDistance = 0.5f;
            if (stationaryWorker)
            {
                _agent.ResetPath();
                _agent.enabled = false;
                transform.position = _origin;
                transform.rotation = _originRot;
            }
            else if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(_origin);
            }
        }

        CombatFXHelper.SpawnFloatingText(transform.position + Vector3.up * 0.8f, $"{displayName} kembali ke desa", Color.white, 0.95f);
    }

    private void UpdateVillageMode()
    {
        bool canAnimate = CanAnimate();
        if (stationaryWorker || _agent == null)
        {
            if (canAnimate) _animator.SetFloat("Speed", 0f);
            return;
        }

        if (!_warpedToNavMesh || !_agent.isOnNavMesh)
        {
            TryBindToNavMesh();
            if (canAnimate) _animator.SetFloat("Speed", 0f);
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= 0.8f)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                PickNextPoint();
                _waitTimer = Random.Range(2.5f, 6.0f);
            }
        }

        if (canAnimate) _animator.SetFloat("Speed", _agent.velocity.magnitude, 0.10f, Time.deltaTime);
    }

    private void UpdateCompanionMode()
    {
        bool canAnimate = CanAnimate();
        ThirdPersonPlayerController player = ThirdPersonPlayerController.Instance;
        if (player == null)
        {
            if (canAnimate) _animator.SetFloat("Speed", 0f, 0.10f, Time.deltaTime);
            return;
        }

        if (_isDowned)
        {
            _downedTimer -= Time.deltaTime;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
            if (canAnimate) _animator.SetFloat("Speed", 0f, 0.10f, Time.deltaTime);

            if (_downedTimer <= 0f)
            {
                _isDowned = false;
                _currentHp = maxHp * 0.65f;
                if (canAnimate) SetAnimBoolSafe(_animator, "IsDowned", false);
                CombatFXHelper.SpawnFloatingText(transform.position + Vector3.up * 0.8f, $"{displayName} BANGKIT!", new Color(0.45f, 1f, 0.65f), 1.0f);
            }
            return;
        }

        if (_agent == null || !_agent.enabled)
        {
            if (_agent != null) _agent.enabled = true;
        }
        if (!_warpedToNavMesh || (_agent != null && !_agent.isOnNavMesh))
        {
            TryBindToNavMesh();
            if (_agent == null || !_agent.isOnNavMesh)
            {
                WarpNearPosition(player.transform.position + Vector3.back * 2.2f);
                if (canAnimate) _animator.SetFloat("Speed", 0f);
                return;
            }
        }

        RefreshCombatTarget(player.transform.position);

        if (_combatTarget != null && !_combatTarget.IsDead)
        {
            FightTarget(_combatTarget);
        }
        else
        {
            _currentHp = Mathf.Min(maxHp, _currentHp + 3.5f * Time.deltaTime);
            FollowPlayerFormation(player.transform);
        }

        if (canAnimate)
        {
            float spd = _agent != null ? _agent.velocity.magnitude : 0f;
            _animator.SetFloat("Speed", spd, 0.10f, Time.deltaTime);
        }
    }

    private void RefreshCombatTarget(Vector3 playerPos)
    {
        if (_combatTarget != null && (_combatTarget.IsDead ||
            HorizontalDistance(transform.position, _combatTarget.transform.position) > aggroRange * 1.45f))
        {
            _combatTarget = null;
        }

        EnemyAIController best = _combatTarget;
        float bestScore = best != null ? HorizontalDistance(transform.position, best.transform.position) * 0.85f : float.MaxValue;

        for (int i = 0; i < EnemyAIController.ActiveEnemies.Count; i++)
        {
            EnemyAIController enemy = EnemyAIController.ActiveEnemies[i];
            if (enemy == null || enemy.IsDead) continue;

            float dNpc = HorizontalDistance(transform.position, enemy.transform.position);
            float dPlayer = HorizontalDistance(playerPos, enemy.transform.position);
            if (dNpc <= aggroRange * 1.15f || dPlayer <= aggroRange)
            {
                float score = Mathf.Min(dNpc, dPlayer + 2.0f);
                if (enemy.CurrentState != EnemyAIController.AIState.Patrol) score -= 1.5f;
                if (score < bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }
        }

        _combatTarget = best;
    }

    private void FightTarget(EnemyAIController target)
    {
        NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
        float targetRadius = targetAgent != null ? targetAgent.radius : 0.6f;
        float selfRadius = _agent != null ? _agent.radius : 0.35f;
        float effectiveRange = Mathf.Max(attackRange + targetRadius * 0.9f, selfRadius + targetRadius + 1.65f);
        float horizDist = HorizontalDistance(transform.position, target.transform.position);
        float dy = Mathf.Abs(target.transform.position.y - transform.position.y);

        if (horizDist > effectiveRange || dy > 3.6f)
        {
            _agent.speed = horizDist > 8.5f ? sprintSpeed : followRunSpeed;
            _agent.stoppingDistance = Mathf.Max(0.75f, targetRadius + 0.45f);
            if (_repathTimer <= 0f)
            {
                Vector3 dest = GetSurroundAttackPosition(target.transform.position, targetRadius);
                _agent.SetDestination(dest);
                _repathTimer = 0.16f;
            }
        }
        else
        {
            if (_agent.hasPath) _agent.ResetPath();
            FaceTarget(target.transform.position);

            if (_attackTimer <= 0f)
            {
                _attackTimer = attackCooldown;
                if (CanAnimate())
                {
                    SetAnimIntSafe(_animator, "AttackVariant", _attackVariant);
                    _animator.SetTrigger("Attack");
                }
                _attackVariant = (_attackVariant + 1) % 3;
                _pendingHitTarget = target;
                _pendingHitTimer = 0.17f;
            }
        }
    }

    private void ExecutePendingNpcHit()
    {
        if (!_isRecruited || _isDowned || _pendingHitTarget == null || _pendingHitTarget.IsDead) return;
        float dist = HorizontalDistance(transform.position, _pendingHitTarget.transform.position);
        if (dist <= attackRange + 2.2f)
        {
            _pendingHitTarget.TakeDamage(attackDamage, ElementType.None, transform.position, isPlayerAttack: false);
        }
    }

    private Vector3 GetSurroundAttackPosition(Vector3 targetPos, float targetRadius)
    {
        int slot = GetCompanionSlotIndex();
        // Sebar sudut serang keliling target agar semua NPC tidak bertumpuk di titik pusat yang sama
        float angleRad = (slot * 68f) * Mathf.Deg2Rad;
        float ringRadius = targetRadius + 1.05f;
        Vector3 cand = targetPos + new Vector3(Mathf.Cos(angleRad) * ringRadius, 0f, Mathf.Sin(angleRad) * ringRadius);

        if (NavMesh.SamplePosition(cand, out NavMeshHit slotHit, 3.0f, NavMesh.AllAreas))
        {
            return slotHit.position;
        }
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit centerHit, 5.0f, NavMesh.AllAreas))
        {
            return centerHit.position;
        }
        return targetPos;
    }

    private int GetCompanionSlotIndex()
    {
        int slot = 0;
        for (int i = 0; i < AllNpcs.Count; i++)
        {
            VillageNPCController n = AllNpcs[i];
            if (n == this) break;
            if (n != null && n.IsRecruited) slot++;
        }
        return slot;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void FollowPlayerFormation(Transform playerTf)
    {
        float distToPlayer = Vector3.Distance(transform.position, playerTf.position);
        if (distToPlayer > 28f)
        {
            WarpNearPosition(playerTf.position + GetFormationOffset(playerTf));
            return;
        }

        Vector3 desiredSlot = playerTf.position + GetFormationOffset(playerTf);
        float distToSlot = Vector3.Distance(transform.position, desiredSlot);

        if (distToSlot <= 1.35f && distToPlayer <= 4.5f)
        {
            if (_agent.hasPath) _agent.ResetPath();
            FaceTarget(transform.position + playerTf.forward);
            return;
        }

        _agent.stoppingDistance = 0.9f;
        if (distToPlayer > 10f) _agent.speed = sprintSpeed;
        else if (distToPlayer > 4.2f) _agent.speed = followRunSpeed;
        else _agent.speed = 4.4f;

        if (_repathTimer <= 0f)
        {
            if (NavMesh.SamplePosition(desiredSlot, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
            else
            {
                _agent.SetDestination(playerTf.position);
            }
            _repathTimer = 0.18f;
        }
    }

    private Vector3 GetFormationOffset(Transform playerTf)
    {
        int slot = GetCompanionSlotIndex();
        float sideSign = (slot % 2 == 0) ? -1f : 1f;
        int ring = slot / 2;
        float lateral = sideSign * (1.8f + ring * 1.1f);
        float back = -(2.2f + ring * 1.2f);
        return playerTf.right * lateral + playerTf.forward * back;
    }

    public void TakeDamage(float amount, ElementType element, Vector3 hitSourcePos, bool isPlayerAttack = false)
    {
        if (!_isRecruited || _isDowned || isPlayerAttack) return;

        _currentHp = Mathf.Max(0f, _currentHp - amount);
        if (CanAnimate() && _currentHp > 0f) _animator.SetTrigger("Hit");
        CombatFXHelper.SpawnFloatingText(transform.position, $"-{amount:F0}", new Color(1f, 0.45f, 0.35f), 0.85f);

        if (_currentHp <= 0f)
        {
            _isDowned = true;
            _pendingHitTimer = -1f;
            _downedTimer = 10f;
            _combatTarget = null;
            if (CanAnimate()) SetAnimBoolSafe(_animator, "IsDowned", true);
            CombatFXHelper.SpawnFloatingText(transform.position + Vector3.up * 0.7f, $"{displayName} TERJATUH!", Color.yellow, 1.0f);
            return;
        }

        if (_combatTarget == null || _combatTarget.IsDead)
        {
            EnemyAIController nearest = null;
            float best = 6f;
            for (int i = 0; i < EnemyAIController.ActiveEnemies.Count; i++)
            {
                EnemyAIController e = EnemyAIController.ActiveEnemies[i];
                if (e == null || e.IsDead) continue;
                float d = Vector3.Distance(hitSourcePos, e.transform.position);
                if (d < best)
                {
                    best = d;
                    nearest = e;
                }
            }
            if (nearest != null) _combatTarget = nearest;
        }
    }

    public void WarpNearPosition(Vector3 targetPos)
    {
        if (_agent == null) return;
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 7f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            _warpedToNavMesh = true;
        }
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
    }

    private bool CanAnimate()
    {
        return _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;
    }

    private void SyncAnimatorBaseParams()
    {
        if (CanAnimate())
        {
            _animator.Rebind();
            _animator.SetInteger("WorkMode", workMode);
            _animator.SetBool("IsCompanion", _isRecruited);
            SetAnimBoolSafe(_animator, "IsDowned", _isDowned);
            SetAnimFloatSafe(_animator, "CycleOffset", _cycleOffset);
        }
    }

    private static bool HasAnimParam(Animator anim, string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == paramName) return true;
        }
        return false;
    }

    private static void SetAnimFloatSafe(Animator anim, string paramName, float value)
    {
        if (HasAnimParam(anim, paramName)) anim.SetFloat(paramName, value);
    }

    private static void SetAnimIntSafe(Animator anim, string paramName, int value)
    {
        if (HasAnimParam(anim, paramName)) anim.SetInteger(paramName, value);
    }

    private static void SetAnimBoolSafe(Animator anim, string paramName, bool value)
    {
        if (HasAnimParam(anim, paramName)) anim.SetBool(paramName, value);
    }

    private static void SetAnimTriggerSafe(Animator anim, string paramName)
    {
        if (HasAnimParam(anim, paramName)) anim.SetTrigger(paramName);
    }

    private void SetWeaponDrawn(bool drawn)
    {
        if (_weaponGrip != null)
        {
            _weaponGrip.gameObject.SetActive(drawn);
        }
    }

    private void CreateStatusLabel()
    {
        GameObject lbl = new GameObject("NPC_Status_Label");
        lbl.transform.SetParent(transform, false);
        lbl.transform.localPosition = new Vector3(0f, 2.25f, 0f);
        _statusLabel = lbl.AddComponent<TextMesh>();
        _statusLabel.fontSize = 26;
        _statusLabel.characterSize = 0.055f;
        _statusLabel.alignment = TextAlignment.Center;
        _statusLabel.anchor = TextAnchor.LowerCenter;
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel == null) return;

        ThirdPersonPlayerController player = ThirdPersonPlayerController.Instance;
        float distToPlayer = player != null ? Vector3.Distance(transform.position, player.transform.position) : 99f;

        bool visible = _isRecruited || distToPlayer <= 6.0f;
        _statusLabel.gameObject.SetActive(visible);
        if (!visible) return;

        if (_isRecruited)
        {
            int hpPct = Mathf.Clamp(Mathf.RoundToInt((_currentHp / Mathf.Max(1f, maxHp)) * 100f), 0, 100);
            _statusLabel.text = _isDowned
                ? $"[ALLY] {displayName}\nRECOVER {_downedTimer:F0}s"
                : $"[ALLY] {displayName} ({hpPct}%)";
            _statusLabel.color = _isDowned ? Color.yellow : new Color(0.4f, 1f, 0.65f);
        }
        else
        {
            _statusLabel.text = distToPlayer <= recruitPromptRadius
                ? $"{displayName}\n[E] Ajak Bertarung"
                : displayName;
            _statusLabel.color = distToPlayer <= recruitPromptRadius ? new Color(0.95f, 0.9f, 0.45f) : Color.white;
        }

        if (Camera.main != null)
        {
            _statusLabel.transform.rotation = Quaternion.LookRotation(_statusLabel.transform.position - Camera.main.transform.position);
        }
    }

    private void TryBindToNavMesh()
    {
        if (_agent == null) return;
        if (_agent.isOnNavMesh)
        {
            _warpedToNavMesh = true;
            if (!_isRecruited) PickNextPoint();
            return;
        }

        Vector3 sampleOrigin = _isRecruited ? transform.position : _origin;
        if (NavMesh.SamplePosition(sampleOrigin, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            _warpedToNavMesh = true;
            if (!_isRecruited) PickNextPoint();
        }
    }

    private void PickNextPoint()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        Vector3 cand = _origin + new Vector3(rnd.x, 0f, rnd.y);
        if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    private void SnapToGround()
    {
        Vector3 pos = transform.position;
        float bestY = pos.y;
        bool foundTerrain = false;

        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain t = terrains[i];
            if (t == null || t.terrainData == null) continue;
            Vector3 tp = t.transform.position;
            Vector3 sz = t.terrainData.size;
            if (pos.x >= tp.x && pos.x <= tp.x + sz.x &&
                pos.z >= tp.z && pos.z <= tp.z + sz.z)
            {
                bestY = t.SampleHeight(pos) + tp.y;
                foundTerrain = true;
                break;
            }
        }

        if (foundTerrain) pos.y = bestY;
        if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 4.5f, NavMesh.AllAreas))
        {
            pos = navHit.position;
        }
        transform.position = pos;
    }

    private static Vector3 RandomPointOffset(int index)
    {
        float ang = index * 1.4f;
        return new Vector3(Mathf.Cos(ang) * 2.2f, 0f, Mathf.Sin(ang) * 2.2f);
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null) return null;
        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }
}
