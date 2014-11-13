using UnityEngine;
using System.Collections;

public class FillScreen: MonoBehaviour {

	public float widthPercentage = 1f;
	public float heightPercentage = 1f;

	public float xPercentage = 0.5f;
	public float yPercentage = 0.5f;

	private Vector3 upperLeftCorner;
	private Vector3 lowerRightCorner;
	private Rect nearRect;

	void Start()
	{
		transform.parent = Camera.main.transform;
		transform.localRotation = Quaternion.identity;
	}

	void LateUpdate()
	{
		upperLeftCorner = Camera.main.transform.InverseTransformPoint( Camera.main.ScreenToWorldPoint( new Vector3( 0f, 0f, Camera.main.nearClipPlane ) ) );
		lowerRightCorner = Camera.main.transform.InverseTransformPoint( Camera.main.ScreenToWorldPoint( new Vector3( Screen.width, Screen.height, Camera.main.nearClipPlane ) ) );
	

		transform.position = Camera.main.ScreenToWorldPoint( new Vector3( Screen.width * xPercentage, Screen.height * yPercentage, Camera.main.nearClipPlane + 0.001f ) );
		transform.localScale = new Vector3( Mathf.Abs( upperLeftCorner.x - lowerRightCorner.x ) * 1.01f * widthPercentage, Mathf.Abs( upperLeftCorner.y - lowerRightCorner.y ) * 1.01f * heightPercentage, 0f );
	}
}
