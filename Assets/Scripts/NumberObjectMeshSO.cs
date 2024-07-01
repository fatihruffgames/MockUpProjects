using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/NumberObjectMeshScriptableObject", order = 1)]
public class NumberObjectMeshSO : ScriptableObject
{
    public List<ColorData> meshColorData;
}
[System.Serializable]
public class ColorData
{
    public GameObject textMesh;
    public Color color;
}
