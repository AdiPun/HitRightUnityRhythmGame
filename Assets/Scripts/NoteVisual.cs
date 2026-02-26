using UnityEngine;

/// <summary>
/// Handles movement and visuals for a single note lane.
/// Knows its own lane index so it can tell NoteSpawner exactly where to burst particles.
/// Contains a self-built hold trail ParticleSystem that plays during HoldActive phase.
/// </summary>
public class NoteVisual : MonoBehaviour
{
    [SerializeField] private float m_overshootSeconds = 0.15f;
    [SerializeField] private float m_arcHeightFactor = 0.3f;
    [SerializeField] private Transform m_holdTail;

    [HideInInspector] public NoteSpawner m_noteSpawner;

    private NoteType m_noteType;
    private int m_targetBeat;
    private int m_laneIndex;          // set in Initialise, passed to SpawnHitParticles
    private float m_speed;
    private float m_travelTime;
    private float m_elapsed;

    private Vector3 m_spawnPos;
    private Vector3 m_targetPos;
    private Vector3 m_controlPoint;
    private Vector3 m_travelDirection;

    private float m_holdDurationSeconds;
    private bool m_isHoldActive;
    private float m_holdElapsed;

    private enum Phase { Travelling, Overshooting, HoldActive, Done }
    private Phase m_phase = Phase.Done;
    private float m_overshootTimer;

    // Hold trail — built in Initialise the first time a hold note is created
    private ParticleSystem m_holdTrail;

    // -----------------------------------------------------------------------
    // Initialise
    // -----------------------------------------------------------------------

