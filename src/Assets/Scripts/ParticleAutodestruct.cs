using UnityEngine;
using System.Collections;

public class ParticleAutodestruct : MonoBehaviour
{
	void Start ()
	{
		if(!GetComponent<ParticleSystem>().loop)
		{
			Destroy(gameObject, GetComponent<ParticleSystem>().duration);
		}
	}
	
	public void DestroyGracefully()
	{
		DestroyGracefully(gameObject);
	}
	
	static public void DestroyGracefully(GameObject go)
	{
		go.transform.parent = null;
		go.GetComponent<ParticleSystem>().loop = false;
		go.GetComponent<ParticleSystem>().enableEmission = false;
		Destroy(go, go.GetComponent<ParticleSystem>().duration);
	}
}
