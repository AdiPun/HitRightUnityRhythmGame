using System.Collections.Generic;
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

    private int m_lastBeat = 0;
    private int m_activeBeat = -1;
    private bool m_hasBeenInActiveBeat = false;

    private float m_nextBeatStartBeat;
    private float m_nextBeatEndBeat;

    private List<InputHit> m_inputHits = new List<InputHit>();
    private const int MaxHits = 32;

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

    private void CheckForNewBeat()
    {
        int beatIndex = Mathf.FloorToInt(m_musicPlayer.GetElapsedTimeInBeats());

        if (beatIndex != m_lastBeat)
        {
            m_lastBeat = beatIndex;
            m_beatEvent.Invoke(m_lastBeat);
        }
    }

    private void UpdateActiveBeatWindow()
    {
        if (m_judge == null) return;

        float beatMs = m_musicPlayer.GetBeatDurationMs();
        float targetMs = m_judge.GetCurrentTargetBeat() * beatMs;
        float marginMs = m_judge.GetMarginMs();

        m_nextBeatStartBeat = (targetMs - marginMs) / beatMs;
        m_nextBeatEndBeat = (targetMs + marginMs) / beatMs;
    }

    private void HandleActiveBeat()
    {
        float timeBeats = m_musicPlayer.GetElapsedTimeInBeats();
        bool isInActiveBeat = timeBeats >= m_nextBeatStartBeat && timeBeats <= m_nextBeatEndBeat;

        if (isInActiveBeat && !m_hasBeenInActiveBeat)
        {
            m_activeBeat = (m_lastBeat % 4) + 1;
            m_enterBeatEvent.Invoke(m_activeBeat);
            m_hasBeenInActiveBeat = true;
        }
        else if (!isInActiveBeat && m_hasBeenInActiveBeat)
        {
            m_exitBeatEvent.Invoke(m_activeBeat);
            m_activeBeat = -1;
            m_hasBeenInActiveBeat = false;
        }
    }

    public void RecordInput(float deltaMs)
    {
        m_inputHits.Add(new InputHit
        {
            timeMs = m_musicPlayer.GetElapsedTimeInMs(),
            deltaMs = deltaMs
        });

        if (m_inputHits.Count > MaxHits)
            m_inputHits.RemoveAt(0);
    }

    public int GetActiveBeat() => m_activeBeat;
    public int GetElapsedBeats() => m_lastBeat;

    struct InputHit
    {
        public float timeMs;
        public float deltaMs;
    }

    void OnGUI()
    {
        const float barWidth = 600f;
        const float barHeight = 20f;
        const float x = 20f;
        const float y = 20f;

        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float beatMs = m_musicPlayer.GetBeatDurationMs();
        float windowMs = beatMs * 4f;
        float viewStart = nowMs - windowMs * 0.5f;
        float viewEnd = nowMs + windowMs * 0.5f;

        // Background
        GUI.color = Color.gray;
        GUI.Box(new Rect(x, y, barWidth, barHeight), "");

        // Beat grid
        GUI.color = Color.white;
        int firstBeat = Mathf.FloorToInt(viewStart / beatMs);
        int lastBeat = Mathf.CeilToInt(viewEnd / beatMs);

        for (int i = firstBeat; i <= lastBeat; i++)
        {
            float beatT = Mathf.InverseLerp(viewStart, viewEnd, i * beatMs);
            float bx = x + beatT * barWidth;
            GUI.Box(new Rect(bx - 1f, y, 2f, barHeight), "");
        }

        // Active beat window
        float startT = Mathf.InverseLerp(viewStart, viewEnd, m_nextBeatStartBeat * beatMs);
        float endT = Mathf.InverseLerp(viewStart, viewEnd, m_nextBeatEndBeat * beatMs);
        GUI.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        GUI.Box(new Rect(x + startT * barWidth, y, (endT - startT) * barWidth, barHeight), "");

        // Input hits
        foreach (var hit in m_inputHits)
        {
            float hitT = Mathf.InverseLerp(viewStart, viewEnd, hit.timeMs);
            if (hitT < 0f || hitT > 1f) continue;

            float hx = x + hitT * barWidth;
            GUI.color = Mathf.Abs(hit.deltaMs) <= 30f ? Color.cyan
                      : Mathf.Abs(hit.deltaMs) <= 60f ? Color.green
                      : Color.red;

            GUI.Box(new Rect(hx - 2f, y - 10f, 4f, barHeight + 20f), "");
            GUI.Label(new Rect(hx - 30f, y + barHeight + 10f, 60f, 20f), $"{hit.deltaMs:+0;-0}ms");
        }

        // Now cursor
        GUI.color = Color.yellow;
        GUI.Box(new Rect(x + barWidth * 0.5f - 2f, y - 15f, 4f, barHeight + 30f), "");

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y + 40f, 800f, 20f),
            $"Time: {nowMs:0} ms | Beat: {m_lastBeat} | Active: {m_activeBeat}");
    }
}