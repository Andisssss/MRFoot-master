using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExerciseAudioClips
{
    [Tooltip("ID of the exercise to which these audio clips belong.")]
    public int exerciseID;
    [Tooltip("Dynamic demo clip for this exercise.")]
    public AudioClip Demo_1;
    [Tooltip("Dynamic instruction clip for this exercise.")]
    public AudioClip instructionClip;
    [Tooltip("Dynamic switch leg clip when switching to left leg for this exercise.")]
    public AudioClip LeftLeg;
    [Tooltip("Dynamic switch leg clip when switching to right leg for this exercise.")]
    public AudioClip RightLeg;
}

public class audioManager : MonoBehaviour
{
    public static audioManager Instance;

    [Header("Audio Source")]
    [Tooltip("The AudioSource component used to play all audio clips. Attach a component here.")]
    public AudioSource audioSource; // This component plays the sound.

    [Header("Primary Audio Clips (Default)")]
    [Tooltip("Default audio clip for the intro step.")]
    public AudioClip Intro;
    [Tooltip("Default audio clip for the demo step.")]
    public AudioClip Demo_1;
    [Tooltip("Default audio clip for the preparation phase.")]
    public AudioClip Prep;
    public AudioClip Hold;
    [Tooltip("Default audio clip for the release leg phase.")]
    public AudioClip Release;
    [Tooltip("Default generic instruction audio clip.")]
    public AudioClip instructionClip;

    [Header("Switch Leg Audio Clips (Default)")]
    [Tooltip("Default audio clip for switching to left leg.")]
    public AudioClip LeftLeg;
    [Tooltip("Default audio clip for switching to right leg.")]
    public AudioClip RightLeg;

    [Header("Exercise Zone Voice Clips")]
    [Tooltip("List of voice clips for in-exercise zone feedback (zone 1 = index 0, zone 2 = index 1, etc.). Set the size and assign each clip accordingly.")]
    public List<AudioClip> exerciseZoneClips = new List<AudioClip>();

    [Header("Dynamic Exercise Audio Clips")]
    [Tooltip("List of audio clips specific to each exercise (by ExerciseID).")]
    public List<ExerciseAudioClips> exerciseAudioClips = new List<ExerciseAudioClips>();

    void Awake()
    {
        // Singleton setup: ensures one AudioManager instance persists.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist between scenes.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Public Playback Methods

    /// <summary>
    /// Plays the intro audio clip.
    /// </summary>
    public void PlayIntro()
    {
        // Plays the default intro clip.
        PlayClip(Intro, stopPrevious: false);
    }

    /// <summary>
    /// Plays the demo audio clip. If an exerciseID is provided and a dynamic clip exists, that clip is used.
    /// </summary>
    /// <param name="exerciseID">Optional exercise identifier.</param>
    public void PlayDemo(int exerciseID = -1)
    {
        if (exerciseID != -1)
        {
            ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == exerciseID);
            if (ea != null && ea.Demo_1 != null)
            {
                PlayClip(ea.Demo_1, stopPrevious: false);
                return;
            }
        }
        // Fallback to default demo clip.
        PlayClip(Demo_1, stopPrevious: false);
    }

    /// <summary>
    /// Plays the preparation phase audio clip.
    /// </summary>
    public void PlayPreparation()
    {
        PlayClip(RightLeg, stopPrevious: false);
    }

    public void PlayHold()
    {
        PlayClip(Hold, stopPrevious: false);
    }

    /// <summary>
    /// Plays the release leg phase audio clip.
    /// </summary>
    public void PlayReleaseLeg()
    {
        PlayClip(Release, stopPrevious: false);
    }

    /// <summary>
    /// Plays the switch leg audio clip based on the current leg.
    /// If an exerciseID is provided and a dynamic clip exists, that clip is used.
    /// </summary>
    /// <param name="isLeftLeg">True if switching to left leg; otherwise false.</param>
    /// <param name="exerciseID">Optional exercise identifier.</param>
    public void PlaySwitchLeg(bool isLeftLeg, int exerciseID = -1)
    {
        if (exerciseID != -1)
        {
            ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == exerciseID);
            if (ea != null)
            {
                if (isLeftLeg && ea.LeftLeg != null)
                {
                    PlayClip(ea.LeftLeg, stopPrevious: false);
                    return;
                }
                else if (!isLeftLeg && ea.RightLeg != null)
                {
                    PlayClip(ea.RightLeg, stopPrevious: false);
                    return;
                }
            }
        }
        // Fallback to default switch leg clips.
        if (isLeftLeg && LeftLeg != null)
        {
            PlayClip(LeftLeg, stopPrevious: false);
        }
        else if (!isLeftLeg && RightLeg != null)
        {
            PlayClip(RightLeg, stopPrevious: false);
        }
        else
        {
            Debug.LogWarning("AudioManager: No switch leg clip found for " + (isLeftLeg ? "left" : "right") + " leg.");
        }
    }

    /// <summary>
    /// Plays a generic instruction audio clip. If an exerciseID is provided and a dynamic clip exists, that clip is used.
    /// </summary>
    /// <param name="exerciseID">Optional exercise identifier.</param>
    public void PlayInstruction(int exerciseID = -1)
    {
        if (exerciseID != -1)
        {
            ExerciseAudioClips ea = exerciseAudioClips.Find(x => x.exerciseID == exerciseID);
            if (ea != null && ea.instructionClip != null)
            {
                PlayClip(ea.instructionClip, stopPrevious: false);
                return;
            }
        }
        // Fallback to default instruction clip.
        PlayClip(instructionClip, stopPrevious: false);
    }

    /// <summary>
    /// Plays the in-exercise voice clip for a given zone exclusively.
    /// This stops any previous zone voice so that audio doesn't overlap.
    /// Expects zone numbers starting at 1.
    /// </summary>
    /// <param name="zone">The zone number (e.g., 1, 2, 3...)</param>
    public void PlayExerciseZoneVoice(int zone)
    {
        int index = zone - 1; // Adjust for 0-indexed list.
        if (index >= 0 && index < exerciseZoneClips.Count)
        {
            PlayExclusiveClip(exerciseZoneClips[index]);
        }
        else
        {
            Debug.LogWarning("AudioManager: Invalid zone number: " + zone);
        }
    }

    #endregion

    /// <summary>
    /// Helper method to play a clip. If stopPrevious is true, stops any currently playing clip before playing.
    /// Uses PlayOneShot (which normally allows overlapping).
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="stopPrevious">If true, stops any currently playing clip before playing.</param>
    private void PlayClip(AudioClip clip, bool stopPrevious)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Tried to play a null clip.");
            return;
        }

        if (stopPrevious && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Helper method to play a clip exclusively.
    /// Stops any currently playing audio before assigning the new clip and playing it.
    /// This method is used for zone audio so that new voice instructions cut off the previous one.
    /// </summary>
    /// <param name="clip">The clip to play exclusively.</param>
    private void PlayExclusiveClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Tried to play a null exclusive clip.");
            return;
        }
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
