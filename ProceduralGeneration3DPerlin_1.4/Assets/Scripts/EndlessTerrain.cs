using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class EndlessTerrain : MonoBehaviour
{
	private const float viewerMoveThresholdForChunkUpdate = 25f;

	private const float sqrViewerMoveThresholdForChunkUpdate =
		viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	[SerializeField] private int colliderLODIndex;
	[SerializeField] private static float maxViewDst;
	[SerializeField] private LODInfo[] detailLevels;
	[SerializeField] private Transform viewer;
	[SerializeField] private GameObject waterPlane;
	
	[SerializeField] private Material meshMaterial;
	[SerializeField] private Material pixelMaterial;
	[SerializeField] private float waterLevel;

	[SerializeField] private static Vector2 viewerPosition;
	private Vector2 viewerPositionOld;
	private static MapGenerator mapGenerator;
	private static PlacementGenerator placementGenerator;
	private float meshWorldSize;
	private int chunksVisibleInViewDst;

	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	private void Start()
	{
		mapGenerator = FindObjectOfType<MapGenerator>();
		placementGenerator = FindObjectOfType<PlacementGenerator>();
		maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		UpdateVisibleChunks();
	}

	private void Update()
	{
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
		{
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	private void UpdateVisibleChunks()
	{
		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
		{
			terrainChunksVisibleLastUpdate[i].SetVisible(false);
		}
		terrainChunksVisibleLastUpdate.Clear();

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
		{
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
				{
					terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
				}
				else
				{
					if (mapGenerator.meshSettings.usePixelMaterial)
					{
						terrainChunkDictionary.Add(viewedChunkCoord,
							new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform,
								pixelMaterial, waterPlane, waterLevel));
					}
					else
					{
						terrainChunkDictionary.Add(viewedChunkCoord,
							new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform,
								meshMaterial, waterPlane, waterLevel));
					}
				}
			}
		}
	}

	public class TerrainChunk
	{
		private GameObject meshObject;
		private Vector2 sampleCentre;
		private Bounds bounds;

		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;
		private MeshCollider meshCollider;

		private LODInfo[] detailLevels;
		private LODMesh[] lodMeshes;
		private int colliderLODIndex;
		private LODMesh collisionLODMesh;

		private MapData mapData;
		private bool mapDataReceived;
		private int previousLODIndex = -1;

		private GameObject water;
		private float waterLevel;

		public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIndex,
			Transform parent, Material material, GameObject waterPlane, float waterLevel)
		{
			this.detailLevels = detailLevels;
			this.colliderLODIndex = colliderLODIndex;
			this.waterLevel = waterLevel;

			sampleCentre = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
			Vector3 position = coord * meshWorldSize;

			bounds = new Bounds(position, Vector2.one * meshWorldSize);
			
			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshRenderer.material = material;
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();

			meshObject.transform.position = new Vector3(position.x, 0, position.y);
			meshObject.transform.parent = parent;
			water = Instantiate(waterPlane, meshObject.transform.position + new Vector3(0, this.waterLevel, 0), Quaternion.identity, parent);

			SetVisible(false);

			lodMeshes = new LODMesh[this.detailLevels.Length];
			for (int i = 0; i < this.detailLevels.Length; i++)
			{
				lodMeshes[i] = new LODMesh(this.detailLevels[i].lod, UpdateTerrainChunk);
				if (this.detailLevels[i].lod == this.colliderLODIndex)
				{
					collisionLODMesh = lodMeshes[i];
				}
			}

			mapGenerator.RequestMapData(sampleCentre, OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData)
		{
			this.mapData = mapData;
			mapDataReceived = true;

			if (mapGenerator.meshSettings.usePixelMaterial)
			{
				Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapGenerator.meshSettings.numVertsPerLine,
					mapGenerator.meshSettings.numVertsPerLine);
				meshRenderer.material.mainTexture = texture;
			}

			UpdateTerrainChunk();
		}

		public void UpdateTerrainChunk()
		{
			if (mapDataReceived)
			{
				float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				if (visible)
				{
					int lodIndex = 0;

					for (int i = 0; i < detailLevels.Length - 1; i++)
					{
						if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
							lodIndex = i + 1;
						else
							break;
					}

					if (lodIndex != previousLODIndex)
					{
						LODMesh lodMesh = lodMeshes[lodIndex];
						if (lodMesh.hasMesh)
						{
							previousLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
							int seed = ((int)meshObject.transform.position.x * 73103 + (int)meshObject.transform.position.z * 83299) % 1779033703;
							placementGenerator.GeneratePrefabOnChunk(meshObject.transform,
								new Vector2(-120 + meshObject.transform.position.x,
									120 + meshObject.transform.position.x),
								new Vector2(-120 + meshObject.transform.position.z,
									120 + meshObject.transform.position.z), seed);
						}
						else if (!lodMesh.hasRequestedMesh)
						{
							lodMesh.RequestMesh(mapData);
						}
					}

					if (lodIndex == 0)
					{
						if (collisionLODMesh.hasMesh)
						{
							meshCollider.sharedMesh = collisionLODMesh.mesh;
						}
						else if (!collisionLODMesh.hasRequestedMesh)
						{
							collisionLODMesh.RequestMesh(mapData);
						}
					}

					terrainChunksVisibleLastUpdate.Add(this);
				}
				SetVisible(visible);
			}
		}

		public void SetVisible(bool visible)
		{
			meshObject.SetActive(visible);
			water.SetActive(visible);
		}

		public bool IsVisible()
		{
			return meshObject.activeSelf;
		}
	}

	class LODMesh
	{
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		private int lod;
		private System.Action updateCallback;

		public LODMesh(int lod, System.Action updateCallback)
		{
			this.lod = lod;
			this.updateCallback = updateCallback;
		}
		public void RequestMesh(MapData mapData)
		{
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}

		void OnMeshDataReceived(MeshData meshData)
		{
			mesh = meshData.CreateMesh();
			hasMesh = true;
			updateCallback();
		}
	}

	[System.Serializable]
	public struct LODInfo
	{
		public int lod;
		public float visibleDstThreshold;
	}
}
