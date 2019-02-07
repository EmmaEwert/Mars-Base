namespace Game.Server {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using UnityEngine;

	public abstract class ComponentSystem : MonoBehaviour {
		List<(MethodInfo method, Type[] parameters)> updateMethods = new List<(MethodInfo, Type[])>();

		EntityManager manager;
		protected EntityManager Manager => manager = manager ?? FindObjectOfType<EntityManager>();

		protected void Awake() {
			var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var method in methods) {
				if (method.Name == "OnUpdate") {
					var parameters = new List<Type>();
					foreach (var parameter in method.GetParameters()) {
						parameters.Add(parameter.ParameterType);
					}
					updateMethods.Add((method, parameters.ToArray()));
				}
			}
		}

		protected void Update() {
			foreach (var updateMethod in updateMethods) {
				var types = updateMethod.parameters;
				var entities = Manager.FindAll(types);
				foreach (var entity in entities) {
					var parameters = types.Select(t => entity.GetComponent(t)).ToArray();
					updateMethod.method.Invoke(this, parameters);
				}
			}
		}
	}
}