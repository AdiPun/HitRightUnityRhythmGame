using UnityEngine;

public class NoteVisual : MonoBehaviour
{
    private int m_targetBeat;
    private Metronome m_metronome;
    private float m_travelBeats;

    private Vector3 m_spawnPos;
    private Vector3 m_targetPos;

    public void Initialize(int targetBeat, Metronome metronome, float travelBeats, Vector3 spawnPos, Vector3 targetPos)
    {
        m_targetBeat = targetBeat;
        m_metronome = metronome;
        m_travelBeats = travelBeats;
        m_spawnPos = spawnPos;
        m_targetPos = targetPos;

        transform.position = spawnPos;
    }

    void Update()
    {
        float currentBeat = m_metronome.GetActiveBeat();

        float startBeat = m_targetBeat - m_travelBeats;

        float t = Mathf.InverseLerp(startBeat, m_targetBeat, currentBeat);

        transform.position = Vector3.Lerp(m_spawnPos, m_targetPos, t);

        if (currentBeat > m_targetBeat + 1f)
        {
            Destroy(gameObject);
        }
    }

}
