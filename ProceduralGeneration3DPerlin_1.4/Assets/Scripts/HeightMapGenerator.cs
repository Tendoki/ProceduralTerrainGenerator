using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public static class HeightMapGenerator
{
	public static MapData GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
	{
		float[,] values = Noise.GenerateNoiseMap(width + 2, height + 2, settings.noisesSettings[0], sampleCentre);

		if (settings.useFalloff)
		{
			float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(width + 2);

			for (int y = 0; y < height + 2; y++)
				for (int x = 0; x < width + 2; x++)
					if (settings.useFalloff)
						values[x, y] = Mathf.Clamp01(values[x, y] - falloffMap[x, y]);
		}

		Color[] colorMap = ColorMapForRegions(values, width, height, settings.useFalloff, settings.regions);

		AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.typesCurves[0].curve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < values.GetLength(0); i++)
		{
			for (int j = 0; j < values.GetLength(1); j++)
			{
				values[i, j] = heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

				if (values[i, j] > maxValue)
					maxValue = values[i, j];
				if (values[i, j] < minValue)
					minValue = values[i, j];
			}
		}

		return new MapData(values, colorMap, minValue, maxValue);
	}

	public static float[,] GenerateNoiseWithCurve(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
	{
		float[,] values = Noise.GenerateNoiseMap(width + 2, height + 2, settings.noisesSettings[0], sampleCentre);
		AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.typesCurves[0].curve.keys);

		for (int i = 0; i < values.GetLength(0); i++)
		{
			for (int j = 0; j < values.GetLength(1); j++)
			{
				values[i, j] = heightCurve_threadsafe.Evaluate(values[i, j]);
			}
		}

		return values;
	}

	public static MapData GenerateMultiNoiseMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
	{
		int numOfNoises = settings.noisesSettings.Count;
		List<float[,]> values_list = new List<float[,]>();
		AnimationCurve[] heightCurves = new AnimationCurve[numOfNoises];
		AnimationCurve smoothingCurve = new AnimationCurve(settings.smoothingCurve.keys);

		for (int i = 0; i < numOfNoises; i++)
		{
			values_list.Add(Noise.GenerateNoiseMap(width + 2, height + 2, settings.noisesSettings[i], sampleCentre));
			heightCurves[i] = new AnimationCurve(settings.typesCurves[i].curve.keys);
		}

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		float[,] values = new float[height + 2, width + 2];
		for (int i = 0; i < values.GetLength(0); i++)
		{
			for (int j = 0; j < values.GetLength(1); j++)
			{
				values[i, j] = 0;
				float avg = 0;
				for (int k = 0; k < numOfNoises; k++)
				{
					values[i, j] += heightCurves[k].Evaluate(values_list[k][i, j]) * settings.noisesSettings[k].impact;
					avg += settings.noisesSettings[k].impact;
				}
				values[i, j] = smoothingCurve.Evaluate(values[i, j] / avg);
			}
		}

		Color[] colorMap = ColorMapForRegions(values, width, height, false, settings.regions);

		for (int i = 0; i < height + 2; i++)
		{
			for (int j = 0; j < width + 2; j++)
			{
				values[i, j] *= settings.heightMultiplier;
				if (values[i, j] > maxValue)
					maxValue = values[i, j];
				if (values[i, j] < minValue)
					minValue = values[i, j];
			}
		}

		return new MapData(values, colorMap, minValue, maxValue);
	}

	public static float[,] GenerateMultiNoiseWithCurve(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
	{
		int numOfNoises = settings.noisesSettings.Count;
		List<float[,]> values_list = new List<float[,]>();
		AnimationCurve[] heightCurves = new AnimationCurve[numOfNoises];
		AnimationCurve smoothingCurve = new AnimationCurve(settings.smoothingCurve.keys);

		for (int i = 0; i < numOfNoises; i++)
		{
			values_list.Add(Noise.GenerateNoiseMap(width + 2, height + 2, settings.noisesSettings[i], sampleCentre));
			heightCurves[i] = new AnimationCurve(settings.typesCurves[i].curve.keys);
		}

		float[,] values = new float[height + 2, width + 2];
		for (int i = 0; i < values.GetLength(0); i++)
		{
			for (int j = 0; j < values.GetLength(1); j++)
			{
				values[i, j] = 0;
				float avg = 0;
				for (int k = 0; k < numOfNoises; k++)
				{
					values[i, j] += heightCurves[k].Evaluate(values_list[k][i, j]) * settings.noisesSettings[k].impact;
					avg += settings.noisesSettings[k].impact;
				}
				values[i, j] = smoothingCurve.Evaluate(values[i, j] / avg);
			}
		}

		return values;
	}

	private static Color[] ColorMapForRegions(float[,] noiseMap, int mapWidth, int mapHeight, bool useFalloff, TerrainType[] regions)
	{
		Color[] colorMap = new Color[mapWidth * mapHeight];
		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < regions.Length; i++)
				{
					if (currentHeight >= regions[i].height)
					{
						colorMap[y * mapWidth + x] = regions[i].color;
					}
					else
					{
						break;
					}
				}
			}
		}

		return colorMap;
	}
}

public struct MapData
{
	public readonly float[,] valuesPerlin;
	public readonly float minValue;
	public readonly float maxValue;
	public readonly Color[] colorMap;

	public MapData(float[,] valuesPerlin, Color[] colorMap, float minValue, float maxValue)
	{
		this.valuesPerlin = valuesPerlin;
		this.colorMap = colorMap;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}