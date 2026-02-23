using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int m_combo = 0;
    [SerializeField] private int m_maxCombo = 0;
    [SerializeField] private int m_hp = 10;
    [SerializeField] private int m_hit = 0;
    [SerializeField] private int m_miss = 0;

    [SerializeField] private UnityEvent m_gameStart;
    [SerializeField] private Composer m_composer;
    [SerializeField] private NoteSpawner m_noteSpawner;

    public void ResetLevel()
    {
        m_combo = 0;
        m_maxCombo = 0;
        m_hp = 10;
        m_hit = 0;
        m_miss = 0;

        m_composer.Reset();
        m_noteSpawner.ResetSpawner();
    }

    public void UpdateScore(InputOutcome outcome)
    {
        bool isHit = outcome == InputOutcome.Hit
                  || outcome == InputOutcome.Perfect
                  || outcome == InputOutcome.Early
                  || outcome == InputOutcome.Late;

        if (isHit)
        {
            m_combo++;
            m_hit++;
        }
        else
        {
            if (m_combo > m_maxCombo)
                m_maxCombo = m_combo;

            m_combo = 0;
            m_miss++;
        }
    }
}

public enum InputOutcome
{
    Miss,
    Hit,
    Perfect,
    Early,
    Late
}

public enum InputLane
{
    Lane1,
    Lane2,
    Lane3,
    Lane4
}