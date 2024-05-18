using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
	public float meshScale = 1f;

	public bool useFlatShading;
	public bool usePixelMaterial;

	// num verts per line of mesh rendered at LOD = 0
	public int numVertsPerLine
	{
		get
		{
			if (useFlatShading)
				return 97;
			else
				return 241;
		}
	}

	public float meshWorldSize
	{
		get
		{
			return (numVertsPerLine - 1) * meshScale;
		}
	}
}
