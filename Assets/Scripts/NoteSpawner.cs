using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Composer m_composer;
    [SerializeField] private NoteVisual m_notePrefab;
    [SerializeField] private float m_noteTravelTimeSeconds = 2.0f;

    [SerializeField] private Transform[] m_noteSpawn;
    [SerializeField] private Transform[] m_noteTarget;

    private float m_travelBeats;
    private int m_nextSpawnIndex = 0;

    // Maps beat index → (lane → NoteVisual) so individual lanes can be hit independently
    private Dictionary<int, Dictionary<InputLane, NoteVisual>> m_activeNotes = new();

    // One particle system per lane, configured in Start
    private ParticleSystem[] m_laneParticles;

    void Start()
    {
        m_travelBeats = m_noteTravelTimeSeconds / m_musicPlayer.GetBeatDurationSeconds();
        BuildLaneParticleSystems();
    }

    void Update()
    {
        float currentBeat = m_musicPlayer.GetElapsedTimeInBeats();

        while (m_nextSpawnIndex < m_composer.m_chart.Count)
        {
            RequiredGoal goal = m_composer.m_chart[m_nextSpawnIndex];

            if (currentBeat >= goal.absoluteBeatIndex - m_travelBeats)
            {
                SpawnNotePrefab(goal);
                m_nextSpawnIndex++;
            }
            else
            {
                break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Spawning
    // -----------------------------------------------------------------------

    public void SpawnNotePrefab(RequiredGoal goal)
    {
        float holdSeconds = goal.holdDurationBeats * m_musicPlayer.GetBeatDurationSeconds();

        List<InputLane> lanes = goal.noteType == NoteType.Multi
            ? goal.multiLanes
            : new List<InputLane> { goal.lane };

        if (!m_activeNotes.ContainsKey(goal.absoluteBeatIndex))
            m_activeNotes[goal.absoluteBeatIndex] = new Dictionary<InputLane, NoteVisual>();

        foreach (InputLane lane in lanes)
        {
            int laneIndex = (int)lane;

            NoteVisual note = Instantiate(m_notePrefab);
            note.m_noteSpawner = this;

            float distance = Vector3.Distance(m_noteSpawn[laneIndex].position, m_noteTarget[laneIndex].position);
            float speed = distance / m_noteTravelTimeSeconds;

            note.Initialise(
                m_noteTarget[laneIndex],
                m_noteSpawn[laneIndex],
                goal.absoluteBeatIndex,
                speed,
                goal.noteType,
                holdSeconds,
                laneIndex
            );

            m_activeNotes[goal.absoluteBeatIndex][lane] = note;
        }
    }

    // -----------------------------------------------------------------------
    // Note lifecycle — called by Judge
    // -----------------------------------------------------------------------

    /// <summary>
    /// Hit a specific lane at a beat. For tap/multi, only that lane's visual is removed.
    /// Returns true if all lanes at this beat have now been hit (so Judge can resolve the goal).
    /// </summary>
    public bool HitLane(int beatIndex, InputLane lane)
    {
        if (!m_activeNotes.TryGetValue(beatIndex, out var laneMap)) return true;
        if (!laneMap.TryGetValue(lane, out NoteVisual note)) return false;

        note.Hit(lane);               // hit just this lane's visual
        laneMap.Remove(lane);

        if (laneMap.Count == 0)
        {
            m_activeNotes.Remove(beatIndex);
            return true;              // all lanes cleared
        }
        return false;                 // other lanes still pending
    }

    /// <summary>Miss all remaining notes at a beat (used for auto-miss).</summary>
    public void MissAllAtBeat(int beatIndex)
    {
        if (!m_activeNotes.TryGetValue(beatIndex, out var laneMap)) return;
        // Don't call Hit — just deactivate so no particles spawn
        foreach (var note in laneMap.Values)
            if (note != null) note.ForceDeactivate();
        m_activeNotes.Remove(beatIndex);
    }

    public void BeginHoldAtBeat(int beatIndex)
    {
        if (!m_activeNotes.TryGetValue(beatIndex, out var laneMap)) return;
        foreach (var note in laneMap.Values) note?.BeginHold();
    }

    public void ReleaseHold(int beatIndex, bool withinWindow)
    {
        if (!m_activeNotes.TryGetValue(beatIndex, out var laneMap)) return;
        foreach (var note in laneMap.Values) note?.ReleaseHold(withinWindow);
        m_activeNotes.Remove(beatIndex);
    }

    public void ResetSpawner()
    {
        foreach (var laneMap in m_activeNotes.Values)
            foreach (var note in laneMap.Values)
                if (note != null) Destroy(note.gameObject);

        m_activeNotes.Clear();
        m_nextSpawnIndex = 0;
    }

    // -----------------------------------------------------------------------
    // Particles
    // -----------------------------------------------------------------------

    /// <summary>
    /// Spawn a hit burst at a specific lane's target position.
    /// Called by NoteVisual.Hit, which passes its own lane index.
    /// </summary>
    public void SpawnHitParticles(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= m_laneParticles.Length) return;
        var ps = m_laneParticles[laneIndex];
        ps.transform.position = m_noteTarget[laneIndex].position;
        ps.Play();
    }

    private void BuildLaneParticleSystems()
    {
        int count = m_noteTarget.Length;
        m_laneParticles = new ParticleSystem[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject($"HitParticles_Lane{i}");
            go.transform.SetParent(transform);
            go.transform.position = m_noteTarget[i].position;

            var ps = go.AddComponent<ParticleSystem>();
            ConfigureHitBurst(ps);
            m_laneParticles[i] = ps;
        }
    }

    private static void ConfigureHitBurst(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 1f),
            new Color(0.2f, 0.85f, 1f, 1f)
        );

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 28, 1, 0.01f) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.1f, 0.7f, 1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-200f, 200f);
    }
}