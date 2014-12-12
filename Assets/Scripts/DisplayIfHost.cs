using UnityEngine;
using System.Collections;

public class DisplayIfHost : MonoBehaviour {

	void Start ()
	{
		enabled = NetworkAgent.GetIsHost();
	}
}
