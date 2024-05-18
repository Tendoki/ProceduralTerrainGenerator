using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
	[SerializeField] private Renderer textureRenderer;
	[SerializeField] private MeshFilter meshFilter;
	[SerializeField] private MeshRenderer meshRenderer;
	[SerializeField] private MeshCollider meshCollider;

	public void DrawTexture(Texture2D texture)
	{
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1,  texture.height);
	}

	public void DrawMesh(MeshData meshData, Texture2D texture)
	{
		Mesh mesh = meshData.CreateMesh();
		meshFilter.sharedMesh = mesh;
		meshRenderer.sharedMaterial.mainTexture = texture;
		meshCollider.sharedMesh = mesh;
	}
}
