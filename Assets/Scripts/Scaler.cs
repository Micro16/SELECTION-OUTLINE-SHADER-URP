using UnityEngine;


[ExecuteAlways]
public class Scaler : MonoBehaviour
{
    public AnimationCurve smooth;

    public float minDistance = 5.0f;
    public float maxDistance = 100.0f;

    public float minThickness;
    public float maxThickness;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LayerOutlineFeatureAccessor.Instance.Initialize(Camera.main);
    }

    // Update is called once per frame
    void Update()
    {
        if (smooth == null)
            return;     
        
        float m = Vector3.Magnitude(Camera.main.transform.position - transform.position);
        float n = Mathf.InverseLerp(minDistance, maxDistance, m);
        float s = smooth.Evaluate(n);
        float t = Mathf.Lerp(minThickness, maxThickness, 1.0f - s);

        if (Application.isPlaying)
        {
            LayerOutlineFeatureAccessor.Instance.SetLayerOutlineThickness("Monkey", t);
            LayerOutlineFeatureAccessor.Instance.SetLayerOutlineThickness("Others", t);
        }
        else if (Application.isEditor)
        {
            LayerOutlineFeatureAccessor.EditorInstance(Camera.main).SetLayerOutlineThickness("Monkey", t);
            LayerOutlineFeatureAccessor.EditorInstance(Camera.main).SetLayerOutlineThickness("Others", t);
        }
    }
}
