using NUnit.Framework;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;

public class Metronome : MonoBehaviour
{
    [SerializeField] private MusicPlayer m_musicPlayer;

    [Header("Timing Windows (ms)")]
    [SerializeField] private float m_margin = 0.1f;
    [SerializeField] private float m_lateTiming = 0.06f;
    [SerializeField] private float m_perfectTiming = 0.02f;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> m_beatEvent;
    [SerializeField] private UnityEvent<int> m_enterBeatEvent;
    [SerializeField] private UnityEvent<int> m_exitBeatEvent;

    private int m_lastBeat = 0;
    private int m_activeBeat = -1;

    private float m_activeBeatStartPosition;
    private float m_activeBeatEndPosition;
    private bool m_hasBeenInActiveBeat = false;

    void Start()
    {
        UpdateActiveBeatWindow();
    }

    void Update()
    {
        CheckForNewBeat();

        UpdateActiveBeatWindow();

        HandleActiveBeat();
    }

    // --- Beat Progression ---
    public void CheckForNewBeat()
    {
        float elapsedBeats = m_musicPlayer.GetElapsedTimeInBeats();
        int beatIndex = Mathf.FloorToInt(elapsedBeats);

        if (beatIndex != m_lastBeat)
        {
            m_lastBeat = beatIndex;
            m_beatEvent.Invoke((beatIndex % 4) + 1);
        }
    }

    private void UpdateActiveBeatWindow()
    {
        float beatCenter = m_lastBeat;

        m_activeBeatStartPosition = beatCenter - m_margin;
        m_activeBeatEndPosition = beatCenter + m_margin;
    }



    // --- Active Beat ---

    // If the audio's position is within the margins, m_activeBeat is set to the m_lastBeat otherwise it's -1
    public void HandleActiveBeat()
    {
        float timeBeats = m_musicPlayer.GetElapsedTimeInBeats();

        // Within active beat window
        bool isInActiveBeat =
            timeBeats >= m_activeBeatStartPosition &&
            timeBeats <= m_activeBeatEndPosition;

        // Enter active beat and fire once
        if (isInActiveBeat && !m_hasBeenInActiveBeat)
        {
            m_activeBeat = m_lastBeat;
            m_enterBeatEvent.Invoke(m_activeBeat);
            m_hasBeenInActiveBeat = true;
            Debug.Log("Entered active beat: " + m_activeBeat);
        }
        // Exit active beat and fire once
        else if (!isInActiveBeat && m_hasBeenInActiveBeat)
        {
            m_exitBeatEvent.Invoke(m_activeBeat);
            m_activeBeat = -1;
            m_hasBeenInActiveBeat = false;
            Debug.Log("Exited active beat");
        }
    }


    // --- Getters ---
    public int GetActiveBeat() => m_activeBeat;

    public InputOutcome GetInputTimingOutcome()
    {
        float inputBeat = m_musicPlayer.GetElapsedTimeInBeats();
        float beatCenter = m_lastBeat;

        float delta = inputBeat - beatCenter;
        float absDelta = Mathf.Abs(delta);

        if (absDelta <= m_perfectTiming)
            return InputOutcome.Perfect;

        if (absDelta <= m_lateTiming)
            return InputOutcome.Hit;

        if (absDelta <= m_margin)
            return delta < 0 ? InputOutcome.Early : InputOutcome.Late;

        return InputOutcome.Miss;
    }


    void OnGUI()
    {
        const float barWidth = 600f;
        const float barHeight = 20f;
        const float x = 20f;
        const float y = 20f;

        float timeBeats = m_musicPlayer.GetElapsedTimeInBeats();

        float viewStart = m_lastBeat - 1f;
        float viewEnd = m_lastBeat + 1f;

        float t = Mathf.InverseLerp(viewStart, viewEnd, timeBeats);

        // Background
        GUI.color = Color.gray;
        GUI.Box(new Rect(x, y, barWidth, barHeight), "");

        // Active beat window
        GUI.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        GUI.Box(
            new Rect(
                x,
                y,
                barWidth,
                barHeight
            ),
            ""
        );

        // Current time cursor
        GUI.color = Color.red;
        GUI.Box(
            new Rect(
                x + t * barWidth - 2f,
                y - 5f,
                4f,
                barHeight + 10f
            ),
            ""
        );

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y + 30, 800, 20),
            $"Time: {timeBeats:0} beats | Beat: {m_lastBeat} | Active: {m_activeBeat}"
        );
    }


}
