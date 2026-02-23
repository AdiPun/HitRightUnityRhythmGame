using UnityEngine;

public class NoteVisual : MonoBehaviour
{
    private float m_speed;
    private int m_targetBeat;
    private Transform m_target;
    private bool m_isActive = false;

    public void Initialise(Transform target, Transform spawn, int targetBeat, float speed)
    {
        m_target = target;
        m_targetBeat = targetBeat;
        m_speed = speed;
        m_isActive = true;
        transform.position = spawn.position;
    }

    void Update()
    {
        if (!m_isActive) return;

        transform.position = Vector3.MoveTowards(transform.position, m_target.position, m_speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, m_target.position) < 0.01f)
        {
            // Reached the target without being hit, deactivate it
            m_isActive = false;
            gameObject.SetActive(false);
        }
    }

    public void Hit()
    {
        m_isActive = false;
        gameObject.SetActive(false);
    }

    public int GetTargetBeat() => m_targetBeat;
}