using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(FunctionObjectController))]
public class FunctionObjectControllerEditor : Editor {

	public override void OnInspectorGUI()
	{
		FunctionObjectController foc = (FunctionObjectController)target;

		foc.functionMode = (FunctionObjectController.FunctionMode)EditorGUILayout.EnumPopup( "Function Mode", foc.functionMode );
		foc.functionName = (FunctionObjectController.FunctionName)EditorGUILayout.EnumPopup( "Function Name", foc.functionName );

		switch( foc.functionName )
		{
			case FunctionObjectController.FunctionName.ChangeSanity:
			{
				foc.sanityChange = EditorGUILayout.FloatField( "Sanity Change", foc.sanityChange ); 
				foc.sanityDamageOverTime = EditorGUILayout.Toggle( "Damage Over Time", foc.sanityDamageOverTime );
			} break;

			case FunctionObjectController.FunctionName.ChangeFear:
			{
				foc.fearChange = EditorGUILayout.FloatField( "Fear Change", foc.fearChange );
				foc.fearDamageOverTime = EditorGUILayout.Toggle( "Damage Over Time", foc.fearDamageOverTime );
			} break;

			case FunctionObjectController.FunctionName.SetFlashlightTo:
			{
				foc.flashlightOn = EditorGUILayout.Toggle( "Flashlight On", foc.flashlightOn ); 
			} break;

			case FunctionObjectController.FunctionName.TeleportTo:
			{
				foc.coordinate = EditorGUILayout.Vector3Field( "Coordinate", foc.coordinate ); 
			} break;

			case FunctionObjectController.FunctionName.DisplayMessage:
			{
				foc.message = EditorGUILayout.TextField( "Message", foc.message ); 
			} break;
		}

		foc.eventName = EditorGUILayout.TextField( "Event Name", foc.eventName );
	}
}
