using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using DeafMath = ExMath;

//please for the love of your sanity ask me how this shit works before digging in
//cuz this cruxes on graph theory and batshit insane insanity
//im always available on discord
//-Psd

namespace NetMaker {
	public class Node {
		public static Node NullNode = new Node('-');

		public char Label;
		public Node VertiConn;
		public Node ClockConn;
		public Node CounterConn;

		public Node(char u){
			Label = u;
			VertiConn = NullNode;
			ClockConn = NullNode;
			CounterConn = NullNode;
		}

		public Node(char u, Node v, Node c, Node w){
			Label = u;
			VertiConn = v;
			ClockConn = c;
			CounterConn = w;
		}

		public override string ToString() {
			char VLab	= VertiConn		!= NullNode ? VertiConn.Label 	: '-';
			char CWLab	= ClockConn		!= NullNode ? ClockConn.Label	: '-';
			char CCWLab = CounterConn	!= NullNode ? CounterConn.Label	: '-';
			return string.Format("[{0}:{1}{2}{3}]", Label, VLab, CWLab, CCWLab);
		}

		public Node GetNodeAtEdge(int i){
			switch (i) {
				case 0: return this.VertiConn;
				case 1: return this.ClockConn;
				case 2: return this.CounterConn;
				default: throw new ArgumentException(string.Format("Invalid edge index: {0} (for node: {1})", i, this.ToString()));
			}
		}

		public void SetNodeAtEdge(Node m, int i){
			switch (i) {
				case 0:
					this.VertiConn = m;
					break;
				case 1:
					this.ClockConn = m;
					break;
				case 2:
					this.CounterConn = m;
					break;
				default: throw new ArgumentException(string.Format("Invalid edge index: {0} (for node: {1})", i, this.ToString()));
			}
		}

		public int GetEdgeNodeIndex(char m){
			if(this.VertiConn.Label == m) return 0;
			if(this.ClockConn.Label == m) return 1;
			if(this.CounterConn.Label == m) return 2;

			return -1;
		}
	}

	public class Graph {
		public List<Node> Listma = new List<Node>();

		public Graph(){
			for(int i = 0; i < 4; i++){
				Listma.Add(new Node("ABCDEFGHIJKLMNOPQRSTUVWXYZ"[i]));
			}
		}

		public Graph(List<Node> li){
			for(int i = 0; i < li.Count; i++)
				Listma.Add(li[i]);
		}

		//node, v, c, w,
		//"ABCD;BCAD;"
		public Graph(string ky){
			string[] kys = ky.ToUpper().Split(';');

			foreach (string k in kys){
				if(k == "") continue;
				if(!Regex.IsMatch(k, @"^\w[A-Z-]{3}$")) throw new ArgumentException(string.Format("Invalid node key: {0}", k));
				Listma.Add(new Node(k[0]));
			}

			foreach(string k in kys){
				if(k == "") continue;
				GetNode(k[0]).VertiConn		= k[1] == '-' ? Node.NullNode : GetNode(k[1]);
				GetNode(k[0]).ClockConn		= k[2] == '-' ? Node.NullNode : GetNode(k[2]);
				GetNode(k[0]).CounterConn	= k[3] == '-' ? Node.NullNode : GetNode(k[3]);
			}
		}

		public Node GetNode(int i){
			return Listma[i];
		}

		public Node GetNode(char c){
			var n = Listma.FirstOrDefault(x => x.Label == c);
			if(n != null) return n;
			
			return Node.NullNode;
		}

		public int GetNodeIndex(Node c){
			for(int i = 0; i < Listma.Count; i++){
				if(Listma[i].Label == c.Label) return i;
			}

			throw new ArgumentException(string.Format("Cannot find node {0}", c));
			return -1;
		}

		public int GetNodeIndex(char c){
			for(int i = 0; i < Listma.Count; i++){
				if(Listma[i].Label == c) return i;
			}

			throw new ArgumentException(string.Format("Cannot find node {0}", c));
			return -1;
		}

		public Graph DisconnectedGraph(){
			string newKey = "";
			for(int i = 0; i < Listma.Count; i++){
				newKey = newKey + Listma[i].Label + "---;";
			}
			return new Graph(newKey);
		}

		public static Graph MakeNet(Graph srcGraph){
			List<Node> net = new List<Node>();
			List<Node> pieces = srcGraph.DisconnectedGraph().Listma;

			int seedIndex = Rnd.Range(0, pieces.Count);
			net.Add(pieces[seedIndex]);
			pieces.RemoveAt(seedIndex);

			while(pieces.Count > 0) {
				int branchLoc = Rnd.Range(0, net.Count);
				int edgeIndex = Rnd.Range(0, 3);

				//if branch hasnt been made from this node on this side
				if(net[branchLoc].GetNodeAtEdge(edgeIndex) != Node.NullNode) continue;
				Node preplaced = net[branchLoc];
				Node postplaced = new Node(srcGraph.GetNode(preplaced.Label).GetNodeAtEdge(edgeIndex).Label);

				//if next node is still up for grabs
				if(pieces.Count(x => x.Label == postplaced.Label) != 1) continue;
				
				preplaced.SetNodeAtEdge(postplaced, edgeIndex);
				postplaced.SetNodeAtEdge(preplaced, srcGraph.GetNode(postplaced.Label).GetEdgeNodeIndex(preplaced.Label));

				net.Add(postplaced);
				pieces = pieces.Where(x => x.Label != postplaced.Label).ToList();
			}

			return new Graph(net);
		}

		public static List<Graph> MakePartialNet(Graph srcGraph, int numPerPart) {
			List<Graph> ans = new List<Graph>();
			List<Node> pieces = srcGraph.DisconnectedGraph().Listma;

			int breakOut = 1000;

			while(pieces.Count > 0){
				List<Node> partialNet = new List<Node>();
				
				int seedIndex = Rnd.Range(0, pieces.Count);
				partialNet.Add(pieces[seedIndex]);
				pieces.RemoveAt(seedIndex);

				while(partialNet.Count < numPerPart && breakOut-- >= 0) {
					int branchLoc = Rnd.Range(0, partialNet.Count);
					int edgeIndex = Rnd.Range(0, 3);

					//if branch hasnt been made from this node on this side
					if(partialNet[branchLoc].GetNodeAtEdge(edgeIndex) != Node.NullNode) continue;
					Node preplaced = partialNet[branchLoc];
					Node postplaced = new Node(srcGraph.GetNode(preplaced.Label).GetNodeAtEdge(edgeIndex).Label);

					//if next node is still up for grabs
					if(pieces.Count(x => x.Label == postplaced.Label) != 1) continue;
					
					preplaced.SetNodeAtEdge(postplaced, edgeIndex);
					postplaced.SetNodeAtEdge(preplaced, srcGraph.GetNode(postplaced.Label).GetEdgeNodeIndex(preplaced.Label));

					partialNet.Add(postplaced);
					pieces = pieces.Where(x => x.Label != postplaced.Label).ToList();
				}

				ans.Add(new Graph(partialNet));
			}

			return ans.Count > 4 ? MakePartialNet(srcGraph, numPerPart) : ans;
		}
	}
	//i cannot fucking believe the crack cocaine shithole of code worked first try
	//put this on a paper
	//-Psd
}