    public void Initialise(
        Transform target,
        Transform spawn,
        int targetBeat,
        float speed,
        NoteType noteType,
        float holdDurationSeconds,
        int laneIndex = 0)
    {
        m_targetBeat = targetBeat;
        m_laneIndex = laneIndex;
        m_speed = speed;
        m_noteType = noteType;
        m_holdDurationSeconds = holdDurationSeconds;
        m_isHoldActive = false;
        m_holdElapsed = 0f;
        m_overshootTimer = 0f;

        m_spawnPos = spawn.position;
        m_targetPos = target.position;
        m_travelDirection = (m_targetPos - m_spawnPos).normalized;

        Vector3 mid = (m_spawnPos + m_targetPos) * 0.5f;
        Vector3 perp = Vector3.Cross(m_travelDirection, Vector3.forward).normalized;
        float span = Vector3.Distance(m_spawnPos, m_targetPos);
        m_controlPoint = mid + perp * span * m_arcHeightFactor;

        m_travelTime = ApproxArcLength(20) / m_speed;
        m_elapsed = 0f;
        m_phase = Phase.Travelling;

        transform.position = m_spawnPos;
        gameObject.SetActive(true);

        if (m_holdTail != null)
            m_holdTail.gameObject.SetActive(noteType == NoteType.Hold);

        // Build hold trail once, reuse on subsequent Initialise calls
        if (noteType == NoteType.Hold)
        {
            if (m_holdTrail == null)
                m_holdTrail = BuildHoldTrail();
            m_holdTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else if (m_holdTrail != null)
        {
            m_holdTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // -----------------------------------------------------------------------
    // Update FSM
    // -----------------------------------------------------------------------

    void Update()
    {
        switch (m_phase)
        {
            case Phase.Travelling: UpdateTravelling(); break;
            case Phase.Overshooting: UpdateOvershooting(); break;
            case Phase.HoldActive: UpdateHold(); break;
        }
    }

    private void UpdateTravelling()
    {
        m_elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(m_elapsed / m_travelTime);
        transform.position = EvalBezier(t);

        if (m_noteType == NoteType.Hold && m_holdTail != null)
        {
            float tailT = Mathf.Clamp01(1f - t);
            m_holdTail.localScale = new Vector3(1f, tailT * m_holdDurationSeconds * m_speed, 1f);
        }

        if (t >= 1f) OnReachedTarget();
    }

    private void OnReachedTarget()
    {
        if (m_noteType == NoteType.Hold && m_isHoldActive)
        {
            StartHoldPhase();
        }
        else
        {
            m_phase = Phase.Overshooting;
            m_overshootTimer = 0f;
        }
    }

    private void UpdateOvershooting()
    {
        transform.position += m_travelDirection * m_speed * Time.deltaTime;
        m_overshootTimer += Time.deltaTime;
        if (m_overshootTimer >= m_overshootSeconds)
            Deactivate();
    }

    private void UpdateHold()
    {
        m_holdElapsed += Time.deltaTime;

        if (m_holdTail != null)
        {
            float remaining = Mathf.Clamp01(1f - m_holdElapsed / m_holdDurationSeconds);
            m_holdTail.localScale = new Vector3(1f, remaining * m_holdDurationSeconds * m_speed, 1f);
        }

        if (m_holdElapsed >= m_holdDurationSeconds)
        {
            // Hold completed — burst particles at this lane and deactivate
            m_noteSpawner?.SpawnHitParticles(m_laneIndex);
            StopHoldTrail();
            Deactivate();
        }
    }

    // -----------------------------------------------------------------------
    // Public API — called by NoteSpawner
    // -----------------------------------------------------------------------

    /// <summary>Tap or multi hit on this lane. lane is passed so NoteSpawner can burst at the right position.</summary>
    public void Hit(InputLane lane)
    {
        m_noteSpawner?.SpawnHitParticles((int)lane);
        Deactivate();
    }

    public void BeginHold()
    {
        m_isHoldActive = true;

        if (m_phase == Phase.Overshooting)
        {
            // Already past the line — snap back and start hold immediately
            transform.position = m_targetPos;
            StartHoldPhase();
        }
        // If still Travelling, OnReachedTarget will call StartHoldPhase when it arrives
    }

    public void ReleaseHold(bool withinWindow)
    {
        if (withinWindow && m_holdElapsed >= m_holdDurationSeconds * 0.5f)
            m_noteSpawner?.SpawnHitParticles(m_laneIndex);

        StopHoldTrail();
        Deactivate();
    }

    /// <summary>Silently deactivate without particles — used for auto-miss.</summary>
    public void ForceDeactivate() => Deactivate();

    // -----------------------------------------------------------------------
    // Hold trail
    // -----------------------------------------------------------------------

    private void StartHoldPhase()
    {
        m_phase = Phase.HoldActive;
        m_holdElapsed = 0f;

        if (m_holdTrail != null)
        {
            m_holdTrail.transform.position = m_targetPos;
            m_holdTrail.Play();
        }
    }

    private void StopHoldTrail()
    {
        m_holdTrail?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>
    /// Builds a looping particle trail as a child of this GameObject.
    /// No prefab or asset needed — purely constructed in code.
    /// </summary>
    private ParticleSystem BuildHoldTrail()
    {
        var go = new GameObject("HoldTrail");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f, 1f),   // warm yellow core
            new Color(1f, 0.4f, 0.1f, 1f)    // hot orange edge
        );

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        // Gentle outward velocity to give a flickering ember feel
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.radial = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        return ps;
    }

    // -----------------------------------------------------------------------
    // Bezier helpers
    // -----------------------------------------------------------------------

    private Vector3 EvalBezier(float t)
    {
        float u = 1f - t;
        return u * u * m_spawnPos
             + 2f * u * t * m_controlPoint
             + t * t * m_targetPos;
    }

    private float ApproxArcLength(int steps)
    {
        float length = 0f;
        Vector3 prev = EvalBezier(0f);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 curr = EvalBezier(i / (float)steps);
            length += Vector3.Distance(prev, curr);
            prev = curr;
        }
        return length;
    }

    private void Deactivate()
    {
        m_phase = Phase.Done;
        gameObject.SetActive(false);
    }

    public int GetTargetBeat() => m_targetBeat;
}