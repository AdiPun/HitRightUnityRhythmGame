using UnityEngine;

/// <summary>
/// Moves a note along a quadratic Bezier arc that produces a boomerang/frisbee
/// sweep. The control point is placed far out to the side of the pitcher→target
/// line, so the note visibly leaves in a wide outward direction and curves back
/// to arrive at the target.
///
/// Each lane's control point is offset in a distinct camera-space direction so
/// the player can read the lane from the initial throw angle.
///
/// Quadratic Bezier: B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
///   P0 = pitcher (spawn)
///   P1 = the wide "throw" control point — far off to the side
///   P2 = target
/// </summary>
public class NoteVisual : MonoBehaviour
{
    [Header("Arc")]
    [Tooltip("How far the control point sits away from the midpoint of the spawn→target line. " +
             "Larger = wider, more dramatic sweep.")]
    [SerializeField] private float m_arcRadius = 3.5f;

    [SerializeField] private float m_overshootSeconds = 0.18f;
    [SerializeField] private Transform m_holdTail;

    [HideInInspector] public NoteSpawner m_noteSpawner;

    // Runtime
    private NoteType m_noteType;
    private int m_targetBeat;
    private int m_laneIndex;
    private float m_speed;
    private float m_travelTime;
    private float m_elapsed;

    // Bezier points
    private Vector3 m_P0; // spawn (pitcher)
    private Vector3 m_P1; // control — wide off-axis throw point
    private Vector3 m_P2; // target

    // Direction at t=1 for overshoot drift
    private Vector3 m_arrivalDirection;

    private float m_holdDurationSeconds;
    private bool m_isHoldActive;
    private float m_holdElapsed;

    private enum Phase { Travelling, Overshooting, HoldActive, Done }
    private Phase m_phase = Phase.Done;
    private float m_overshootTimer;

    private ParticleSystem m_holdTrail;

    // -----------------------------------------------------------------------
    // Initialise
    // -----------------------------------------------------------------------

    /// <param name="controlOffset">
    /// Camera-space offset applied to the midpoint of P0→P2 to build P1.
    /// Supplied by NoteSpawner per lane. The direction is the "throw direction"
    /// the player sees. e.g. (-3, 2) = throws wide left and high, curves back in.
    /// </param>
    public void Initialise(
        Transform target,
        Transform spawn,
        int targetBeat,
        float travelTime,
        NoteType noteType,
        float holdDurationSeconds,
        int laneIndex,
        Vector3 cameraRight,
        Vector3 cameraUp,
        Vector2 controlOffset)
    {
        m_targetBeat = targetBeat;
        m_laneIndex = laneIndex;
        m_noteType = noteType;
        m_holdDurationSeconds = holdDurationSeconds;
        m_isHoldActive = false;
        m_holdElapsed = 0f;
        m_overshootTimer = 0f;




        m_P0 = spawn.position;
        m_P2 = target.position;

        // Build the control point by taking the midpoint of spawn→target and
        // pushing it far out in the lane's camera-space throw direction.
        // The arc radius scales the magnitude of that push.
        Vector3 mid = (m_P0 + m_P2) * 0.5f;
        m_P1 = mid
             + cameraRight * (controlOffset.x * m_arcRadius)
             + cameraUp * (controlOffset.y * m_arcRadius);

        // Arrival direction = tangent at t=1: 2(P2 - P1)
        m_arrivalDirection = (m_P2 - m_P1).normalized;

        float arcLen = ApproxArcLength(24);
        m_travelTime = travelTime;
        m_speed = arcLen / m_travelTime;
        m_elapsed = 0f;
        m_phase = Phase.Travelling;

        transform.position = m_P0;
        gameObject.SetActive(true);

        if (m_holdTail != null)
            m_holdTail.gameObject.SetActive(noteType == NoteType.Hold);

        if (noteType == NoteType.Hold)
        {
            if (m_holdTrail == null) m_holdTrail = BuildHoldTrail();
            m_holdTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            m_holdTrail?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // -----------------------------------------------------------------------
    // FSM
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
        transform.position = EvalQuadratic(t);

        // Face the direction of travel so the note visually rotates as it sweeps
        Vector3 tangent = EvalTangent(t);
        if (tangent.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(tangent);

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
            StartHoldPhase();
        else
        {
            m_phase = Phase.Overshooting;
            m_overshootTimer = 0f;
        }
    }

    private void UpdateOvershooting()
    {
        // Drift in the arrival direction so the note flies past the target naturally
        transform.position += m_arrivalDirection * m_speed * Time.deltaTime;
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
            m_noteSpawner?.SpawnHitParticles(m_laneIndex);
            StopHoldTrail();
            Deactivate();
        }
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

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
            transform.position = m_P2;
            StartHoldPhase();
        }
    }

    public void ReleaseHold(bool withinWindow)
    {
        if (withinWindow) m_noteSpawner?.SpawnHitParticles(m_laneIndex);
        StopHoldTrail();
        Deactivate();
    }

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
            m_holdTrail.transform.position = m_P2;
            m_holdTrail.Play();
        }
    }

    private void StopHoldTrail()
        => m_holdTrail?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

    private ParticleSystem BuildHoldTrail()
    {
        var go = new GameObject("HoldTrail");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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
            new Color(1f, 0.9f, 0.3f, 1f),
            new Color(1f, 0.4f, 0.1f, 1f));

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.radial = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        return ps;
    }

    // -----------------------------------------------------------------------
    // Bezier math
    // -----------------------------------------------------------------------

    private Vector3 EvalQuadratic(float t)
    {
        float u = 1f - t;
        return u * u * m_P0
             + 2f * u * t * m_P1
             + t * t * m_P2;
    }

    // Tangent (first derivative) of the quadratic Bezier — used to orient the note
    private Vector3 EvalTangent(float t)
    {
        // B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)
        return 2f * (1f - t) * (m_P1 - m_P0)
             + 2f * t * (m_P2 - m_P1);
    }

    private float ApproxArcLength(int steps)
    {
        float len = 0f;
        Vector3 prev = EvalQuadratic(0f);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 curr = EvalQuadratic(i / (float)steps);
            len += Vector3.Distance(prev, curr);
            prev = curr;
        }
        return len;
    }

    private void Deactivate()
    {
        m_phase = Phase.Done;
        gameObject.SetActive(false);
    }

    public int GetTargetBeat() => m_targetBeat;
}