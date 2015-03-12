using UnityEngine;
using System.Collections;

public class DisplayIfHost : MonoBehaviour {

	void Start ()
	{
		GetComponent<Renderer>().enabled = NetworkAgent.GetIsHost();
	}
}
