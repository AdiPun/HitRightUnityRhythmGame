using UnityEngine;
using UnityEngine.Events;

// This is the metronome
public class BeatController : MonoBehaviour
{
    [SerializeField] private float _bpm;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Intervals[] _intervals;
    [SerializeField] private UnityEvent _trigger;


    private void Update()
    {
        foreach (Intervals interval in _intervals)
        {
            // sampledTime is an accurate elapsed time in beats, this method works even with compressed audio like mp3s
            // .timeSamples gives us the current elapsed samples through the track
            // .frequency is the sample frequency in HZ
            // .GetBeatLength will get the length of a beat in seconds at the BPM
            float sampledTime = _audioSource.timeSamples / (_audioSource.clip.frequency * interval.GetBeatLength(_bpm));

            // Then we make each interval check if the sampled time has passed the next interval and trigger an event if so
            interval.CheckForNewInterval(sampledTime);
        }
    }
}

[System.Serializable]
public class Intervals
{
    [SerializeField] private float _noteLength;
    [SerializeField] private UnityEvent _trigger;
    private int _lastInterval;

    public float GetBeatLength(float bpm) // Gets the length of the current beat we're tracking
    {
        // 60 seconds divided by BPM tells us how many seconds there are in a beat
        // This multiplied by note length
        // if note length is 2 we'll get twice as many beats and if it's 0.5 we'll get half as many beats
        // This is to have half and quarter beats
        return 60f / (bpm * _noteLength);
    }

    public void CheckForNewInterval(float interval)
    {
        // This is rounding the interval down to an int and once it changes, we know we've moved to the next beat
        // if we have, we trigger an event
        if (Mathf.FloorToInt(interval) != _lastInterval)
        {
            _lastInterval = Mathf.FloorToInt(interval);
            _trigger.Invoke(); // We're using event triggers so that we can easily drag other things in the inspector to be triggered by this beat script
        }

        // We dont do:
        // if (interval % 1 == 0)
        // because we're working with floats and framerates, so we can't guarantee we hit an actual whole number with interval
    }

}
