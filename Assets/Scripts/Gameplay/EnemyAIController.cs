using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Game AI NPC Musuh dengan FSM + Perception (Sight/Hearing/Memory) + NavMesh.
/// Perilaku (Sight Range, Attack Cooldown, Flanking, TakeCover, Flee Threshold)
/// langsung dipengaruhi oleh DDAManager.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour, IDamageable
{
    public static readonly List<EnemyAIController> ActiveEnemies = new List<EnemyAIController>();

    public enum EnemyArchetype
    {
        ImpScout,
        ImpCaster,
        PuglinBrute
    }

    public enum AIState
    {
        Patrol,
        Investigate,
        Chase,
        Flank,
        Attack,
        TakeCover,
        Flee,
        Dead
    }

    [Header("Enemy Config")]
    public EnemyArchetype archetype = EnemyArchetype.ImpScout;
    public float maxHp = 65f;
    public float attackDamage = 12f;
    public float attackRange = 2.2f;
    public float patrolRadius = 11f;
    public float moveSpeed = 3.6f;

    [Header("Optional Territory Link")]
    public TerritoryCaptureZone ownerTerritory;
    public RuntimeAnimatorController fallbackController;

    private NavMeshAgent _agent;
    private Animator _animator;
    private TextMesh _debugLabel;
    private AIState _state = AIState.Patrol;
    private ElementType _elementAura = ElementType.None;

    private float _currentHp;
    private Vector3 _homeAnchor;
    private Vector3 _patrolTarget;
    private Vector3 _lastKnownPlayerPos;
    private float _memoryTimer;
    private float _attackCooldownTimer;
    private float _stateTimer;
    private float _engagementStartTime;
    private bool _engagedOnce;
    private float _flankSign = 1f;
    private float _cycleOffset;
    private int _attackSwingCount;
    private float _pendingAttackTimer = -1f;
    private Transform _pendingAttackTargetTf;
    private IDamageable _pendingAttackTargetDmg;
    private float _pendingAttackReach;
    private Transform _recentAttacker;
    private float _recentAttackerTimer;
    private Transform _lockedTarget;

    public AIState CurrentState => _state;
    public bool IsDead => _state == AIState.Dead;
    public bool IsEngagedOrDamaged =>
        _currentHp < maxHp - 0.5f ||
        _state == AIState.Chase ||
        _state == AIState.Attack ||
        _state == AIState.Flank ||
        _state == AIState.TakeCover ||
        _state == AIState.Investigate;
    public Transform Transform => transform;

    public static bool IsAnyEnemyAttackingNear(Vector3 pos, float radius)
    {
        for (int i = 0; i < ActiveEnemies.Count; i++)
        {
            EnemyAIController e = ActiveEnemies[i];
            if (e != null && !e.IsDead && (e._state == AIState.Attack || e._state == AIState.Chase))
            {
                if (Vector3.Distance(e.transform.position, pos) <= radius) return true;
            }
        }
        return false;
    }

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        EnsureAnimatorController(fallbackController != null
            ? fallbackController
            : (PCGWorldEcosystemManager.Instance != null ? PCGWorldEcosystemManager.Instance.monsterAnimatorController : null));
        _currentHp = maxHp;
        _homeAnchor = transform.position;
        _flankSign = Random.value > 0.5f ? 1f : -1f;
        _cycleOffset = Random.value;
        SetAnimFloatSafe(_animator, "CycleOffset", _cycleOffset);
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr != null) smr.updateWhenOffscreen = true;
        }
        CreateDebugLabel();
    }

    public void EnsureAnimatorController(RuntimeAnimatorController controllerFallback)
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_animator == null) return;

        if (_animator.runtimeAnimatorController == null && controllerFallback != null)
        {
            _animator.runtimeAnimatorController = controllerFallback;
        }
        _animator.applyRootMotion = false;
        _animator.Rebind();
        SetAnimFloatSafe(_animator, "CycleOffset", _cycleOffset);
        SetAnimBoolSafe(_animator, "IsDead", _state == AIState.Dead);
        _animator.Update(0f);

        var clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        if (_animator.runtimeAnimatorController == null ||
            _animator.avatar == null ||
            !_animator.avatar.isValid ||
            !_animator.avatar.isHuman ||
            clipInfos.Length == 0)
        {
            Debug.LogError($"[Monster Animator] '{gameObject.name}' bermasalah: ctrl={_animator.runtimeAnimatorController != null}, " +
                           $"avatarValid={_animator.avatar != null && _animator.avatar.isValid}, human={_animator.avatar != null && _animator.avatar.isHuman}, clips={clipInfos.Length}");
        }
    }

    private void Start()
    {
        _homeAnchor = transform.position;
        if (_agent != null)
        {
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = Mathf.Max(1.2f, attackRange * 0.72f);
        }
        PickNewPatrolPoint();
    }

    private void Update()
    {
        if (_state == AIState.Dead) return;

        _attackCooldownTimer -= Time.deltaTime;
        _memoryTimer -= Time.deltaTime;
        _stateTimer -= Time.deltaTime;
        _recentAttackerTimer -= Time.deltaTime;

        if (_pendingAttackTimer >= 0f)
        {
            _pendingAttackTimer -= Time.deltaTime;
            if (_pendingAttackTimer <= 0f)
            {
                _pendingAttackTimer = -1f;
                ExecutePendingAttackHit();
            }
        }

        ThirdPersonPlayerController player = ThirdPersonPlayerController.Instance;
        DDAManager dda = DDAManager.Instance;
        float sightRange = dda != null ? dda.AISightRange : 14f;
        float sightAngle = dda != null ? dda.AISightAngle : 105f;
        float fleeThreshold = dda != null ? dda.AIFleeHpThreshold : 0.25f;
        bool useTactics = dda != null ? dda.AIUseTacticalBehavior : true;

        Transform targetTf = SelectHostileTarget(player, sightRange, sightAngle,
            out IDamageable targetDmg, out float distToTarget, out bool canSee, out float effectiveAttackRange);
        _lockedTarget = targetTf;

        if (targetTf == null)
        {
            if (_state != AIState.Patrol) SwitchState(AIState.Patrol);
            RunPatrol();
            UpdateVisuals();
            return;
        }

        bool canHear = distToTarget <= sightRange * 0.55f;

        if (canSee || canHear)
        {
            _lastKnownPlayerPos = targetTf.position;
            _memoryTimer = dda != null ? dda.AIMemoryDuration : 4.5f;
            if (!_engagedOnce)
            {
                _engagedOnce = true;
                _engagementStartTime = Time.time;
            }
        }

        // --- Decision Making (FSM + Tactical Rules dipengaruhi DDA) ---
        float hpRatio = _currentHp / maxHp;
        if (hpRatio <= fleeThreshold && _state != AIState.Flee)
        {
            SwitchState(AIState.Flee);
        }
        else if (_state == AIState.Flee)
        {
            if (hpRatio > fleeThreshold + 0.15f || distToTarget > sightRange * 1.4f)
            {
                SwitchState(AIState.Patrol);
            }
        }
        else if (canSee)
        {
            float stayAttackDist = _state == AIState.Attack ? effectiveAttackRange + 0.55f : effectiveAttackRange;
            if (archetype == EnemyArchetype.ImpCaster && useTactics && distToTarget < 4.5f)
            {
                SwitchState(AIState.TakeCover);
            }
            else if (distToTarget <= stayAttackDist)
            {
                SwitchState(AIState.Attack);
            }
            else if (useTactics && archetype == EnemyArchetype.ImpScout && distToTarget > effectiveAttackRange * 1.5f && distToTarget < sightRange * 0.85f)
            {
                SwitchState(AIState.Flank);
            }
            else
            {
                SwitchState(AIState.Chase);
            }
        }
        else if (_memoryTimer > 0f)
        {
            SwitchState(AIState.Investigate);
        }
        else
        {
            SwitchState(AIState.Patrol);
        }

        ExecuteCurrentState(targetTf, targetDmg, dda, effectiveAttackRange, distToTarget);
        UpdateVisuals();
    }

    private Transform SelectHostileTarget(
        ThirdPersonPlayerController player,
        float sightRange,
        float sightAngle,
        out IDamageable targetDmg,
        out float bestDist,
        out bool bestCanSee,
        out float bestAttackReach)
    {
        Transform bestTf = null;
        targetDmg = null;
        bestDist = float.MaxValue;
        bestCanSee = false;
        bestAttackReach = attackRange;

        float bestScore = float.MaxValue;

        if (player != null && !player.IsDead)
        {
            EvaluateTargetCandidate(player.transform, player, sightRange, sightAngle,
                ref bestTf, ref targetDmg, ref bestDist, ref bestCanSee, ref bestAttackReach, ref bestScore);
        }

        for (int i = 0; i < VillageNPCController.AllNpcs.Count; i++)
        {
            VillageNPCController ally = VillageNPCController.AllNpcs[i];
            if (ally == null || !ally.IsRecruited || ally.IsDowned) continue;

            EvaluateTargetCandidate(ally.transform, ally, sightRange * 1.15f, sightAngle,
                ref bestTf, ref targetDmg, ref bestDist, ref bestCanSee, ref bestAttackReach, ref bestScore);
        }

        return bestTf;
    }

    private void EvaluateTargetCandidate(
        Transform candTf,
        IDamageable candDmg,
        float sightRange,
        float sightAngle,
        ref Transform bestTf,
        ref IDamageable bestDmg,
        ref float bestDist,
        ref bool bestCanSee,
        ref float bestReach,
        ref float bestScore)
    {
        float horizDist = HorizontalDistance(transform.position, candTf.position);
        float dy = Mathf.Abs(candTf.position.y - transform.position.y);
        float reach = ComputeEffectiveAttackReach(candTf);
        bool see = dy <= 3.6f && CanSeeTarget(candTf, sightRange, sightAngle, horizDist, reach);

        float score = horizDist + (see ? 0f : 9f);
        if (_recentAttacker == candTf && _recentAttackerTimer > 0f) score -= 1.8f;
        else if (_lockedTarget == candTf) score -= 0.75f;

        if (score < bestScore)
        {
            bestScore = score;
            bestTf = candTf;
            bestDmg = candDmg;
            bestDist = horizDist;
            bestCanSee = see;
            bestReach = reach;
        }
    }

    private float ComputeEffectiveAttackReach(Transform targetTf)
    {
        if (archetype == EnemyArchetype.ImpCaster) return attackRange;
        float selfRadius = _agent != null ? _agent.radius : 0.5f;
        float targetRadius = 0.4f;
        CharacterController cc = targetTf.GetComponent<CharacterController>();
        if (cc != null) targetRadius = cc.radius;
        else
        {
            NavMeshAgent targetAgent = targetTf.GetComponent<NavMeshAgent>();
            if (targetAgent != null) targetRadius = targetAgent.radius;
        }
        return Mathf.Max(attackRange, selfRadius + targetRadius + 1.45f);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private bool CanSeeTarget(Transform targetTf, float range, float fovAngle, float horizDist, float effectiveReach)
    {
        if (horizDist > range) return false;

        // Dalam jarak tempur dekat (termasuk saat dikerubungi banyak NPC), jangan terblokir FOV
        bool closeCombat = horizDist <= Mathf.Max(effectiveReach + 1.5f, 4.8f);
        if (!closeCombat)
        {
            Vector3 flatTo = targetTf.position - transform.position;
            flatTo.y = 0f;
            float angle = Vector3.Angle(transform.forward, flatTo);
            if (angle > fovAngle * 0.5f) return false;
        }

        Vector3 eyePos = transform.position + Vector3.up * 1.45f;
        Vector3 targetPos = targetTf.position + Vector3.up * 1.35f;
        Vector3 toTarget = targetPos - eyePos;
        float rayDist = toTarget.magnitude;
        if (rayDist < 0.2f) return true;

        int mask = ~LayerMask.GetMask("Ignore Raycast");
        RaycastHit[] hits = Physics.RaycastAll(eyePos, toTarget.normalized, rayDist, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null) continue;
            // Karakter lain (player/NPC/monster lain) tidak membutakan garis pandang AI
            if (col.GetComponentInParent<EnemyAIController>() != null) continue;
            if (col.GetComponentInParent<VillageNPCController>() != null) continue;
            if (col.GetComponentInParent<ThirdPersonPlayerController>() != null) continue;
            if (col.isTrigger) continue;
            return false;
        }
        return true;
    }

    private void ExecuteCurrentState(Transform targetTf, IDamageable targetDmg, DDAManager dda, float effectiveReach, float distToTarget)
    {
        switch (_state)
        {
            case AIState.Patrol:
                RunPatrol();
                break;

            case AIState.Investigate:
                SetAgentSpeed(moveSpeed * 0.9f);
                SafeSetDestination(_lastKnownPlayerPos);
                break;

            case AIState.Chase:
                if (_agent != null) _agent.stoppingDistance = Mathf.Max(0.85f, effectiveReach * 0.65f);
                SetAgentSpeed(moveSpeed * 1.15f);
                SafeSetDestination(targetTf.position);
                break;

            case AIState.Flank:
                if (_agent != null) _agent.stoppingDistance = Mathf.Max(0.85f, effectiveReach * 0.65f);
                SetAgentSpeed(moveSpeed * 1.25f);
                Vector3 toEnemy = (transform.position - targetTf.position).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, toEnemy) * _flankSign;
                Vector3 flankPos = targetTf.position + (toEnemy * 2.0f + side * 2.8f);
                SafeSetDestination(flankPos);
                break;

            case AIState.TakeCover:
                SetAgentSpeed(moveSpeed * 1.1f);
                Vector3 awayDir = (transform.position - targetTf.position).normalized;
                Vector3 coverPos = transform.position + awayDir * 6.5f;
                SafeSetDestination(coverPos);
                FaceTarget(targetTf.position);
                if (_attackCooldownTimer <= 0f) PerformAttack(targetTf, targetDmg, dda, effectiveReach);
                break;

            case AIState.Attack:
                if (distToTarget > effectiveReach * 0.88f && _agent != null && _agent.isOnNavMesh)
                {
                    SetAgentSpeed(moveSpeed * 0.95f);
                    SafeSetDestination(targetTf.position);
                }
                else if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                }
                FaceTarget(targetTf.position);
                if (_attackCooldownTimer <= 0f) PerformAttack(targetTf, targetDmg, dda, effectiveReach);
                break;

            case AIState.Flee:
                SetAgentSpeed(moveSpeed * 1.25f);
                Vector3 fleeDir = (transform.position - targetTf.position).normalized;
                Vector3 retreatTarget = Vector3.Lerp(transform.position + fleeDir * 9f, _homeAnchor, 0.35f);
                SafeSetDestination(retreatTarget);
                break;
        }
    }

    private void RunPatrol()
    {
        SetAgentSpeed(moveSpeed * 0.65f);
        if (_agent == null || !_agent.isOnNavMesh) return;

        if (!_agent.pathPending && (_agent.remainingDistance <= 1.2f || _stateTimer <= 0f))
        {
            PickNewPatrolPoint();
            _stateTimer = Random.Range(4f, 8f);
        }
        SafeSetDestination(_patrolTarget);
    }

    private void PickNewPatrolPoint()
    {
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        Vector3 candidate = _homeAnchor + new Vector3(rnd.x, 0f, rnd.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            _patrolTarget = hit.position;
        }
        else
        {
            _patrolTarget = _homeAnchor;
        }
    }

    private void PerformAttack(Transform targetTf, IDamageable targetDmg, DDAManager dda, float effectiveReach)
    {
        _attackCooldownTimer = dda != null ? dda.AIAttackCooldown : 1.7f;
        if (_animator != null)
        {
            int atkType = archetype == EnemyArchetype.ImpCaster
                ? 2
                : (archetype == EnemyArchetype.PuglinBrute ? 1 : (_attackSwingCount % 2));
            _attackSwingCount++;
            SetAnimIntSafe(_animator, "AttackType", atkType);
            _animator.SetTrigger("Attack");
        }

        _pendingAttackTargetTf = targetTf;
        _pendingAttackTargetDmg = targetDmg;
        _pendingAttackReach = effectiveReach;
        _pendingAttackTimer = archetype == EnemyArchetype.ImpCaster ? 0.18f : 0.22f;
    }

    private void ExecutePendingAttackHit()
    {
        if (_state == AIState.Dead || _pendingAttackTargetTf == null) return;

        Transform targetTf = _pendingAttackTargetTf;
        IDamageable targetDmg = _pendingAttackTargetDmg;
        float effectiveReach = _pendingAttackReach;

        if (archetype == EnemyArchetype.ImpCaster)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.15f + transform.forward * 0.7f;
            Vector3 dir = (targetTf.position + Vector3.up * 1.0f - spawnPos).normalized;
            GameObject boltGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boltGo.name = "EnemyMagicBolt";
            boltGo.transform.position = spawnPos;
            boltGo.transform.localScale = Vector3.one * 0.34f;
            Destroy(boltGo.GetComponent<Collider>());

            ProjectileBolt bolt = boltGo.AddComponent<ProjectileBolt>();
            bolt.speed = 13f;
            bolt.Launch(dir, attackDamage, ElementType.Electro, isFromPlayer: false, tint: new Color(0.75f, 0.25f, 1f));
        }
        else if (targetDmg != null && !targetDmg.IsDead)
        {
            float dist = HorizontalDistance(transform.position, targetTf.position);
            float dy = Mathf.Abs(targetTf.position.y - transform.position.y);
            if (dist <= effectiveReach + 0.95f && dy <= 3.6f)
            {
                targetDmg.TakeDamage(attackDamage, ElementType.None, transform.position, false);
            }
        }
    }

    public void TakeDamage(float amount, ElementType element, Vector3 hitSourcePos, bool isPlayerAttack = false)
    {
        if (_state == AIState.Dead) return;

        float mult = CombatFXHelper.EvaluateElementalReaction(_elementAura, element, out string reaction, out _elementAura);
        float finalDmg = amount * mult;
        _currentHp -= finalDmg;

        if (_animator != null && _currentHp > 0f) _animator.SetTrigger("Hit");
        Color txtColor = mult > 1f ? new Color(1f, 0.85f, 0.2f) : Color.white;
        string msg = string.IsNullOrEmpty(reaction) ? $"{finalDmg:F0}" : $"{reaction} {finalDmg:F0}";
        CombatFXHelper.SpawnFloatingText(transform.position, msg, txtColor, mult > 1f ? 1.2f : 0.95f);

        // Alert dan prioritaskan penyerang terbaru (player maupun sekutu NPC)
        _lastKnownPlayerPos = hitSourcePos;
        _memoryTimer = 6f;
        UpdateRecentAttacker(hitSourcePos, isPlayerAttack);
        if (!_engagedOnce)
        {
            _engagedOnce = true;
            _engagementStartTime = Time.time;
        }

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void UpdateRecentAttacker(Vector3 hitSourcePos, bool isPlayerAttack)
    {
        if (isPlayerAttack && ThirdPersonPlayerController.Instance != null && !ThirdPersonPlayerController.Instance.IsDead)
        {
            _recentAttacker = ThirdPersonPlayerController.Instance.transform;
            _recentAttackerTimer = 4.5f;
            return;
        }

        VillageNPCController closest = null;
        float best = 4.5f;
        for (int i = 0; i < VillageNPCController.AllNpcs.Count; i++)
        {
            VillageNPCController npc = VillageNPCController.AllNpcs[i];
            if (npc == null || !npc.IsRecruited || npc.IsDowned) continue;
            float d = Vector3.Distance(hitSourcePos, npc.transform.position);
            if (d < best)
            {
                best = d;
                closest = npc;
            }
        }
        if (closest != null)
        {
            _recentAttacker = closest.transform;
            _recentAttackerTimer = 4.5f;
        }
    }

    private void Die()
    {
        _state = AIState.Dead;
        _pendingAttackTimer = -1f;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.enabled = false;
        }
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (_debugLabel != null) _debugLabel.gameObject.SetActive(false);

        if (DDAManager.Instance != null)
        {
            float ttk = _engagedOnce ? (Time.time - _engagementStartTime) : 5f;
            DDAManager.Instance.RecordEnemyKilled(ttk);
        }

        if (ownerTerritory != null) ownerTerritory.NotifyDefenderKilled(this);

        // Putar animasi Death01 asli dari UAL1; fallback miring hanya jika Animator belum ter-upgrade
        if (HasAnimParam(_animator, "IsDead"))
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("IsDead", true);
            Destroy(gameObject, 2.8f);
        }
        else
        {
            transform.Rotate(Vector3.right, 85f);
            Destroy(gameObject, 2.2f);
        }
    }

    private void SwitchState(AIState next)
    {
        if (_state == next) return;
        _state = next;
    }

    private void SafeSetDestination(Vector3 target)
    {
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
            else
            {
                _agent.SetDestination(target);
            }
        }
    }

    private void SetAgentSpeed(float s)
    {
        if (_agent != null) _agent.speed = s;
    }

    private void FaceTarget(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
    }

    private void UpdateVisuals()
    {
        if (_animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null && _agent != null)
        {
            _animator.SetFloat("Speed", _agent.velocity.magnitude, 0.10f, Time.deltaTime);
        }

        if (_debugLabel != null)
        {
            bool show = DDAManager.Instance == null || DDAManager.Instance.showAiDebugLabels;
            _debugLabel.gameObject.SetActive(show);
            if (show)
            {
                int hpPct = Mathf.Clamp(Mathf.RoundToInt((_currentHp / maxHp) * 100f), 0, 100);
                string aura = _elementAura == ElementType.None ? "" : $" [{_elementAura}]";
                _debugLabel.text = $"{archetype}\n{_state} | {hpPct}%{aura}";
                _debugLabel.color = GetStateColor(_state);
                if (Camera.main != null)
                {
                    _debugLabel.transform.rotation = Quaternion.LookRotation(_debugLabel.transform.position - Camera.main.transform.position);
                }
            }
        }
    }

    private Color GetStateColor(AIState st)
    {
        switch (st)
        {
            case AIState.Patrol: return new Color(0.6f, 0.95f, 0.6f);
            case AIState.Investigate: return Color.yellow;
            case AIState.Chase:
            case AIState.Flank: return new Color(1f, 0.55f, 0.15f);
            case AIState.Attack: return Color.red;
            case AIState.TakeCover: return new Color(0.8f, 0.45f, 1f);
            case AIState.Flee: return Color.cyan;
            default: return Color.white;
        }
    }

    private void CreateDebugLabel()
    {
        float invScale = 1f / Mathf.Max(0.25f, transform.localScale.y);
        float worldHeight = _agent != null && _agent.height > 0.5f ? _agent.height : 2.1f;

        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null) continue;
            float top = rends[i].bounds.max.y - transform.position.y;
            if (top > worldHeight) worldHeight = top;
        }

        GameObject lbl = new GameObject("AI_State_Label");
        lbl.transform.SetParent(transform, false);
        lbl.transform.localScale = Vector3.one * invScale;
        lbl.transform.localPosition = new Vector3(0f, (worldHeight + 0.45f) * invScale, 0f);
        _debugLabel = lbl.AddComponent<TextMesh>();
        _debugLabel.fontSize = 28;
        _debugLabel.characterSize = 0.06f;
        _debugLabel.alignment = TextAlignment.Center;
        _debugLabel.anchor = TextAnchor.LowerCenter;
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
}
