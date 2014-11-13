using UnityEngine;
using System.Collections;

public class MenuController : MonoBehaviour {

	private InputController inputController;

	void Awake()
	{
		inputController = GetComponent<InputController>();
		
		if( inputController == null )
		{
			Debug.LogError( "No input controller on " + gameObject.name );
			enabled = false;
		}
	}

	void OnEnable()
	{
		inputController.OnButtonDown += OnButtonDown;
		inputController.OnButtonHeld += OnButtonHeld;
		inputController.OnButtonUp += OnButtonUp;
	}
	
	void OnDisable()
	{
		inputController.OnButtonDown -= OnButtonDown;
		inputController.OnButtonHeld -= OnButtonHeld;
		inputController.OnButtonUp -= OnButtonUp;
	}

	//event handlers
	private void OnButtonDown( InputController.ButtonType button )
	{	

	}
	
	private void OnButtonHeld( InputController.ButtonType button )
	{	

	}
	
	private void OnButtonUp( InputController.ButtonType button )
	{
		
	}
	//end event handlers
}
