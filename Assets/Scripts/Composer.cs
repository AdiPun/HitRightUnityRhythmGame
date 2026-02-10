using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Composer : MonoBehaviour
{
    [SerializeField] private List<RequiredGoal> m_goals = new();
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private Judge m_judge;
    [SerializeField] private UnityEvent<RequiredGoal> m_sendNextGoalEvent;


    void Start()
    {
        CreateLevelChart();
    }
    void Update()
    {
        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float beatMs = m_musicPlayer.GetBeatDurationMs();

        if (m_goals.Count == 0) return;

        RequiredGoal next = m_goals[0];
        float targetMs = next.absoluteBeatIndex * beatMs;

        if (nowMs >= targetMs - m_judge.GetMarginMs())
        {
            m_sendNextGoalEvent.Invoke(next);
            m_goals.RemoveAt(0);
        }
    }

    public void CreateLevelChart()
    {
        // Clear the list
        m_goals.Clear();

        // Find the number of beats in the track
        float trackLength = m_musicPlayer.GetTrackLengthSeconds();
        float beatDuration = m_musicPlayer.GetBeatDurationSeconds();
        int totalBeats = Mathf.FloorToInt(trackLength / beatDuration);

        for (int i = 0; i < totalBeats; i++)
        {
            RequiredGoal goal = new RequiredGoal
            {
                absoluteBeatIndex = i,
                lane = (InputLane)2
            };

            m_goals.Add(goal);
        }
    }
}

[Serializable]
public class RequiredGoal
{
    public int absoluteBeatIndex;
    public InputLane lane;
}
