using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Threading;
using Unity.VisualScripting;

public class MapGenerator : MonoBehaviour
{
	public enum DrawMode
	{
		NoiseMap,
		ColorMap,
		Mesh,
		FalloffMap
	}

	[SerializeField] private DrawMode drawMode;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureData;

	[SerializeField] private Material terrainMaterial;

	[Range(0,6)]
	[SerializeField] private int editorPreviewLOD;

	public bool autoUpdate;

	public TerrainType[] regions;

	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void OnValuesUpdated()
	{
		if (!Application.isPlaying)
		{
			DrawMapInEditor();
		}
	}

	public void DrawMapInEditor()
	{
		//float[,] values = Noise.GenerateNoiseMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine,
		//	heightMapSettings.noisesSettings[0], Vector2.zero);
		//float[,] values = HeightMapGenerator.GenerateNoiseWithCurve(meshSettings.numVertsPerLine,
		//	meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
		float[,] values = HeightMapGenerator.GenerateMultiNoiseWithCurve(meshSettings.numVertsPerLine,
			meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

		MapData mapData = HeightMapGenerator.GenerateMultiNoiseMap(meshSettings.numVertsPerLine,
			meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
		//MapData mapData = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine,
		//	meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();

		if (drawMode == DrawMode.NoiseMap)
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(values));
		else if (drawMode == DrawMode.ColorMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, meshSettings.numVertsPerLine, meshSettings.numVertsPerLine));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			display.DrawMesh(
				MeshGenerator.GenerateTerrainMesh(mapData.valuesPerlin, meshSettings, editorPreviewLOD), 
					TextureGenerator.TextureFromColorMap(mapData.colorMap, meshSettings.numVertsPerLine, meshSettings.numVertsPerLine));
		}
		else if (drawMode == DrawMode.FalloffMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine)));
		}
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback)
	{
		ThreadStart threadStart = delegate { MapDataThread(centre, callback); };
		new Thread(threadStart).Start();
	}

	void MapDataThread(Vector2 centre, Action<MapData> callback)
	{
		//MapData mapData = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine,
		//	meshSettings.numVertsPerLine, heightMapSettings, centre);
		MapData mapData = HeightMapGenerator.GenerateMultiNoiseMap(meshSettings.numVertsPerLine,
			meshSettings.numVertsPerLine, heightMapSettings, centre);
		lock (mapDataThreadInfoQueue)
		{
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate { MeshDataThread(mapData, lod, callback); };
		new Thread(threadStart).Start();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData =
			MeshGenerator.GenerateTerrainMesh(mapData.valuesPerlin, meshSettings, lod);
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	private void Update()
	{
		if (mapDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parametr);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parametr);
			}
		}
	}

	private void OnValidate()
	{
		if (meshSettings != null)
		{
			meshSettings.OnValuesUpdated -= OnValuesUpdated;
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}

		if (heightMapSettings != null)
		{
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
	}

	struct MapThreadInfo<T>
	{
		public readonly Action<T> callback;
		public readonly T parametr;

		public MapThreadInfo(Action<T> callback, T parametr)
		{
			this.callback = callback;
			this.parametr = parametr;
		}
	}
}

