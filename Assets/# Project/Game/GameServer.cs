namespace Game.Server {
	using Unity.Mathematics;
	using UnityEngine;

	public class GameServer : MonoBehaviour {
		string[] catStrings = {
			"Hum hum hummm...",
			"Nice day out today!",
			"Did you see the sunrise?",
			"There should be a mewteor shower tonight!",
			"Can't wait for the next shipment from Purth...",
			"...",
			"Hmm.",
			"Well, that's not right...",
			"Hello.",
			"Hello!",
			"Hey.",
			"Hey!",
			"Heya.",
			"Hi.",
			"Hi!",
			"Hiya!",
			"Greetings.",
			"Nice to see you.",
			"This Miaurtian dust gets everywhere...",
			"Chu-!",
			"Hungryyyyy...",
			"Can we pause for food?",
			"I wish the rations included snacks...",
			"Do you think we could grow mice ?",
			"Sleepy...",
			"Tired...",
			"Tiiiiired...",
			"I want a naaaaap.",
			"Not now.",
			"Talk later ?",
			"Sorry, busy.",
			"...do you ever wish we had enough rovers to race them?",
			"Eugh... hearing the dust storms against the ice shield makes my fur stand on end.",
		};
		string[] catSprites = {
			"cat1",
			"cat2",
			"cat3",
			"cat4",
			"cat5",
			"cat6",
			"cat7",
			"cat8",
			"cat9",
			"cat10",
			"cat11",
			"cat12",
		};

		void Start() {
			Net.Server.Listen<PlayerTransformMessage>(m => m.Broadcast());
			gameObject.AddComponent<Net.Server>();
			var random = new Unity.Mathematics.Random(1); 
			var manager = FindObjectOfType<Server.EntityManager>();

			// Habicat
			var entity = manager.Create();
			var sprite = manager.AddComponent<RenderSprite>(entity);
			var position = manager.AddComponent<Position>(entity);
			sprite.resource = "habicat";
			position.value = new float3(5, 1.5f, 1);
			// Path node
			var habPathEntity = manager.Create();
			position = manager.AddComponent<Position>(habPathEntity);
			manager.AddComponent<PathNode>(habPathEntity);
			position.value = new float3(3.5f, 1.125f, 0);

			// Science lab
			entity = manager.Create();
			sprite = manager.AddComponent<RenderSprite>(entity);
			position = manager.AddComponent<Position>(entity);
			sprite.resource = "science-lab";
			position.value = new float3(-5, 1.5f, 1);
			// Path node
			var labPathEntity = manager.Create();
			position = manager.AddComponent<Position>(labPathEntity);
			manager.AddComponent<PathNode>(labPathEntity);
			position.value = new float3(-3.5f, 1.125f, 0);

			// Cats
			for (var i = 0; i < catStrings.Length; ++i) {
				entity = manager.Create();
				sprite = manager.AddComponent<RenderSprite>(entity);
				position = manager.AddComponent<Position>(entity);
				var velocity = manager.AddComponent<Velocity>(entity);
				var path = manager.AddComponent<Destination>(entity);
				sprite.resource = catSprites[random.NextUInt() % catSprites.Length];
				position.value = new float3(random.NextInt() % 128, 0.5f, 0);
				velocity.value.x = random.NextFloat();
				path.nodeID = habPathEntity.id;
			}
		}
	}
}