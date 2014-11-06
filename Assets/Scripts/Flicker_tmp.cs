using UnityEngine;
using System.Collections;

public class Flicker_tmp : MonoBehaviour {
	
	float flickerSpeed;
	float newBrightness;
	SpriteRenderer lightSprite;
	public Light lightSource;
	
	// Use this for initialization
	void Start () {
		flickerSpeed = Random.Range(.05f,.25f);
		newBrightness = Random.Range(.5f,1f);
		if(lightSource != null)
		{
			StartCoroutine("flickerSprite");
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	IEnumerator flickerSprite(){	
		while(true)
		{
			lightSource.intensity = newBrightness;
			newBrightness = Random.Range(.1f,.5f);
			flickerSpeed = Random.Range(.03f,.3f);
			yield return new WaitForSeconds (flickerSpeed);
			
			lightSource.intensity = newBrightness;
			newBrightness = Random.Range(.2f,.5f);
			flickerSpeed = Random.Range(.01f,.1f);
			yield return new WaitForSeconds (flickerSpeed);
			
		}
	}
}
