//this singe script might contain the most unhinged single locs i have ever written
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class Roshambo : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved;

	public KMSelectable[] InputKMS;
	public GameObject[] InputObjects;
	public Material[] Mats;

	private readonly string[] RPS = {"Rock", "Paper", "Scissors"};
	private readonly static string Alphabet = "#ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private Vector2[] InputPositions = new Vector2[3];
	private int[] InputColours = new int[3];
	private static char FurthestUp;
	private static char FurthestLeft;

	private Coroutine AnimationCoroutine;
	private Coroutine MusicCoroutine;
	private Coroutine StrikeCoroutine;

	private string TournSeeding = "";
	private List<Contestant> ContestantList = new List<Contestant>();
	
	private bool[] isInputHeldDown = new bool[] {false, false, false};
	
	private List<char> Solution = new List<char>();

	Dictionary<char, string> NatoDictionaty = new Dictionary<char, string>() {
		{'0', "ZERO"},
		{'1', "ONE"},
		{'2', "TWO"},
		{'3', "THREE"},
		{'4', "FOUR"},
		{'5', "FIVE"},
		{'6', "SIX"},
		{'7', "SEVEN"},
		{'8', "EIGHT"},
		{'9', "NINE"},
		{'A', "ALPHA"},
		{'B', "BRAVO"},
		{'C', "CHARLIE"},
		{'D', "DELTA"},
		{'E', "ECHO"},
		{'F', "FOXTROT"},
		{'G', "GOLF"},
		{'H', "HOTEL"},
		{'I', "INDIA"},
		{'J', "JULIET"},
		{'K', "KILO"},
		{'L', "LIMA"},
		{'M', "MIKE"},
		{'N', "NOVEMBER"},
		{'O', "OSCAR"},
		{'P', "PAPA"},
		{'Q', "QUEBEC"},
		{'R', "ROMEO"},
		{'S', "SIERRA"},
		{'T', "TANGO"},
		{'U', "UNIFORM"},
		{'V', "VICTOR"},
		{'W', "WHISKEY"},
		{'X', "XRAY"},
		{'Y', "YANKEE"},
		{'Z', "ZULU"}
	};

	private struct Contestant {
		public char Name;
		public int Seed;
		public List<char> Moves;

		public Contestant(char n, int sd){
			Name = n;
			Seed = sd;
			Moves = new List<char>();
			if(Alphabet.Contains(Name)){
				int i = Alphabet.IndexOf(Name);
				Moves.Add("RPS"[i%3]); i /= 3;
				Moves.Add("RPS"[i%3]); i /= 3;
				Moves.Add("RPS"[i%3]);
				if(Seed < 16) Moves.Add(FurthestUp);
				else Moves.Add(FurthestLeft);
			}
		}
	}

	void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
		ModuleId = ModuleIdCounter++;
		//GetComponent<KMBombModule>().OnActivate += Activate;

		foreach (KMSelectable btn in InputKMS) {
			btn.OnInteract += delegate () { InputPress(btn); return false; };
			btn.OnInteractEnded += delegate () { InputRelease(btn); };
		}
	}

	void InputPress(KMSelectable btn) {	
		btn.AddInteractionPunch();
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);

		if(ModuleSolved) return;

		int i = 0;
		for(; i < 3; i++){
			if(btn == InputKMS[i]) break;
		}

		isInputHeldDown[i] = true;

		if(StrikeCoroutine != null || AnimationCoroutine != null || MusicCoroutine != null) return;
		AnimationCoroutine = StartCoroutine(SubmitAni());

	}

	void InputRelease(KMSelectable btn) {
		int i = 0;
		for(; i < 3; i++){
			if(btn == InputKMS[i]) break;
		}

		isInputHeldDown[i] = false;
	}

	void Start () { //Shit that you calculate, usually a majority if not all of the module
		AnimationCoroutine = null;
		MusicCoroutine = null;
		StrikeCoroutine = null;

		//input positions / colours
		HashSet<Vector2> vect2Hash = new HashSet<Vector2>();
		for(int i = 0; i < 3;){
			Vector2 currentV2 = new Vector2(Rnd.Range(-1,2), Rnd.Range(-1,2));
			if(!vect2Hash.Add(currentV2)) continue;

			InputPositions[i] = currentV2;
			InputKMS[i].transform.localPosition = new Vector3(InputPositions[i].x*0.055f, 0.01468f, InputPositions[i].y*0.055f);
			i++;
		}

		HashSet<int> clrHash = new HashSet<int>();
		for(int i = 0; i < 3;){
			int currentClr = Rnd.Range(2,5);
			if(!clrHash.Add(currentClr)) continue;

			InputColours[i] = currentClr;
			InputObjects[i].GetComponent<Renderer>().material = Mats[InputColours[i]];
			i++;
		}

		FurthestUp = Furthest(false);
		FurthestLeft = Furthest(true);

		//used for rpw final considerations
		float[] inputDistances = new float[] {0,0,0};
		for(int i = 0; i < 3; i++) inputDistances[i] = Vector2.Distance(InputPositions[i], InputPositions[(i+1)%3]);

		//tournament setup	
		string snNato = "";
		string regContKey = "";
		for(int i = 0; i < 6; i++) snNato += NatoDictionaty[Bomb.GetSerialNumber()[i]];
		snNato += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		HashSet<char> keyHash = new HashSet<char>();
		foreach(char i in snNato) if(keyHash.Add(i)) regContKey += i;
		int hashInsertPoint = 0;
		foreach (int num in Bomb.GetSerialNumberNumbers()){
			hashInsertPoint *= 10;
			hashInsertPoint += num;
		}		
		regContKey = regContKey.Insert(hashInsertPoint % 27, "#");
		string TournSeeding = regContKey;

		TournSeeding = TournSeeding.Insert(0, "*");
		TournSeeding = TournSeeding.Insert(15, "--");
		TournSeeding = TournSeeding.Insert(23, "-");
		TournSeeding = TournSeeding.Insert(31, "!");

		for(int i = 0; i < 32; i++) ContestantList.Add(new Contestant(TournSeeding[i], i));
		
		string[] contestantSubsets = {"", "", "", ""};
		for(int j = 0; j < 4; j++){
			List<Contestant> templist = new List<Contestant>();
			for(int i = 0; i < ContestantList.Count/2; i++){
				templist.Add(CheckMatchup(ContestantList[2*i], ContestantList[2*i+1], j));
			}
			ContestantList = templist;
			foreach(Contestant c in ContestantList){
				contestantSubsets[j]+=c.Name;
			}
		}

		//RPW considerations:
		List<char> rpwConsid = new List<char>();
		
		rpwConsid.Add(CondensePattern(ContestantList[1].Moves)); //rpw moves

		rpwConsid.Add(PickRYB( //bats
			Bomb.GetBatteryCount(Battery.AA)/2,
			-1,
			Bomb.GetBatteryCount(Battery.D)));

		rpwConsid.Add(PickRYB( //corners, MM, edges
			InputPositions.Count(v2 => Mathf.Abs(v2.x) + Mathf.Abs(v2.y) == 2),
			InputPositions.Count(v2 => v2 == new Vector2(0,0))*5-1,
			InputPositions.Count(v2 => Mathf.Abs(v2.x) + Mathf.Abs(v2.y) == 1)));

		rpwConsid.Add(PickRYB( //sn letters
			Bomb.GetSerialNumber().Count(x => "ROCK".Contains(x)),
			Bomb.GetSerialNumber().Count(x => "PAER".Contains(x)),
			Bomb.GetSerialNumber().Count(x => "SIZX".Contains(x))));

		rpwConsid.Add(PickRYB( //sn numbers
			Bomb.GetSerialNumber().Count(x => "089".Contains(x)),
			Bomb.GetSerialNumber().Count(x => "135".Contains(x)),
			Bomb.GetSerialNumber().Count(x => "267".Contains(x))));

		if(inputDistances.Count(x => x==1f) >= 2) rpwConsid.Add(PickRYB(1,0,0)); else //positions movements
		if(inputDistances.Count(x => x>=1.5f) <= 1) rpwConsid.Add(PickRYB(0,0,1)); else
		rpwConsid.Add(PickRYB(0,1,0));
		
		rpwConsid.Add(PickRYB( //plates
			Bomb.GetPortPlates().Count(x => x.Contains("Parallel") || x.Contains("Serial")),
			-1,
			Bomb.GetPortPlates().Count(x => x.Contains("RJ-45") || x.Contains("DVI") || x.Contains("PS2") || x.Contains("StereoRCA"))));

		rpwConsid.Add(CounterPick(CondensePattern(ContestantList[0].Moves))); //your moves

		ContestantList[1].Moves.Add(CondensePattern(rpwConsid));
		ContestantList[0].Moves.Add(CounterPick(ContestantList[1].Moves[4]));
		Solution = ContestantList[0].Moves;

		//logging
		for(int i = 0; i < 3; i++){
			Debug.LogFormat("[Roshambo #{0}] The {1} button is {2}-{3} and is {4}.", ModuleId, RPS[i],
			InputPositions[i].y == -1	? "bottom"	: InputPositions[i].y	== 0 ? "middle" : "top",
			InputPositions[i].x == -1	? "left"	: InputPositions[i].x	== 0 ? "middle" : "right",
			InputColours[i]		== 2	? "red"		: InputColours[i]		== 3 ? "yellow" : "blue");
		}

		Debug.LogFormat("[Roshambo #{0}] Full seeding of the tournament: {1}", ModuleId, TournSeeding);
		Debug.LogFormat("[Roshambo #{0}] Furthest up disregarding ties, using R/Y/B based on indicators: {1}", ModuleId, RPS["RPS".IndexOf(FurthestUp)]);
		Debug.LogFormat("[Roshambo #{0}] Furthest left disregarding ties, using R/Y/B based on indicators: {1}", ModuleId, RPS["RPS".IndexOf(FurthestLeft)]);

		for(int i = 0; i < 4; i++) Debug.LogFormat("[Roshambo #{0}] The tournament after {1} round(s): {2}", ModuleId, i+1, contestantSubsets[i]);
		Debug.LogFormat("[Roshambo #{0}] (Remember, *=You, !=RPW, -=Special Guest)", ModuleId);

		Debug.LogFormat("[Roshambo #{0}] The Rock Paper Wizard will have played these moves so far: {1}",	ModuleId, String.Join(", ", ContestantList[1].Moves.GetRange(0, 4).Select(c => RPS["RPS".IndexOf(c)]).ToArray()));
		Debug.LogFormat("[Roshambo #{0}] You will have played these moves so far: {1}",	ModuleId, String.Join(", ", ContestantList[0].Moves.GetRange(0, 4).Select(c => RPS["RPS".IndexOf(c)]).ToArray()));
		
		Debug.LogFormat("[Roshambo #{0}] The Rock Paper Wizard will consider these moves in order: {1}", ModuleId, string.Join(", ", rpwConsid.Select(x => RPS["RPS".IndexOf(x)]).ToArray()));
		Debug.LogFormat("[Roshambo #{0}] The Rock Paper Wizard's final move: {1}", ModuleId, RPS["RPS".IndexOf(ContestantList[1].Moves[4])]);

		Debug.LogFormat("[Roshambo #{0}] Your solution to winning the tournament: {1}", ModuleId, string.Join(", ", ContestantList[0].Moves.Select(c => RPS["RPS".IndexOf(c)]).ToArray()));
	}

	void Solve () {
		for(int i = 0; i < 3; i++) InputObjects[i].GetComponent<Renderer>().material = Mats[5];
		Debug.LogFormat("[Roshambo #{0}] The Rock Paper Wizard has been dethroned! What an upset!", ModuleId);
		Debug.LogFormat("[Roshambo #{0}] Congratulations, you won the prize of a solved module!", ModuleId);
		ModuleSolved = true;
		Audio.PlaySoundAtTransform("RSB_Win", Bomb.transform);

		GetComponent<KMBombModule>().HandlePass();
	}

	void Strike () {
		if(AnimationCoroutine != null){
			StopCoroutine(AnimationCoroutine);
			AnimationCoroutine = null;
		}
		if(MusicCoroutine != null){
			StopCoroutine(MusicCoroutine);
			MusicCoroutine = null;
		}
		if(StrikeCoroutine == null){
			StrikeCoroutine = StartCoroutine(PlayStrike());
		}

		GetComponent<KMBombModule>().HandleStrike();
		
		Debug.LogFormat("[Roshambo #{0}] The Rock Paper Wizard wins again! Better luck next time.", ModuleId);
		Debug.LogFormat("[Roshambo #{0}] Y̴̼̏o̷̥͘u̴͙̕ ̵̣̏f̶̙͗è̴̮e̴͔͆ĺ̶͔ ̶̫̃t̸̛͖i̵̥͘ṃ̶̚ḛ̷̈́ ̵̱͝r̸̯̎ȩ̷̉v̵̨̈́e̶͕̎r̸̯̒t̷̤͝i̷̲̔ṇ̶͌ģ̵͂ ̵̢͊b̷̪͐ă̵ͅc̶̗͛k̵̰̊ ̵̹̈t̵̥͘o̵͈̒ ̸̨͂b̶̺̃ê̴̮f̵̡͑ō̴̟r̵̃ͅe̸̘̅ ̴̫̔i̶̟͐t̶͉͊ ̴̨͋h̶͈̚a̵̰̓p̶̹̎p̵̻̽e̶̤͂n̴͔̈́e̵̮̔d̵̲̒.", ModuleId);
	}

	//maybe dont touch ..?
	IEnumerator SubmitAni () {
		Debug.LogFormat("[Roshambo #{0}] Entering the tournament, good luck!", ModuleId);
		MusicCoroutine = StartCoroutine(PlayHomeRun());

		List<int[]> inpAff = new List<int[]>() {
			new int[] {0,1,2},
			new int[] {0},
			new int[] {0,1},
			new int[] {1,2},
			new int[] {2},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[0]))), 5}, // r1
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[0])))}, //5 means check input
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[1]))), 5}, // r2
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[1])))},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[2]))), 5}, // r3
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[2])))},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[3]))), 5}, // r4
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[3])))},
			new int[] {0,1,2},
			new int[] {0,1,2},
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[4]))), 5}, // r5
			new int[] {"RPS".IndexOf(CounterPick(CounterPick(Solution[4])))},
		};

		int round = 0;
		bool incorrect = false;

		float[] timings = {0.569f, 0.384f, 0.387f, 0.401f, 0.412f, 0.261f, 0.291f, 0.267f, 0.378f, 0.38f, 0.375f, 0.408f, 0.46f, 0.358f, 0.357f, 0.382f, 0.38f, 0.332f, 0.354f, 0.349f, 0.348f, 0.317f, 0.33f, 0.308f, 0.328f, 0.358f, 0.344f, 0.32f, 0.334f, 0.325f};
		bool[] isOff = {false, false, false};

		for(int i = 0; i < timings.Length; i++){
			foreach(int j in inpAff[i]){

				//input check
				if(j == 5){
					Debug.LogFormat("[Roshambo #{0}] Played: {1}", ModuleId, GetCurrentInput() == '-' ? "Nothing" : RPS["RPS".IndexOf(GetCurrentInput())]);
					incorrect = GetCurrentInput() != Solution[round];
					round++;

				//regular mat change
				} else {
					InputObjects[j].GetComponent<Renderer>().material = Mats[isOff[j] ? 1 : 0];
					isOff[j] = !isOff[j];

				}
			
			}
			
			yield return new WaitForSeconds(timings[i]);
			
			if(incorrect){
				Strike();
				AnimationCoroutine = null;
				yield break;
			}
		}

		Solve();
		AnimationCoroutine = null;
		yield break;
	}

	IEnumerator PlayHomeRun () {
		Audio.PlaySoundAtTransform("RSB_1", Bomb.transform);
		yield return new WaitForSeconds(5.335f);

		Audio.PlaySoundAtTransform("RSB_2", Bomb.transform);
		yield return new WaitForSeconds(1.478f);
		
		Audio.PlaySoundAtTransform("RSB_3", Bomb.transform);
		yield return new WaitForSeconds(1.383f);
		
		Audio.PlaySoundAtTransform("RSB_4", Bomb.transform);
		yield return new WaitForSeconds(1.327f);

		Audio.PlaySoundAtTransform("RSB_5", Bomb.transform);
		yield return new WaitForSeconds(1.274f);

		MusicCoroutine = null;
	}

	char GetCurrentInput(){
		for(int i = 0; i < 3; i++) if(isInputHeldDown[i]) return "RPS"[i];
		return '-';
	}

	IEnumerator PlayStrike () {
		float[] timings = {0.739f, 0.348f, 0.360f, 0.353f, 1.226f};
		bool[] isOff = {false, false, false};

		Audio.PlaySoundAtTransform("RSB_Lose", Bomb.transform);

		for(int i = 0; i < timings.Length; i++){
			for(int j = 0; j < 3; j++){
				InputObjects[j].GetComponent<Renderer>().material = Mats[isOff[j] ? 2 : 0];
				isOff[j] = !isOff[j];
			}
			
			yield return new WaitForSeconds(timings[i]);			
		}

		for(int i = 0; i < 3; i++) InputObjects[i].GetComponent<Renderer>().material = Mats[InputColours[i]];
		StrikeCoroutine = null;
	}

	//calc related functions
	Contestant CheckMatchup (Contestant a, Contestant b, int round){
		//get the special guests out of here
		if(a.Name == '-') return b;
		if(b.Name == '-') return a;

		//auto pass you or RPW
		if(a.Name == '!' || a.Name == '*'){
			a.Moves.Add(CounterPick(b.Moves[round]));
			return a;
		}
		if(b.Name == '!' || b.Name == '*'){
			b.Moves.Add(CounterPick(a.Moves[round]));
			return b;
		}

		//between 2 regular contestants
		if(b.Moves[round] == CounterPick(a.Moves[round])) return b;
		if(a.Moves[round] == CounterPick(b.Moves[round])) return a;

		//return the one with the higher seed (always b)
		return b;
	}

	char CounterPick(char c){
		return "RPS"[("RPS".IndexOf(c)+1)%3];
	}

	char Furthest(bool left){
		int currentSliceCount;
		for(int i = (left ? -1 : 1); Mathf.Abs(i) < 2; i += (left ? 1 : -1)){		
			currentSliceCount = 0;
			int candidate = -1;
			for(int j = 0; j < 3; j++){
				if((left ? InputPositions[j].x : InputPositions[j].y) == i){
					currentSliceCount++;
					candidate = j;
				}
			}
			if(currentSliceCount == 1) return "RPS"[candidate];
		}

		//default to RYB w/ indicators
		return PickRYB(Bomb.GetOnIndicators().Count(), -1, Bomb.GetOffIndicators().Count());
	}

	char PickRYB(int r, int y, int b){
		int targetClr;

		if(r > y && r > b) targetClr = 2; else
		if(y > b && y > r) targetClr = 3; else
		if(b > r && b > y) targetClr = 4; else
		if(y == b) targetClr = 2; else
		if(b == r) targetClr = 3; else
		if(r == y) targetClr = 4; else
		targetClr = 3;

		for(int i = 0; i < 3; i++){
			if(InputColours[i] == targetClr) return "RPS"[i];
		}

		return 'J'; //???
	}

	char CondensePattern(List<char> m){
		List<char> newList = new List<char>();

		for(int i = 0; i < m.Count/2; i++){
			char a = m[2*i];
			char b = m[2*i+1];
			if(a == b) newList.Add(a);
			else newList.Add("RPS".Replace(a+"", string.Empty).Replace(b+"", string.Empty)[0]);
		}

		if(newList.Count == 1) return newList[0];
		return CondensePattern(newList);
	}

	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} paper scissors r p s to submit that answer.";
	#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		Command = Command.Trim().ToUpper();
		string[] Commands = Command.Split(' ');
		yield return null;

		if(MusicCoroutine != null || AnimationCoroutine != null || StrikeCoroutine != null){
			yield return "sendtochaterror Please wait for animations to finish.";
			yield break;
		}

		if(Commands.Length != 5){
			yield return "sendtochaterror Expected 5 commands, received " + Commands.Length;
			yield break;
		}

		List<char> TPlist = new List<char>();
		
		foreach(string cmd in Commands){
			if(Regex.IsMatch(cmd, @"^R($|OCK)")) TPlist.Add('R'); else
			if(Regex.IsMatch(cmd, @"^P($|APER)")) TPlist.Add('P'); else
			if(Regex.IsMatch(cmd, @"^S($|CISSORS)")) TPlist.Add('S'); else {
				yield return "sendtochaterror Invalid command: " + cmd;
				yield break;
			}
		}

		StartCoroutine(TPinput(TPlist));
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;

		if(AnimationCoroutine != null){
			//mad props if you get here
			StopCoroutine(AnimationCoroutine);
			AnimationCoroutine = null;
		}
		if(MusicCoroutine != null){
			StopCoroutine(MusicCoroutine);
			MusicCoroutine = null;
		}

		//wait for strike anim to finish
		while(StrikeCoroutine != null) yield return null;

		//reset mod if autosolver is invoked mid animation
		for(int i = 0; i < 3; i++){
			isInputHeldDown[i] = false;
			InputObjects[i].GetComponent<Renderer>().material = Mats[InputColours[i]];
		}

		StartCoroutine(TPinput(Solution));
	}

	IEnumerator TPinput (List<char> moves) {
		yield return null;
		List<int> moveInt = moves.Select(x => "RPS".IndexOf(x)).ToList();

		//get started
		InputKMS[0].OnInteract();
		InputKMS[0].OnInteractEnded();

		yield return new WaitForSeconds(3.724f);

		float[] timings = {1.345f, 0.267f, 1.241f, 0.259f, 1.165f, 0.196f, 1.110f, 0.224f, 1.138f};

		for(int i = 0; i < timings.Length; i++){
			if(i % 2 == 0){
				InputKMS[moveInt[i/2]].OnInteract();
			} else {
				InputKMS[moveInt[i/2]].OnInteractEnded();
			}

			yield return new WaitForSeconds(timings[i]);

			//stop if ya strike
			if(StrikeCoroutine != null) yield break;
		}
	}
}
