using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PT_PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    public float walkSpeed = 6f;
    public float sprintSpeed = 12f;
    public float gravity = -9.18f;
    public float jumpHeight = 2.2f;

    public Transform groundCheck;
    public float groundDistance = 0.35f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller != null && controller.stepOffset < 0.45f)
        {
            controller.stepOffset = 0.45f;
        }
    }

    void Update()
    {
        if (controller == null) return;

        bool controllerGrounded = controller.isGrounded;
        bool sphereGrounded = false;
        if (groundCheck != null && groundMask.value != 0 && groundCheck != transform && groundCheck.IsChildOf(transform))
        {
            sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            Vector3 feetPos = transform.position + controller.center - Vector3.up * (controller.height * 0.5f - controller.radius * 0.5f);
            int mask = groundMask.value != 0 ? groundMask.value : ~LayerMask.GetMask("Ignore Raycast");
            Collider[] hits = Physics.OverlapSphere(feetPos, controller.radius + 0.12f, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform != transform && !hits[i].transform.IsChildOf(transform))
                {
                    sphereGrounded = true;
                    break;
                }
            }
        }

        isGrounded = controllerGrounded || sphereGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // ── Input ────────────────────────────────────────────────────────────
        float x        = 0f;
        float z        = 0f;
        bool  sprint   = false;
        bool  jumpDown = false;

#if ENABLE_INPUT_SYSTEM
        // New Input System (Unity 6 / package com.unity.inputsystem)
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) z  =  1f;
            if (kb.sKey.isPressed) z  = -1f;
            if (kb.aKey.isPressed) x  = -1f;
            if (kb.dKey.isPressed) x  =  1f;

            sprint   = kb.leftShiftKey.isPressed;
            jumpDown = kb.spaceKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        // Legacy Input Manager (Unity 2022 and earlier)
        x        = Input.GetAxis("Horizontal");
        z        = Input.GetAxis("Vertical");
        sprint   = Input.GetKey(KeyCode.LeftShift);
        jumpDown = Input.GetButtonDown("Jump");
#endif
        // ─────────────────────────────────────────────────────────────────────

        float currentSpeed = sprint ? sprintSpeed : walkSpeed;

        Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (jumpDown && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, -25f);
        controller.Move(velocity * Time.deltaTime);
    }
}
