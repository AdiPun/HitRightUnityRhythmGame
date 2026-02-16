using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private NoteVisual m_notePrefab;
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private Composer m_composer;
    [SerializeField] private float m_travelBeats = 4f;
    [SerializeField] private Transform[] m_spawnPos;
    [SerializeField] private Transform[] m_targetPos;

    private int m_nextNoteIndex;

    public void SpawnNote(RequiredGoal targetGoal)
    {
        GameObject note = Instantiate(m_notePrefab.gameObject);
        NoteVisual noteVisual = note.GetComponent<NoteVisual>();
        noteVisual.Initialize(targetGoal.absoluteBeatIndex, m_metronome, m_travelBeats, m_spawnPos[(int)targetGoal.lane].position, m_targetPos[(int)targetGoal.lane].position);
    }
}
