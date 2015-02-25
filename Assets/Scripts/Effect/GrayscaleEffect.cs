using UnityEngine;
using System.Collections;

public class GrayscaleEffect : MonoBehaviour {

	private Shader shader;
	private Material material;

	private float grayscaleAmount = 1f;

	void Start()
	{
		if( GetComponents<GrayscaleEffect>().Length > 1 )
			Destroy( this );

		shader = PlayerAgent.GetStunShader();

		if( shader == null )
		{
			enabled = false;
			return;
		}

		material = new Material( shader );
	}

	void Update()
	{
		grayscaleAmount = Mathf.Clamp01( grayscaleAmount );
	}

	void OnRenderImage( RenderTexture source, RenderTexture destination )
	{
		if( material == null )
			return;

		material.SetFloat( "_GrayscaleAmount", grayscaleAmount );
		Graphics.Blit( source, destination, material );
	}
}