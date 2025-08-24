using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotAlchemy : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved;

	public KMSelectable InvertButton;
	public Material[] ReadOnlyMats;
	public TextMesh[] Displays; //screen, sub, redr

	public GameObject[] Everything;


	private Material[] AllMats;

	


	private bool isInverted = false;
	private bool isInverting = false;

	void Awake(){
		InvertButton.OnInteract += delegate () { InvertPress(); return false; };

		AllMats = (Material[])ReadOnlyMats.Clone();
	}

	void InvertPress(){
		if(isInverting) return;

		isInverted = !isInverted;
		isInverting = true;
		foreach(Material m in AllMats) StartCoroutine(LerpInvertMats(m));
		foreach(TextMesh t in Displays) StartCoroutine(LerpInvertText(t));
	}

	void Start(){
		
	}
	
	void Update(){
		
	}

	IEnumerator LerpInvertMats(Material m){
		float q = 0f;
		Color fro = m.color;
		Color to = new Color(1f - fro.r, 1f - fro.g, 1f - fro.b);

		while(q < 1f){
			m.color = Color.Lerp(fro, to, q);
			q+= 0.05f;			
			yield return null;
		}
		m.color = to;

		isInverting = false;
	}

	IEnumerator LerpInvertText(TextMesh t){
		float q = 0f;
		Color fro = t.color;
		Color to = new Color(1f - fro.r, 1f - fro.g, 1f - fro.b);

		while(q < 1f){
			t.color = Color.Lerp(fro, to, q);
			q+= 0.05f;			
			yield return null;
		}
		t.color = to;
	}

}
