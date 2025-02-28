using UnityEngine;

public class DistanceFromCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Magnitude(transform.position - Camera.main.transform.position);
        string name = this.name;

        //Debug.Log(name + " : " +  distance.ToString());
    }
}
