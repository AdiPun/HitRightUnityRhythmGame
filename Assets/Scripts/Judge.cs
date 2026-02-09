using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class Judge : MonoBehaviour
{
    [SerializeField] private Metronome m_metronome;
    [SerializeField] private PlayerInput m_playerInput;
    [SerializeField] private UnityEvent<InputOutcome> m_judgeOutcomeEvent;
    private RequiredGoal m_currentGoal;
    private bool m_goalResolved;


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
        m_goalResolved = false;
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
            Debug.Log("Checking Timing");
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
        if (m_goalResolved) // Don't check timing if the goals been resolved
        {
            Debug.Log("Checking metronome Timing Goal already resolved ");
            return;
        }

        if (m_metronome.GetActiveBeat() != m_currentGoal.beatInBar)
        {
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss); // Send outcome to GameManager
            Debug.Log("Miss after checking metronome timing, active beat: " + m_metronome.GetActiveBeat() + " goal beat: " + m_currentGoal.beatInBar);
            m_goalResolved = true;
            return;
        }

        m_judgeOutcomeEvent.Invoke(m_metronome.GetInputTimingOutcome()); // Send outcome to GameManager
        Debug.Log("Outcome: " + m_metronome.GetInputTimingOutcome());
        m_goalResolved = true;
    }

    public void CheckForMiss() // Listens to Metronome's exit beat event to know when to miss
    {
        if (m_goalResolved) // Don't check if we've already resolved the goal
        {
            return;
        }
        
        if (m_metronome.GetActiveBeat() == m_currentGoal.beatInBar)
        {
            m_judgeOutcomeEvent.Invoke(InputOutcome.Miss); // Send outcome to GameManager
            m_goalResolved = true;
        }
    }
}
