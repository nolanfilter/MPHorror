using UnityEngine;
using System.Collections;

public class Utilities {

	public static int HexToInt( char hex )
	{
		switch( hex )
		{
			case 'a': return 10;
			case 'b': return 11;
			case 'c': return 12;
			case 'd': return 13;
			case 'e': return 14;
			case 'f': return 15;
				
			default: return (int)(hex - '0');
		}
	}
}
