using Unity.VisualScripting;
using UnityEngine;

public class ZMove : MonoBehaviour
{
    public float startZ;
    public float endZ;
    public float duration;

    private float time;
    private float halfDuration;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        time = 0.0f;
        Vector3 pos = transform.position;
        pos.z = startZ;
        halfDuration = duration / 2.0f;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        float z, t;
        Vector3 position = transform.position;
        if (time <= halfDuration)
        {
            t = time / halfDuration;
            z = Mathf.Lerp(startZ, endZ, t);
        }
        else
        {
            t = (time - halfDuration) / halfDuration;
            z = Mathf.Lerp(endZ, startZ, t);
            if (t >= 1.0f)
                time = 0.0f;
        }

        position.z = z;
        transform.position = position;
    }
}
