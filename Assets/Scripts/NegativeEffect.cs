using UnityEngine;

public class NegativeEffect : MonoBehaviour {

	public float negativeAmount = 1f;

	private Shader shader;
	private Material material;

	void Start()
	{
		shader = Shader.Find( "Custom/Negative" );
		material = new Material( shader );
	}

	void Update()
	{
		negativeAmount = Mathf.Clamp01 (negativeAmount);
	}

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		material.SetFloat("_NegativeAmount", negativeAmount);
		Graphics.Blit (source, destination, material);
	}
}