using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;

public class Metronome : MonoBehaviour
{
    [SerializeField] private MusicPlayer m_musicPlayer;

    [Header("Timing Windows (ms)")]
    [SerializeField] private float m_marginMs = 100f;
    [SerializeField] private float m_lateTimingMs = 60f;
    [SerializeField] private float m_perfectTimingMs = 20f;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> m_beatEvent;
    [SerializeField] private UnityEvent<int> m_exitBeatEvent;

    private float m_beatDurationMs;

    private int m_elapsedBeats;
    private int m_lastInterval;

    private int m_lastBeat;
    private int m_activeBeat = -1;

    private float m_activeBeatStartPositionMs;
    private float m_activeBeatEndPositionMs;

    private bool m_wasInActiveBeat;

    void Start()
    {
        m_beatDurationMs = m_musicPlayer.GetBeatDurationMs();
        m_elapsedBeats = 0;
        m_lastInterval = 0;

        UpdateActiveBeatWindow();
    }

    void Update()
    {
        float beatTime = m_musicPlayer.GetElapsedTimeInBeats();

        CheckForNewBeat(beatTime);
        HandleActiveBeat();

    }

    // --- Beat Progression ---
    public void CheckForNewBeat(float beatTime)
    {
        // This is rounding the interval down to an int and once it changes, we know we've moved to the next beat
        // if we have, we trigger an event
        int interval = Mathf.FloorToInt(beatTime);

        if (interval != m_lastInterval)
        {
            // Set lastInterval to the floored int
            m_lastInterval = interval;
            // Increment beat by one
            m_elapsedBeats++;

            // Last beat is modulo 4
            m_lastBeat = (m_elapsedBeats % 4) + 1;

            // Set error margins
            UpdateActiveBeatWindow();
            m_beatEvent.Invoke(m_elapsedBeats); // We're using event triggers so that we can easily drag other things in the inspector to be triggered by this beat script
            Debug.Log("Last Beat: " + m_lastBeat);
            Debug.Log("Active Beat: " + m_activeBeat);
        }
    }

    private void UpdateActiveBeatWindow()
    {
        float beatCenterMs = m_elapsedBeats * m_beatDurationMs;

        m_activeBeatStartPositionMs = beatCenterMs - m_marginMs;
        m_activeBeatEndPositionMs = beatCenterMs + m_marginMs;
    }


    // --- Active Beat ---

    // If the audio's position is within the margins, m_activeBeat is set to the m_lastBeat otherwise it's -1
    public void HandleActiveBeat()
    {
        float timeMs = m_musicPlayer.GetElapsedTimeInMs();

        bool isInActiveBeat = timeMs >= m_activeBeatStartPositionMs && timeMs <= m_activeBeatEndPositionMs;

        if (isInActiveBeat && !m_wasInActiveBeat) // If we just entered a beat
        {
            Debug.Log("Entering Beat: " + m_lastBeat);
            m_activeBeat = m_lastBeat;
        }

        if (m_wasInActiveBeat && !isInActiveBeat) // If we exit the beat we were in
        {
            Debug.Log("Exiting Beat" + m_activeBeat);
            m_activeBeat = -1;
            m_exitBeatEvent.Invoke(m_lastBeat); // Judge listens to this to know when to miss
        }

        m_wasInActiveBeat = isInActiveBeat; // Set this to false if we're not in active beat
    }

    // --- Getters ---
    public int GetElapsedBeats() => m_elapsedBeats;

    public int GetActiveBeat() => m_activeBeat;

    public InputOutcome GetInputTimingOutcome()
    {
        float inputTimeMs = m_musicPlayer.GetElapsedTimeInMs();

        float beatCenterMs = (m_activeBeatStartPositionMs + m_activeBeatEndPositionMs) * 0.5f;

        float deltaMs = inputTimeMs - beatCenterMs;
        float absDeltaMs = Mathf.Abs(deltaMs); // Judging based on how large the difference is between the input time and the beat center

        if (absDeltaMs <= m_perfectTimingMs)
        {
            return InputOutcome.Perfect;
        }

        if (absDeltaMs <= m_lateTimingMs)
        {
            return InputOutcome.Hit;
        }

        if (absDeltaMs <= m_marginMs)
        {
            return deltaMs < 0 ? InputOutcome.Early : InputOutcome.Late;
        }

        return InputOutcome.Miss;
    }

}
