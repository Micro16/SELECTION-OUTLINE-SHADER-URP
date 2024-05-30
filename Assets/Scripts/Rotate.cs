using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float rotateSpeed;
    public float movementAmplitude;
    
    
    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        float y =  Mathf.Sin(Time.realtimeSinceStartup) * movementAmplitude;
        Vector3 pos = transform.position;

        pos.y = y;

        transform.position = pos;
        
        
        transform.rotation = Quaternion.Euler(0.0f, rotateSpeed * Time.deltaTime, 0.0f) * transform.rotation;
    }
}
