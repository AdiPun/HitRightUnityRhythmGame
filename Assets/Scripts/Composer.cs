using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Composer : MonoBehaviour
{
    public List<RequiredGoal> m_chart { get; private set; } = new();

    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Judge m_judge;
    [SerializeField] private UnityEvent<RequiredGoal> m_sendNextGoalEvent;
    [SerializeField] private int m_leadInBeats = 4;

    private int m_nextGoalIndex = 0;

    void Start()
    {
        CreateLevelChart();
        Reset();

        // Send the first goal to the Judge so it knows what to listen for.
        // NoteSpawner handles its own spawning via Update(); do not spawn here.
        if (m_chart.Count > 0)
        {
            m_sendNextGoalEvent?.Invoke(m_chart[0]);
        }
    }

    public void Reset()
    {
        m_nextGoalIndex = 0;
    }

    public void CreateLevelChart()
    {
        m_chart.Clear();

        float trackLength = m_musicPlayer.GetTrackLengthSeconds();
        float beatDuration = m_musicPlayer.GetBeatDurationSeconds();
        int totalBeats = Mathf.FloorToInt(trackLength / beatDuration);

        for (int i = 0; i < totalBeats; i++)
        {
            // Example pattern: every 8th is a multi, every 4th is a hold, rest are taps
            NoteType type = NoteType.Tap;
            float holdBeats = 0f;
            List<InputLane> lanes = new List<InputLane> { InputLane.Lane3 };

            if (i > 0 && i % 8 == 0)
            {
                type = NoteType.Multi;
                lanes = new List<InputLane> { InputLane.Lane2, InputLane.Lane3 };
            }
            else if (i > 0 && i % 4 == 0)
            {
                type = NoteType.Hold;
                holdBeats = 2f;
            }

            m_chart.Add(new RequiredGoal
            {
                absoluteBeatIndex = i + m_leadInBeats,
                lane = lanes[0],
                noteType = type,
                holdDurationBeats = holdBeats,
                multiLanes = lanes
            });
        }
    }

    public RequiredGoal GetNextGoal()
    {
        if (m_nextGoalIndex >= m_chart.Count)
        {
            return null;
        }

        return m_chart[m_nextGoalIndex];
    }

    public void AdvanceGoal()
    {
        m_nextGoalIndex++;

        if (m_nextGoalIndex < m_chart.Count)
        {
            m_sendNextGoalEvent?.Invoke(m_chart[m_nextGoalIndex]);
        }
    }
}

[Serializable]
public enum NoteType
{
    Tap,
    Hold,
    Multi
}

[Serializable]
public class RequiredGoal // These are the notes the Composer sends to the Judge and holds the info about them
{
    public int absoluteBeatIndex;
    public InputLane lane;

    public NoteType noteType;

    public float holdDurationBeats;

    public List<InputLane> multiLanes;
}