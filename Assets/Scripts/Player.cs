using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class Player : MonoBehaviour {

    public static Player main;

    [HideInInspector]
    public bool exit = false;

    public AudioSource audioScream;

    public List<Prisoner> followers; 

    [SerializeField]
    LayerMask layerMaskDoor;

    [HideInInspector]
    public Rigidbody rb;
    private Vector2 inputMove;
    
    private float inputRotate;

    // Start is called before the first frame update
    void Start() {
        main = this;

        rb = GetComponent<Rigidbody>();
        followers = new List<Prisoner>();
    }

    // Update is called once per frame
    void Update() {
        
    }

    private void FixedUpdate() {
        if (!exit && GameController.main.state == GameController.State.Game) {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(Vector3.up * inputRotate * 30f * Time.fixedDeltaTime));
            rb.MovePosition(transform.position + (((transform.forward * inputMove.y) + (transform.right * inputMove.x)) * Time.fixedDeltaTime * 2)); 
        }
        else if (exit) {
            rb.MovePosition(Vector3.MoveTowards(transform.position, GameController.main.finish.position, Time.fixedDeltaTime * 2));
        }
    }

    private void OnTriggerEnter(Collider other) {
        rb.isKinematic = true;
        exit = true;
    }

    public void InputMove(InputAction.CallbackContext context) {
        inputMove = context.ReadValue<Vector2>();
    }

    public void InputInteract(InputAction.CallbackContext context) {
        if (context.performed && GameController.main.state == GameController.State.Game) {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 2f, layerMaskDoor);
            if (colliders.Length == 1)
                colliders[0].GetComponent<Bars>().Unlock();
        }
    }

    public bool InteractAvailable() {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 2f, layerMaskDoor);
        return colliders.Length == 1;
    }

    public void InputScream(InputAction.CallbackContext context) {
        if (context.performed) {
            if (GameController.main.state == GameController.State.Game) {
                // Make player scream
                audioScream.Play();

                // Make prisoner scream
                Collider[] colliders = Physics.OverlapSphere(transform.position, 2f, layerMaskDoor);
                if (colliders.Length == 1) {
                    Bars bars = colliders[0].GetComponent<Bars>();
                    if (bars.prisoner != null)
                        bars.prisoner.Scream();
                }

                // Alert the nearest guard (within a radius)
                Guard guardBest = null;
                float guardBestDistance = Mathf.Infinity;
                foreach (Guard guard in GameController.main.guards) {
                    float guardDistance = Vector3.Distance(transform.position, guard.transform.position);
                    if (guardDistance < guardBestDistance && guardDistance < 5f) {
                        guardBest = guard;
                        guardBestDistance = guardDistance;
                    }
                }
                if (guardBest != null)
                    guardBest.Inspect(transform.position);
            }
            else if (GameController.main.state == GameController.State.Intro) {
                GameController.main.Play();
            }
        }
    }

    public void InputRotate(InputAction.CallbackContext context) {
        inputRotate = context.ReadValue<Vector2>().x;
    }

    public void InputDeviceChange(PlayerInput playerInput) {
        if (playerInput.currentControlScheme == "Keyboard&Mouse") {
            GameController.main.uiControlsMode = GameController.uiControlsModes.PC;
        }
        else if (playerInput.devices[0].name == "XInputControllerWindows") {
            GameController.main.uiControlsMode = GameController.uiControlsModes.Xbox;
        }
        else {
            GameController.main.uiControlsMode = GameController.uiControlsModes.Playstation;
        }
    }
}
