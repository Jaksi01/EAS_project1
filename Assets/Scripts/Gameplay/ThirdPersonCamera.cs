using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kamera orbit Third-Person bergaya Action RPG dengan anti-tembus dinding (SphereCast),
/// auto-reacquire cursor lock saat klik di Game View, dukungan drag mouse kanan/tengah,
/// serta tombol panah sebagai cadangan untuk trackpad laptop.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);
    public float distance = 4.6f;
    public float minDistance = 1.6f;
    public float maxDistance = 7.5f;
    public float sensitivity = 0.20f;
    public float keyOrbitSpeed = 110f;
    public float minPitch = -25f;
    public float maxPitch = 65f;

    private float _yaw;
    private float _pitch = 15f;
    private bool _freeCursorMode;
    private Vector2 _lastMousePos;
    private bool _hasLastMousePos;
    private int _skipDeltaFrames;

    public float Yaw => _yaw;
    public bool IsFreeCursorMode => _freeCursorMode;

    private void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p != null) target = p.transform;
        }
        if (target != null)
        {
            _yaw = target.eulerAngles.y;
        }
        SetCursorLock(true);
        _skipDeltaFrames = 2;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _hasLastMousePos = false;
        _skipDeltaFrames = 2;
        if (hasFocus && !_freeCursorMode)
        {
            SetCursorLock(true);
        }
    }

    private void Update()
    {
        bool toggleFree = false;
        bool escPressed = false;
        bool holdAlt = false;
        bool mouseClick = false;
        bool mouseDragHeld = false;
        Vector2 rawDelta = Vector2.zero;
        Vector2 mousePos = Vector2.zero;
        bool hasMousePos = false;
        Vector2 keyOrbit = Vector2.zero;
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
        if (kb != null)
        {
            if (!kb.enabled) InputSystem.EnableDevice(kb);
            if (kb.tabKey.wasPressedThisFrame) toggleFree = true;
            if (kb.escapeKey.wasPressedThisFrame) escPressed = true;
            holdAlt |= kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;

            if (kb.leftArrowKey.isPressed) keyOrbit.x -= 1f;
            if (kb.rightArrowKey.isPressed) keyOrbit.x += 1f;
            if (kb.upArrowKey.isPressed) keyOrbit.y += 1f;
            if (kb.downArrowKey.isPressed) keyOrbit.y -= 1f;
        }

        var mouse = Mouse.current ?? InputSystem.GetDevice<Mouse>();
        if (mouse != null)
        {
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            mouseClick |= mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
            mouseDragHeld |= mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            rawDelta = mouse.delta.ReadValue();
            mousePos = mouse.position.ReadValue();
            hasMousePos = true;
            scroll = mouse.scroll.ReadValue().y * 0.01f;
        }

        if (rawDelta.sqrMagnitude < 0.0001f && Pointer.current != null)
        {
            rawDelta = Pointer.current.delta.ReadValue();
            if (!hasMousePos)
            {
                mousePos = Pointer.current.position.ReadValue();
                hasMousePos = true;
            }
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Tab)) toggleFree = true;
        if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
        holdAlt |= Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (keyOrbit.sqrMagnitude < 0.001f)
        {
            if (Input.GetKey(KeyCode.LeftArrow)) keyOrbit.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) keyOrbit.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) keyOrbit.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) keyOrbit.y -= 1f;
        }

        mouseClick |= Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        mouseDragHeld |= Input.GetMouseButton(1) || Input.GetMouseButton(2);
        if (rawDelta.sqrMagnitude < 0.0001f)
        {
            rawDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
        }
        if (!hasMousePos)
        {
            mousePos = Input.mousePosition;
            hasMousePos = true;
        }
        if (Mathf.Abs(scroll) < 0.001f)
        {
            scroll = Input.GetAxis("Mouse ScrollWheel") * 5f;
        }
#endif

        bool insideGameView = !hasMousePos ||
                              (mousePos.x >= 0f && mousePos.x <= Screen.width &&
                               mousePos.y >= 0f && mousePos.y <= Screen.height);

        // Fallback delta dari selisih posisi kursor bila delta driver bernilai nol saat unlocked
        if (rawDelta.sqrMagnitude < 0.0001f && hasMousePos && Cursor.lockState != CursorLockMode.Locked)
        {
            if (_hasLastMousePos)
            {
                rawDelta = mousePos - _lastMousePos;
            }
        }
        if (hasMousePos)
        {
            _lastMousePos = mousePos;
            _hasLastMousePos = true;
        }

        if (toggleFree)
        {
            _freeCursorMode = !_freeCursorMode;
            SetCursorLock(!_freeCursorMode);
            _skipDeltaFrames = 2;
        }
        else if (escPressed)
        {
            _freeCursorMode = true;
            SetCursorLock(false);
        }
        else if (holdAlt)
        {
            SetCursorLock(false);
        }
        else
        {
            // Klik di dalam Game View selalu mengembalikan kontrol kamera (keluar dari FreeCursorMode)
            if (mouseClick && insideGameView)
            {
                if (_freeCursorMode || Cursor.lockState != CursorLockMode.Locked)
                {
                    _freeCursorMode = false;
                    SetCursorLock(true);
                    _skipDeltaFrames = 2;
                }
            }
            else if (_freeCursorMode)
            {
                SetCursorLock(false);
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Application.isFocused && !Application.isEditor)
            {
                SetCursorLock(true);
            }
        }

        if (_skipDeltaFrames > 0)
        {
            _skipDeltaFrames--;
            rawDelta = Vector2.zero;
        }

        // Di Editor, izinkan orbit selama kursor berada di Game View & tidak dalam FreeCursorMode/Alt,
        // meskipun Unity Editor sementara gagal mengunci Cursor.lockState.
        bool canOrbitWithMouse =
            (!_freeCursorMode && !holdAlt && (Cursor.lockState == CursorLockMode.Locked || insideGameView)) ||
            mouseDragHeld;

        if (canOrbitWithMouse && rawDelta.sqrMagnitude > 0.00001f)
        {
            Vector2 clamped = Vector2.ClampMagnitude(rawDelta, 120f);
            _yaw += clamped.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - clamped.y * sensitivity, minPitch, maxPitch);
        }

        if (keyOrbit.sqrMagnitude > 0.01f)
        {
            _yaw += keyOrbit.x * keyOrbitSpeed * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch - keyOrbit.y * keyOrbitSpeed * Time.deltaTime, minPitch, maxPitch);
        }

        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * 0.5f, minDistance, maxDistance);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivot = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        // Posisikan kamera lurus di belakang pemain agar tombol W selalu bergerak searah sumbu pandang layar
        Vector3 desiredOffset = rot * new Vector3(0f, 0f, -distance);

        float actualDist = desiredOffset.magnitude;
        Vector3 dir = desiredOffset.normalized;

        int mask = ~LayerMask.GetMask("Ignore Raycast");
        if (Physics.SphereCast(pivot, 0.25f, dir, out RaycastHit hit, actualDist, mask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(target) && hit.transform != target)
            {
                actualDist = Mathf.Max(0.75f, hit.distance - 0.15f);
            }
        }

        transform.position = pivot + dir * actualDist;
        transform.LookAt(pivot);
    }

    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
