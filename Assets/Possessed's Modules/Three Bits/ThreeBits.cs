//A shave and a haircut at an inflated price!?!? Unacceptable.
//if theres ever a bug with this mod (especially with the tri-maze), ping me first bc reading the code is a nightmare and im the only one who knows how it works
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class ThreeBits : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   const string HalfAlphabet = "BCDEGJKPQTVZ";

   public KMSelectable[] LetterButton;
   public KMSelectable QueryButton;
   public KMSelectable SubmitButton;
   public TextMesh ModDisplay;

   private char[] Queries = {'_', '_', '_'};
   private char[] Mirrors = {'_', '_', '_'};

   private string State;

   private struct node {
      public string Name;
      public string[] EntryDir;
      public string[] ExitNode;
      public string[] ExitDir;
   };

   //dont ask
   private node[] Network = new node[] {
      new node { Name = "AA", EntryDir = new string[] {"AB", "AC", "BA", "CA"}, ExitNode = new string[] {"BB", "CC", "AB", "AC"}, ExitDir = new string[] {"AB", "AC", "BA", "CA"} },
      new node { Name = "AB", EntryDir = new string[] {"BA", "BC", "AB", "CB"}, ExitNode = new string[] {"_B", "_D", "AA", "AC"}, ExitDir = new string[] {"BA", "BC", "AB", "CB"} },
      new node { Name = "AC", EntryDir = new string[] {"CA", "CB", "AC", "BC"}, ExitNode = new string[] {"_C", "_E", "AA", "AB"}, ExitDir = new string[] {"CA", "CB", "AC", "BC"} },

      new node { Name = "BA", EntryDir = new string[] {"AB", "AC", "BA", "CA"}, ExitNode = new string[] {"_V", "_T", "BB", "BC"}, ExitDir = new string[] {"AB", "AC", "BA", "CA"} },
      new node { Name = "BB", EntryDir = new string[] {"BA", "BC", "AB", "CB"}, ExitNode = new string[] {"AA", "CC", "BA", "BC"}, ExitDir = new string[] {"BA", "BC", "AB", "CB"} },
      new node { Name = "BC", EntryDir = new string[] {"CA", "CB", "AC", "BC"}, ExitNode = new string[] {"_G", "_J", "BA", "BB"}, ExitDir = new string[] {"CA", "CB", "AC", "BC"} },

      new node { Name = "CA", EntryDir = new string[] {"AB", "AC", "BA", "CA"}, ExitNode = new string[] {"_Z", "_P", "CB", "CC"}, ExitDir = new string[] {"AB", "AC", "BA", "CA"} },
      new node { Name = "CB", EntryDir = new string[] {"BA", "BC", "AB", "CB"}, ExitNode = new string[] {"_Q", "_K", "CA", "CC"}, ExitDir = new string[] {"BA", "BC", "AB", "CB"} },
      new node { Name = "CC", EntryDir = new string[] {"CA", "CB", "AC", "BC"}, ExitNode = new string[] {"AA", "BB", "CA", "CB"}, ExitDir = new string[] {"CA", "CB", "AC", "BC"} },

      new node { Name = "_C", EntryDir = new string[] {"X"}, ExitNode = new string[] {"AC"}, ExitDir = new string[] {"AC"} },
      new node { Name = "_E", EntryDir = new string[] {"X"}, ExitNode = new string[] {"AC"}, ExitDir = new string[] {"BC"} },
      
      new node { Name = "_G", EntryDir = new string[] {"X"}, ExitNode = new string[] {"BC"}, ExitDir = new string[] {"AC"} },
      new node { Name = "_J", EntryDir = new string[] {"X"}, ExitNode = new string[] {"BC"}, ExitDir = new string[] {"BC"} },

      new node { Name = "_V", EntryDir = new string[] {"X"}, ExitNode = new string[] {"BA"}, ExitDir = new string[] {"BA"} },
      new node { Name = "_T", EntryDir = new string[] {"X"}, ExitNode = new string[] {"BA"}, ExitDir = new string[] {"CA"} },

      new node { Name = "_Z", EntryDir = new string[] {"X"}, ExitNode = new string[] {"CA"}, ExitDir = new string[] {"BA"} },
      new node { Name = "_P", EntryDir = new string[] {"X"}, ExitNode = new string[] {"CA"}, ExitDir = new string[] {"CA"} },

      new node { Name = "_K", EntryDir = new string[] {"X"}, ExitNode = new string[] {"CB"}, ExitDir = new string[] {"CB"} },
      new node { Name = "_Q", EntryDir = new string[] {"X"}, ExitNode = new string[] {"CB"}, ExitDir = new string[] {"AB"} },

      new node { Name = "_D", EntryDir = new string[] {"X"}, ExitNode = new string[] {"AB"}, ExitDir = new string[] {"CB"} },
      new node { Name = "_B", EntryDir = new string[] {"X"}, ExitNode = new string[] {"AB"}, ExitDir = new string[] {"AB"} }

   };

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      
      foreach (KMSelectable Letter in LetterButton) {
         Letter.OnInteract += delegate () { LetterPress(Letter); return false; };
      }

      QueryButton.OnInteract += delegate () { QueryPress(); return false; };
      SubmitButton.OnInteract += delegate () { SubmitPress(); return false; };
   }

   void LetterPress (KMSelectable Bun){
      Bun.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Bun.transform);

      if(State == "STRIKE") return;
      
      if(State == "SOLVED"){
         Strike("SOLVED");
         return;
      }

      if(State != "IDLE"){
         Strike("PRESSWHENUNREADY");
         return;
      }
      
      for(int i = 0; i < 12; i++){
         if(Bun == LetterButton[i]){
         
            for(int j = 0; j < 4; j++){

               //Strike when you press a fourth button
               if(j==3){
                  Strike("FOURQUERIES");
                  return;
               }

               if(Queries[j] == '_'){
                  Queries[j] = HalfAlphabet[i];
                  break;
               }
            }
         }
      }
      ModDisplay.text = Queries[0] + " " + Queries[1] + " " + Queries[2];
   }

   void QueryPress () {
      QueryButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, QueryButton.transform);

      if(State == "STRIKE") return;

      if(State == "SOLVED"){
         Strike("SOLVED");
         return;
      }

      if(State != "IDLE"){
         Strike("PRESSWHENUNREADY");
         return;
      }

      if(Queries[2] == '_'){
         Strike("INCOMPLETEQUERY");
         return;
      }

      if(Queries[0] == Queries[1] || Queries[1] == Queries[2] || Queries[2] == Queries[0]){
         Strike("DUPEQUERY");
         return;
      }

      Debug.LogFormat("[Three Bits #{0}] Querying: {1} {2} {3}", ModuleId, Queries[0], Queries[1], Queries[2]);
      State = "QUERYING";
      ModDisplay.text = "WORKING...";

      char[] Outputs = {GetOutput(Queries[0]), GetOutput(Queries[1]), GetOutput(Queries[2]) };
      
      //shuffle Outputs
      for(int i = 0; i < 2; i++){
         int j = Rnd.Range(i, 3);
         char temp = Outputs[i];
         Outputs[i] = Outputs[j];
         Outputs[j] = temp;
      }

      Debug.LogFormat("[Three Bits #{0}] Output: {1} {2} {3}", ModuleId, Outputs[0], Outputs[1], Outputs[2]);
      StartCoroutine(WaitToDisplay(Outputs[0] + " " + Outputs[1] + " " + Outputs[2]));
   }

   void SubmitPress () {
      QueryButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, QueryButton.transform);
      
      if(State == "STRIKE") return;
    
      if(State == "SOLVED"){
         Strike("SOLVED");
         return;
      }

      if(State != "IDLE"){
         Strike("PRESSWHENUNREADY");
         return;
      }

      if(Queries[2] == '_'){
         Strike("INCOMPLETESUB");
         return;
      }

      if(Queries[0] == Queries[1] || Queries[1] == Queries[2] || Queries[2] == Queries[0]){
         Strike("DUPESUB");
         return;
      }

      ModDisplay.text = "WORKING...";
      State = "SUBMITING";

      HashSet<char> MirrorsHash = new HashSet<char>();
      HashSet<char> SubHash = new HashSet<char>();

      for(int i = 0; i < 3; i++){
         MirrorsHash.Add(Mirrors[i]);
         SubHash.Add(Queries[i]);
      }

      Debug.LogFormat("[Three Bits #{0}] Submitted {1} {2} {3}.", ModuleId, Queries[0], Queries[1], Queries[2]);
      
      if(MirrorsHash.SetEquals(SubHash)){
         StartCoroutine(WaitToSolve());
      } else {
         StartCoroutine(WaitToStrike());
      }


   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      State = "SETUP";
      ModDisplay.text = "STARTING...";

      //gen Mirrors
      HashSet<char> MirrorsHash = new HashSet<char>();
      while(MirrorsHash.Count != 3){
         MirrorsHash.Add( HalfAlphabet[Rnd.Range(0,12)] );
      }

      MirrorsHash.CopyTo(Mirrors);
      Debug.LogFormat("[Three Bits #{0}] Generated mirrors: {1} {2} {3}.", ModuleId, Mirrors[0], Mirrors[1], Mirrors[2]);

      //apply mirrors
      for(int i = 0; i < 3; i++){
         switch(Mirrors[i]){
            case 'C':
               ShiftNode("AA", false);
               break;
            case 'E':
               ShiftNode("AC", false);
               break;
            case 'G':
               ShiftNode("BC", false);
               break;
            case 'J':
               ShiftNode("BB", true);
               break;
            case 'V':
               ShiftNode("BB", false);
               break;
            case 'T':
               ShiftNode("BA", false);
               break;
            case 'Z':
               ShiftNode("CA", false);
               break;
            case 'P':
               ShiftNode("CC", true);
               break;
            case 'K':
               ShiftNode("CC", false);
               break;
            case 'Q':
               ShiftNode("CB", false);
               break;
            case 'D':
               ShiftNode("AB", false);
               break;
            case 'B':
               ShiftNode("AA", true);
               break;
         }
      }

      State = "IDLE";
      ModDisplay.text = Queries[0] + " " + Queries[1] + " " + Queries[2];
   }

   void Solve () {
      State = "SOLVED";
      Debug.LogFormat("[Three Bits #{0}] Solved!", ModuleId);
      ModDisplay.text = "✓ ✓ ✓";
      ModuleSolved = true;
      GetComponent<KMBombModule>().HandlePass();
      Audio.PlaySoundAtTransform("3B Solve", SubmitButton.transform);
   }

   void Strike (string Reason) {
      GetComponent<KMBombModule>().HandleStrike();
      
      State = "STRIKE";
      ModDisplay.text = "ERROR";
      StartCoroutine(SoftReset());

      switch(Reason){
         case "FOURQUERIES":
            Debug.LogFormat("[Three Bits #{0}] Attempted to press another letter button when query was full! Strike!", ModuleId);
            break;
         case "INCOMPLETEQUERY":
            Debug.LogFormat("[Three Bits #{0}] Attempted to query less than three letters! Strike!", ModuleId);
            break;
         case "INCOMPLETESUB":
            Debug.LogFormat("[Three Bits #{0}] Attempted to submit less than three letters! Strike!", ModuleId);
            break;
         case "PRESSWHENUNREADY":
            Debug.LogFormat("[Three Bits #{0}] Attempted to press a button while the module was working! Strike!", ModuleId);
            break;
         case "DUPEQUERY":
            Debug.LogFormat("[Three Bits #{0}] Attempted to query two or more of the same letter! Strike!", ModuleId);
            break;
         case "DUPESUB":
            Debug.LogFormat("[Three Bits #{0}] Attempted to submit two or more of the same letter! Strike!", ModuleId);
            break;
         case "INCORRECTSUB":
            Debug.LogFormat("[Three Bits #{0}] Incorrect submission! Strike!", ModuleId);
            break;
         case "SOLVED":
            Debug.LogFormat("[Three Bits #{0}] Attempted to press a button when the module was solved! Strike!", ModuleId);
            break;
         default:
            Debug.LogFormat("[Three Bits #{0}] Something went wrong, I don't know what (Pls ping Possessed)! Code: {1}  Strike!", ModuleId, Reason);
            break;
      }

   }

   IEnumerator SoftReset(){
      Queries[0] = '_';
      Queries[1] = '_';
      Queries[2] = '_';
      yield return new WaitForSeconds(0.3f);

      if(!ModuleSolved){
         State = "IDLE";
         ModDisplay.text = Queries[0] + " " + Queries[1] + " " + Queries[2];
      } else {
         State = "SOLVED";
         ModDisplay.text = "✓ ✓ ✓";
      }
   }

   IEnumerator WaitToDisplay (string text){
      yield return new WaitForSeconds(0.3f);
      if(State == "STRIKE") yield break;
      ModDisplay.text = text;
      StartCoroutine(SoftReset());
   }

   IEnumerator WaitToStrike(){
      yield return new WaitForSeconds(0.3f);
      Strike("INCORRECTSUB");
   }

   IEnumerator WaitToSolve(){
      yield return new WaitForSeconds(0.3f);
      Solve();
   }

   char GetOutput (char Query){  
      string currentName = "_" + Query;
      string currentDir = "X";
      string nextNode = "_";
      string nextDir = "_";

      while(true){
         for(int i = 0; i < Network.Length; i++){
            if(Network[i].Name == currentName){
               if(Network[i].EntryDir.Contains(currentDir)){
                  nextNode = Network[i].ExitNode[Network[i].EntryDir.ToList().IndexOf(currentDir)];
                  nextDir = Network[i].ExitDir[Network[i].EntryDir.ToList().IndexOf(currentDir)];
                  break;
               } else {
                  return currentName[1];
               }
            }
         }

         currentDir = nextDir;
         currentName = nextNode;
      }
   }

   void ShiftNode(node target, bool special){
      char anchor = target.Name[1];
      char[] nonAnchor = "ABC".Replace(anchor.ToString(), "").ToCharArray();

      for(int i = 0; i < target.EntryDir.Length; i++){
         target.EntryDir[i] = target.EntryDir[i].Replace(nonAnchor[0], '_').Replace(nonAnchor[1], nonAnchor[0]).Replace('_', nonAnchor[1]);
         if(!special) target.EntryDir[i] = target.EntryDir[i][1].ToString() +  target.EntryDir[i][0].ToString();
      }
   }

   void ShiftNode(string targetName, bool special = false){
      for(int i = 0; i < Network.Length; i++){
         if(Network[i].Name == targetName){
            ShiftNode(Network[i], special);
            return;
         }
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"!{0} B G K QUERY P V Z SUBMIT to press those buttons, chainable with spaces.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      while(State != "IDLE"){
         yield return new WaitForSeconds(0.3f);
      }

      Command = Command.Trim().ToUpper();
      string[] Commands = Command.Split(' ');
      yield return null;

      foreach(string cmd in Commands){
         if(cmd == "QUERY") QueryButton.OnInteract();
         else if(cmd == "SUBMIT") SubmitButton.OnInteract();
         else if(Regex.IsMatch(cmd, @"[BCDEGJKPQTVZ]") && cmd.Length == 1){
            LetterButton[HalfAlphabet.IndexOf(cmd)].OnInteract();
         } else {
            yield return "sendtochaterror Invalid command: " + cmd;
         }

         yield return new WaitForSeconds(0.7f);
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      
      Debug.LogFormat("[Three Bits #{0}] Module is being autosolved by Twitch Plays.", ModuleId);
      while(State != "IDLE"){
         if(State == "SOLVED") yield break; //how are you here
         yield return new WaitForSeconds(0.3f);
      }

      //im too lazy to implement global hashsets or changing the global arrays to hashsets 
      HashSet<char> MirrorsHash = new HashSet<char>(Mirrors);


      //force softreset if inputs contains dupes, exclusive to autosolver
      HashSet<char> currentQueries = new HashSet<char>();
      for(int i = 0; i < 3; i++){
         if(!currentQueries.Add(Queries[i]) && Queries[i] != '_'){
            StartCoroutine(SoftReset());
            yield return new WaitForSeconds(0.7f);
         }
      }

      currentQueries = new HashSet<char>(Queries);
      currentQueries.Remove('_');

      //if inputs are not already part of the answer
      if(!currentQueries.IsSubsetOf(MirrorsHash)){

         //query 3 letters to clear inputs
         while(Queries[2] == '_'){
            int newLetterIndex = Rnd.Range(0,12);
            if(currentQueries.Add(HalfAlphabet[newLetterIndex])){
               LetterButton[newLetterIndex].OnInteract();
               yield return new WaitForSeconds(0.7f);
            }
         }

         QueryButton.OnInteract();
         currentQueries.Clear();
         yield return new WaitForSeconds(0.7f);
      }

      for(int i = 0; i < 3; i++){
         if(!currentQueries.Contains(Mirrors[i])){
            LetterButton[HalfAlphabet.IndexOf(Mirrors[i])].OnInteract();
            yield return new WaitForSeconds(0.7f);
         }
      }

      SubmitButton.OnInteract();
   }
}
