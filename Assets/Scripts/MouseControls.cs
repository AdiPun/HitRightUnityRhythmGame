using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class MouseControls : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject m_bat;
    [SerializeField] private NoteSpawner m_noteSpawner;
    [SerializeField] private Judge m_judge;
    [SerializeField] private Composer m_composer;

    [Header("Bat Float")]
    [SerializeField] private float m_floatDepth = 8f;
    [SerializeField] private float m_followSmoothing = 0.06f;

    [Header("Slash Detection")]
    [SerializeField] private float m_slashSpeedThreshold = 18f;   // world units/sec
    [SerializeField] private float m_slashHitRadius = 0.6f;        // how close to slash line counts as a hit
    [SerializeField] private LayerMask m_noteLayer;

    [Header("Slash Trail Visual")]
    [SerializeField] private float m_trailDuration = 0.18f;
    [SerializeField] private float m_trailWidth = 0.08f;
    [SerializeField] private Color m_trailColourStart = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color m_trailColourEnd   = new Color(1f, 0.3f,  0.1f, 0f);

    // Bat movement
    private Vector3 m_targetWorldPos;
    private Vector3 m_lastWorldPos;
    private Vector3 m_smoothVelocity;

    // Slash state
    private bool m_isSlashing;
    private LineRenderer m_slashLine;

    // -----------------------------------------------------------------------

    void Awake()
    {
        BuildSlashLineRenderer();
    }

    void Update()
    {
        MoveBat();
        CheckSlash();
    }

    // -----------------------------------------------------------------------
    // Bat
    // -----------------------------------------------------------------------

    void MoveBat()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 screenWithDepth = new Vector3(screenPos.x, screenPos.y, m_floatDepth);
        m_targetWorldPos = Camera.main.ScreenToWorldPoint(screenWithDepth);

        m_bat.transform.position = Vector3.SmoothDamp(
            m_bat.transform.position,
            m_targetWorldPos,
            ref m_smoothVelocity,
            m_followSmoothing);

        // Rotate bat to face movement direction
        Vector3 moveDelta = m_targetWorldPos - m_lastWorldPos;
        if (moveDelta.magnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveDelta.y, moveDelta.x) * Mathf.Rad2Deg;
            m_bat.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        m_lastWorldPos = m_targetWorldPos;
    }

    // -----------------------------------------------------------------------
    // Slash
    // -----------------------------------------------------------------------

    void CheckSlash()
    {
        float speed = m_smoothVelocity.magnitude;

        if (speed >= m_slashSpeedThreshold && !m_isSlashing)
            StartCoroutine(DoSlash(m_lastWorldPos, m_targetWorldPos));
    }

    IEnumerator DoSlash(Vector3 from, Vector3 to)
    {
        m_isSlashing = true;

        // Show the trail
        ShowSlashTrail(from, to);

        // Find every NoteVisual in the scene and check if it lies near the slash line
        NoteVisual[] allNotes = FindObjectsByType<NoteVisual>(FindObjectsSortMode.None);
        var hitBeats = new HashSet<int>();

        foreach (NoteVisual note in allNotes)
        {
            if (!note.gameObject.activeSelf) continue;

            float dist = DistancePointToSegment(note.transform.position, from, to);
            if (dist > m_slashHitRadius) continue;

            int beat = note.GetTargetBeat();
            if (hitBeats.Contains(beat)) continue; // already processing this beat

            // Only register if the Judge considers this beat within its timing window
            if (Mathf.Abs(beat - m_judge.GetCurrentTargetBeat()) > 1) continue;

            hitBeats.Add(beat);
        }

        // Ask the Judge to evaluate each beat we sliced through
        // We route through Judge so timing windows and outcomes stay consistent
        foreach (int beat in hitBeats)
            TriggerSlashHit(beat);

        // Fade the trail out
        yield return StartCoroutine(FadeSlashTrail());

        m_isSlashing = false;
    }

    // Fires a hit for a beat by calling the same path the Judge would use for a tap
    private void TriggerSlashHit(int beatIndex)
    {
        RequiredGoal goal = m_composer.GetGoalAtBeat(beatIndex);
        if (goal == null) return;

        List<InputLane> lanes = goal.noteType == NoteType.Multi
            ? goal.multiLanes
            : new List<InputLane> { goal.lane };

        foreach (InputLane lane in lanes)
            m_noteSpawner.HitLane(beatIndex, lane);

        // Report the outcome through the Judge's event so GameManager scores it
        m_judge.RegisterSlashHit();
    }

    // -----------------------------------------------------------------------
    // Trail visual
    // -----------------------------------------------------------------------

    void BuildSlashLineRenderer()
    {
        var go = new GameObject("SlashTrail");
        go.transform.SetParent(transform);
        m_slashLine = go.AddComponent<LineRenderer>();

        m_slashLine.positionCount = 2;
        m_slashLine.widthCurve = AnimationCurve.EaseInOut(0f, m_trailWidth, 1f, m_trailWidth * 0.3f);
        m_slashLine.material = new Material(Shader.Find("Sprites/Default"));
        m_slashLine.startColor = m_trailColourStart;
        m_slashLine.endColor   = m_trailColourEnd;
        m_slashLine.enabled = false;
        m_slashLine.useWorldSpace = true;
        m_slashLine.sortingOrder = 10;
    }

    void ShowSlashTrail(Vector3 from, Vector3 to)
    {
        m_slashLine.SetPosition(0, from);
        m_slashLine.SetPosition(1, to);
        m_slashLine.enabled = true;
        m_slashLine.startColor = m_trailColourStart;
        m_slashLine.endColor   = m_trailColourEnd;
    }

    IEnumerator FadeSlashTrail()
    {
        float t = 0f;
        while (t < m_trailDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / m_trailDuration);

            m_slashLine.startColor = new Color(
                m_trailColourStart.r, m_trailColourStart.g, m_trailColourStart.b, alpha);
            m_slashLine.endColor = new Color(
                m_trailColourEnd.r, m_trailColourEnd.g, m_trailColourEnd.b, alpha);

            yield return null;
        }

        m_slashLine.enabled = false;
    }

    // -----------------------------------------------------------------------
    // Math
    // -----------------------------------------------------------------------

    static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return Vector3.Distance(p, a + t * ab);
    }
}