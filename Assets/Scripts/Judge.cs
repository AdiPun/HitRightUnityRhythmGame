using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class Judge : MonoBehaviour
{
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private PlayerInput m_playerInput;
    [SerializeField] private UnityEvent<InputOutcome> m_judgeOutcomeEvent;
    private RequiredGoal m_currentGoal = new RequiredGoal();

    [SerializeField] private MusicPlayer m_musicPlayer;

    [Header("Timing Windows (ms)")]
    [SerializeField] private float m_perfectMs = 30f;
    [SerializeField] private float m_hitMs = 60f;
    [SerializeField] private float m_marginMs = 100f;


    private bool m_goalHit;

    void Start()
    {

    }

    void Update()
    {
        CheckInputToNextGoal();
    }

    public void SetCurrentGoal(RequiredGoal goal) // Listen to Composer's GetNextRequiredGoal event and set the current goal
    {
        m_currentGoal = goal;
        m_goalHit = false;
    }

    private void CheckInputToNextGoal() // Listen to player input
    {
        if (m_playerInput.actions["Button 1"].WasPressedThisFrame())
        {
            CheckInput(InputLane.Lane1);
        }
        if (m_playerInput.actions["Button 2"].WasPressedThisFrame())
        {
            CheckInput(InputLane.Lane2);
        }
        if (m_playerInput.actions["Button 3"].WasPressedThisFrame())
        {
            CheckInput(InputLane.Lane3);
        }
        if (m_playerInput.actions["Button 4"].WasPressedThisFrame())
        {
            CheckInput(InputLane.Lane4);
        }
    }

    private void CheckInput(InputLane lane)
    {
        if (lane == m_currentGoal.lane)
        {
            CheckMetronomeTiming();
        }
        else
        {
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss); // Send outcome to GameManager
            Debug.Log("Wrong input");
        }
    }

    private void CheckMetronomeTiming() // Check if metronome window is open
    {
        if (m_metronome.GetActiveBeat() != m_currentGoal.beatInBar)
        {
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss); // Send outcome to GameManager
            Debug.Log("Correct lane: " + m_currentGoal.lane + " but incorrect beat: " + m_metronome.GetActiveBeat() + " goal beat: " + m_currentGoal.beatInBar);
            return;
        }

        m_goalHit = true;
        Debug.Log("Correct lane: " + m_currentGoal.lane + " and correct beat: " + m_metronome.GetActiveBeat() + " goal beat: " + m_currentGoal.beatInBar);
        Debug.Log("Outcome: " + GetInputTimingOutcome());
        m_judgeOutcomeEvent.Invoke(GetInputTimingOutcome()); // Send outcome to GameManager
    }

    public void CheckForMiss(int lastBeat) // Listens to Metronome's exit beat event to know when to miss
    {
        Debug.Assert(m_currentGoal != null, "current goal is not assigned!");
        Debug.Assert(m_metronome != null, "metronome is not assigned!");

        if (!m_goalHit && m_metronome.GetActiveBeat() == m_currentGoal.beatInBar)
        {
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss); // Send outcome to GameManager
            Debug.Log("Missed beat: " + m_currentGoal.beatInBar);
        }
    }

    // Compares current time in beats to last beat and returns a Perfect, Hit, Early or Late based on how close it was to the beat
    public InputOutcome GetInputTimingOutcome()
    {
        float inputBeat = m_musicPlayer.GetElapsedTimeInBeats();
        float beatCenter = m_metronome.GetElapsedBeats() + 1f;

        float deltaBeats = inputBeat - beatCenter;
        float deltaMs = deltaBeats * m_musicPlayer.GetBeatDurationMs();
        float absDeltaMs = Mathf.Abs(deltaMs);

        Debug.Log("Delta: " + absDeltaMs);

        if (absDeltaMs <= m_perfectMs)
            return InputOutcome.Perfect;

        if (absDeltaMs <= m_hitMs)
            return InputOutcome.Hit;

        if (absDeltaMs <= m_marginMs)
            return deltaMs < 0 ? InputOutcome.Early : InputOutcome.Late;

        return InputOutcome.Miss;
    }

    // --- Getters ---
    public float GetMarginMs() => m_marginMs;
}
