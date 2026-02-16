using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class Judge : MonoBehaviour
{
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private PlayerInput m_playerInput;
    [SerializeField] private Composer m_composer;
    [SerializeField] private MusicPlayer m_musicPlayer;

    [SerializeField] private UnityEvent<InputOutcome> m_judgeOutcomeEvent;
    private RequiredGoal m_currentGoal;


    [Header("Timing Windows (ms)")]
    [SerializeField] private float m_perfectMs = 50f;
    [SerializeField] private float m_hitMs = 80f;
    [SerializeField] private float m_marginMs = 100f;

    private bool m_goalHit;

    void Start()
    {

    }

    void Update()
    {
    }

    public void SetCurrentGoal(RequiredGoal goal)
    {
        m_currentGoal = goal;
        m_goalHit = false;
        //Debug.Log($"NEW TARGET BEAT: {goal.absoluteBeatIndex}");
    }

    private void CheckInput(InputLane lane)
    {
        if (m_currentGoal == null)
        {
            Debug.Log("No current goal set!");
            return;
        }

        if (lane == m_currentGoal.lane)
        {
            CheckMetronomeTiming();
        }
        else
        {
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss); // Send outcome to GameManager
            Debug.Log("Wrong input");
            m_goalHit = true;
            m_composer.AdvanceGoal();
        }
    }
    public void OnButton1(InputAction.CallbackContext context)
    {
        Debug.Log("Button 1 callback fired");

        if (!context.performed) return;

        CheckInput(InputLane.Lane1);
    }


    public void OnButton2(InputAction.CallbackContext context)
    {
        Debug.Log("Button 2 callback fired");

        if (!context.performed) return;
        CheckInput(InputLane.Lane2);
    }

    public void OnButton3(InputAction.CallbackContext context)
    {
        Debug.Log("Button 3 callback fired");

        if (!context.performed) return;
        CheckInput(InputLane.Lane3);
    }

    public void OnButton4(InputAction.CallbackContext context)
    {
        Debug.Log("Button 4 callback fired");

        if (!context.performed) return;
        CheckInput(InputLane.Lane4);
    }

    private void CheckMetronomeTiming()
    {
        if (m_goalHit) return;

        m_goalHit = true;

        InputOutcome outcome = GetInputTimingOutcome();
        m_judgeOutcomeEvent.Invoke(outcome);
        Debug.Log("Outcome: " + outcome);

        m_composer.AdvanceGoal();
    }


    public void CheckForMiss(int lastBeat) // Listens to Metronome's exit beat event to know when to miss
    {
        if (m_currentGoal == null)
        {
            Debug.Log("No current goal set!");
            return;
        }
        Debug.Assert(m_metronome != null, "metronome is not assigned!");

        if (m_goalHit) return;

        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float targetMs = m_currentGoal.absoluteBeatIndex * m_musicPlayer.GetBeatDurationMs();

        if (!m_goalHit && nowMs > targetMs + m_marginMs)
        {
            m_goalHit = true;
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss);

            m_composer.AdvanceGoal();
        }

    }

    // Compares current time in beats to last beat and returns a Perfect, Hit, Early or Late based on how close it was to the beat
    private InputOutcome GetInputTimingOutcome()
    {
        float nowMs = m_musicPlayer.GetElapsedTimeInMs();
        float beatMs = m_musicPlayer.GetBeatDurationMs();

        float targetMs = m_currentGoal.absoluteBeatIndex * beatMs;
        float deltaMs = nowMs - targetMs;

        Debug.Log($"Now: {nowMs:0} | Target: {targetMs:0} | Delta: {deltaMs:0}");

        if (Mathf.Abs(deltaMs) <= m_perfectMs)
            return InputOutcome.Perfect;

        if (Mathf.Abs(deltaMs) <= m_hitMs)
            return InputOutcome.Hit;

        if (Mathf.Abs(deltaMs) <= m_marginMs)
            return deltaMs < 0 ? InputOutcome.Early : InputOutcome.Late;

        return InputOutcome.Miss;
    }

    // --- Getters ---
    public float GetMarginMs() => m_marginMs;
    public int GetCurrentTargetBeat()
    {
        return m_currentGoal != null ? m_currentGoal.absoluteBeatIndex : -1;
    }

}
