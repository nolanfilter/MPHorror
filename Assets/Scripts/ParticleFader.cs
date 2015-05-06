using UnityEngine;
using System.Collections;

public class ParticleFader : MonoBehaviour {
	
	void Awake ()
	{
		
	}

	void Update ()
	{

	}

	public void DisableEmission()
	{
		this.GetComponent<ParticleSystem>().enableEmission = false;
	}
}
