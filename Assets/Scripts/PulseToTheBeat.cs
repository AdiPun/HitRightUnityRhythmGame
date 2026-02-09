using System.Collections;
using UnityEngine;

public class PulseToTheBeat : MonoBehaviour
{
    [SerializeField] bool _useTestBeat;
    [SerializeField] float _pulseSize = 1.15f;
    [SerializeField] float _returnSpeed = 5f;

    private Vector3 _startSize;

    private void Start()
    {
        _startSize = transform.localScale; // _startSize is just the scale of the object

        // If this is checked, it'll just pulse every second
        if (_useTestBeat)
        {
            StartCoroutine(TestBeat());
        }
    }

    private void Update()
    {
        // The scale will always gradually shrink down to original size
        transform.localScale = Vector3.Lerp(transform.localScale, _startSize, Time.deltaTime * _returnSpeed);
    }

    public void Pulse()
    {
        // Makes the scale multiplied by the size of the pulse
        transform.localScale = _startSize * _pulseSize;
    }

    IEnumerator TestBeat()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            Pulse();
        }
    }

}
