using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EndScreen : MonoBehaviour {

	public Image background;
	public Sprite bestVacation;
	public Sprite betrayed;
	public Sprite byeBye;
	public Sprite soulCollector;
	public Sprite soulless;
	public Sprite defeatMonster;

	private static EndScreen mInstance = null;
	public static EndScreen instance
	{
		get
		{
			return mInstance;
		}
	}
	
	void Awake()
	{
		if( mInstance != null )
		{
			Debug.LogError( string.Format( "Only one instance of EndScreen allowed! Destroying:" + gameObject.name +", Other:" + mInstance.gameObject.name ) );
			return;
		}
		
		mInstance = this;

		if( background )
		{
			switch( PlayerAgent.GetClientState() )
			{
				case PlayerController.State.Monster:
				{
					if( MannequinAgent.GetAllMannequinsDisabled() )
					{
						if( defeatMonster != null )
							background.sprite = defeatMonster;
					}
					else
					{
						if( soulCollector != null )
							background.sprite = soulCollector;
					}
				} break;

				case PlayerController.State.Voyeur:
				{
					if( Random.value < 0.5f )
					{
						if( betrayed != null )
							background.sprite = betrayed;
					}
					else
					{
						if( soulless != null )
							background.sprite = soulless;
					}
				} break;
				
				case PlayerController.State.None:
				{
					if( MannequinAgent.GetAllMannequinsDisabled() )
					{

						if( Random.value < 0.5f )
						{
							if( bestVacation != null )
								background.sprite = bestVacation;
						}
						else
						{
							if( byeBye != null )
								background.sprite = byeBye;
						}
					}
					else
					{
						if( Random.value < 0.5f )
						{
							if( betrayed != null )
								background.sprite = betrayed;
						}
						else
						{
							if( soulless != null )
								background.sprite = soulless;
						}
					}
				} break;
			}
		}
	}	
}
