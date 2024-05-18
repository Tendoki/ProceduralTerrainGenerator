using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
	public TerrainType[] regions;
	public List<NoiseSettings> noisesSettings;
	public List<TypeCurve> typesCurves;
	public AnimationCurve smoothingCurve;

	public float heightMultiplier;
	public bool useFalloff;

	protected override void OnValidate()
	{
		for (int i = 0; i < noisesSettings.Count; i++)
		{
			noisesSettings[i].ValidateValues();
		}
		base.OnValidate();
	}
}

[System.Serializable]
public struct TerrainType
{
	public string name;
	public float height;
	public Color color;
}

[System.Serializable]
public struct TypeCurve
{
	public string name;
	public AnimationCurve curve;
}
