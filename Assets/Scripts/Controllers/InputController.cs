using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class InputController : MonoBehaviour {
	
	public delegate void ButtonDownEventHandler( InputController.ButtonType button );
	public event ButtonDownEventHandler OnButtonDown;
	
	public delegate void ButtonHoldEventHandler( InputController.ButtonType button );
	public event ButtonHoldEventHandler OnButtonHeld;
	
	public delegate void ButtonUpEventHandler( InputController.ButtonType button );
	public event ButtonUpEventHandler OnButtonUp;
	
	public enum ButtonType
	{
		Up = 0,
		Down = 1,
		Left = 2,
		Right = 3,
		RUp = 4,
		RDown = 5,
		RLeft = 6,
		RRight = 7,
		Zoom = 8,
		Flashlight = 9,
		Photograph = 10,
		Invalid = 11,
	}
	
	private string verticalAxisString = "Vertical";
	private string horizontalAxisString = "Horizontal";
	private string rVerticalAxisString = "RVertical";
	private string rHorizontalAxisString = "RHorizontal";
	private string leftTriggerAxisString = "Left Trigger";
	private string rightTriggerAxisString = "Right Trigger";
	
	private KeyCode[] codes = new KeyCode[ Enum.GetNames( typeof( ButtonType ) ).Length - 1 ];
	
	private Array buttonTypes = Enum.GetValues( typeof( ButtonType ) );
	
	private Dictionary<ButtonType, bool> currentButtonList;
	private Dictionary<ButtonType, bool> oldButtonList;

	private String currentInputType = "Invalid";
	
	void Start()
	{
		currentButtonList = new Dictionary<ButtonType, bool>();
		oldButtonList = new Dictionary<ButtonType, bool>();

		CheckInputType();
	}
	
	void Update()
	{
		float verticalAxis = Input.GetAxisRaw( verticalAxisString );
		float horizontalAxis = Input.GetAxisRaw( horizontalAxisString );
		
		currentButtonList[ ButtonType.Up ] = verticalAxis > 0f;
		currentButtonList[ ButtonType.Right ] = horizontalAxis > 0f;
		currentButtonList[ ButtonType.Down ] = verticalAxis < 0f;
		currentButtonList[ ButtonType.Left ] = horizontalAxis < 0f;

		float rVerticalAxis = Input.GetAxisRaw( rVerticalAxisString );
		float rHorizontalAxis = Input.GetAxisRaw( rHorizontalAxisString );

		currentButtonList[ ButtonType.RUp ] = rVerticalAxis > 0f;
		currentButtonList[ ButtonType.RRight ] = rHorizontalAxis > 0f;
		currentButtonList[ ButtonType.RDown ] = rVerticalAxis < 0f;
		currentButtonList[ ButtonType.RLeft ] = rHorizontalAxis < 0f;

		currentButtonList[ ButtonType.Zoom ] = ( Input.GetAxisRaw( leftTriggerAxisString ) > 0f );
		currentButtonList[ ButtonType.Photograph ] = ( Input.GetAxisRaw( rightTriggerAxisString ) > 0f );
		
		for( int i = 0; i < codes.Length; i++ )
		{
			if( i == (int)ButtonType.Up || i == (int)ButtonType.Right || i == (int)ButtonType.Down || i == (int)ButtonType.Left ||
				i == (int)ButtonType.RUp || i == (int)ButtonType.RRight || i == (int)ButtonType.RDown || i == (int)ButtonType.RLeft || 
			   	i == (int)ButtonType.Zoom || i == (int)ButtonType.Photograph )
				currentButtonList[ (ButtonType)i ] = currentButtonList[ (ButtonType)i ] || Input.GetKey( codes[ i ] );
			else
				currentButtonList[ (ButtonType)i ] = Input.GetKey( codes[ i ] );
		}
		
		foreach( ButtonType button in buttonTypes )
		{
			if( currentButtonList[ button ] )
			{
				if( oldButtonList[ button ] )
					SendHeldEvent( button );
				else
					SendDownEvent( button );
			}
			else
			{
				if( oldButtonList[ button ] )
					SendUpEvent( button );
			}
			
			oldButtonList[ button ] = currentButtonList[ button ];
		}

		CheckInputType();
	}

	private void CheckInputType()
	{
		if( ( Input.GetJoystickNames().Length > 0 && currentInputType != Input.GetJoystickNames()[0] ) ||
		   ( Input.GetJoystickNames().Length == 0 && currentInputType != "Keyboard" ) )
		{
			if( Input.GetJoystickNames().Length > 0 )
			{
				//hardcoded for PS3 controller, PS4 controller, and PC
				switch( Input.GetJoystickNames()[0] )
				{
					//PS3
				case "Sony PLAYSTATION(R)3 Controller":
				{
					
				} break;
					
					//PS4
				case "Sony Computer Entertainment Wireless Controller":
				{
					
				} break;
					
					//SNES
				case " 2Axes 11Keys Game  Pad":
				{
					
				} break;
					
					//XBOX 360
				case "©Microsoft Corporation Controller": case "":
				{
					codes[ (int)ButtonType.Up ] = (KeyCode)( (int)KeyCode.Joystick1Button5 );
					codes[ (int)ButtonType.Down ] = (KeyCode)( (int)KeyCode.Joystick1Button6 );
					codes[ (int)ButtonType.Left ] = (KeyCode)( (int)KeyCode.Joystick1Button7 );
					codes[ (int)ButtonType.Right ] = (KeyCode)( (int)KeyCode.Joystick1Button8 );
					codes[ (int)ButtonType.RUp ] = (KeyCode)( (int)KeyCode.Joystick1Button19 );
					codes[ (int)ButtonType.RDown ] = (KeyCode)( (int)KeyCode.Joystick1Button19 );
					codes[ (int)ButtonType.RLeft ] = (KeyCode)( (int)KeyCode.Joystick1Button19 );
					codes[ (int)ButtonType.RRight ] = (KeyCode)( (int)KeyCode.Joystick1Button19 );
					codes[ (int)ButtonType.Zoom ] = (KeyCode)( (int)KeyCode.Joystick1Button19 );
					codes[ (int)ButtonType.Flashlight ] = (KeyCode)( (int)KeyCode.Joystick1Button12 );
					codes[ (int)ButtonType.Photograph ] = (KeyCode)( (int)KeyCode.Joystick1Button19 );
					
				} break;
				}
				
				currentInputType = Input.GetJoystickNames()[0];
			}
			else
			{
				codes[ (int)ButtonType.Up ] = KeyCode.W;
				codes[ (int)ButtonType.Down ] = KeyCode.S;
				codes[ (int)ButtonType.Left ] = KeyCode.A;
				codes[ (int)ButtonType.Right ] = KeyCode.D;
				codes[ (int)ButtonType.RUp ] = KeyCode.UpArrow;
				codes[ (int)ButtonType.RDown ] = KeyCode.DownArrow;
				codes[ (int)ButtonType.RLeft ] = KeyCode.LeftArrow;
				codes[ (int)ButtonType.RRight ] = KeyCode.RightArrow;
				codes[ (int)ButtonType.Zoom ] = KeyCode.Space;
				codes[ (int)ButtonType.Flashlight ] = KeyCode.Delete;
				codes[ (int)ButtonType.Photograph ] = KeyCode.Return;
				
				currentInputType = "Keyboard";
			}
			//end hardcoded
			
			currentButtonList.Clear();
			oldButtonList.Clear();
			
			foreach( ButtonType button in buttonTypes )
			{
				currentButtonList.Add( button, false );
				oldButtonList.Add( button, false );
			}
		}
		
	}
	
	private void SendDownEvent( ButtonType button )
	{						
		//Debug.Log( button );
		
		if( OnButtonDown != null )
			OnButtonDown( button );		
	}
	
	private void SendHeldEvent( ButtonType button )
	{
		if( OnButtonHeld != null )
			OnButtonHeld( button );	
	}
	
	private void SendUpEvent( ButtonType button )
	{
		if( OnButtonUp != null )
			OnButtonUp( button );	
	}
	
	public Vector3 getRawAxes()
	{
		float verticalAxis = Input.GetAxisRaw( verticalAxisString );
		float horizontalAxis = Input.GetAxisRaw( horizontalAxisString );
		
		Vector3 rawAxes = new Vector3( horizontalAxis, verticalAxis, 0f );
		
		if( rawAxes == Vector3.zero )
		{
			if( currentButtonList[ ButtonType.Up ] )
				rawAxes += Vector3.up;
			
			if( currentButtonList[ ButtonType.Down ] )
				rawAxes += Vector3.down;
			
			if( currentButtonList[ ButtonType.Left ] )
				rawAxes += Vector3.left;
			
			if( currentButtonList[ ButtonType.Right ] )
				rawAxes += Vector3.right;
		}
		
		return rawAxes;
	}

	public Vector3 getRawRAxes()
	{
		float rVerticalAxis = Input.GetAxisRaw( rVerticalAxisString );
		float rHorizontalAxis = Input.GetAxisRaw( rHorizontalAxisString );
		
		Vector3 rawRAxes = new Vector3( rHorizontalAxis, rVerticalAxis, 0f );
		
		return rawRAxes;
	}
}
