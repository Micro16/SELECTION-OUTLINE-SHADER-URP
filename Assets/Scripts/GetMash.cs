using UnityEngine;


[RequireComponent (typeof(MeshFilter))]
public class GetMash : MonoBehaviour
{
    private Mesh mesh;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
