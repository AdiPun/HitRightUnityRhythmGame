using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Composer m_composer;
    [SerializeField] private NoteVisual m_notePrefab;
    [SerializeField] private Transform[] m_noteSpawn;
    [SerializeField] private Transform[] m_noteTarget;
    [SerializeField] private float m_noteTravelTimeSeconds = 2.0f;

    private float m_travelBeats;
    private int m_nextSpawnIndex = 0;
    private Dictionary<int, NoteVisual> m_activeNotes = new();

    void Start()
    {
        m_travelBeats = m_noteTravelTimeSeconds / m_musicPlayer.GetBeatDurationSeconds();
    }

    void Update()
    {
        float currentBeat = m_musicPlayer.GetElapsedTimeInBeats();

        // Use while loop to catch up if multiple notes become due in the same frame
        while (m_nextSpawnIndex < m_composer.m_chart.Count)
        {
            RequiredGoal goal = m_composer.m_chart[m_nextSpawnIndex];

            if (currentBeat >= goal.absoluteBeatIndex - m_travelBeats)
            {
                SpawnNote(goal);
                m_nextSpawnIndex++;
            }
            else
            {
                break;
            }
        }
    }

    public void SpawnNote(RequiredGoal goal)
    {
        int laneIndex = (int)goal.lane;

        NoteVisual note = Instantiate(m_notePrefab);
        note.transform.position = m_noteSpawn[laneIndex].position;

        float distance = Vector3.Distance(m_noteSpawn[laneIndex].position, m_noteTarget[laneIndex].position);
        float speed = distance / m_noteTravelTimeSeconds;

        note.Initialise(m_noteTarget[laneIndex], m_noteSpawn[laneIndex], goal.absoluteBeatIndex, speed);

        m_activeNotes[goal.absoluteBeatIndex] = note;
    }

    public void RemoveNote(int beatIndex)
    {
        if (m_activeNotes.TryGetValue(beatIndex, out NoteVisual note))
        {
            note.Hit();
            m_activeNotes.Remove(beatIndex);
        }
    }

    public void ResetSpawner()
    {
        foreach (var note in m_activeNotes.Values)
        {
            if (note != null)
                Destroy(note.gameObject);
        }

        m_activeNotes.Clear();
        m_nextSpawnIndex = 0;
    }
}