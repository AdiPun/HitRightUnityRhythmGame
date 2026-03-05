using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SongData", menuName = "Scriptable Objects/SongData")]
public class SongData : ScriptableObject
{
    [Header("Info")]
    public string songTitle = "Untitled";
    public string artist = "";
    public Sprite albumArt;           // shown in menus
    public float bpm = 120f;
    public float noteLength = 1f;

    [Header("Audio")]
    public AudioClip clip;

    [Header("Story Mode")]
    public bool unlockedByDefault = false; // first level always true
    public string storyDescription = "";

    [Header("Chart")]
    public List<RequiredGoal> chart = new();
}
