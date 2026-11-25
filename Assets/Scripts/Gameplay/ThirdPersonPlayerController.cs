using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kontroler Third-Person Action RPG (ala Genshin Impact):
/// Gerak relatif kamera, Sprint, Dodge/Dash (i-frame), Normal Attack,
/// Elemental Skill (E), Elemental Burst (Q), dan Party Swap 2 Karakter (1/2).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonPlayerController : MonoBehaviour, IDamageable
{
    public static ThirdPersonPlayerController Instance { get; private set; }

    [Header("Character Party Visuals")]
    public GameObject pyroVisual;   // Karakter 1: Male Ranger (Pyro)
    public GameObject hydroVisual;  // Karakter 2: Female Ranger (Hydro)
    public Animator activeAnimator;

    [Header("Movement")]
    public float walkSpeed = 6.2f;
    public float sprintSpeed = 11.2f;
    public float jumpHeight = 2.1f;
    public float gravity = -16f;
    public float turnSmoothSpeed = 14f;

    [Header("Stats")]
    public float maxHp = 150f;
    public float maxStamina = 100f;
    public float maxEnergy = 100f;

    [Header("Combat & Cooldowns")]
    public float meleeDamage = 24f;
    public float meleeRange = 2.9f;
    public float attackCooldown = 0.48f;
    public float skillCooldown = 4.0f;
    public float skillDamage = 34f;
    public float burstDamage = 75f;
    public float dashCooldown = 1.1f;
    public float dashSpeed = 17f;
    public float dashDuration = 0.24f;

    private CharacterController _controller;
    private ThirdPersonCamera _cam;
    private Vector3 _velocity;
    private bool _isGrounded;

    private float _currentHp;
    private float _currentStamina;
    private float _currentEnergy;
    private int _activeCharIndex; // 0 = Pyro (Male Ranger), 1 = Hydro (Female Ranger)

    private float _attackTimer;
    public float comboWindow = 2.6f;
    private int _comboStep;
    private float _lastAttackTime = -10f;
    private float _skillTimer;
    private float _burstTimer;
    private float _dashTimer;
    private float _dashActiveRemaining;
    private Vector3 _dashDir;
    private float _lungeRemaining;
    private float _lungeSpeed;
    private Vector3 _lungeDir;
    private float _pendingHitTimer = -1f;
    private float _pendingHitDamage;
    private float _pendingHitRange;
    private float _iFrameTimer;
    private float _respawnTimer;
    private Vector3 _homeSpawnPos;

    public float CurrentHp => _currentHp;
    public float CurrentStamina => _currentStamina;
    public float CurrentEnergy => _currentEnergy;
    public float SkillCooldownRemaining => Mathf.Max(0f, _skillTimer);
    public int ActiveCharacterIndex => _activeCharIndex;
    public ElementType ActiveElement => _activeCharIndex == 0 ? ElementType.Pyro : ElementType.Hydro;
    public string ActiveCharacterName => _activeCharIndex == 0 ? "Pyro Ranger (1)" : "Hydro Ranger (2)";
    public bool IsDead => _currentHp <= 0f;
    public Transform Transform => transform;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        _controller = GetComponent<CharacterController>();
        if (_controller != null)
        {
            _controller.enabled = true;
            if (_controller.stepOffset < 0.45f) _controller.stepOffset = 0.45f;
        }

        _velocity = Vector3.zero;
        _dashActiveRemaining = 0f;
        _lungeRemaining = 0f;
        _pendingHitTimer = -1f;
        _burstTimer = 0f;
        _respawnTimer = 0f;
        _currentHp = maxHp;
        _currentStamina = maxStamina;
        _currentEnergy = 50f;
        _homeSpawnPos = transform.position;
    }

    private void Start()
    {
        _cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
        AutoBindCharacterVisuals();
        SwitchCharacter(0, silent: true);
    }

    private void AutoBindCharacterVisuals()
    {
        if (pyroVisual == null)
        {
            Transform t = transform.Find("Visual_Pyro_Ranger");
            if (t != null) pyroVisual = t.gameObject;
        }
        if (hydroVisual == null)
        {
            Transform t = transform.Find("Visual_Hydro_Ranger");
            if (t != null) hydroVisual = t.gameObject;
        }
        UpdateActiveAnimator();
    }

    public void SetHomeSpawn(Vector3 pos)
    {
        _homeSpawnPos = pos;
    }

    private void Update()
    {
        if (IsDead)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) RespawnAtHome();
            return;
        }

        _attackTimer -= Time.deltaTime;
        _skillTimer -= Time.deltaTime;
        _burstTimer -= Time.deltaTime;
        _dashTimer -= Time.deltaTime;
        _iFrameTimer -= Time.deltaTime;

        if (_pendingHitTimer >= 0f)
        {
            _pendingHitTimer -= Time.deltaTime;
            if (_pendingHitTimer <= 0f)
            {
                _pendingHitTimer = -1f;
                PerformMeleeAttack(_pendingHitDamage, _pendingHitRange);
            }
        }

        CheckGrounded();
        ReadInputs(out float x, out float z, out bool sprint, out bool jumpDown,
            out bool attackDown, out bool dashDown, out bool skillDown, out bool burstDown,
            out int swapChoice);

        if (swapChoice >= 0 && swapChoice != _activeCharIndex)
        {
            SwitchCharacter(swapChoice, silent: false);
        }

        // Dash / Dodge roll
        if (dashDown && _dashTimer <= 0f && _currentStamina >= 20f)
        {
            StartDash(x, z);
        }

        if (_dashActiveRemaining > 0f)
        {
            _dashActiveRemaining -= Time.deltaTime;
            if (_controller != null && _controller.enabled)
                _controller.Move(_dashDir * (dashSpeed * Time.deltaTime));
        }
        else
        {
            if (_lungeRemaining > 0f)
            {
                _lungeRemaining -= Time.deltaTime;
                if (_controller != null && _controller.enabled)
                    _controller.Move(_lungeDir * (_lungeSpeed * Time.deltaTime));
            }
            HandleMovement(x, z, sprint, jumpDown);
        }

        HandleCombat(attackDown, skillDown, burstDown);
    }

    private void CheckGrounded()
    {
        Vector3 feetPos = transform.position + _controller.center - Vector3.up * (_controller.height * 0.5f - _controller.radius * 0.45f);
        int mask = ~LayerMask.GetMask("Ignore Raycast");
        bool hitGround = false;
        Collider[] hits = Physics.OverlapSphere(feetPos, _controller.radius + 0.14f, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform != transform && !hits[i].transform.IsChildOf(transform))
            {
                hitGround = true;
                break;
            }
        }
        _isGrounded = _controller.isGrounded || hitGround;
        if (_isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        if (activeAnimator != null) activeAnimator.SetBool("IsGrounded", _isGrounded);
    }

    private void HandleMovement(float x, float z, bool sprint, bool jumpDown)
    {
        Vector3 inputDir = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
        bool wantsSprint = sprint && inputDir.sqrMagnitude > 0.05f && _currentStamina > 5f;
        if (wantsSprint)
        {
            _currentStamina = Mathf.Max(0f, _currentStamina - 22f * Time.deltaTime);
        }
        else
        {
            _currentStamina = Mathf.Min(maxStamina, _currentStamina + 18f * Time.deltaTime);
        }

        float targetSpeed = wantsSprint ? sprintSpeed : walkSpeed;
        Vector3 moveWorld = Vector3.zero;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            GetCameraPlanarAxes(out Vector3 fwd, out Vector3 right);
            moveWorld = (fwd * inputDir.z + right * inputDir.x).normalized;

            Quaternion lookRot = Quaternion.LookRotation(moveWorld, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSmoothSpeed * Time.deltaTime);
        }

        if (_controller != null && _controller.enabled)
            _controller.Move(moveWorld * (targetSpeed * Time.deltaTime));

        if (jumpDown && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (activeAnimator != null) activeAnimator.SetTrigger("Jump");
        }

        _velocity.y = Mathf.Max(_velocity.y + gravity * Time.deltaTime, -28f);
        if (_controller != null && _controller.enabled)
            _controller.Move(_velocity * Time.deltaTime);

        if (activeAnimator != null)
        {
            float animSpeed = inputDir.magnitude * (wantsSprint ? 1.5f : 1.0f);
            activeAnimator.SetFloat("Speed", animSpeed, 0.08f, Time.deltaTime);
            SetAnimFloatSafe(activeAnimator, "VerticalVel", _velocity.y);
        }
    }

    private void GetCameraPlanarAxes(out Vector3 fwd, out Vector3 right)
    {
        Transform camTf = _cam != null ? _cam.transform : (Camera.main != null ? Camera.main.transform : null);
        if (camTf != null)
        {
            fwd = camTf.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
            {
                fwd.Normalize();
                right = Vector3.Cross(Vector3.up, fwd).normalized;
                return;
            }
        }

        float camYaw = _cam != null ? _cam.Yaw : transform.eulerAngles.y;
        fwd = Quaternion.Euler(0f, camYaw, 0f) * Vector3.forward;
        right = Quaternion.Euler(0f, camYaw, 0f) * Vector3.right;
    }

    private void StartDash(float x, float z)
    {
        _currentStamina -= 20f;
        _dashTimer = dashCooldown;
        _dashActiveRemaining = dashDuration;
        _lungeRemaining = 0f;
        _iFrameTimer = dashDuration + 0.12f;

        Vector3 inputDir = new Vector3(x, 0f, z);
        if (inputDir.sqrMagnitude > 0.05f)
        {
            GetCameraPlanarAxes(out Vector3 fwd, out Vector3 right);
            _dashDir = (fwd * inputDir.z + right * inputDir.x).normalized;
            transform.rotation = Quaternion.LookRotation(_dashDir, Vector3.up);
        }
        else
        {
            _dashDir = transform.forward;
        }

        if (activeAnimator != null) activeAnimator.SetTrigger("Dash");

        // Catat jika dash dilakukan saat ada musuh mengancam di dekat pemain (untuk DDA)
        if (DDAManager.Instance != null && EnemyAIController.IsAnyEnemyAttackingNear(transform.position, 4.5f))
        {
            DDAManager.Instance.RecordSuccessfulDodge();
            CombatFXHelper.SpawnFloatingText(transform.position, "DODGE!", Color.cyan, 0.9f);
        }
    }

    private void HandleCombat(bool attackDown, bool skillDown, bool burstDown)
    {
        // Heavy Dash-Slash (Q) memakai clip asli Sword_Dash dari UAL2
        if (burstDown && _burstTimer <= 0f && _attackTimer <= 0f && _currentStamina >= 24f)
        {
            _currentStamina -= 24f;
            _burstTimer = 3.2f;
            _attackTimer = 0.55f;
            FaceNearestEnemyInRange(meleeRange + 2.4f);

            _lungeDir = transform.forward;
            _lungeSpeed = 10.5f;
            _lungeRemaining = 0.22f;

            if (activeAnimator != null)
            {
                activeAnimator.SetTrigger("Burst");
            }
            ScheduleMeleeHit(meleeDamage * 1.75f, meleeRange + 0.85f, 0.16f);
            return;
        }

        // Normal Attack (4-Hit Sword Combo: Sword_Attack -> Sword_Regular_C -> Sword_Regular_Combo -> Sword_Heavy_Combo)
        if (attackDown && _attackTimer <= 0f)
        {
            if (Time.time - _lastAttackTime > comboWindow) _comboStep = 0;
            _lastAttackTime = Time.time;

            FaceNearestEnemyInRange(meleeRange + 1.6f);
            _lungeDir = transform.forward;
            _lungeSpeed = _comboStep >= 2 ? 5.6f : 3.4f;
            _lungeRemaining = 0.13f;

            if (activeAnimator != null)
            {
                activeAnimator.SetInteger("ComboStep", _comboStep);
                activeAnimator.SetTrigger("Attack");
            }

            float dmgMult = _comboStep == 3 ? 1.55f : (_comboStep == 2 ? 1.30f : (_comboStep == 1 ? 1.10f : 1.0f));
            float hitDelay = _comboStep >= 2 ? 0.18f : 0.14f;
            _attackTimer = _comboStep >= 2 ? 0.56f : 0.45f;
            ScheduleMeleeHit(meleeDamage * dmgMult, meleeRange + (_comboStep >= 2 ? 0.35f : 0f), hitDelay);
            _comboStep = (_comboStep + 1) % 4;
        }
    }

    private void ScheduleMeleeHit(float damage, float range, float delay)
    {
        if (_pendingHitTimer >= 0f)
        {
            PerformMeleeAttack(_pendingHitDamage, _pendingHitRange);
        }
        _pendingHitDamage = damage;
        _pendingHitRange = range;
        _pendingHitTimer = delay;
    }

    private void FaceNearestEnemyInRange(float searchRadius)
    {
        EnemyAIController best = null;
        float bestDist = searchRadius;
        for (int i = 0; i < EnemyAIController.ActiveEnemies.Count; i++)
        {
            EnemyAIController e = EnemyAIController.ActiveEnemies[i];
            if (e == null || e.IsDead) continue;
            Vector3 toE = e.transform.position - transform.position;
            toE.y = 0f;
            float d = toE.magnitude;
            if (d < bestDist && d > 0.05f)
            {
                bestDist = d;
                best = e;
            }
        }
        if (best != null)
        {
            Vector3 dir = best.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
    }

    private void PerformMeleeAttack(float damageAmount, float rangeAmount)
    {
        EnemyAIController[] enemies = EnemyAIController.ActiveEnemies.ToArray();
        bool hitAny = false;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyAIController e = enemies[i];
            if (e == null || e.IsDead) continue;
            Vector3 toEnemy = e.transform.position - transform.position;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;
            if (dist <= rangeAmount && (dist < 0.001f || Vector3.Dot(transform.forward, toEnemy / dist) > -0.2f))
            {
                e.TakeDamage(damageAmount, ActiveElement, transform.position, true);
                hitAny = true;
            }
        }
        if (DDAManager.Instance != null) DDAManager.Instance.RecordPlayerShot(hitAny);
    }

    public void SwitchCharacter(int index, bool silent)
    {
        _activeCharIndex = index == 1 ? 1 : 0;
        if (pyroVisual != null) pyroVisual.SetActive(_activeCharIndex == 0);
        if (hydroVisual != null) hydroVisual.SetActive(_activeCharIndex == 1);
        UpdateActiveAnimator();

        if (!silent)
        {
            Color c = ActiveElement == ElementType.Pyro ? new Color(1f, 0.45f, 0.15f) : new Color(0.25f, 0.8f, 1f);
            CombatFXHelper.SpawnFloatingText(transform.position, ActiveCharacterName, c, 1.0f);
        }
    }

    private void UpdateActiveAnimator()
    {
        GameObject activeObj = _activeCharIndex == 0 ? pyroVisual : hydroVisual;
        if (activeObj != null)
        {
            activeObj.transform.localPosition = Vector3.zero;
            activeObj.transform.localRotation = Quaternion.identity;
            activeAnimator = activeObj.GetComponentInChildren<Animator>();
            if (activeAnimator != null)
            {
                activeAnimator.applyRootMotion = false;
                activeAnimator.Rebind();
                SetAnimBoolSafe(activeAnimator, "IsGrounded", _isGrounded);
                SetAnimBoolSafe(activeAnimator, "IsDead", IsDead);
                activeAnimator.Update(0f);
                var clipInfos = activeAnimator.GetCurrentAnimatorClipInfo(0);
                string clipName = clipInfos.Length > 0 && clipInfos[0].clip != null ? clipInfos[0].clip.name : "NONE";
                if (activeAnimator.runtimeAnimatorController == null || activeAnimator.avatar == null || !activeAnimator.avatar.isValid || clipInfos.Length == 0)
                {
                    Debug.LogError($"[Player Animator] Animator pada '{activeObj.name}' bermasalah: ctrl={activeAnimator.runtimeAnimatorController != null}, " +
                                   $"avatarValid={activeAnimator.avatar != null && activeAnimator.avatar.isValid}, human={activeAnimator.avatar != null && activeAnimator.avatar.isHuman}, " +
                                   $"init={activeAnimator.isInitialized}, clip={clipName}");
                }
            }
        }
    }

    public void TakeDamage(float amount, ElementType element, Vector3 hitSourcePos, bool isPlayerAttack = false)
    {
        if (IsDead || _iFrameTimer > 0f) return;

        _currentHp = Mathf.Max(0f, _currentHp - amount);
        if (activeAnimator != null && _currentHp > 0f)
        {
            SetAnimIntSafe(activeAnimator, "HitType", Random.Range(0, 2));
            activeAnimator.SetTrigger("Hit");
        }
        CombatFXHelper.SpawnFloatingText(transform.position, $"-{amount:F0}", Color.red, 1.0f);

        if (DDAManager.Instance != null)
        {
            DDAManager.Instance.RecordPlayerDamageTaken(amount);
        }

        if (_currentHp <= 0f)
        {
            _pendingHitTimer = -1f;
            _lungeRemaining = 0f;
            _dashActiveRemaining = 0f;
            _respawnTimer = 2.5f;
            SetAnimBoolSafe(activeAnimator, "IsDead", true);
            CombatFXHelper.SpawnFloatingText(transform.position + Vector3.up * 0.6f, "KNOCKED OUT!", Color.red, 1.3f);
            if (DDAManager.Instance != null) DDAManager.Instance.RecordPlayerDeath();
        }
    }

    public void Heal(float amount)
    {
        _currentHp = Mathf.Min(maxHp, _currentHp + amount);
    }

    public void GainEnergy(float amount)
    {
        _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy + amount);
    }

    private void RespawnAtHome()
    {
        _controller.enabled = false;
        transform.position = _homeSpawnPos + Vector3.up * 0.5f;
        _controller.enabled = true;
        _velocity = Vector3.zero;
        _currentHp = maxHp;
        _currentStamina = maxStamina;
        SetAnimBoolSafe(activeAnimator, "IsDead", false);
        CombatFXHelper.SpawnFloatingText(transform.position, "RESPAWNED", Color.green, 1.1f);
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

    private void ReadInputs(
        out float x, out float z, out bool sprint, out bool jumpDown,
        out bool attackDown, out bool dashDown, out bool skillDown, out bool burstDown,
        out int swapChoice)
    {
        x = 0f; z = 0f;
        sprint = false; jumpDown = false;
        attackDown = false; dashDown = false; skillDown = false; burstDown = false;
        swapChoice = -1;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
        if (kb != null)
        {
            if (!kb.enabled) InputSystem.EnableDevice(kb);
            if (kb.wKey.isPressed) z += 1f;
            if (kb.sKey.isPressed) z -= 1f;
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;

            sprint |= kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            jumpDown |= kb.spaceKey.wasPressedThisFrame;
            skillDown |= kb.eKey.wasPressedThisFrame;
            burstDown |= kb.qKey.wasPressedThisFrame;
            if (kb.leftCtrlKey.wasPressedThisFrame) dashDown = true;
            if (kb.digit1Key.wasPressedThisFrame) swapChoice = 0;
            if (kb.digit2Key.wasPressedThisFrame) swapChoice = 1;
        }
        var mouse = Mouse.current ?? InputSystem.GetDevice<Mouse>();
        if (mouse != null && (_cam == null || !_cam.IsFreeCursorMode))
        {
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            attackDown |= mouse.leftButton.wasPressedThisFrame;
            if (mouse.rightButton.wasPressedThisFrame) dashDown = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        float legX = 0f;
        float legZ = 0f;
        if (Input.GetKey(KeyCode.W)) legZ += 1f;
        if (Input.GetKey(KeyCode.S)) legZ -= 1f;
        if (Input.GetKey(KeyCode.A)) legX -= 1f;
        if (Input.GetKey(KeyCode.D)) legX += 1f;
        if (Mathf.Abs(x) < 0.01f && Mathf.Abs(legX) > 0.01f) x = legX;
        if (Mathf.Abs(z) < 0.01f && Mathf.Abs(legZ) > 0.01f) z = legZ;

        sprint |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        jumpDown |= Input.GetKeyDown(KeyCode.Space);
        skillDown |= Input.GetKeyDown(KeyCode.E);
        burstDown |= Input.GetKeyDown(KeyCode.Q);
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetMouseButtonDown(1)) dashDown = true;
        if (_cam == null || !_cam.IsFreeCursorMode) attackDown |= Input.GetMouseButtonDown(0);
        if (swapChoice < 0 && Input.GetKeyDown(KeyCode.Alpha1)) swapChoice = 0;
        if (swapChoice < 0 && Input.GetKeyDown(KeyCode.Alpha2)) swapChoice = 1;
#endif
    }
}
