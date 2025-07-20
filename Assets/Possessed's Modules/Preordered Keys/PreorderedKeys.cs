//vflyer your order is here
//key parent base y: 0.0034
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

   private int ScreenIndex = 0;
   private List<string> KeysStrings = new List<string>();
   private string ModState = "";
   

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
   }

   void ScreenPress (){
      if(KeysStrings.Count == 0) return;

      ScreenButton.AddInteractionPunch();
      Audio.PlaySoundAtTransform("ScreenPress", ScreenButton.transform);

      ScreenIndex = (ScreenIndex + 1) % KeysStrings.Count;
      UpdateScreen(ScreenIndex);
   }

   void KeyPress (KMSelectable KMS){
      if(ModState != "ARRIVED") return;
      if(KMS.transform.localPosition.y < 0.005) return;

      KMS.transform.localPosition += new Vector3(0f, -0.01f, 0f);
      KMS.AddInteractionPunch();
   }

   void SlotPress (KMSelectable Slot){
      int i = 0;
      for(; i < 6; i++){
         if(SlotKMS[i] == Slot) break;
      }

      if(KeyOBJ[i].activeInHierarchy) return;

      Slot.AddInteractionPunch();
      Debug.LogFormat("[Preordered Keys #{0}] Placed {1} into position {2}.", ModuleId, KeysStrings[ScreenIndex], i+1);

      SetKeyProp(KeyOBJ[i], KeysStrings[ScreenIndex]);
      KeyOBJ[i].SetActive(true);
      
      StartCoroutine(InsertKey(KeyKMS[i]));
      StartCoroutine(WaitToDeactivate(SlotOBJ[i]));

      KeysStrings.Remove(KeysStrings[ScreenIndex]);
      
      if(KeysStrings.Count != 0){
         ScreenIndex %= KeysStrings.Count;
      } else {
         StartCoroutine(SecondPhaseAni());
      }
      UpdateScreen(ScreenIndex);
   }


   void OnDestroy () { //Shit you need to do when the bomb ends
      
   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on      

   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module 
      ModState = "PREORDERED"; 
      Debug.LogFormat("[Preordered Keys #{0}] Your preorder consists of: ", ModuleId);    
      for(int i = 0; i < 6; i++){
         KeysStrings.Add("RGBCMY"[Rnd.Range(0, 6)] + Rnd.Range(1, 7).ToString() + "RGBCMY"[Rnd.Range(0, 6)]);
         Debug.LogFormat("[Preordered Keys #{0}] {1}", ModuleId, KeysStrings[i]);    
      }

      UpdateScreen(0);

      foreach(KMSelectable KMS in KeyKMS){
         KMS.transform.localPosition += new Vector3(0f,0.5f,0f);
      }
      foreach(GameObject OBJ in KeyOBJ){
         OBJ.SetActive(false);
      }
   }

   void Update () { //Shit that happens at any point after initialization

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

   IEnumerator WaitToDeactivate(GameObject Slot){
      yield return new WaitForSeconds(1.0f);
      Slot.SetActive(false);
   }

   IEnumerator SecondPhaseAni(){
      yield return new WaitForSeconds(1.5f);
      foreach(KMSelectable KMS in KeyKMS){
         KMS.transform.localPosition += new Vector3(0f,0.01f,0f);
      }
      ModState = "ARRIVED";
      //[!] sound button release
   }

   void SetKeyProp(GameObject OBJ, string Props){
      TextMesh Txt = OBJ.GetComponentInChildren<TextMesh>();
      
      OBJ.GetComponent<Renderer>().material.color = DullColour["RGBCMY".IndexOf(Props[0])];
      Txt.text = Props[0] + "\n" + Props[1] + "\n\n" + Props[2];
      Txt.color = BrightColour["RGBCMY".IndexOf(Props[2])];
   }

   void UpdateScreen(int i){
      if(KeysStrings.Count == 0){
         DisplayText.color = Color.black;
         DisplayText.text = "";
         ScreenMat.material.color = Color.black;
      } else {
         DisplayText.color = BrightColour["RGBCMY".IndexOf(KeysStrings[i][2])];
         DisplayText.text = KeysStrings[i][0] + " " + KeysStrings[i][1] + " " + KeysStrings[i][2];
         ScreenMat.material.color = DullColour["RGBCMY".IndexOf(KeysStrings[i][0])];
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} to ?????.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
   }
}
