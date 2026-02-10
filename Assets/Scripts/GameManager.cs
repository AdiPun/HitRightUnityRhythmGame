using System;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int m_combo = 0;
    [SerializeField] private int m_maxCombo = 0;
    [SerializeField] private int m_hp = 10;
    [SerializeField] private int m_hit;
    [SerializeField] private int m_miss;
    [SerializeField] private UnityEvent m_gameStart;

    void Start()
    {

    }

    void Update()
    {

    }

    public void ResetLevel()
    {
        m_combo = 0;
        m_maxCombo = 0;
        m_hp = 10;
        m_hit = 0;
        m_miss = 0;
    }

    public void UpdateScore(InputOutcome outcome)
    {
        if (outcome == InputOutcome.Hit || outcome == InputOutcome.Perfect || outcome == InputOutcome.Late || outcome == InputOutcome.Early)
        {
            m_combo += 1;
            m_hit += 1;
        }
        else
        {
            if (m_combo > m_maxCombo)
            {
                m_maxCombo = m_combo;
            }
            m_combo = 0;
            m_miss += 1;
        }
    }

}

public enum InputOutcome // These are the outcomesof the player's input
{
    Miss,
    Hit,
    Perfect,
    Early,
    Late
}

// Which lane, maps to input button 1,2,3 and 4
public enum InputLane
{
    Lane1,
    Lane2,
    Lane3,
    Lane4
}

