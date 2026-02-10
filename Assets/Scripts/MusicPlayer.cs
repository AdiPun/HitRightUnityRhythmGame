using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource;

    [SerializeField] private float m_bpm = 120f;

    [SerializeField] private float m_noteLength = 1;


    void Update()
    {
    }

    public void StartAudioTrack()
    {
        if (!m_audioSource.isPlaying)
        {
            m_audioSource.Play();
        }
    }

    public float GetElapsedTimeInBeats()
    {
        // sampledTime is an accurate elapsed time in beats, this method works even with compressed audio like mp3s
        // .timeSamples gives us the current elapsed samples through the track
        // .frequency is the sample frequency in HZ
        // .GetBeatDurationSeconds will get the length of a beat in seconds at the BPM
        float elapsedTimeInBeats = m_audioSource.timeSamples / (m_audioSource.clip.frequency * GetBeatDurationSeconds());
        return elapsedTimeInBeats;
    }

    public float GetElapsedTimeInMs()
    {
        float elapsedBeats = GetElapsedTimeInBeats();
        float elapsedTimeInMs = elapsedBeats * GetBeatDurationMs();
        return elapsedTimeInMs;
    }

    // Gets the length of the current beat we're tracking in seconds
    public float GetBeatDurationSeconds()
    {
        // 60 seconds divided by BPM tells us how many seconds there are in a beat
        // This multiplied by note length
        // if note length is 2 we'll get twice as many beats and if it's 0.5 we'll get half as many beats
        // This is to have half and quarter beats
        return 60f / (m_bpm * m_noteLength);
    }

    // Gets the length of the current beat we're tracking in ms
    public float GetBeatDurationMs()
    {
        return GetBeatDurationSeconds() * 1000f;
    }

    public float GetTrackLengthSeconds()
    {
        return m_audioSource.clip.length;
    }

    public float GetBPM()
    {
        return m_bpm;
    }
}
