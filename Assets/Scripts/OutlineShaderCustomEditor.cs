using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


#if UNITY_EDITOR

/// <summary>
/// Class for defining custom GUI for Outline shader properties
/// </summary>
public class OutlineShaderCustomEditor : ShaderGUI
{
    
    /// <summary>
    /// Called by the owner (material) to render custom editor window
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


        /// Setup outline color field
        Color color = EditorGUILayout.ColorField("Outline Color", material.GetColor("_OutlineColor"));


        /// Setup scale with depth toggle
        bool scaleWithDepth = EditorGUILayout.Toggle("Scale with Depth", Convert.ToBoolean(material.GetFloat("_ScaleWithDepth")));


        /// Store maximum depth value
        float maxDepth = material.GetFloat("_MaxDepth");


        /// Store minimum thickness value
        float minThickness = material.GetFloat("_MinThickness");


        /// If scale with depth is checked 
        if (scaleWithDepth)
        {
            /// Setup maximum depth slider 
            maxDepth = EditorGUILayout.Slider("Maximum Depth", material.GetFloat("_MaxDepth"), 0.0f, 1.0f);


            /// Setup minimum thickness field
            minThickness = EditorGUILayout.FloatField("Minimum Thickness", material.GetFloat("_MinThickness"));
        }


        /// Edit shader properties if change has occurred
        if (EditorGUI.EndChangeCheck())
        {
            material.SetFloat("_OutlineThickness", thickness);
            material.SetColor("_OutlineColor", color);
            material.SetFloat("_ScaleWithDepth", scaleWithDepth ? 1.0f : 0.0f);
            material.SetFloat("_MaxDepth", maxDepth);
            material.SetFloat("_MinThickness", minThickness);
        }
    }

}

#endif