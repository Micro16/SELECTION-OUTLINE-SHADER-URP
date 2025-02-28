using UnityEngine;

public class ModifyTestValue : MonoBehaviour
{
    public OutlineScriptableObject refTest;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (refTest != null)
        {
            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                refTest.test += 1;
            }
            else if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                refTest.test -= 1;
            }
        }
    }
}
