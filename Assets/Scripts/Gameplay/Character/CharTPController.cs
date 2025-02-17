﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CharTPController : MonoBehaviourPun
{
    [Header("References, look for them in children")]
    public List<Transform> lookTargets; //places for cam to look

    public CharJumpCheck jumpChk;
    public CharCrouchCheck crouchChk;

    [Header("Camera Settings")]    
    public float targetCamDist;    
    public float cameraBobAmt;
    public float cameraBobFreq;    


    [Header("Speed settings")]
    [SerializeField]
    [Tooltip("Vertical and Horizontal movement speed")]
    private float moveSpeed = 30;

    [SerializeField]
    [Tooltip("How fast to deccelerate while moving, keep this low")]
    private float kineticFriction = 1;
    
    [SerializeField]
    [Tooltip("How fast to deccelerate after letting go of movement key, keep this very high")]
    private float staticFriction = 10;

    [SerializeField]
    [Tooltip("Higher means able to move more in air")]
    private float airMoveSpeed = 20;

    [SerializeField]
    [Tooltip("Speed of crouch walk")]
    private float crouchSpeed = 25;

    [SerializeField]
    [Tooltip("Speed of jump")]
    private float jumpSpeed = 5;

    [Header("Mouse Settings")]
    [SerializeField]
    [Tooltip("Mouse sensitivity, affects rotation speed")]
    private float mouseSens = 1;

    [SerializeField]
    [Tooltip("Clamps how far you can look up or down")]
    private float maxLookY = 0.9f;

    [SerializeField]
    [Tooltip("Starting y-axis look direction, from -1 to 1")]
    private float initialLookY = -0.5f;

    public bool disableMovement { private set; get; }
    public bool disableKeyInput { private set; get; }
    public bool disableMouseInput { private set; get; }
    public bool disableFriction { private set; get; }

    public Vector3 position { get { return transform.position; } }
    public Vector3 forward { get { return transform.forward; } }
    public float velY { private set; get; }
    public float displacement { private set; get; } //how fast im moving xz
    public Vector3 lookDir { private set; get; }

    //these are just to cache values
    [HideInInspector]
    public Rigidbody rb;

    private Vector3 pPos, pforward;
    private Vector3 right;
    private float currSpeed;
    private Vector3 moveAmt;

    private struct InputData
    {
        public float vert, hori;
        public float mouseY, mouseX;
        public bool jump, crouch;
    };

    private InputData inp;

    // List of all objs with CharTPController, updated in OnEnable and OnDisable, to be used for anywhere that needs references to other players
    public struct PlayerControllerData
    {
        public string name;
        public CharTPController controller;

        public PlayerControllerData(PlayerControllerData other)
        {
            name = other.name;
            controller = other.controller;
        }
    }
    private static List<PlayerControllerData> playerControllerRefs = new List<PlayerControllerData>();
    public static List<PlayerControllerData> PlayerControllerRefs { get => playerControllerRefs; }
    public delegate void OnPlayerAddCallback(PlayerControllerData newPlayer);
    public delegate void OnPlayerRemoveCallback(PlayerControllerData removedPlayer);

    public static OnPlayerAddCallback OnPlayerAdd;
    public static OnPlayerRemoveCallback OnPlayerRemoved;

    private void OnEnable()
    {
        PlayerControllerData newPlayer = new PlayerControllerData() { name = photonView.Owner.NickName, controller = this };
        playerControllerRefs.Add(newPlayer);
        OnPlayerAdd?.Invoke(newPlayer);
    }
    private void OnDisable()
    {
        PlayerControllerData removedPlayer = default;
        for (int i = 0; i < playerControllerRefs.Count; ++i)
            if (playerControllerRefs[i].controller == this)
            {
                removedPlayer = new PlayerControllerData(playerControllerRefs[i]);
                playerControllerRefs.RemoveAt(i);
                break;
            }
        OnPlayerRemoved?.Invoke(removedPlayer);
    }

    private void Start()
    {
        initialLookY = Mathf.Clamp(initialLookY, -maxLookY, maxLookY);

        pPos = transform.position;
        pforward = transform.forward;
        pforward.y = 0;
        lookDir = new Vector3(pforward.x, initialLookY, pforward.z).normalized;
        pforward.Normalize();

        currSpeed = moveSpeed;
        disableMouseInput = disableKeyInput = disableMovement = false;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
            return;
        if (!disableKeyInput)
        {
            inp.vert = Input.GetAxisRaw("Vertical");
            inp.hori = Input.GetAxisRaw("Horizontal");
            inp.crouch = Input.GetAxisRaw("Crouch") != 0;
            inp.jump = Input.GetAxisRaw("Jump") != 0;
        }
        else
        {
            inp.vert = 0;
            inp.hori = 0;
            inp.crouch = false;
            inp.jump = false;
        }
        if (!disableMouseInput)
        {
            inp.mouseY = Input.GetAxisRaw("Mouse Y");
            inp.mouseX = Input.GetAxisRaw("Mouse X");
        }
        else
        {
            inp.mouseY = 0;
            inp.mouseX = 0;
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
            return;

        //check y
        if ((lookDir.y > maxLookY && inp.mouseY > 0) || (lookDir.y < -maxLookY && inp.mouseY < 0))
            inp.mouseY = 0;
        //calculate where cam and player is facing
        lookDir = Quaternion.Euler(0, inp.mouseX * mouseSens, 0) * lookDir;
        lookDir = Quaternion.AngleAxis(-inp.mouseY * mouseSens, right) * lookDir;
        lookDir.Normalize();

        if (!disableMovement)
        {
            //remove y for movement
            pforward.Set(lookDir.x, 0, lookDir.z);
            pforward.Normalize();
            right = Vector3.Cross(Vector3.up, pforward);

            crouchChk.Crouch(inp.crouch && !jumpChk.airborne);
            if (crouchChk.crouching)
                currSpeed = crouchSpeed;
            else if (jumpChk.airborne)
                currSpeed = airMoveSpeed;
            else
                currSpeed = moveSpeed;

            moveAmt = (pforward * inp.vert + right * inp.hori).normalized;
            moveAmt.y = 0;

            velY = rb.velocity.y;
            displacement = moveAmt.magnitude * currSpeed;

            if (velY > 0)
                jumpChk.Jumping();
            if (inp.jump && !jumpChk.airborne)
                rb.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);

            //move player
            //pPos = rb.position;
            //pPos += moveAmt * Time.deltaTime * currSpeed;
            //Move(pPos);
            rb.AddForce(moveAmt * currSpeed, ForceMode.Acceleration);
            //rotate player
            transform.rotation = Quaternion.LookRotation(pforward, Vector3.up);
        }

        if (!disableFriction)
        {
            //deccelerate to 0
            Vector3 temp = rb.velocity;
            temp = Vector3.Lerp(temp, Vector3.zero, Time.deltaTime * kineticFriction);
            temp.y = rb.velocity.y;
            rb.velocity = temp;
            if (displacement <= Mathf.Epsilon)
            {
                temp = rb.velocity;
                temp = Vector3.Lerp(temp, Vector3.zero, Time.deltaTime * staticFriction);
                temp.y = rb.velocity.y;
                rb.velocity = temp;
            }
        }
    }

    public void DisableKeyInput(bool b)
    {
        Debug.LogFormat("Disabled key input : {0}", b);
        disableKeyInput = b;
    }

    public void DisableMouseInput(bool b)
    {
        Debug.LogFormat("Disabled mouse input : {0}", b);
        disableMouseInput = b;
    }
    public void DisableMovement(bool b)
    {
        Debug.LogFormat("Disabled movement : {0}", b);
        disableMovement = b;
    }
    public void DisableFriction(bool b)
    {
        Debug.LogFormat("Disabled friction : {0}", b);
        disableFriction = b;
    }
}