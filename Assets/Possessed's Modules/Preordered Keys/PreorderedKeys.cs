//Your order has arrived! It's another Ordered Keys.
//its so fucking messy :(
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class PreorderedKeys : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved;

	public Color[] DullColour;
	private static Color[] BrightColour = {Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow};
	
	public TextMesh DisplayText;
	public MeshRenderer ScreenMat;
	public KMSelectable ScreenButton;

	public KMSelectable[] KeyKMS;
	public GameObject[] KeyOBJ;
	
	public KMSelectable[] SlotKMS;
	public GameObject[] SlotOBJ;

	public KMSelectable HingeButton;
	public GameObject Hatch;

	public KMSelectable[] UnderButtonKMS;
	public GameObject[] UnderButtonHL;
	public TextMesh[] UnderText;

	private bool isModOpen = false;
	private string[] KeyStrings = new string[6];
	private int ScreenIndex = 0;
	private List<string> KeysInDisplay = new List<string>();
	private HashSet<string[]> Permutations = new HashSet<string[]>();
	private int RuleIndex = 0;
	private List<string[]> Rules = new List<string[]>();
	private Coroutine HatchCoroutine = null;

	void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
		ModuleId = ModuleIdCounter++;
		GetComponent<KMBombModule>().OnActivate += Activate;

		ScreenButton.OnInteract += delegate () { ScreenPress(); return false; };

		foreach (KMSelectable Key in KeyKMS) {
			Key.OnInteract += delegate () { KeyPress(Key); return false; };
		}

		foreach (KMSelectable Slot in SlotKMS){
			Slot.OnInteract += delegate () { SlotPress(Slot); return false; };
		}

		foreach(KMSelectable UB in UnderButtonKMS){
			UB.OnInteract += delegate () { UBPress(UB); return false; };
		}

		HingeButton.OnInteract += delegate () { HBPress(); return false; };
	}

	void ScreenPress (){
		if(isModOpen){
			isModOpen = false;
			HatchCoroutine = StartCoroutine(OpenCloseHatch());
			return;
		}

		ScreenButton.AddInteractionPunch(0.1f);
		Audio.PlaySoundAtTransform("PROK_ScreenPress", ScreenButton.transform);

		if(KeysInDisplay.Count == 0) return;

		ScreenIndex = (ScreenIndex + 1) % KeysInDisplay.Count;
		UpdateScreen();
	}

	void KeyPress (KMSelectable KMS){
		if(isModOpen){
			isModOpen = false;
			HatchCoroutine = StartCoroutine(OpenCloseHatch());
			return;
		}
		
		if(KeysInDisplay.Count != 0) return;
		if(KMS.transform.localPosition.y < 0.005) return;
		//[!] do not repress key when its in its put-in ani

		KMS.transform.localPosition += new Vector3(0f, -0.01f, 0f);
		KMS.AddInteractionPunch(0.1f);
	}

	void SlotPress (KMSelectable Slot){
		if(isModOpen){
			isModOpen = false;
			HatchCoroutine = StartCoroutine(OpenCloseHatch());
			return;
		}

		int i = 0;
		for(; i < 6; i++){
			if(SlotKMS[i] == Slot) break;
		}

		if(KeyOBJ[i].activeInHierarchy) return;

		Slot.AddInteractionPunch(0.1f);
		Debug.LogFormat("[Preordered Keys #{0}] Placed {1} into position {2}.", ModuleId, KeysInDisplay[ScreenIndex], i+1);

		SetKeyProp(KeyOBJ[i], KeysInDisplay[ScreenIndex]);
		KeyOBJ[i].SetActive(true);
		
		StartCoroutine(InsertKey(KeyKMS[i]));
		SlotOBJ[i].SetActive(false);

		KeysInDisplay.Remove(KeysInDisplay[ScreenIndex]);
		
		if(KeysInDisplay.Count != 0){
			ScreenIndex %= KeysInDisplay.Count;
		} else {
			StartCoroutine(SecondPhaseAni());
		}
		UpdateScreen();
	}

	void HBPress(){
		ScreenButton.AddInteractionPunch();
		if(HatchCoroutine != null) return;

		isModOpen = !isModOpen;
		HatchCoroutine = StartCoroutine(OpenCloseHatch());
	}

	void UBPress(KMSelectable UB){
		if(!isModOpen) return;

		UB.AddInteractionPunch(0.1f);
		Audio.PlaySoundAtTransform("PROK_ScreenPress", ScreenButton.transform);

		RuleIndex += (UB == UnderButtonKMS[1]) ? 1 : -1;

		if(RuleIndex < 0) RuleIndex = 0;
		if(RuleIndex >= Rules.Count) Rules.Add(GenNewRule());

		UpdateUnderScreen();
	}

	void OnDestroy () { //Shit you need to do when the bomb ends
		
	}

	void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on		

	}

	void Start () { //Shit that you calculate, usually a majority if not all of the module 
		HashSet<string> keyGenHash = new HashSet<string>();
		while(keyGenHash.Count < 6) keyGenHash.Add("RGBCMY"[Rnd.Range(0, 6)] + Rnd.Range(1, 7).ToString() + "RGBCMY"[Rnd.Range(0, 6)]);

		KeysInDisplay = keyGenHash.ToList();
		KeyStrings = keyGenHash.ToArray();

		Debug.LogFormat("[Preordered Keys #{0}] Your preorder consists of: {1}", ModuleId, string.Join(", ", KeysInDisplay.ToArray()));	 
		UpdateScreen();

		foreach(KMSelectable KMS in KeyKMS) KMS.transform.localPosition += new Vector3(0f,0.5f,0f);
		foreach(GameObject OBJ in KeyOBJ) OBJ.SetActive(false);
		foreach(GameObject HL in UnderButtonHL) HL.SetActive(false);

		//add all permutations
		for(int i = 0; i < 720; i++) Permutations.Add(Permuto(KeyStrings, i));

		Rules.Add(new string[] {"", "", ""});
	}

	void Solve () {
		GetComponent<KMBombModule>().HandlePass();
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
	}

	IEnumerator InsertKey(KMSelectable Key){
		float x = Key.transform.localPosition.x;
		float y = Key.transform.localPosition.y;
		float z = Key.transform.localPosition.z;
		while(y > 0.001f){
			y *= 0.9f;
			Key.transform.localPosition = new Vector3(x,y,z);
			yield return null;
		}
		Key.transform.localPosition = new Vector3(x,0,z);
	}

	IEnumerator SecondPhaseAni(){
		yield return new WaitForSeconds(1.5f);
		foreach(KMSelectable KMS in KeyKMS){
			KMS.transform.localPosition += new Vector3(0f,0.01f,0f);
		}
		//[!] sound button release
	}

	IEnumerator OpenCloseHatch(){
		float fro = Hatch.transform.localEulerAngles.x;
		float to = isModOpen ? 100f : 0f;

		if(isModOpen) Audio.PlaySoundAtTransform("PROK_Open", ScreenButton.transform);
		else Audio.PlaySoundAtTransform("PROK_Close", ScreenButton.transform);

		for(int i = 0; i < 30; i++){
			Hatch.transform.localEulerAngles = new Vector3(Mathf.Lerp(fro, to, sigmoidLerp(i/30f)),0f,0f);
			yield return new WaitForSeconds(0.01f);
		}
		Hatch.transform.localEulerAngles = new Vector3(to,0f,0f);
		
		foreach(GameObject HL in UnderButtonHL) HL.SetActive(isModOpen);
		HatchCoroutine = null;
	}

	void SetKeyProp(GameObject OBJ, string Props){
		TextMesh Txt = OBJ.GetComponentInChildren<TextMesh>();
		
		OBJ.GetComponent<Renderer>().material.color = DullColour["RGBCMY".IndexOf(Props[0])];
		Txt.text = Props[0] + "\n" + Props[1] + "\n\n" + Props[2];
		Txt.color = BrightColour["RGBCMY".IndexOf(Props[2])];
	}

	void UpdateScreen(){
		if(KeysInDisplay.Count == 0){
			DisplayText.color = Color.black;
			DisplayText.text = "";
			ScreenMat.material.color = Color.black;
		} else {
			DisplayText.color = BrightColour["RGBCMY".IndexOf(KeysInDisplay[ScreenIndex][2])];
			DisplayText.text = KeysInDisplay[ScreenIndex][0] + " " + KeysInDisplay[ScreenIndex][1] + " " + KeysInDisplay[ScreenIndex][2];
			ScreenMat.material.color = DullColour["RGBCMY".IndexOf(KeysInDisplay[ScreenIndex][0])];
		}
	}

	void UpdateUnderScreen(){
		if(RuleIndex == 0){
			foreach(TextMesh t in UnderText) t.text = "";
			return;
		}

		//red, key label/color
		//green, key pos/labcol
		string left = Rules[RuleIndex][0];
		string mid = Rules[RuleIndex][1];
		string right = Rules[RuleIndex][2];

		UnderText[0].text = left[0] + "";
		UnderText[1].text = mid[0] + "";
		UnderText[2].text = right[0] + "";

		UnderText[0].color = (left.Contains("!")) ? Color.red : Color.green;
		UnderText[1].color = (mid.Contains("!")) ? Color.red : Color.green;
		UnderText[2].color = (right.Contains("!")) ? Color.red : Color.green;
	}

	string[] Permuto(string[] a, int i){ //trust
		string[] arr = new string[6];
		Array.Copy(a,arr,6);

		for(int j = 0; j < 6; j++){
			string c = arr[j];
			arr[j] = arr[(i%(6-j))+j];
			arr[(i%(6-j))+j] = c;
        }

		return arr;
	}

	void Swap(string a, string b){
		string c = a; a = b; b = c;
	}

	string[] GenNewRule(){
		//red, key label/color
		//green, key pos/labcol
		string left = "";
		string mid = "";
		string right = "";

		string[] gateList = new string[] {"<", ">", "↔"}; 

		int leftTargetProp = Rnd.Range(0,4);
		int rightTargetProp = Rnd.Range(0,4);

		//L prop type
		if(leftTargetProp == 3) left = Rnd.Range(1,7) + "!";
		else left = KeyStrings[Rnd.Range(0,6)][leftTargetProp] + (leftTargetProp == 2 ? "!" : "");
		
		//R prop type
		if(rightTargetProp == 3) right = Rnd.Range(1,7) + "!";
		else right = KeyStrings[Rnd.Range(0,6)][rightTargetProp] + (rightTargetProp == 2 ? "!" : "");

		//gate
		mid = gateList[Rnd.Range(0,gateList.Length)];
		
		//negate
		mid += (Rnd.Range(0,2) == 1) ? "" : "!";

		//check if rule works

		if(false) return GenNewRule();
		else return new string[] {left, mid, right};

	}

	float sigmoidLerp(float i){ return 2.013475894f/(1+Mathf.Pow(2.718281828459f, -7*i)) - 1.0f; } //see 6dsp

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} SOLVE to force solve (unsupported).";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		if(Command.ToUpper() == "SOLVE") Solve();
		yield return null;
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
	}
}
