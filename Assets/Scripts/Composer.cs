using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Composer : MonoBehaviour
{
    public List<RequiredGoal> m_chart { get; private set; } = new();

    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Judge m_judge;
    [SerializeField] private UnityEvent<RequiredGoal> m_sendNextGoalEvent;
    [SerializeField] private int m_leadInBeats = 4;

    private int m_nextGoalIndex = 0;

    void Start()
    {
        CreateLevelChart();
        Reset();

        if (m_chart.Count > 0)
            m_sendNextGoalEvent?.Invoke(m_chart[0]);
    }

    public void Reset()
    {
        m_nextGoalIndex = 0;
    }

    // -----------------------------------------------------------------------
    // Chart authoring
    //
    // Notes are written as (beat, lane, type, holdBeats).
    // Beat 0 = first beat after lead-in. The lead-in offset is added below.
    //
    // Pattern design goals:
    //   - Alternating single taps build the base rhythm
    //   - Short holds punctuate every phrase
    //   - Multi notes land on phrase downbeats for impact
    //   - Gaps of at least 1 beat after every hold end so the player can
    //     release and be ready for the next note
    // -----------------------------------------------------------------------

    public void CreateLevelChart()
    {
        m_chart.Clear();

        // --- Hand-authored pattern (repeats twice then has a break) ---
        // Format: (relativeBeat, lane, noteType, holdDurationBeats)
        // holdDurationBeats is only used for Hold notes.
        var pattern = new List<(int beat, InputLane lane, NoteType type, float hold)>
        {
            // -- Bar 1-2: opening alternating taps --
            (0,  InputLane.Lane3, NoteType.Tap,  0),
            (1,  InputLane.Lane2, NoteType.Tap,  0),
            (2,  InputLane.Lane3, NoteType.Tap,  0),
            (3,  InputLane.Lane2, NoteType.Tap,  0),

            // -- Bar 3: short hold on 1, tap on 4 --
            (4,  InputLane.Lane3, NoteType.Hold, 1.5f),   // ends at 5.5 → next note at 6
            (6,  InputLane.Lane2, NoteType.Tap,  0),
            (7,  InputLane.Lane3, NoteType.Tap,  0),

            // -- Bar 4: syncopated taps --
            (8,  InputLane.Lane2, NoteType.Tap,  0),
            (9,  InputLane.Lane3, NoteType.Tap,  0),
            (10, InputLane.Lane1, NoteType.Tap,  0),
            (11, InputLane.Lane3, NoteType.Tap,  0),

            // -- Bar 5: multi downbeat then quick taps --
            (12, InputLane.Lane2, NoteType.Multi, 0),  // Lane 2+3 simultaneously (multiLanes set below)
            (13, InputLane.Lane3, NoteType.Tap,  0),
            (14, InputLane.Lane2, NoteType.Tap,  0),
            (15, InputLane.Lane3, NoteType.Tap,  0),

            // -- Bar 6-7: hold + fills --
            (16, InputLane.Lane1, NoteType.Hold, 2f),   // ends at 18 → next note at 19
            (19, InputLane.Lane3, NoteType.Tap,  0),
            (20, InputLane.Lane2, NoteType.Tap,  0),
            (21, InputLane.Lane3, NoteType.Tap,  0),
            (22, InputLane.Lane1, NoteType.Tap,  0),
            (23, InputLane.Lane2, NoteType.Tap,  0),

            // -- Bar 8: big multi + hold build --
            (24, InputLane.Lane2, NoteType.Multi, 0),   // Lane 2+3
            (26, InputLane.Lane3, NoteType.Hold, 1.5f), // ends at 27.5 → next note at 28
            (28, InputLane.Lane1, NoteType.Tap,  0),
            (29, InputLane.Lane2, NoteType.Tap,  0),
            (30, InputLane.Lane3, NoteType.Tap,  0),
            (31, InputLane.Lane2, NoteType.Tap,  0),

            // -- Bar 9: single-lane staircase --
            (32, InputLane.Lane1, NoteType.Tap,  0),
            (33, InputLane.Lane2, NoteType.Tap,  0),
            (34, InputLane.Lane3, NoteType.Tap,  0),
            (35, InputLane.Lane4, NoteType.Tap,  0),

            // -- Bar 10: hold on 4, then reverse staircase --
            (36, InputLane.Lane4, NoteType.Hold, 1.5f), // ends at 37.5 → next note at 38
            (38, InputLane.Lane3, NoteType.Tap,  0),
            (39, InputLane.Lane2, NoteType.Tap,  0),
            (40, InputLane.Lane1, NoteType.Tap,  0),
            (41, InputLane.Lane2, NoteType.Tap,  0),
            (42, InputLane.Lane3, NoteType.Tap,  0),
            (43, InputLane.Lane4, NoteType.Tap,  0),

            // -- Bar 11: two multis with a hold between --
            (44, InputLane.Lane2, NoteType.Multi, 0),   // Lane 2+3
            (46, InputLane.Lane3, NoteType.Hold, 1.5f), // ends at 47.5 → next note at 48
            (48, InputLane.Lane1, NoteType.Multi, 0),   // Lane 1+4

            // -- Bar 12: outro flurry --
            (50, InputLane.Lane3, NoteType.Tap,  0),
            (51, InputLane.Lane2, NoteType.Tap,  0),
            (52, InputLane.Lane1, NoteType.Tap,  0),
            (53, InputLane.Lane2, NoteType.Tap,  0),
            (54, InputLane.Lane3, NoteType.Tap,  0),
            (55, InputLane.Lane4, NoteType.Tap,  0),

            // -- Bar 13: long hold to finish the phrase --
            (56, InputLane.Lane3, NoteType.Hold, 3f),   // ends at 59 → phrase end
        };

        // Multi-lane definitions (indexed by beat offset in the pattern above)
        var multiLaneMap = new Dictionary<int, List<InputLane>>
        {
            { 12, new List<InputLane> { InputLane.Lane2, InputLane.Lane3 } },
            { 24, new List<InputLane> { InputLane.Lane2, InputLane.Lane3 } },
            { 44, new List<InputLane> { InputLane.Lane2, InputLane.Lane3 } },
            { 48, new List<InputLane> { InputLane.Lane1, InputLane.Lane4 } },
        };

        // Build chart entries, validating that no hold overlaps the next note
        for (int i = 0; i < pattern.Count; i++)
        {
            var (beat, lane, type, hold) = pattern[i];

            // Safety: clamp hold so it ends at least 1 beat before the next note
            if (type == NoteType.Hold && hold > 0f && i + 1 < pattern.Count)
            {
                int nextBeat = pattern[i + 1].beat;
                float maxHold = nextBeat - beat - 1f;
                if (hold > maxHold)
                {
                    hold = Mathf.Max(0.5f, maxHold);
                    Debug.LogWarning($"Chart: hold at beat {beat} clamped to {hold} to avoid overlap with next note at {nextBeat}");
                }
            }

            multiLaneMap.TryGetValue(beat, out List<InputLane> multiLanes);

            m_chart.Add(new RequiredGoal
            {
                absoluteBeatIndex = beat + m_leadInBeats,
                lane = lane,
                noteType = type,
                holdDurationBeats = hold,
                multiLanes = multiLanes ?? new List<InputLane> { lane }
            });
        }
    }

    public RequiredGoal GetNextGoal()
    {
        if (m_nextGoalIndex >= m_chart.Count) return null;
        return m_chart[m_nextGoalIndex];
    }

    public void AdvanceGoal()
    {
        m_nextGoalIndex++;
        if (m_nextGoalIndex < m_chart.Count)
            m_sendNextGoalEvent?.Invoke(m_chart[m_nextGoalIndex]);
    }
}

public enum NoteType { Tap, Hold, Multi }

[Serializable]
public class RequiredGoal
{
    public int absoluteBeatIndex;
    public InputLane lane;
    public NoteType noteType;
    public float holdDurationBeats;
    public List<InputLane> multiLanes;
}