using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
	public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
	{
		int borderedSize = heightMap.GetLength(0);
		int meshSize = borderedSize - 2;
		float topLeftX = (borderedSize - 1) / -2f;
		float topLeftZ = (borderedSize - 1) / 2f;

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData(verticesPerLine, meshSettings.useFlatShading, meshSettings.usePixelMaterial);
		int[,] vertexIndicesMap = new int[verticesPerLine + 2, verticesPerLine + 2];

		int vertexIndex = 0;
		int borderVertexIndex = -1;

		int incrementX = 1;
		int incrementY = 1;

		int ivim = 0; // индекс для vertexIndicesMap
		int jvim = 0; //

		for (int y = 0; y < borderedSize; y+= incrementY)
		{
			for (int x = 0; x < borderedSize; x+= incrementX)
			{
				Vector3 vertexPosition = new Vector3((topLeftX + x) * meshSettings.meshScale, heightMap[x, y], (topLeftZ - y) * meshSettings.meshScale);
				Vector2 uv = new Vector2((x - 1) / (float)meshSize, (y - 1) / (float)meshSize);

				bool isBorderVertex = x == 0 || x == borderedSize - 1 || y == 0 || y == borderedSize - 1;
				if (isBorderVertex)
				{
					vertexIndicesMap[ivim, jvim] = borderVertexIndex;
					meshData.AddVertex(vertexPosition, uv, borderVertexIndex);
					borderVertexIndex--;
				}
				else
				{
					vertexIndicesMap[ivim, jvim] = vertexIndex;
					meshData.AddVertex(vertexPosition, uv, vertexIndex);
					vertexIndex++;
				}

				jvim++;

				if (x == 1)
					incrementX = meshSimplificationIncrement;
				else if (x + incrementX > borderedSize - 1)
					incrementX = 1;
			}

			jvim = 0;
			ivim++;

			if (y == 1)
				incrementY = meshSimplificationIncrement;
			else if (y + incrementY > borderedSize - 1)
				incrementY = 1;
		}

		for (int i = 0; i < vertexIndicesMap.GetLength(0); i++)
		{
			for (int j = 0; j < vertexIndicesMap.GetLength(1); j++)
			{
				if (i < vertexIndicesMap.GetLength(0) - 1 && j < vertexIndicesMap.GetLength(1) -1)
				{
					int a = vertexIndicesMap[i, j];
					int b = vertexIndicesMap[i, j + 1];
					int c = vertexIndicesMap[i + 1, j];
					int d = vertexIndicesMap[i + 1, j + 1];
					meshData.AddTriangle(a, d, c);
					meshData.AddTriangle(d, a, b);
				}
			}
		}

		meshData.ProcessMesh();

		return meshData;
	}
}

public class MeshData
{
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;
	private Vector3[] bakedNormals;

	private Vector3[] borderVertices;
	private int[] borderTriangles;

	private int triangleIndex;
	private int borderTriangleIndex;

	private bool useFlatShading;
	private bool usePixelMaterial;

	public MeshData(int verticesPerLine, bool useFlatShading, bool usePixelMaterial)
	{
		this.useFlatShading = useFlatShading;
		this.usePixelMaterial = usePixelMaterial;
		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
		triangleIndex = 0;
		borderTriangleIndex = 0;

		borderVertices = new Vector3[verticesPerLine * 4 + 4];
		borderTriangles = new int[24 * verticesPerLine];
	}

	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
	{
		if (vertexIndex < 0)
		{
			borderVertices[-vertexIndex - 1] = vertexPosition;
		}
		else
		{
			vertices[vertexIndex] = vertexPosition;
			if (usePixelMaterial)
				uvs[vertexIndex] = uv;
			else
				uvs[vertexIndex] = new Vector2(0, vertexPosition.y);
		}
	}

	public void AddTriangle(int a, int b, int c)
	{
		if (a < 0 || b < 0 || c < 0)
		{
			borderTriangles[borderTriangleIndex] = a;
			borderTriangles[borderTriangleIndex + 1] = b;
			borderTriangles[borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
		}
		else
		{
			triangles[triangleIndex] = a;
			triangles[triangleIndex + 1] = b;
			triangles[triangleIndex + 2] = c;
			triangleIndex += 3;
		}
	}

	Vector3[] CalculateNormals()
	{
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;
		for (int i = 0; i < triangleCount; i++)
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex + 1];
			int vertexIndexC = triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		int borderTriangleCount = borderTriangles.Length / 3;

		for (int i = 0; i < borderTriangleCount; i++)
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = borderTriangles[normalTriangleIndex];
			int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			if (vertexIndexA >= 0)
				vertexNormals[vertexIndexA] += triangleNormal;
			if (vertexIndexB >= 0)
				vertexNormals[vertexIndexB] += triangleNormal;
			if (vertexIndexC >= 0)
				vertexNormals[vertexIndexC] += triangleNormal;
		}

		for (int i = 0; i < vertexNormals.Length; i++)
		{
			vertexNormals[i].Normalize();
		}

		return vertexNormals;
	}

	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
	{
		Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
		Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
		Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross(sideAB, sideAC).normalized;
	}

	private void BakeNormals()
	{
		bakedNormals = CalculateNormals();
	}

	void FlatShading()
	{
		Vector3[] flatShadedVertices = new Vector3[triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[triangles.Length];

		for (int i = 0; i < triangles.Length; i++)
		{
			flatShadedVertices[i] = vertices[triangles[i]];
			flatShadedUvs[i] = uvs[triangles[i]];
			triangles[i] = i;
		}

		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
	}

	public void ProcessMesh()
	{
		if (useFlatShading)
		{
			FlatShading();
		}
		else
		{
			BakeNormals();
		}
	}

	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		if (useFlatShading)
		{
			mesh.RecalculateNormals();
		}
		else
		{
			mesh.normals = bakedNormals;
		}
		return mesh;
	}
}
