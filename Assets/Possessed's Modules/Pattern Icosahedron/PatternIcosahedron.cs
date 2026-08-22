using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using DeafMath = ExMath;

using NetMaker;

public class PatternIcosahedron : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	static int ModuleIdCounter = 1;
	int ModuleId;	
	private bool ModuleSolved;

	public GameObject[] TrisOBJ;
	public KMSelectable[] TrisKMS;
	public GameObject[] SideTrisOBJ;
	public KMSelectable[] SideTrisKMS;
	public GameObject TriParent;
	public Sprite[] IcoSprites;
	public Material[] BorderMats;

	//mostly for debug, tetra and octo were for testing the net maker
	private const int FACE_COUNT = 20;
	private const string TETRAHEDRON = "ABCD;BADC;CDAB;DCBA";
	private const string OCTAHEDRON = "AHFB;BGCA;CHBD;DGEC;EHDF;FGAE;GDBF;HCEA";
	private const string ICOSAHEDRON = "AFEB;BGAC;CHBD;DICE;EJDA;FALK;GBML;HCNM;IDON;JEKO;KPJF;LQFG;MRGH;NSHI;OTIJ;PKQT;QLRP;RMSQ;SNTR;TOPS";
	private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	bool RotaAni = false;
	bool HintMode = false;
	int CurrentDisplay = 0;

	private Graph[] GeneratedGraphs = new Graph[5];
	private bool[] RevealedIcons = new bool[FACE_COUNT];
	private char[] HintedIcons = new char[8];
	private int[] RotationScrambler = new int[FACE_COUNT];

	private int SelectedIconRota = 0;
	private char SelectedIco = '/';
	
	private int[] NetTriRotas;

	//tp
	private bool ShowLabel = false;

	void Awake () { //buttons
		ModuleId = ModuleIdCounter++;
		GetComponent<KMBombModule>().OnActivate += Activate;

		foreach (KMSelectable tri in TrisKMS) {
			tri.OnInteract += delegate () { TriPress(tri); return false; };
		}

		foreach (KMSelectable tri in SideTrisKMS) {
			tri.OnInteract += delegate () { SideTriPress(tri); return false; };
		}
	}

	void TriPress(KMSelectable tri) {
		if(ModuleSolved) return;

		tri.AddInteractionPunch(0.5f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, tri.transform);

		int i;
		for(i = 0; i < FACE_COUNT; i++) if(TrisKMS[i] == tri) break;
		char c = GetTriIcon(TrisOBJ[i]);

		if(CurrentDisplay != 0){
			SelectIco(c);
			return;
		}

		if(c != SelectedIco || SelectedIconRota != (NetTriRotas[i]+RotationScrambler[ALPHABET.IndexOf(c)])%3 ) {
			Strike();
			return;
		}

		Log(string.Format("Correctly placed pattern {0}.", c));
		RevealIco(i);
		if(RevealedIcons.Count(x => x == true) == FACE_COUNT) Solve();
	}

	void SideTriPress(KMSelectable tri) {
		if(ModuleSolved) return;

		tri.AddInteractionPunch(0.5f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, tri.transform);

		int i;
		for(i = 0; i < 6; i++) if(SideTrisKMS[i] == tri) break;

		switch(i){
			case 0:
				if(CurrentDisplay == 1) return;
				CurrentDisplay = 1;
				DisplayNet(GeneratedGraphs[1]);
				break;
			case 1:
				if(CurrentDisplay == 2) return;
				CurrentDisplay = 2;
				DisplayNet(GeneratedGraphs[2]);
				break;
			case 2:
			default:
				StartCoroutine(RotaSelectedIcon());
				if(CurrentDisplay == 0) return;
				CurrentDisplay = 0;
				DisplayNet(GeneratedGraphs[0]);
				break;
			case 3:
				StartCoroutine(RotaSelectedIcon());
				if(CurrentDisplay == 0) return;
				CurrentDisplay = 0;
				DisplayNet(GeneratedGraphs[0]);
				break;
			case 4:
				if(CurrentDisplay == 3) return;
				CurrentDisplay = 3;
				DisplayNet(GeneratedGraphs[3]);
				break;
			case 5:
				if(CurrentDisplay == 4) return;
				CurrentDisplay = 4;
				DisplayNet(GeneratedGraphs[4]);
				break;
		}		
	}


	void OnDestroy () { //bomb ends
		
	}

	void Activate () { //lights on

	}

	void Start () { //calculate
		if(FACE_COUNT != 4 && FACE_COUNT != 8 && FACE_COUNT != 20) throw new ArgumentException(string.Format("Invalid face count: {0}.", FACE_COUNT));
		RevealedIcons = RevealedIcons.Select(x => false).ToArray();
		IcoSprites.Shuffle();

		Log("Check filtered log for debug information, full logging comming soon!");
		SoftLog(string.Format("Attempting to make a net of a(n) {0}.", FACE_COUNT == 4 ? "tetrahedron" : FACE_COUNT == 8 ? "octahedron" : "icosahedron"));
		Graph net = Graph.MakeNet(new Graph(FACE_COUNT == 4 ? TETRAHEDRON : FACE_COUNT == 8 ? OCTAHEDRON : ICOSAHEDRON));
		GeneratedGraphs[0] = net;

		List<Graph> partial = Graph.MakePartialNet(new Graph(ICOSAHEDRON), 5);
		for(int i = 0; i < 4; i ++) GeneratedGraphs[i+1] = partial[i];

		for(int i = 0; i < FACE_COUNT; i++) SoftLog(string.Format("Main node #{0}: {1}", i+1, net.Listma[i]));
		for(int i = 0; i < 4; i++) for(int j = 0; j < 5; j++) SoftLog(string.Format("Partial #{0}: Node {1}: {2}", i+1, j+1, partial[i].Listma[j]));

		for(int i = 0; i < 4; i++){
			int seed = Rnd.Range(0, 5);
			HintedIcons[2*i] = partial[i].GetNode(seed).Label;
			HintedIcons[2*i+1] = partial[i].GetNode((seed + Rnd.Range(1,5))%5).Label;

			SoftLog(string.Format("Hint #{0}, {1} & {2}", i+1, HintedIcons[2*i], HintedIcons[2*i+1]));
		}

		RotationScrambler = RotationScrambler.Select(x => Rnd.Range(0,3)).ToArray();
		DisplayNet(net);
	}


	void Update () { //update

	}

	void Solve () {
		GetComponent<KMBombModule>().HandlePass();
		ModuleSolved = true;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, TriParent.transform);
		Log("Solved!");
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
	}

	// ========== helpers ==========

	void DisplayNet (Graph net) {
		int nodeCount = net.Listma.Count;
		Vector2[] triCoords = new Vector2[nodeCount];
		NetTriRotas = new int[nodeCount];
		int[] toBeInspected = new int[nodeCount];
		for(int i = 0; i < nodeCount; i++) toBeInspected[i] = 0;

		int seedIndex = Rnd.Range(0, nodeCount);

		toBeInspected[seedIndex] = 1;
		triCoords[seedIndex] = new Vector2(0, 0);
		NetTriRotas[seedIndex] = Rnd.Range(0, 3);

		//iterate thru each tbI
		while(toBeInspected.Count(x => x == 1) > 0){
			int inspIndx = Array.IndexOf(toBeInspected, 1);

			Node inspNode = net.GetNode(inspIndx);
			Vector2 inspVec = triCoords[inspIndx];
			int inspRota = NetTriRotas[inspIndx];
			bool isUpsideDown = Math.Abs(inspVec.x + inspVec.y) % 2 == 1;

			Node nextNode = Node.NullNode;
			int nextIndex;

			Vector2[] offsets = new Vector2[] {new Vector2(0, -1), new Vector2(-1, 0), new Vector2(1, 0)};

			for(int side = 0; side < 3; side++){
				nextNode = inspNode.GetNodeAtEdge((side - inspRota +3)%3);
				if(nextNode == Node.NullNode) continue;

				nextIndex = net.GetNodeIndex(nextNode);
				
				if(toBeInspected[nextIndex] == 0){
					triCoords[nextIndex] = inspVec + (!isUpsideDown ? offsets[side] : offsets[side]*-1f); 
					toBeInspected[nextIndex] = 1;
					NetTriRotas[nextIndex] = (inspRota + inspNode.GetEdgeNodeIndex(nextNode.Label) - nextNode.GetEdgeNodeIndex(inspNode.Label) + 3) % 3;
				}
			}

			toBeInspected[inspIndx] = 2;
		}

		float maxX = -99f;
		float minX = 99f;
		float maxY = -99f;
		float minY = 99f;

		for(int i = 0; i < 20; i++){
			if(i >= nodeCount){
				TrisOBJ[i].SetActive(false);
				continue;
			}

			//place and rotate the objs
			bool isUpsideDown = Math.Abs(triCoords[i].x + triCoords[i].y) % 2 == 1;
			Vector2 triRealPos = new Vector2(triCoords[i].x, triCoords[i].y * (float)Math.Sin(Math.PI/3)*2f + (isUpsideDown ? 0.58f : 0f));
			TrisOBJ[i].transform.localPosition = new Vector3(triRealPos.x, 0, triRealPos.y);
			TrisOBJ[i].SetActive(true);

			float rota = isUpsideDown ? -90f : 90f;
			rota += NetTriRotas[i]* 120f;
			TrisOBJ[i].transform.localEulerAngles = new Vector3(0,rota,0);

			//get the bounding box of all the tris so parent can be centered
			maxX = maxX > triRealPos.x ? maxX : triRealPos.x;
			minX = minX < triRealPos.x ? minX : triRealPos.x;
			maxY = maxY > triRealPos.y ? maxY : triRealPos.y;
			minY = minY < triRealPos.y ? minY : triRealPos.y;

			//set icons
			SetIco(ALPHABET.IndexOf(net.Listma[i].Label), TrisOBJ[i]);
			TrisOBJ[i].transform.Find("Ico").localScale = ((CurrentDisplay != 0 || RevealedIcons[i]) && !ShowLabel) ? new Vector3(0.4521315f, 0.4521315f, 0.4521315f) : new Vector3(0f, 0f, 0f);
			InstantRotaIcon(TrisOBJ[i].transform.Find("Ico"), RotationScrambler[ALPHABET.IndexOf(net.Listma[i].Label)]);

			//set border/base colours
			int hintStatus = Array.IndexOf(HintedIcons, net.Listma[i].Label);
			SetBorderMat(i, 4);
			SetBaseMat(i, 0);
			if(hintStatus != -1){
				if(hintStatus % 2 == 0){
					SetBorderMat(i, (hintStatus/2 +1)%4);
				} else {
					SetBaseMat(i, ((hintStatus/2)) %4 +1);
				}
			}


		}

		float scale = nodeCount != 20 ? 0.02f : (0.02f * 0.65f);
		Vector2 parentOffset = new Vector2((minX+maxX)/-2f +1f, (minY+maxY)/-2f -0.5f);
		TriParent.transform.localPosition = new Vector3(parentOffset.x * scale, 0.01595f, parentOffset.y * scale);
		StartCoroutine(FancyPlaceTriParent(scale));
	}

	void RevealIco (int i) {
		RevealedIcons[i] = true;
		StartCoroutine(FancyRevealIco(TrisOBJ[i].transform.Find("Ico")));
	}

	void SetIco (int ico, GameObject tri) {
		tri.transform.Find("Ico").GetComponent<SpriteRenderer>().sprite = IcoSprites[ico];
	}

	void SelectIco (char ico){
		SetIco(ALPHABET.IndexOf(ico), SideTrisOBJ[2]);
		SetIco(ALPHABET.IndexOf(ico), SideTrisOBJ[3]);
		StartCoroutine(FancyRevealIco(SideTrisOBJ[2].transform.Find("Ico")));
		StartCoroutine(FancyRevealIco(SideTrisOBJ[3].transform.Find("Ico")));
		SelectedIco = ico;
	}

	char GetTriIcon (GameObject tri){
		Sprite target = tri.transform.Find("Ico").GetComponent<SpriteRenderer>().sprite;
		for(int i = 0; i < 20; i++) if(target == IcoSprites[i]) return ALPHABET[i];
		return '/';
	}

	void SetBorderMat(int tri, int clr) {
		//01234 => krgby
		TrisOBJ[tri].transform.Find("Border").GetComponent<Renderer>().material = BorderMats[clr];
	}

	void SetBaseMat(int tri, int clr) {
		TrisOBJ[tri].transform.Find("Base").GetComponent<Renderer>().material = BorderMats[clr];
	}

	void Log(string msg) {
		Debug.LogFormat("[Pattern Icosahedron #{0}] {1}", ModuleId, msg);
	}

	void SoftLog(string msg) {
		Debug.LogFormat("<Pattern Icosahedron #{0}> {1}", ModuleId, msg);
	}

	float Lerp(float fro, float to, float delta){
		return fro + (to - fro) * delta;
	}

	void InstantRotaIcon(Transform ico, int r) {
		Vector2[] posOffsets = new Vector2[] {
			new Vector2(-0.124f, 0f),
			new Vector2(0.085f, 0.124f),
			new Vector2(0.085f, -0.124f)
		};

		ico.localEulerAngles = new Vector3(90f, -90f + 120f*r, 0f);
		ico.localPosition = new Vector3(posOffsets[r].x, -0.039f, posOffsets[r].y);
	}

	// ========== animations ==========

	IEnumerator FancyPlaceTriParent(float scale){
		for(int i = 5; i >= 1; i--){
			TriParent.transform.localScale = new Vector3((float)Math.Pow(i, 0.03f) * scale, 0.02f, (float)Math.Pow(i, 0.03f) * scale);
			yield return new WaitForSeconds(0.01f);
		}
		TriParent.transform.localScale = new Vector3(scale, 0.02f, scale);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, TriParent.transform);
	}

	IEnumerator FancyRevealIco(Transform icoTransform){
		for(int i = 5; i >= 1; i--){
			icoTransform.localScale = new Vector3(1f, 1f, 1f) * 0.4521315f * (float)Math.Pow(i, 0.2f);
			yield return new WaitForSeconds(0.01f);
		}
		icoTransform.localScale = new Vector3(1f, 1f, 1f) * 0.4521315f;
	}

	IEnumerator RotaSelectedIcon() {
		while(RotaAni) yield return new WaitForSeconds(0.01f);

		RotaAni = true;

		Transform icoTransform1 = SideTrisOBJ[2].transform.Find("Ico");
		Transform icoTransform2 = SideTrisOBJ[3].transform.Find("Ico");
		float preRota = icoTransform1.localEulerAngles.y;		
		
		
		SelectedIconRota = (SelectedIconRota+1)%3;
		
		Vector2[] posOffsets = new Vector2[] {
			new Vector2(-0.124f, 0f),
			new Vector2(0.085f, 0.124f),
			new Vector2(0.085f, -0.124f)
		};

		for(int i = 0; i <= 5; i++){
			icoTransform1.localEulerAngles = new Vector3(90f, preRota + 120f * i/5f, 0f);
			icoTransform1.localPosition = new Vector3(
				Lerp(posOffsets[(SelectedIconRota+2)%3].x, posOffsets[SelectedIconRota].x, i/5f),
				-0.039f,
				Lerp(posOffsets[(SelectedIconRota+2)%3].y, posOffsets[SelectedIconRota].y, i/5f)
			);

			icoTransform2.localEulerAngles = new Vector3(90f, preRota + 120f * i/5f, 0f);
			icoTransform2.localPosition = new Vector3(
				Lerp(posOffsets[(SelectedIconRota+2)%3].x, posOffsets[SelectedIconRota].x, i/5f),
				-0.039f,
				Lerp(posOffsets[(SelectedIconRota+2)%3].y, posOffsets[SelectedIconRota].y, i/5f)
			);

			yield return new WaitForSeconds(0.01f);
		}

		RotaAni = false;
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} toggle to toggle the labels/patterns of the triangles. !{0} net 0/1/2/3/4 to show the associated net, where 0 is the main net. !{0} rotate/rota cw/ccw to rotate the selected pattern. !{0} select/place X to place the pattern on the triangle labeled 'X'.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		Command = Command.Trim().ToUpper();
		string[] cmds = Command.Split(' ');
		yield return null;

		if(cmds[0] == "TOGGLE" || cmds[0] == "T") {
			ShowLabel = !ShowLabel;
			UpdateLabels();
			yield break;
		}


		if(cmds[0] == "NET" || cmds[0] == "N"){	
			if(cmds.Length != 2){
				yield return "sendtochaterror Please supply the net number and only the net number!";
				yield break;
			}

			if(Regex.IsMatch(cmds[1], @"^[0-4]$")){
				int targetNet = cmds[1][0] - '0';
				if(CurrentDisplay != targetNet) SideTriPress(SideTrisKMS[new int[] {2, 0, 1, 4, 5}[targetNet]]);
				UpdateLabels();
				yield break;
			}
			
			yield return "sendtochaterror Invalid net number: " + cmds[1];
			yield break;
		} 
		
		if(cmds[0] == "ROTATE" || cmds[0] == "ROTA" || cmds[0] == "R") {
			if(cmds.Length != 2){
				yield return "sendtochaterror Please supply only 1 rotation! CW or CCW";
				yield break;
			}

			if(cmds.Length < 2 || cmds[1] == "CW"){
				SideTriPress(SideTrisKMS[2]);
				yield break;
			}

			if(cmds[1] == "CCW"){
				SideTriPress(SideTrisKMS[2]);
				SideTriPress(SideTrisKMS[2]);
				yield break;
			}

			yield return "sendtochaterror Invalid rotation: " + cmds[1];
			yield break;
		}

		if(cmds[0] == "SELECT" || cmds[0] == "SAMPLE" || cmds[0] == "S"){
			if(CurrentDisplay == 0){
				yield return "sendtochaterror Can only select on nets 1-4!";
				yield break;
			}

			if(cmds.Length != 2){
				yield return "sendtochaterror Please supply only the label of the triangle you want to sample!";
				yield break;
			}

			if(Regex.IsMatch(cmds[1], @"^[A-T]$")){
				int targetTri = ALPHABET.IndexOf(cmds[1][0]);
				TriPress(TrisKMS[targetTri]);
				yield break;
			}
		}

		if(cmds[0] == "PRESS" || cmds[0] == "PLACE" || cmds[0] == "P"){
			if(CurrentDisplay != 0){
				yield return "sendtochaterror Can only place on net 0!";
				yield break;
			}

			if(cmds.Length != 2){
				yield return "sendtochaterror Please supply only the label of the triangle you want to press!";
				yield break;
			}

			if(Regex.IsMatch(cmds[1], @"^[A-T]$")){
				int targetTri = ALPHABET.IndexOf(cmds[1][0]);
				TriPress(TrisKMS[targetTri]);
				UpdateLabels();
				yield break;
			}

		}

		yield return "sendtochaterror Invalid command: " + cmds[0];
		yield break;

	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
		ShowLabel = false;
		UpdateLabels();

		Log("Autosolving...");
		float spd = 0.1f;

		while(!ModuleSolved) {
			int currentSolveTarget = Array.IndexOf(RevealedIcons, false);
			char sovleChar = GeneratedGraphs[0].GetNode(currentSolveTarget).Label;

			for(int i = 1; i <= 4; i++){
				if(GeneratedGraphs[i].GetNode(sovleChar) == Node.NullNode) continue;
				yield return new WaitForSeconds(spd);

				SideTriPress(SideTrisKMS[new int[] {0, 0, 1, 4, 5}[i]]);
				yield return new WaitForSeconds(spd);
				
				TriPress(TrisKMS[GeneratedGraphs[i].GetNodeIndex(sovleChar)]);
				yield return new WaitForSeconds(spd);
				
				SideTriPress(SideTrisKMS[2]);
				yield return new WaitForSeconds(spd);
				
				while(SelectedIconRota != (NetTriRotas[currentSolveTarget]+RotationScrambler[ALPHABET.IndexOf(sovleChar)])%3){
					SideTriPress(SideTrisKMS[2]);
					yield return new WaitForSeconds(spd);
				}

				TriPress(TrisKMS[GeneratedGraphs[0].GetNodeIndex(sovleChar)]);
			}
		}

		yield break;
	}
	
	void UpdateLabels() {
		for(int i = 0; i < 20; i++){
			GameObject label = TrisOBJ[i].transform.Find("Label").gameObject;

			if(ShowLabel){
				label.GetComponent<TextMesh>().text = ALPHABET[i] + "";

				if(CurrentDisplay == 0 && RevealedIcons[i]){
					label.SetActive(false);
					TrisOBJ[i].transform.Find("Ico").localScale = new Vector3(0.4521315f, 0.4521315f, 0.4521315f);
				} else {
					label.SetActive(true);
					TrisOBJ[i].transform.Find("Ico").localScale = new Vector3(0f, 0f, 0f);
				}
			} else {
				label.SetActive(false);

				if(CurrentDisplay != 0 || RevealedIcons[i])
					TrisOBJ[i].transform.Find("Ico").localScale = new Vector3(0.4521315f, 0.4521315f, 0.4521315f);
			}
		}
	}
}
