using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class PlacementGenerator : MonoBehaviour
{
	[SerializeField] public bool autoUpdate;

	[Header("Raycast Settings")]

	[Space] 

	[SerializeField] private float minHeight;
	[SerializeField] private float maxHeight;
	[SerializeField] private Vector2 xRange;
	[SerializeField] private Vector2 zRange;

	[Header("Prefab Variation Settings")]
	[SerializeField, Range(0, 1)] float rotateTowardsNormal;
	[SerializeField] Vector2 rotationRange;

	[SerializeField] private int seed;
	//[SerializeField] private Vector3 minScale;
	//[SerializeField] private Vector3 maxScale;

	[SerializeField] private List<PlacedPrefab> prefabs;

	public void Generate()
	{
		Clear();

		GeneratePrefabOnChunk(transform, xRange, zRange, seed);
	}

	public void GeneratePrefabOnChunk(Transform parent, Vector2 xRange, Vector2 zRange, int seed)
	{
		for (int i = 0; i < prefabs.Count; i++)
		{
			GeneratePrefab(parent, prefabs[i], xRange, zRange, seed + i);
		}
	}

	public void GeneratePrefab(Transform parent, PlacedPrefab prefab, Vector2 xRange, Vector2 zRange, int seed)
	{
		System.Random prng = new System.Random(seed);
		for (int i = 0; i < prefab.density; i++)
		{
			//float sampleX = Random.Range(xRange.x, xRange.y);
			//float sampleY = Random.Range(zRange.x, zRange.y);

			float sampleX = prng.Next((int)xRange.x, (int)xRange.y);
			float sampleY = prng.Next((int)zRange.x, (int)zRange.y);

			Vector3 rayStart = new Vector3(sampleX, prefab.maxHeight, sampleY);
			RaycastHit hit;
			if (!Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity))
			{
				continue;
			}


			if (hit.point.y < prefab.minHeight)
				continue;

			//GameObject instantiatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(this.prefab.transform);
			GameObject instantiatedPrefab = Instantiate(prefab.gameObject, parent);
			instantiatedPrefab.transform.position = (hit.point - new Vector3(0, prefab.offsetY, 0));

			instantiatedPrefab.transform.Rotate(Vector3.up, UnityEngine.Random.Range(rotationRange.x, rotationRange.y), Space.Self);
			//instantiatedPrefab.transform.rotation = Quaternion.Lerp(transform.rotation,
			//	transform.rotation * Quaternion.FromToRotation(instantiatedPrefab.transform.up, hit.normal),
			//	rotateTowardsNormal);
			instantiatedPrefab.transform.localScale = instantiatedPrefab.transform.localScale * UnityEngine.Random.Range(prefab.minScale, prefab.maxScale);
		}
	}

	public void Clear()
	{
		while (transform.childCount != 0)
		{
			DestroyImmediate(transform.GetChild(0).gameObject);
		}
	}
}

[System.Serializable]
public class PlacedPrefab
{
	public int density;
	public GameObject gameObject;
	public float minScale;
	public float maxScale;
	public float offsetY;
	public float minHeight;
	public float maxHeight;

	public PlacedPrefab(int density, GameObject gameObject, float minScale, float maxScale, float offsetY, float minHeight, float maxHeight)
	{
		this.density = density;
		this.gameObject = gameObject;
		this.minScale = minScale;
		this.maxScale = maxScale;
		this.offsetY = offsetY;
		this.minHeight = minHeight;
		this.maxHeight = maxHeight;
	}
}
