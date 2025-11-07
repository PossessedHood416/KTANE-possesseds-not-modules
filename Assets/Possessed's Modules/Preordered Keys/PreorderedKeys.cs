//Your order has arrived! It's another Ordered Keys.
//there are so many horrible locs here
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
	private Coroutine HatchCoroutine = null;
	private int Phase = 1;

	private string[] KeyStrings = new string[6];
	private List<string[]> Permutations = new List<string[]>();
	
	private int ScreenIndex = 0;
	private List<string> KeysInDisplay = new List<string>();

	private int RuleIndex = 0;
	private List<string[]> Rules = new List<string[]>();

	private string[][] Answers = new string[4][];
	private int LatePhaseCounter = 0;
	
	private string[] Opperators = {"=", "<", ">", "↔", "-", ":", "←", "→"};
	private int[][] OpOffsets = new int[][] {
		new int[] {0},					// =
		new int[] {+1},					// <
		new int[] {-1},					// >
		new int[] {-1, +1},				// ↔
		new int[] {-2, +2},				// -
		new int[] {-3, +3},				// :
		new int[] {+5,+4,+3,+2,+1},		// ←
		new int[] {-5,-4,-3,-2,-1},		// →
	};
	
	//just ship it ffs
	public class StringArrayComparer : IEqualityComparer<string[]>{
		public bool Equals(string[] x, string[] y){
			if (x == null || y == null) return x == y;
			return x.Length == y.Length && !x.Where((t, i) => !t.Equals(y[i])).Any();
		}

		public int GetHashCode(string[] obj){
			if (obj == null) return 0;
			return obj.Aggregate(0, (hash, item) => hash ^ item.GetHashCode());
		}
	}

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

		if(KeysInDisplay.Count != 0) return;

		int i = 0;
		for(; i < 6; i++){
			if(KeyKMS[i] == KMS) break;
		}
		
		if(Phase == 1){
			return;
		} else if(Phase == 2 && isModOpen && KMS.transform.localPosition.y <= 0.001f){
			KMS.transform.localPosition += new Vector3(0f, 0.01f, 0f);

			Answers[2][LatePhaseCounter] = Answers[0][i];

			LatePhaseCounter++;
			if(LatePhaseCounter == 6){
				Phase = 3;
				LatePhaseCounter = 0;
				Debug.LogFormat("[Preordered Keys #{0}] Answer 3: {1}.", ModuleId, string.Join(" ", Answers[2]));
				
				//Check ans
				var comparer = new StringArrayComparer();
				
				Debug.LogFormat("[Preordered Keys #{0}] Answer 3 {1} a valid solution.", ModuleId, CheckAns(2) ? "is" : "is not");
				if(!CheckAns(2)){
					Strike();
					return;
				}

				if(comparer.Equals(Answers[0], Answers[2]) || comparer.Equals(Answers[1], Answers[2])){
					Debug.LogFormat("[Preordered Keys #{0}] Answer 3 is the same as a previous answer!", ModuleId);
					Strike();
					return;
				}

			}

		} else if (Phase == 3 && !isModOpen && KMS.transform.localPosition.y > 0.005f){
			KMS.transform.localPosition += new Vector3(0f, -0.01f, 0f);
			
			Answers[3][LatePhaseCounter] = Answers[0][i];

			LatePhaseCounter++;
			if(LatePhaseCounter == 6){
				Phase = 4;
				Debug.LogFormat("[Preordered Keys #{0}] Answer 4: {1}.", ModuleId, string.Join(" ", Answers[3]));
		
				//Check ans
				var comparer = new StringArrayComparer();
				
				Debug.LogFormat("[Preordered Keys #{0}] Answer 4 {1} a valid solution.", ModuleId, CheckAns(3) ? "is" : "is not");
				if(!CheckAns(2)){
					Strike();
					return;
				}

				if(comparer.Equals(Answers[0], Answers[3]) || comparer.Equals(Answers[1], Answers[3]) || comparer.Equals(Answers[2], Answers[3])){
					Debug.LogFormat("[Preordered Keys #{0}] Answer 4 is the same as a previous answer!", ModuleId);
					Strike();
					return;
				}

				Solve();
			}

		}
		
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

		if(Phase != 1) return;
		if(KeyOBJ[i].activeInHierarchy) return;

		Answers[0][i] = KeysInDisplay[ScreenIndex];
		Answers[1][6 - KeysInDisplay.Count] = KeysInDisplay[ScreenIndex];

		Slot.AddInteractionPunch(0.1f);
		//Debug.LogFormat("[Preordered Keys #{0}] Placed {1} into position {2}.", ModuleId, KeysInDisplay[ScreenIndex], i+1);

		SetKeyProp(KeyOBJ[i], KeysInDisplay[ScreenIndex]);
		StartCoroutine(InsertKey(KeyKMS[i]));
		
		KeyOBJ[i].SetActive(true);
		SlotOBJ[i].SetActive(false);

		KeysInDisplay.Remove(KeysInDisplay[ScreenIndex]);
		
		if(KeysInDisplay.Count != 0){
			ScreenIndex %= KeysInDisplay.Count;
		} else {
			Phase = 2;
			Debug.LogFormat("[Preordered Keys #{0}] All keys placed, moving onto next phase.", ModuleId);
			Debug.LogFormat("[Preordered Keys #{0}] Answer 1: {1}.", ModuleId, string.Join(" ", Answers[0]));
			Debug.LogFormat("[Preordered Keys #{0}] Answer 2: {1}.", ModuleId, string.Join(" ", Answers[1]));
		
			//Check the answers
			var comparer = new StringArrayComparer();
			
			Debug.LogFormat("[Preordered Keys #{0}] Answer 1 {1} a valid solution.", ModuleId, CheckAns(0) ? "is" : "is not");
			if(!CheckAns(0)){
				Strike();
				return;
			}

			Debug.LogFormat("[Preordered Keys #{0}] Answer 2 {1} a valid solution.", ModuleId, CheckAns(1) ? "is" : "is not");
			if(!CheckAns(1)){
				Strike();
				return;
			}

			if(comparer.Equals(Answers[0], Answers[1])){
				Debug.LogFormat("[Preordered Keys #{0}] Answer 1 is equal to answer 2!", ModuleId);
				Strike();
				return;
			}		

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

		if(UB == UnderButtonKMS[0]){ //left
			RuleIndex = (RuleIndex+Rules.Count-1) % Rules.Count;
		} else { //right
			RuleIndex = (RuleIndex+1) % Rules.Count; 
		}

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
		Answers = Answers.Select(x => new string[6]).ToArray();

		foreach(KMSelectable KMS in KeyKMS) KMS.transform.localPosition = new Vector3(KMS.transform.localPosition.x, 0.5f, KMS.transform.localPosition.z);
		foreach(GameObject OBJ in KeyOBJ) OBJ.SetActive(false);
		foreach(GameObject HL in UnderButtonHL) HL.SetActive(false);

		//add all Permutations
		for(int i = 0; i < 720; i++) Permutations.Add(Permuto(KeyStrings, i));
		
		GenRules();
		Debug.LogFormat("[Preordered Keys #{0}] Your preorder consists of: {1}", ModuleId, string.Join(", ", KeysInDisplay.ToArray()));	 

		for(int i = 0; i < Rules.Count; i++){
			Debug.LogFormat("[Preordered Keys #{0}] Rule #{1}: {2}", ModuleId, i+1, string.Join(" ", Rules[i]));
		}

		Debug.LogFormat("[Preordered Keys #{0}] Remaining solutions: {1}", ModuleId, Permutations.Distinct().ToList().Count);

		for(int i = 0; i < 4; i++){
			Debug.LogFormat("[Preordered Keys #{0}] Example solution #{1}: {2}", ModuleId, i+1, string.Join(" ", Permutations[i]));
		}

		//filt log remaining solutions
		for(int i = 4; i < Permutations.Count; i++){
			Debug.LogFormat("<Preordered Keys #{0}> Example solution #{1}: {2}", ModuleId, i+1, string.Join(" ", Permutations[i]));
		}

		UpdateScreen();
		UpdateUnderScreen();
	}

	void Solve () {
		GetComponent<KMBombModule>().HandlePass();
		ModuleSolved = true;
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
		Debug.LogFormat("[Preordered Keys #{0}] Strike!", ModuleId);
		SoftReset();
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

	static float sigmoidLerp(float i){
		//see 6dsp
		return 2.013475894f/(1+Mathf.Pow(2.718281828459f, -7*i)) - 1.0f;
	}

	void GenRules(){
		int i = 0;
		while(Rules.Count < 3 && Permutations.Count > 12 && i < 40){
			i++;
			string[] candidate = MakeRule();
			if(!TestRule(candidate)) continue;
			Rules.Add(candidate);
		}
	}

	string[] MakeRule(){
		int leftProp = Rnd.Range(0,4);
		int rightProp = (leftProp+Rnd.Range(1,4))%4;

		string left = "";
		string right = "";

		if(leftProp == 3) left = Rnd.Range(1,7).ToString(); 
		else left = KeyStrings[Rnd.Range(0,6)][leftProp].ToString();
		if(leftProp < 2) left += "!";

		if(rightProp == 3) right = Rnd.Range(1,7).ToString();
		else right = KeyStrings[Rnd.Range(0,6)][rightProp].ToString();
		if(rightProp < 2) right += "!";

		string mid = Opperators[Rnd.Range(0,Opperators.Length)];
		mid += Rnd.Range(0,4) == 0 ? "!" : ""; //can i hit this fucking wheel

		return new string[] {left, mid, right};
	}

	bool TestRule(string[] rule){
		//linq is fucking goated
		HashSet<string[]> subPermu = new HashSet<string[]>(Permutations.Where(x => TestPerm(x, rule)).ToList(), new StringArrayComparer());

		if(subPermu.Count == Permutations.Count || subPermu.Count < 12) return false;

		Permutations.Clear();
		Permutations = new List<string[]>(subPermu);
		
		return true;
	}

	bool TestPerm(string[] perm, string[] rule){
		List<int> leftCandidates = new List<int>();
		List<int> rightCandidates = new List<int>();
		List<int> expectedCandidates = new List<int>();

		int leftPropType = (rule[0].Contains("!") ? 0 : 2) + (Regex.IsMatch(rule[0], @"[0-9]") ? 1 : 0);
		int rightPropType = (rule[2].Contains("!") ? 0 : 2) + (Regex.IsMatch(rule[2], @"[0-9]") ? 1 : 0);

		if(leftPropType == 3) leftCandidates.Add(int.Parse(rule[0]));
		else for(int i = 0; i < 6; i++){
			if(perm[i][leftPropType] == rule[0][0]) leftCandidates.Add(i+1);
		}

		if(rightPropType == 3) rightCandidates.Add(int.Parse(rule[2]));
		else for(int i = 0; i < 6; i++){
			if(perm[i][rightPropType] == rule[2][0]) rightCandidates.Add(i+1);
		}

		int opIndex = -1;

		//too lazy to do it another way
		for(int i = 0; i < Opperators.Length; i++){
			if(Opperators[i][0] == rule[1][0]){
				opIndex = i;
				break;
			}
		}

		// = < > ↔ - : ← →

		foreach(int offset in OpOffsets[opIndex]){
			foreach(int l in leftCandidates){
				expectedCandidates.Add(l+offset);
			}
		}

		bool isPair = expectedCandidates.Intersect(rightCandidates).Count() > 0;

		/*
		Debug.LogFormat("Testing perm {0}", string.Join(" ", perm));
		Debug.LogFormat("L:{0} || R:{1} || E:{2}",
			string.Join(" ", leftCandidates.Select(x => x.ToString()).ToArray()),
			string.Join(" ", rightCandidates.Select(x => x.ToString()).ToArray()),
			string.Join(" ", expectedCandidates.Select(x => x.ToString()).ToArray()));
		Debug.LogFormat(isPair ? "Pass." : "Fail.");
		*/

		return isPair ^ rule[1].Contains("!");
	}

	bool CheckAns(int i){
		var comparer = new StringArrayComparer();

		foreach(string[] c in Permutations){
			if(comparer.Equals(Answers[i], c))
				return true;
		}

		return false;
	}

	void SoftReset(){
		Audio.PlaySoundAtTransform("PROK_Spark", ScreenButton.transform);

		Phase = 1;
		ScreenIndex = 0;
		KeysInDisplay = new List<string>(KeyStrings);

		Answers = Answers.Select(x => new string[6]).ToArray();
		LatePhaseCounter = 0;

		foreach(KMSelectable KMS in KeyKMS) KMS.transform.localPosition = new Vector3(KMS.transform.localPosition.x, 0.5f, KMS.transform.localPosition.z);
		foreach(GameObject OBJ in KeyOBJ) OBJ.SetActive(false);
		foreach(GameObject OBJ in SlotOBJ) OBJ.SetActive(true);

		UpdateScreen();
		UpdateUnderScreen();

		Debug.LogFormat("[Preordered Keys #{0}] Resetting...", ModuleId);
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} SOLVE to force solve (unsupported).";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		yield return null;
		if(Command.ToUpper() == "SOLVE") Solve();
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
	}
}
