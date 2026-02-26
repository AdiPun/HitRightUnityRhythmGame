using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class Judge : MonoBehaviour
{
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private PlayerInput m_playerInput;
    [SerializeField] private Composer m_composer;
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private NoteSpawner m_noteSpawner;

    [SerializeField] private UnityEvent<InputOutcome> m_judgeOutcomeEvent;
    private RequiredGoal m_currentGoal;

    [Header("Timing Windows (ms)")]
    [SerializeField] private float m_perfectMs = 60f;
    [SerializeField] private float m_hitMs = 80f;
    [SerializeField] private float m_marginMs = 100f;

    private bool m_hasGoalBeenHit;

    // Hold tracking — stored separately so they survive SetCurrentGoal being called
    // for the next note while a hold is still running
    private bool m_isHoldInProgress;
    private int m_holdBeatIndex;
    private float m_holdDurationBeats;

    void Start()
    {
        // Nothing to wire here anymore — RemoveNotesAtBeat replaced by HitLane
    }

    public void SetCurrentGoal(RequiredGoal goal)
    {
        m_currentGoal = goal;
        m_hasGoalBeenHit = false;
        // Do NOT reset m_isHoldInProgress — a hold may still be running
    }

    // --- Input ---
    public void OnButton1(InputAction.CallbackContext context) => HandleButton(context, InputLane.Lane1);
    public void OnButton2(InputAction.CallbackContext context) => HandleButton(context, InputLane.Lane2);
    public void OnButton3(InputAction.CallbackContext context) => HandleButton(context, InputLane.Lane3);
    public void OnButton4(InputAction.CallbackContext context) => HandleButton(context, InputLane.Lane4);

    private void HandleButton(InputAction.CallbackContext context, InputLane lane)
    {
        if (context.performed)
            OnPress(lane);
        else if (context.canceled)
            OnRelease(lane);
    }

    private void OnPress(InputLane inputLane)
    {
        if (m_currentGoal == null) return;
        if (!IsWithinMargin()) return;

        // MuseDash: wrong-lane presses inside the window are silently ignored
        bool isCorrectLane = m_currentGoal.noteType == NoteType.Multi
            ? m_currentGoal.multiLanes.Contains(inputLane)
            : inputLane == m_currentGoal.lane;

        if (!isCorrectLane) return;
        if (m_hasGoalBeenHit) return;

        switch (m_currentGoal.noteType)
        {
            case NoteType.Tap:
                EvaluateTap(inputLane);
                break;
            case NoteType.Multi:
                EvaluateMultiLane(inputLane);
                break;
            case NoteType.Hold:
                BeginHold();
                break;
        }
    }

    private void OnRelease(InputLane lane)
    {
        if (!m_isHoldInProgress) return;

        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float holdEndMs = (m_holdBeatIndex + m_holdDurationBeats) * m_musicPlayer.GetBeatDurationMs();
        bool isWithinWindow = Mathf.Abs(nowMs - holdEndMs) <= m_marginMs;

        m_isHoldInProgress = false;
        m_noteSpawner.ReleaseHold(m_holdBeatIndex, isWithinWindow);

        InputOutcome outcome = isWithinWindow ? InputOutcome.Hit : InputOutcome.Miss;
        m_judgeOutcomeEvent.Invoke(outcome);
        Debug.Log($"Hold release: {outcome}");
    }

    // -----------------------------------------------------------------------
    // Tap
    // -----------------------------------------------------------------------

    private void EvaluateTap(InputLane lane)
    {
        m_hasGoalBeenHit = true;
        InputOutcome outcome = GetTimingOutcome();
        m_judgeOutcomeEvent.Invoke(outcome);
        Debug.Log("Tap: " + outcome);

        // HitLane removes the visual and returns true (single lane always clears in one press)
        m_noteSpawner.HitLane(m_currentGoal.absoluteBeatIndex, lane);
        m_composer.AdvanceGoal();
    }

    // -----------------------------------------------------------------------
    // Multi — each lane press is evaluated independently.
    // Goal only advances once ALL lanes have been hit.
    // -----------------------------------------------------------------------

    private void EvaluateMultiLane(InputLane lane)
    {
        InputOutcome outcome = GetTimingOutcome();
        m_judgeOutcomeEvent.Invoke(outcome);
        Debug.Log($"Multi lane {lane}: {outcome}");

        bool allLanesDone = m_noteSpawner.HitLane(m_currentGoal.absoluteBeatIndex, lane);

        if (allLanesDone)
        {
            m_hasGoalBeenHit = true;
            m_composer.AdvanceGoal();
        }
    }

    // -----------------------------------------------------------------------
    // Hold
    // -----------------------------------------------------------------------

    private void BeginHold()
    {
        m_hasGoalBeenHit = true;
        m_isHoldInProgress = true;
        m_holdBeatIndex = m_currentGoal.absoluteBeatIndex;
        m_holdDurationBeats = m_currentGoal.holdDurationBeats;

        m_noteSpawner.BeginHoldAtBeat(m_holdBeatIndex);

        InputOutcome pressOutcome = GetTimingOutcome();
        m_judgeOutcomeEvent.Invoke(pressOutcome);
        Debug.Log("Hold begin: " + pressOutcome);

        // Advance so the Composer sends the next goal while the hold runs.
        // m_holdBeatIndex and m_holdDurationBeats are stored separately so
        // OnRelease still resolves against the correct beat.
        m_composer.AdvanceGoal();
    }

    // -----------------------------------------------------------------------
    // Auto-miss on exit beat
    // -----------------------------------------------------------------------

    public void CheckForMiss(int lastBeat)
    {
        if (m_currentGoal == null) return;
        if (m_hasGoalBeenHit) return;
        if (m_isHoldInProgress) return; // can't press new notes while holding

        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float targetMs = m_currentGoal.absoluteBeatIndex * m_musicPlayer.GetBeatDurationMs();

        if (nowMs > targetMs + m_marginMs)
        {
            m_hasGoalBeenHit = true;
            m_noteSpawner.MissAllAtBeat(m_currentGoal.absoluteBeatIndex);
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss);
            m_composer.AdvanceGoal();
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private bool IsWithinMargin()
    {
        if (m_currentGoal == null) return false;
        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float targetMs = m_currentGoal.absoluteBeatIndex * m_musicPlayer.GetBeatDurationMs();
        return Mathf.Abs(nowMs - targetMs) <= m_marginMs;
    }

    private InputOutcome GetTimingOutcome()
    {
        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float targetMs = m_currentGoal.absoluteBeatIndex * m_musicPlayer.GetBeatDurationMs();
        float deltaMs = nowMs - targetMs;

        Debug.Log($"Now: {nowMs:0} | Target: {targetMs:0} | Delta: {deltaMs:0}");

        if (Mathf.Abs(deltaMs) <= m_perfectMs) return InputOutcome.Perfect;
        if (Mathf.Abs(deltaMs) <= m_hitMs) return InputOutcome.Hit;
        if (Mathf.Abs(deltaMs) <= m_marginMs) return deltaMs < 0 ? InputOutcome.Early : InputOutcome.Late;
        return InputOutcome.Miss;
    }

    // --- Getters ---
    public float GetMarginMs() => m_marginMs;
    public int GetCurrentTargetBeat() => m_currentGoal != null ? m_currentGoal.absoluteBeatIndex : -1;
}