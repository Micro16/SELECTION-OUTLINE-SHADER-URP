using UnityEngine;

public class DepthCameraMode : MonoBehaviour
{
    private void Awake()
    {
        
        Camera cam = GetComponent<Camera>();
        if (cam.depthTextureMode != DepthTextureMode.Depth )
            cam.depthTextureMode = DepthTextureMode.Depth;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
