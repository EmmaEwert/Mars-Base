namespace Game.Server {
	using System.Collections.Generic;
	using UnityEngine;

	public class PathNode : MonoBehaviour {
		public Dictionary<int, float> connections = new Dictionary<int, float>();
	}
}