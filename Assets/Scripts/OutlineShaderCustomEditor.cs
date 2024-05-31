using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 
/// </summary>
public class OutlineShaderCustomEditor : ShaderGUI
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="materialEditor">The MaterialEditor that are calling this OnGUI (the 'owner')</param>
    /// <param name="properties">Material properties of the current selected shader</param>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        /// Get targeted material
        Material material = materialEditor.target as Material;


        /// Setup title 
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.padding.left = 0;
        EditorGUILayout.LabelField("Settings", style);


        /// Begin to check if values changed
        EditorGUI.BeginChangeCheck();


        /// Setup outline thickness field
        float thickness = EditorGUILayout.FloatField("Outline Thickness", material.GetFloat("_OutlineThickness"));


        /// Setup scale with depth toggle
        bool scaleWithDepth = EditorGUILayout.Toggle("Scale with Depth", Convert.ToBoolean(material.GetFloat("_ScaleWithDepth")));


        /// Store maximum depth value
        float maxDepth = material.GetFloat("_MaxDepth");


        /// If scale with depth is checked 
        if (scaleWithDepth)
        {
            /// Setup maximum depth slider 
            maxDepth = EditorGUILayout.Slider("Maximum Depth", material.GetFloat("_MaxDepth"), 0.0f, 1.0f);
        }


        /// Edit shader properties if change has occurred
        if (EditorGUI.EndChangeCheck())
        {
            material.SetFloat("_OutlineThickness", thickness);
            material.SetFloat("_ScaleWithDepth", scaleWithDepth ? 1.0f : 0.0f);
            material.SetFloat("_MaxDepth", maxDepth);
        }
    }
}
