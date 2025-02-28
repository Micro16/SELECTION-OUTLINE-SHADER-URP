using UnityEngine;

[CreateAssetMenu(fileName = "OutlineScriptableObject", menuName = "Scriptable Objects/OutlineScriptableObject")]
public class OutlineScriptableObject : ScriptableObject
{
    public int test = 0;
    public Material[] materials;
}