using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bars : MonoBehaviour {

    [SerializeField] LayerMask layerMaskPrisoner;
    public bool locked = true;
    [HideInInspector] public Prisoner prisoner;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start() {
        Collider[] colliders = Physics.OverlapBox(transform.position, new Vector3(2, 1, 2), Quaternion.identity, layerMaskPrisoner);
        if (colliders.Length == 1)
            prisoner = colliders[0].GetComponent<Prisoner>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update() {
        if (!locked && transform.position.y > -2.1f)
            transform.Translate(Vector3.down * Time.deltaTime);

        else if (locked && transform.position.y < 0) {
            transform.Translate(Vector3.up * Time.deltaTime);
            if (transform.position.y > 0)
                transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }

    public void Unlock() {
        locked = false;
        audioSource.Play();

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

    public void Lock() {
        locked = true;
        audioSource.Play();
    }
}
