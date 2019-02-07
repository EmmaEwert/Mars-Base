namespace Net {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	internal static class Reflector {
		///<summary>Finds types that are children of the specified type.</summary>
		public static List<System.Type> ImplementationsOf<T>() where T : class {
			return AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(assemby => assemby.GetTypes())
				.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(T)))
				.OrderBy(type => type.Name).ToList();
		}

		///<summary>Creates new instances of types that are children of the specified type.</summary>
		public static List<T> InstancesOf<T>(params object[] constructorArgs) where T : class {
			var objects = new List<T>();
			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
			foreach (var type in types.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)))) {
				objects.Add((T)Activator.CreateInstance(type, constructorArgs));
			}
			return objects;
		}
	}
}
