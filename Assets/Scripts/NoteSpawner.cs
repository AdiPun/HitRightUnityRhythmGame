using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns notes from a single pitcher point. Each lane has a distinct
/// camera-space control offset that determines the initial throw direction
/// of the boomerang arc.
///
/// The control offset is the direction the note appears to leave the pitcher:
///   Lane1 (top-left)     → thrown up-left,  curves back to top-left target
///   Lane2 (top-right)    → thrown up-right, curves back to top-right target
///   Lane3 (bottom-left)  → thrown low-left, curves back to bottom-left target
///   Lane4 (bottom-right) → thrown low-right,curves back to bottom-right target
///
/// Tweak m_controlLaneX in the Inspector to change how wide/dramatic each sweep is.
/// Tweak m_arcRadius on the NoteVisual prefab to scale all arcs uniformly.
/// </summary>
public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private Composer m_composer;
    [SerializeField] private Camera m_camera;
    [SerializeField] private NoteVisual m_notePrefab;

    [Header("Spawn & targets")]
    [Tooltip("The pitcher - single shared spawn point for all notes.")]
    [SerializeField] private Transform m_pitcherSpawn;
    [Tooltip("One target per lane: [0]=Lane1, [1]=Lane2, [2]=Lane3, [3]=Lane4")]
    [SerializeField] private Transform[] m_noteTarget;

    [Header("Flight")]
    [SerializeField] private float m_noteTravelTimeSeconds = 2.0f;

    [Header("Boomerang control offsets (camera space: x=right, y=up)")]
    [Tooltip("Lane1 - left: X button")]
    [SerializeField] private Vector2 m_controlLane1 = new Vector2(-1f, 0);
    [Tooltip("Lane2 - top: Y button")]
    [SerializeField] private Vector2 m_controlLane2 = new Vector2(0, 1f);
    [Tooltip("Lane3 - right: B button")]
    [SerializeField] private Vector2 m_controlLane3 = new Vector2(1f, 0);
    [Tooltip("Lane4 - bottom: A button")]
    [SerializeField] private Vector2 m_controlLane4 = new Vector2(0, -1f);

    // Runtime
    private float m_travelBeats;
    private int m_nextSpawnIndex = 0;
    private Dictionary<int, Dictionary<InputLane, NoteVisual>> m_activeNotes = new();
    private ParticleSystem[] m_laneParticles;

    // -----------------------------------------------------------------------

    void Start()
    {
        if (m_camera == null) m_camera = Camera.main;
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
            else break;
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

        Vector3 camRight = m_camera.transform.right;
        Vector3 camUp = m_camera.transform.up;

        foreach (InputLane lane in lanes)
        {
            int laneIndex = (int)lane;
            Vector2 controlOffset = GetControlOffset(lane);

            float distance = Vector3.Distance(m_pitcherSpawn.position, m_noteTarget[laneIndex].position);
            float speed = distance / m_noteTravelTimeSeconds;

            NoteVisual note = Instantiate(m_notePrefab);
            note.m_noteSpawner = this;

            note.Initialise(
                m_noteTarget[laneIndex],
                m_pitcherSpawn,
                goal.absoluteBeatIndex,
                speed,
                goal.noteType,
                holdSeconds,
                laneIndex,
                camRight,
                camUp,
                controlOffset
            );

            m_activeNotes[goal.absoluteBeatIndex][lane] = note;
        }
    }

    private Vector2 GetControlOffset(InputLane lane) => lane switch
    {
        InputLane.Lane1 => m_controlLane1,
        InputLane.Lane2 => m_controlLane2,
        InputLane.Lane3 => m_controlLane3,
        InputLane.Lane4 => m_controlLane4,
        _ => Vector2.zero
    };

    // -----------------------------------------------------------------------
    // Note lifecycle
    // -----------------------------------------------------------------------

    public bool HitLane(int beatIndex, InputLane lane)
    {
        if (!m_activeNotes.TryGetValue(beatIndex, out var laneMap)) return true;
        if (!laneMap.TryGetValue(lane, out NoteVisual note)) return false;

        note.Hit(lane);
        laneMap.Remove(lane);

        if (laneMap.Count == 0) { m_activeNotes.Remove(beatIndex); return true; }
        return false;
    }

    public void MissAllAtBeat(int beatIndex)
    {
        if (!m_activeNotes.TryGetValue(beatIndex, out var laneMap)) return;
        foreach (var note in laneMap.Values) note?.ForceDeactivate();
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

    public void SpawnHitParticles(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= m_laneParticles.Length) return;
        m_laneParticles[laneIndex].Play();
    }

    private void BuildLaneParticleSystems()
    {
        int count = m_noteTarget.Length;
        m_laneParticles = new ParticleSystem[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"HitParticles_Lane{i}");
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
            new Color(1f, 1f, 1f, 1f), new Color(0.2f, 0.85f, 1f, 1f));

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 28, 1, 0.01f) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.1f, 0.7f, 1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-200f, 200f);
    }
}