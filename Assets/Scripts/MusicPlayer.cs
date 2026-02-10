using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private float _bpm = 120f;

    [SerializeField] private float _noteLength = 1;


    void Update()
    {
    }

    public void StartAudioTrack()
    {
        if (!_audioSource.isPlaying)
        {
            _audioSource.Play();
        }
    }

    public float GetElapsedTimeInBeats()
    {
        // sampledTime is an accurate elapsed time in beats, this method works even with compressed audio like mp3s
        // .timeSamples gives us the current elapsed samples through the track
        // .frequency is the sample frequency in HZ
        // .GetBeatDurationSeconds will get the length of a beat in seconds at the BPM
        float elapsedTimeInBeats = _audioSource.timeSamples / (_audioSource.clip.frequency * GetBeatDurationSeconds());
        return elapsedTimeInBeats;
    }

    // Gets the length of the current beat we're tracking in seconds
    public float GetBeatDurationSeconds()
    {
        // 60 seconds divided by BPM tells us how many seconds there are in a beat
        // This multiplied by note length
        // if note length is 2 we'll get twice as many beats and if it's 0.5 we'll get half as many beats
        // This is to have half and quarter beats
        return 60f / (_bpm * _noteLength);
    }

    // Gets the length of the current beat we're tracking in ms
    public float GetBeatDurationMs()
    {
        return 60f / (_bpm * 1000f * _noteLength);
    }

    public float GetTrackLengthSeconds()
    {
        return _audioSource.clip.length;
    }

    public float GetBPM()
    {
        return _bpm;
    }
}
