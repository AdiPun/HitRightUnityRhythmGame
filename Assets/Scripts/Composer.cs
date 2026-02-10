using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Composer : MonoBehaviour
{
    [SerializeField] private List<RequiredGoal> m_goals = new();
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private UnityEvent<RequiredGoal> m_sendNextGoalEvent;


    void Start()
    {
        CreateLevelChart();
        GetNextRequiredGoal();
    }

    public void GetNextRequiredGoal() // Listen to Metronome's beat event and return the goal for the next beat
    {
        // Might be an off by one error here, need to check
        // int nextBeat = m_metronome.GetElapsedBeats();

        // if (nextBeat < m_goals.Count)
        // {
        //     m_sendNextGoalEvent.Invoke(m_goals[nextBeat]); // Send goal to Judge
        // }
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
            int beatInBar = (i % 4) + 1;

            RequiredGoal goal = new RequiredGoal
            {
                beatInBar = beatInBar,
                lane = (InputLane)UnityEngine.Random.Range(0, 4)
            };

            m_goals.Add(goal);
        }
    }


}

