using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using TMPro;
using UnityEngine;

#region Data Structures
// This local ExerciseConfig mirrors the structure sent by the client.
// (It uses the same ZoneSequenceItem type as defined in HMDDataReceiver.)
[System.Serializable]
public class ExerciseConfig
{
    public int ExerciseID;
    public string Name;
    public string LegsUsed;
    public int Intro;
    public int Demo;
    public int PreparationCop;
    public int TimingCop;
    public int Release;
    public int Switch;
    public int Sets;
    public HMDDataReceiver.ZoneSequenceItem[] ZoneSequence;
}
#endregion

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Test Settings")]
    public bool bypassClientConnect = true;

    [Header("UI Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI instructionText;

    [Header("Audio/Animation")]
    public AudioSource audioSource;
    public Animator characterAnimator;

    [Header("Visual Indicator")]
    public GameObject indicatorSphere; // (May be unused)

    // List of exercise configurations received from the client.
    public List<ExerciseConfig> exerciseConfigs = new List<ExerciseConfig>();

    // Reference to the currently active exercise configuration.
    private ExerciseConfig currentExercise;

    // Control flags.
    private bool preparationSuccessful = false;
    private bool restartExerciseRequested = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (bypassClientConnect)
        {
            CreateDummyExercises();
        }
        // In production, the client will send ExerciseConfig messages,
        // and UpdateExerciseConfiguration() will add them to exerciseConfigs.

        StartCoroutine(RunSequence());
    }

    // ------------------- Main Exercise Flow -------------------

    IEnumerator RunSequence()
    {
        yield return WaitForClientConnection();
        yield return RunIntroStep();
        yield return RunDemoStep();
        yield return RunPreparationPhase();
        yield return RunExerciseExecution();
        instructionText.text = "Pārejam uz nākamo vingrinājumu";
    }

    IEnumerator WaitForClientConnection() // wait for client to send tcp connection establish
    {
        if (bypassClientConnect)
        {
            instructionText.text = "Test režīms: klienta savienojums izlaists.";
            yield return new WaitForSeconds(1f);
        }
        else
        {
            instructionText.text = "Gaida savienojumu ar klientu...";
            while (HMDDataReceiver.Instance == null || !HMDDataReceiver.Instance.IsClientConnected)
            {
                yield return new WaitForSeconds(1f);
            }
            instructionText.text = "Klients savienots!";
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator RunIntroStep()
    {
        // use the Intro timing from the first exercise 
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        if (exerciseConfigs.Count > 0)
        {
            audioManager.Instance.PlayIntro();
            instructionText.text = "Esi sveicināts FIFA11+ treniņu programmā";
            yield return StartCountdown(exerciseConfigs[0].Intro);
        }
        else
        {
            yield return StartCountdown(50); // something wrong then
        }
    }

    IEnumerator RunDemoStep()
    {
        // Run demo animation using the first exercise as reference.
        FootOverlayManagerTwoFeet.Instance?.SetActiveFoot("none");
        if (exerciseConfigs.Count > 0)
        {
           
            currentExercise = exerciseConfigs[0];
            yield return DemonstrateExercise();
        }
        else
        {
            yield return StartCountdown(50);
        }
    }

    IEnumerator RunPreparationPhase()
    {
        preparationSuccessful = false;
        while (!preparationSuccessful)
        {
            if (currentExercise != null) { 
                audioManager.Instance.PlayPreparation();
            instructionText.text = "Nostajies uz LABĀS kājas";
                
                
            }
            else
                instructionText.text = "Sagatavojies uzdevumam!";
            UpdateActiveFootDisplay();
       
            bool isRightLeg = currentExercise.LegsUsed.ToLower() == "right";
            characterAnimator.SetBool("IsLeftLeg", !isRightLeg);
            yield return new WaitForEndOfFrame();
            characterAnimator.SetTrigger("StartExercise");
            yield return FreezeAtLastFrame(currentExercise.PreparationCop);
            characterAnimator.ResetTrigger("StartExercise");
            ReturnToIdle();
            // if (bypassClientConnect)
            // {
            preparationSuccessful = true;
            
            // }
            // else
            // {
            //     yield return new WaitForSeconds(1f);
            //     if (!preparationSuccessful)
            //     {
            //         instructionText.text = "Sagatavošanās neizdevās. Lūdzu mēģiniet vēlreiz.";
            //         yield return new WaitForSeconds(2f);
            //     }
            // }
        }
    }


    IEnumerator RunExerciseExecution()
    {
        int reps = 0;
        int configIndex = 0; // To switch between two configurations

        while (reps < 4)
        {

            bool exerciseCompleted = false;
            currentExercise = exerciseConfigs[configIndex]; // Select the current config

            while (!exerciseCompleted)
            {
                bool isRightLeg = currentExercise.LegsUsed.ToLower() == "right";
                instructionText.text = "Noturi līdzsvaru 30 sekundes!";
                yield return StartCountdown(2); // new delay for the text above
                UpdateActiveFootDisplay();

                // Execute the exercise animation and check for restart.
                yield return ExecuteExerciseAnimation(!isRightLeg);

                if (restartExerciseRequested)
                {
                    restartExerciseRequested = false;
                    instructionText.text = "Balanss zaudēts, atkārtojiet pēc 5 sekundēm!";
                    yield return new WaitForSeconds(4f); // Wait before restarting
                }
                else
                {
                    exerciseCompleted = true;
                }
            }

            // release phase.
            audioManager.Instance.PlayReleaseLeg();
            instructionText.text = "Nostājies uz ABĀM kājām";
            yield return StartCountdown(currentExercise.Release);

            
            reps++;
            configIndex = (configIndex + 1) % 2;

            yield return WaitForExerciseConfigs(); // load for exercise 2 to know which leg to switch to

            
            UpdateActiveFootDisplay();


            if (reps == 4)
            {
                instructionText.text = "Nākamais vingrinājums - test";
            yield return StartCountdown(currentExercise.Release);
            }
            else { 
            if (exerciseConfigs[configIndex].LegsUsed.ToLower() == "left")
            {
                    audioManager.Instance.PlaySwitchLeg(true, 1);
                    instructionText.text = "Nostājies uz KREISĀS kājas.";
                yield return StartCountdown(currentExercise.Switch);
            }
            else
            {
                    audioManager.Instance.PlaySwitchLeg(false, 2);
                    instructionText.text = "Nostājies uz LABĀS kājas.";
                yield return StartCountdown(currentExercise.Switch);
            }
   
            }
        }

    }
    IEnumerator WaitForExerciseConfigs()
    {
        // Wait until at least one new config is received.
        while (exerciseConfigs.Count == 1)
        {
            yield return new WaitForSeconds(0.5f);
        }

    }



    IEnumerator ExecuteExerciseAnimation(bool isLeftLeg)
    {
        characterAnimator.SetBool("IsLeftLeg", isLeftLeg);
        yield return new WaitForEndOfFrame();
        characterAnimator.SetTrigger("StartExercise");
        yield return FreezeAtLastFrame(currentExercise.TimingCop);

        // check if restart was requested during the countdown
        if (restartExerciseRequested)
            yield break;

        ReturnToIdle();
    }







    IEnumerator StartCountdown(int seconds)
    {
        float timer = seconds;
        while (timer > 0f)
        {
            countdownText.text = Mathf.CeilToInt(timer).ToString();
            timer -= Time.deltaTime;
            yield return null;
        }
        countdownText.text = "0";
    }

    // ------------------- Animation Routines -------------------

    IEnumerator DemonstrateExercise()
    {
        audioManager.Instance.PlayDemo();
        instructionText.text = "Vingrojums: Stāvēšana uz vienas kājas 30 sekundes";
        bool isRightLeg = currentExercise.LegsUsed.ToLower() == "right";
        characterAnimator.SetBool("IsLeftLeg", !isRightLeg);
        yield return new WaitForEndOfFrame();
        characterAnimator.SetTrigger("StartExercise");
        yield return FreezeAtLastFrame(currentExercise.Demo);
        characterAnimator.ResetTrigger("StartExercise");
        ReturnToIdle();
    }

    IEnumerator FreezeAtLastFrame(int holdSeconds)
    {
        float waitTime = 0f;
        while (!IsInCorrectAnimationState() && waitTime < 2f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }
        if (waitTime < 2f)
        {
            yield return new WaitWhile(() =>
                characterAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f
            );
        }

        characterAnimator.speed = 0; // Freeze animation

        float timer = holdSeconds;
        while (timer > 0f && !restartExerciseRequested)
        {
            countdownText.text = Mathf.CeilToInt(timer).ToString();
            timer -= Time.deltaTime;
            yield return null;
        }

        characterAnimator.speed = 1; // Resume animation
        countdownText.text = "0";
    }

    bool IsInCorrectAnimationState()
    {
        if (currentExercise == null) return false;
        string targetState = currentExercise.LegsUsed.ToLower() == "right" ? "OneStand_Right" : "OneStand_Left";
        return characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(targetState);
    }

    void ReturnToIdle()
    {
        characterAnimator.ResetTrigger("Idle");
        characterAnimator.SetTrigger("Idle");
    }

    // ------------------- End Animation Routines -------------------

    // ------------------- Methods Called from HMDDataReceiver -------------------

    /// <summary>
    /// Called when an exercise configuration message is received from the client.
    /// </summary>
    /// <param name="config">The exercise configuration message.</param>
    public void UpdateExerciseConfiguration(HMDDataReceiver.ExerciseConfigMessage config)
    {
        Debug.Log($"Saņemta exercise config: ExerciseID {config.ExerciseID}, LegsUsed: {config.LegsUsed}");
        // convert the received message into our local ExerciseConfig structure.
        ExerciseConfig newConfig = new ExerciseConfig
        {
            ExerciseID = config.ExerciseID,
            Name = config.Name,
            LegsUsed = config.LegsUsed,
            Intro = config.Intro,
            Demo = config.Demo,
            PreparationCop = config.PreparationCop,
            TimingCop = config.TimingCop,
            Release = config.Release,
            Switch = config.Switch,
            Sets = config.Sets,
            ZoneSequence = config.ZoneSequence
        };
        // add to the list if not already present.
        if (!exerciseConfigs.Exists(e => e.ExerciseID == newConfig.ExerciseID))
        {
            exerciseConfigs.Add(newConfig);
        }
    }

    /// <summary>
    /// Called when a feedback message is received from the client.
    /// </summary>
    /// <param name="zone">The zone code.</param>
    /// <param name="foot">Which foot ("Left", "Right", or "Both")</param>
    public void UpdateFootStatusForFoot(int zone, string foot)
    {
        if (FootOverlayManagerTwoFeet.Instance != null)
        {
            FootOverlayManagerTwoFeet.Instance.UpdateOverlayForZone(zone, foot);
        }

        // Update instruction text as before...
        if (foot.ToLower() == "left")
        {
            instructionText.text = GetSingleZoneMessage(zone);
        }
        else if (foot.ToLower() == "right")
        {
            instructionText.text = GetSingleZoneMessage(zone);
        }
        else if (foot.ToLower() == "both")
        {
            instructionText.text = GetSingleZoneMessage(zone);
        }
    }

    void UpdateActiveFootDisplay()
    {
        if (currentExercise != null && FootOverlayManagerTwoFeet.Instance != null)
        {
            FootOverlayManagerTwoFeet.Instance.SetActiveFoot(currentExercise.LegsUsed);
        }
    }


    void SetFootGradientForFoot(int zone, Transform footTransform)
    {
        if (footTransform == null)
            return;
        Renderer footRenderer = footTransform.GetComponent<Renderer>();
        if (footRenderer == null || footRenderer.material == null)
            return;

        Color defaultColor = Color.green;
        Color leftColor = defaultColor;
        Color rightColor = defaultColor;
        Color topColor = defaultColor;
        Color bottomColor = defaultColor;

        switch (zone)
        {
            case 1:
                // Correct balance; leave as default.
                break;
            case 2:
                leftColor = rightColor = topColor = bottomColor = Color.red;
                break;
            case 3:
                bottomColor = Color.red;
                break;
            case 4:
                topColor = Color.red;
                break;
            case 5:
                rightColor = Color.red;
                break;
            case 6:
                leftColor = Color.red;
                break;
            case 7:
                RequestExerciseRestart();
                leftColor = rightColor = topColor = bottomColor = Color.red;
                break;
            default:
                break;
        }

        footRenderer.material.SetColor("_LeftColor", leftColor);
        footRenderer.material.SetColor("_RightColor", rightColor);
        footRenderer.material.SetColor("_TopColor", topColor);
        footRenderer.material.SetColor("_BottomColor", bottomColor);
    }

    public string GetSingleZoneMessage(int zone)
    {
        audioManager.Instance.PlayExerciseZoneVoice(zone);
        switch (zone)
        {
            case 1:
                return "Tev lieliski izdodas!";
            case 2:
                return "Nostāties pareizi";
            case 3:
                return "Pārvirzi svaru uz priekšu!";
            case 4:
                return "Pārvirzi svaru uz aizmuguri!";
            case 5:
                return "Pavirzi svaru pa labi!";
            case 6:
                return "Pavirzi svaru pa kreisi!";
            case 7:
                RequestExerciseRestart();
                return "Līdzsvars zaudēts, sāc no sākuma!";
            default:
                return "Nezināma zona.";
        }
    }

    public void MarkPreparationSuccessful()
    {
        preparationSuccessful = true;
    }
  

    public void RequestExerciseRestart()
    {
        restartExerciseRequested = true;
    }

    // ------------------- Dummy/Default Exercise Creation -------------------
    void CreateDummyExercises()
    {
        // Create two dummy exercise configurations: one for right leg and one for left leg.
        HMDDataReceiver.ExerciseConfigMessage configRight = new HMDDataReceiver.ExerciseConfigMessage
        {
            MessageType = "ExerciseConfig",
            ExerciseID = 1,
            Name = "Single-Leg Stance - Right Leg",
            LegsUsed = "right",
            Intro = 1,
            Demo = 3,
            PreparationCop = 3,
            TimingCop = 3,
            Release = 3,
            Switch = 3,
            Sets = 2,
            ZoneSequence = new HMDDataReceiver.ZoneSequenceItem[]
            {
                new HMDDataReceiver.ZoneSequenceItem
                {
                    Duration = 30,
                    GreenZoneX = new Vector2(-1f, 1f),
                    GreenZoneY = new Vector2(-1f, 1f),
                    RedZoneX = new Vector2(-2f, -1f),
                    RedZoneY = new Vector2(-6f, -1.1f)
                }
            }
        };

        HMDDataReceiver.ExerciseConfigMessage configLeft = new HMDDataReceiver.ExerciseConfigMessage
        {
            MessageType = "ExerciseConfig",
            ExerciseID = 2,
            Name = "Single-Leg Stance - Left Leg",
            LegsUsed = "left",
            Intro = 1,
            Demo = 3,
            PreparationCop = 3,
            TimingCop = 3,
            Release = 3,
            Switch = 3,
            Sets = 2,
            ZoneSequence = new HMDDataReceiver.ZoneSequenceItem[]
            {
                new HMDDataReceiver.ZoneSequenceItem
                {
                    Duration = 30,
                    GreenZoneX = new Vector2(-1f, 1f),
                    GreenZoneY = new Vector2(-1f, 1f),
                    RedZoneX = new Vector2(1f, 2f),
                    RedZoneY = new Vector2(-6f, -1.1f)
                }
            }
        };

        UpdateExerciseConfiguration(configRight);
        UpdateExerciseConfiguration(configLeft);
    }
}
