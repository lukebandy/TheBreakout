using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Prisoner : MonoBehaviour {

    public enum State { Follow, Return, Exit }
    [HideInInspector]
    public State state;

    private Vector3 positionStart;

    private Guard returnGuard;

    public AudioSource audioScream;
    [HideInInspector]
    public NavMeshAgent navMeshAgent;

    // Start is called before the first frame update
    void Start() {
        navMeshAgent = GetComponent<NavMeshAgent>();

        positionStart = transform.position;
    }

    // Update is called once per frame
    void Update() {
        switch (state) {
            case State.Follow:
                // Follow player
                if (!Player.main.exit) {

                    NavMeshPath path = new NavMeshPath();
                    navMeshAgent.CalculatePath(Player.main.transform.position, path);
                    if (path.status == NavMeshPathStatus.PathComplete) {
                        if (!Player.main.followers.Contains(this))
                            Player.main.followers.Add(this);

                        if (GameController.main.PathDistance(path) > 2f * (Player.main.followers.IndexOf(this) + 1)) {
                            navMeshAgent.SetPath(path);
                        }
                        else
                            navMeshAgent.ResetPath();
                    }
                    else {
                        if (Player.main.followers.Contains(this))
                            Player.main.followers.Remove(this);
                        navMeshAgent.ResetPath();
                    }
                }
                // Move to exit
                else {
                    NavMeshPath path = new NavMeshPath();
                    navMeshAgent.CalculatePath(GameController.main.finish.GetChild(0).position, path);

                    if (path.corners.Length > 0 && Vector3.Distance(GameController.main.finish.GetChild(0).position, path.corners[path.corners.Length - 1]) < 0.5f)
                        navMeshAgent.SetPath(path);
                    else
                        navMeshAgent.ResetPath();
                }
                break;

            case State.Return:
                // If back in cell then lock door, return guard to patrol and return to trying to follow the player
                if (Vector3.Distance(transform.position, positionStart) < 0.2f) {
                    Bars barBest = null;
                    float barBestDistance = Mathf.Infinity;
                    foreach (Bars bar in GameController.main.bars) {
                        float barDistance = Vector3.Distance(transform.position, bar.transform.position);
                        if (barDistance < barBestDistance) {
                            barBest = bar;
                            barBestDistance = barDistance;
                        }
                    }
                    barBest.Lock();

                    state = State.Follow;
                    returnGuard.JoinPath();
                }
                break;

            case State.Exit:
                transform.position = Vector3.MoveTowards(transform.position, GameController.main.finish.position, Time.deltaTime * navMeshAgent.speed);
                if (transform.position == GameController.main.finish.position)
                    gameObject.SetActive(false);
                break;
        }
    }

    private void OnTriggerEnter(Collider other) {
        navMeshAgent.enabled = false;
        state = State.Exit;
    }

    public void Scream() {
        audioScream.PlayDelayed(Random.Range(0.25f, 0.75f));
    }

    public void Return(Guard guard) {
        if (Player.main.followers.Contains(this))
            Player.main.followers.Remove(this);
        state = State.Return;
        navMeshAgent.SetDestination(positionStart);
        returnGuard = guard;
    }
}
