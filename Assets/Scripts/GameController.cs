using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class GameController : MonoBehaviour {

    public static GameController main;

    private AudioSource audioSource;

    public Prisoner[] prisoners;
    [HideInInspector]
    public Prisoner prisoner;
    [HideInInspector]
    public Guard[] guards;
    [HideInInspector]
    public Bars[] bars;
    public Transform finish;

    public enum uiControlsModes { PC, Xbox, Playstation }
    //[HideInInspector]
    public static uiControlsModes uiControlsMode = uiControlsModes.PC;

    [SerializeField]
    TextMeshProUGUI uiText;
    [SerializeField]
    Image uiFader;
    [SerializeField]
    TextMeshProUGUI uiTitle;

    [SerializeField]
    GameObject uiStartPC;
    [SerializeField]
    GameObject uiStartXbox;
    [SerializeField]
    GameObject uiStartPlaystation;
    [SerializeField]
    GameObject uiControlsPC;
    [SerializeField]
    GameObject uiControlsPCUnlock;
    [SerializeField]
    GameObject uiControlsXbox;
    [SerializeField]
    GameObject uiControlsXboxUnlock;
    [SerializeField]
    GameObject uiControlsPlaystation;
    [SerializeField]
    GameObject uiControlsPlaystationUnlock;

    [SerializeField]
    string[] textIntros;
    [SerializeField]
    string[] textWin;
    [SerializeField]
    string[] textChase;
    [SerializeField]
    string[] textExtra;
    [SerializeField]
    string[] textPlayerCaught;
    [SerializeField]
    string[] textTargetCaught;
    [SerializeField]
    string[] textNoTarget;

    [SerializeField]
    private AudioClip[] audioClipsScreams;

    public enum State { Intro, Game, Outro }
    [HideInInspector]
    public State state = State.Intro;

    // Start is called before the first frame update
    void Start() {
        main = this;

        guards = FindObjectsOfType<Guard>();
        bars = FindObjectsOfType<Bars>();
        audioSource = GetComponent<AudioSource>();

        // Set the target to be a random prisoner
        prisoners = FindObjectsOfType<Prisoner>();
        prisoner = prisoners[Random.Range(0, prisoners.Length)];
        Debug.Log(prisoner.name, prisoner.gameObject);

        // Assign random screams to prisoners and player       
        List<AudioClip> screams = audioClipsScreams.OfType<AudioClip>().ToList();
        for (int i = 0; i < prisoners.Length; i++) {
            int s = Random.Range(0, screams.Count);
            prisoners[i].audioScream.clip = screams[s];
            screams.RemoveAt(s);
        }
        FindObjectOfType<Player>().audioScream.clip = screams[Random.Range(0, screams.Count)];
        audioSource.clip = prisoner.audioScream.clip;

        // Set up UI
        uiText.transform.parent.gameObject.SetActive(true);
        uiFader.color = new Color(uiFader.color.r, uiFader.color.g, uiFader.color.b, 1);
        uiText.text = "";
        StartCoroutine("WriteText", textIntros[Random.Range(0, textIntros.Length)]);
        uiTitle.gameObject.SetActive(SceneManager.GetActiveScene().buildIndex == 0);
        if (PlayerPrefs.GetInt("Completed") == 1)
            uiTitle.text = "The Breakout\n<i>Thanks for playing!";
        else
            uiTitle.text = "The Breakout";
    }

    // Update is called once per frame
    void Update() {
        if (uiFader.color.a > 0 && state != State.Outro) {
            uiFader.color = new Color(uiFader.color.r, uiFader.color.g, uiFader.color.b, uiFader.color.a - (Time.deltaTime * 0.5f));
        }

        if (state == State.Game) {
            // Controls
            uiControlsPC.SetActive(uiControlsMode == uiControlsModes.PC);
            uiControlsXbox.SetActive(uiControlsMode == uiControlsModes.Xbox);
            uiControlsPlaystation.SetActive(uiControlsMode == uiControlsModes.Playstation);
            uiControlsXboxUnlock.SetActive(Player.main.InteractAvailable());
            uiControlsPCUnlock.SetActive(Player.main.InteractAvailable());
            uiControlsPlaystationUnlock.SetActive(Player.main.InteractAvailable());

            // Check to see if the level is finished for whatever reason
            if (Player.main.exit && prisoner.state == Prisoner.State.Exit) {
                // Find out if any prisoners have escaped, or are in the process of doing so
                bool prisonersEscaping = false;
                bool prisonersEscaped = false;
                foreach (Prisoner prisonerCurrent in prisoners) {
                    if (prisonerCurrent != prisoner) {
                        if (prisonerCurrent.state == Prisoner.State.Exit || !prisonerCurrent.navMeshAgent.isOnNavMesh) {
                            prisonersEscaped = true;
                        }
                        else {
                            if (prisonerCurrent.navMeshAgent.isOnNavMesh) {
                                NavMeshPath path = new NavMeshPath();
                                prisonerCurrent.navMeshAgent.CalculatePath(finish.GetChild(0).position, path);
                                if (path.corners.Length > 0 && Vector3.Distance(finish.GetChild(0).position, path.corners[path.corners.Length - 1]) < 0.5f)
                                    prisonersEscaping = true;
                            }
                        }
                        if (prisonersEscaped && prisonersEscaping)
                            break;
                    }
                }
                // Show different finish screens
                if (!prisonersEscaping) {
                    if (prisonersEscaped) {
                        Finish(textExtra[Random.Range(0, textExtra.Length)], false);
                    }
                    else {
                        // Are there any guards chasing 
                        bool beingChased = false;
                        foreach(Guard guard in guards) {
                            if (guard.chasefollowTarget == Player.main.transform || guard.chasefollowTarget == prisoner.transform) {
                                beingChased = true;
                                break;
                            }
                        }
                        if (beingChased)
                            Finish(textChase[Random.Range(0, textChase.Length)], false);
                        else
                            Finish(textWin[Random.Range(0, textWin.Length)], false);
                    }
                }
            }
            else if (Player.main.exit) {
                // Is the target unable to escape
                NavMeshPath path = new NavMeshPath();
                prisoner.navMeshAgent.CalculatePath(finish.GetChild(0).position, path);
                if (!(path.corners.Length > 0 && Vector3.Distance(finish.GetChild(0).position, path.corners[path.corners.Length - 1]) < 0.5f)) {
                    Finish(textNoTarget[Random.Range(0, textNoTarget.Length)], true);
                }
            }
        }

        else if (state == State.Intro) {
            uiStartPC.SetActive(uiControlsMode == uiControlsModes.PC);
            uiStartXbox.SetActive(uiControlsMode == uiControlsModes.Xbox);
            uiStartPlaystation.SetActive(uiControlsMode == uiControlsModes.Playstation);
        }
    }

    public void Play() {
        // Hide controls
        uiStartPC.SetActive(false);
        uiStartPlaystation.SetActive(false);
        uiStartXbox.SetActive(false);

        state = State.Game;
        uiText.transform.parent.gameObject.SetActive(false);
        audioSource.Play();
        uiTitle.gameObject.SetActive(false);
    }

    void Finish(string text, bool fail) {
        if (state != State.Outro) {
            state = State.Outro;

            // Hide controls
            uiControlsPC.SetActive(false);
            uiControlsPlaystation.SetActive(false);
            uiControlsXbox.SetActive(false);

            // Gameover text
            uiText.transform.parent.gameObject.SetActive(true);
            uiText.text = "";
            StartCoroutine("WriteText", text);

            // Fade out and change level
            if (!fail) {
                if (SceneManager.GetActiveScene().buildIndex < SceneManager.sceneCountInBuildSettings - 1)
                    StartCoroutine("LoadLevel", SceneManager.GetActiveScene().buildIndex + 1);
                else {
                    StartCoroutine("LoadLevel", 0);
                    PlayerPrefs.SetInt("Completed", 1);
                }
            }
            else
                StartCoroutine("LoadLevel", SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Fail(bool player) {
        Player.main.rb.isKinematic = true;

        if (player)
            Finish(textPlayerCaught[Random.Range(0, textPlayerCaught.Length)], true);
        else
            Finish(textTargetCaught[Random.Range(0, textTargetCaught.Length)], true);
    }

    public float PathDistance(NavMeshPath path) {
        float val = 0;

        for (int i=0; i < path.corners.Length - 1; i++) {
            val += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return val;
    }

    IEnumerator WriteText(string text) {
        foreach(char c in text) {
            uiText.text += c;
            yield return new WaitForSeconds(0.002f);
        }
    }

    IEnumerator LoadLevel(int level) {
        yield return new WaitForSeconds(3f);
        while (uiFader.color.a < 1) {
            uiFader.color = new Color(uiFader.color.r, uiFader.color.g, uiFader.color.b, uiFader.color.a + (Time.deltaTime * 0.5f));
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(level);
    }
}
