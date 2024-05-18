using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlacementGenerator))]
public class PlacementGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		PlacementGenerator placeGen = (PlacementGenerator)target;

		if (DrawDefaultInspector())
		{
			if (placeGen.autoUpdate)
			{
				placeGen.Generate();
			}
		}

		if (GUILayout.Button("Generate"))
		{
			placeGen.Generate();
		}

		if (GUILayout.Button("Clear"))
		{
			placeGen.Clear();
		}
	}
}
