using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Guard : MonoBehaviour {

    [SerializeField]
    private Transform path;
    private int pathTarget;
    private int pathDirection = 1;

    [HideInInspector]
    public Transform chasefollowTarget;

    [HideInInspector]
    public NavMeshAgent navMeshAgent;

    public enum State { Patrol, Inspect, Chase, Return }
    [HideInInspector]
    public State state = State.Patrol;

    // Start is called before the first frame update
    void Start() {
        navMeshAgent = GetComponent<NavMeshAgent>();

        JoinPath();
    }

    // Update is called once per frame
    void Update() {
        // Can the guard see any escaped prisoners
        if (state == State.Patrol || state == State.Inspect) {
            for (float rot = -70; rot <= 70; rot += 5) {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Quaternion.AngleAxis(rot, transform.up) * transform.forward, out hit, 25f) && (hit.transform.CompareTag("Player") || hit.transform.CompareTag("Prisoner"))) {
                    foreach (Guard guard in GameController.main.guards) {
                        guard.Chase(hit.transform);
                    }
                }
            }
        }

        // Behaviour
        switch(state) {
            case State.Patrol:
                // Get next path node
                if (Vector3.Distance(transform.position + Vector3.down, path.GetChild(pathTarget).transform.position) < 0.1f) {
                    if ((pathDirection == 1 && pathTarget < path.childCount - 1) || (pathDirection == -1 && pathTarget > 0)) {
                        pathTarget += pathDirection;
                    }
                    // If at an end of the path, should it loop or deverse? 
                    else {
                        if (pathDirection == 1) {
                            if (Vector3.Distance(path.GetChild(pathTarget).transform.position, path.GetChild(0).transform.position) < Vector3.Distance(path.GetChild(pathTarget).transform.position, path.GetChild(pathTarget - 1).transform.position)) {
                                pathTarget = 0;
                            }
                            else {
                                pathDirection = -1;
                                pathTarget--;
                            }
                        }
                        else {
                            if (Vector3.Distance(path.GetChild(0).transform.position, path.GetChild(1).transform.position) < Vector3.Distance(path.GetChild(0).transform.position, path.GetChild(path.childCount - 1).transform.position)) {
                                pathDirection = 1;
                                pathTarget = 1;
                            }
                            else {
                                pathTarget = path.childCount - 1;
                            }
                        }
                    }
                    // Set new destination
                    navMeshAgent.SetDestination(path.GetChild(pathTarget).transform.position);
                }
                break;

            case State.Inspect:
                // If close to the inspection spot, return to patrol
                if (Vector3.Distance(transform.position, navMeshAgent.pathEndPosition + Vector3.up) < 0.1f) {
                    state = State.Patrol;
                    navMeshAgent.speed = 1f;
                    JoinPath();
                }
                break;

            case State.Chase:
                // Chase the target until the guard can't get any closer, return to patrol
                navMeshAgent.SetDestination(chasefollowTarget.position);
                if (Vector3.Distance(transform.position, navMeshAgent.pathEndPosition + Vector3.up) < 0.1f) {
                    state = State.Patrol;
                    navMeshAgent.speed = 1f;
                    JoinPath();
                }
                break;

            case State.Return:
                // TODO: Path distance, not heuristic distance
                if (Vector3.Distance(transform.position, chasefollowTarget.position) > 2.5f)
                    navMeshAgent.SetDestination(chasefollowTarget.position);
                else
                    navMeshAgent.ResetPath();
                break;
        }
    }

    public void OnCollisionEnter(Collision collision) {
        // Caught player or target prisoner
        if (collision.gameObject.CompareTag("Player")) {
            GameController.main.Fail(true);
            navMeshAgent.enabled = false;
            enabled = false;
        }
        else if (collision.transform == GameController.main.prisoner.transform) {
            GameController.main.Fail(false);
        }

        // Caught other prisoner
        if (state != State.Return && collision.gameObject.CompareTag("Prisoner")) {
            // Take prisoner back to cell
            collision.gameObject.GetComponent<Prisoner>().Return(this);
            state = State.Return;
            chasefollowTarget = collision.transform;
            // Tell other guards to return to patrol
            foreach (Guard guard in GameController.main.guards) {
                if (guard != this)
                    guard.JoinPath();
            }
        }
    }

    // Set the destination to the nearest node 
    // TODO: This should be a path length rather than a heurestic
    public void JoinPath() {
        state = State.Patrol;
        pathTarget = 0;
        float pathTargetDistance = Vector3.Distance(transform.position, path.GetChild(0).transform.position);

        for (int i = 1; i < path.childCount; i++) {
            float iDist = Vector3.Distance(transform.position, path.GetChild(i).transform.position);
            if (iDist < pathTargetDistance) {
                pathTarget = i;
                pathTargetDistance = iDist;
            }
        }

        navMeshAgent.SetDestination(path.GetChild(pathTarget).transform.position);
    }

    public void Inspect(Vector3 position) {
        state = State.Inspect;
        navMeshAgent.speed = 2f;

        // Navigate to position
        NavMeshPath navPath = new NavMeshPath();
        navMeshAgent.CalculatePath(position, navPath);
        if (navPath.status == NavMeshPathStatus.PathComplete) {
            navMeshAgent.SetPath(navPath);
        }
        // If position isn't available, navigate to the nearest door instead
        else {
            Bars barBest = null;
            float barBestDistance = Mathf.Infinity;
            foreach (Bars bar in GameController.main.bars) {
                float barDistance = Vector3.Distance(transform.position, bar.transform.position);
                if (barDistance < barBestDistance) {
                    barBest = bar;
                    barBestDistance = barDistance;
                }
            }
            navMeshAgent.SetDestination(barBest.transform.position);
        }
    }

    public void Chase(Transform transform) {
        state = State.Chase;
        chasefollowTarget = transform;
        navMeshAgent.speed = 3f;
    }
}