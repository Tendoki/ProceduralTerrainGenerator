using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
	public Color[] baseColours;
	[Range(0, 1)]
	public float[] baseStartHeights;

	float savedMinHeight;
	float savedMaxHeight;
}
