using NUnit.Framework;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;

public class Metronome : MonoBehaviour
{
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Judge m_judge;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> m_beatEvent;
    [SerializeField] private UnityEvent<int> m_enterBeatEvent;
    [SerializeField] private UnityEvent<int> m_exitBeatEvent;

    [SerializeField] private int m_lastBeat = 0;
    [SerializeField] private int m_activeBeat = -1;

    private float m_nextBeatBeat;
    private float m_nextBeatStartBeat;
    private float m_nextBeatEndBeat;

    [SerializeField] private bool m_hasBeenInActiveBeat = false;

    void Start()
    {
        UpdateActiveBeatWindow();
    }

    void Update()
    {
        CheckForNewBeat();
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
            UpdateActiveBeatWindow();

            m_beatEvent.Invoke(m_lastBeat);
        }
    }

    private void UpdateActiveBeatWindow()
    {
        m_nextBeatBeat = m_lastBeat + 1f;

        float marginBeats = m_judge.GetMarginMs() / m_musicPlayer.GetBeatDurationMs();

        m_nextBeatStartBeat = m_nextBeatBeat - marginBeats;
        m_nextBeatEndBeat = m_nextBeatBeat + marginBeats;
    }


    // --- Active Beat ---

    // If the audio's position is within the margins, m_activeBeat is set to the m_lastBeat otherwise it's -1
    public void HandleActiveBeat()
    {
        float timeBeats = m_musicPlayer.GetElapsedTimeInBeats();

        bool isInActiveBeat =
            timeBeats >= m_nextBeatStartBeat &&
            timeBeats <= m_nextBeatEndBeat;

        // Enter active beat and fire once
        if (isInActiveBeat && !m_hasBeenInActiveBeat)
        {
            m_activeBeat = ((m_lastBeat) % 4) + 1;
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
        }
    }

    // --- Getters ---
    public int GetActiveBeat() => m_activeBeat;
    public int GetElapsedBeats() => m_lastBeat;

    void OnGUI()
    {
        const float barWidth = 600f;
        const float barHeight = 20f;
        const float x = 20f;
        const float y = 20f;

        float timeMs = m_musicPlayer.GetElapsedTimeInMs();
        float beatMs = m_musicPlayer.GetBeatDurationMs();

        float viewCenter = (m_lastBeat + 1f) * beatMs;
        float viewStart = viewCenter - beatMs;
        float viewEnd = viewCenter + beatMs;


        float t = Mathf.InverseLerp(viewStart, viewEnd, timeMs);

        // Background
        GUI.color = Color.gray;
        GUI.Box(new Rect(x, y, barWidth, barHeight), "");

        // Active beat window
        float startT = Mathf.InverseLerp(
            viewStart,
            viewEnd,
            m_nextBeatStartBeat * beatMs
        );

        float endT = Mathf.InverseLerp(
            viewStart,
            viewEnd,
            m_nextBeatEndBeat * beatMs
        );

        GUI.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        GUI.Box(
            new Rect(
                x + startT * barWidth,
                y,
                (endT - startT) * barWidth,
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
        GUI.Label(
            new Rect(x, y + 30, 800, 20),
            $"Time: {timeMs:0} ms | Beat: {m_lastBeat} | Active: {m_activeBeat}"
        );
    }


}
