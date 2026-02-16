using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Composer : MonoBehaviour
{
    public List<RequiredGoal> m_chart { get; private set; } = new();
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private Judge m_judge;
    [SerializeField] private UnityEvent<RequiredGoal> m_sendNextGoalEvent;
    [SerializeField] private int m_leadInBeats = 4; // 4 beats before the first note gives the player time

    private int m_nextGoalIndex = 0;

    void Start()
    {
        CreateLevelChart();
        Reset();

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
            RequiredGoal goal = new RequiredGoal
            {
                absoluteBeatIndex = i + m_leadInBeats,
                lane = InputLane.Lane3
            };

            m_chart.Add(goal);
        }
    }


    public RequiredGoal GetNextGoal()
    {
        if (m_nextGoalIndex >= m_chart.Count)
            return null;

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
public class RequiredGoal
{
    public int absoluteBeatIndex;
    public InputLane lane;
}
