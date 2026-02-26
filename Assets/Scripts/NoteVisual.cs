using UnityEngine;
using UnityEngine.Events;

public class NoteVisual : MonoBehaviour
{
    [SerializeField] private float m_speed;
    [SerializeField] private float m_overshootSeconds = 6f;
    public NoteSpawner m_noteSpawner;
    private int m_targetBeat;
    private Transform m_target;
    private bool m_isActive = false;
    private Vector3 m_travelDirection; // To do things after it reaches the target
    private float m_overshootTimer = 0f;
    private bool m_overshooting = false;
    public void Initialise(Transform target, Transform spawn, int targetBeat, float speed)
    {
        m_target = target;
        m_targetBeat = targetBeat;
        m_speed = speed;
        m_isActive = true;
        m_overshooting = false;
        m_overshootTimer = 0f;
        m_travelDirection = (target.position - spawn.position).normalized;
        transform.position = spawn.position;
    }

    void Update()
    {
        if (!m_isActive) return;

        if (!m_overshooting) // Move towards target if it's not in the overshooting phase
        {
            transform.position += m_travelDirection * m_speed * Time.deltaTime;

            // Check if we've reached or passed the target
            Vector3 toTarget = m_target.position - transform.position;
            if (Vector3.Dot(toTarget, m_travelDirection) <= 0f)
            {
                m_overshooting = true;
                m_overshootTimer = 0f;
            }
        }
        else
        {
            // Keep moving in the same direction so the note passes the hitline
            // Later this can change the object into a physics object and get hit away
            transform.position += m_travelDirection * m_speed * Time.deltaTime;
            m_overshootTimer += Time.deltaTime;

            if (m_overshootTimer >= m_overshootSeconds)
            {
                m_isActive = false;
                gameObject.SetActive(false);
            }
        }
    }

    public void Hit()
    {
        m_isActive = false;
        m_noteSpawner.SpawnParticles(transform); // Create particles
        gameObject.SetActive(false);
    }

    public int GetTargetBeat() => m_targetBeat;
}