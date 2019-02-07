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
			Net.Server.Listen<CatTalkMessage>(m => m.Broadcast());
			gameObject.AddComponent<Net.Server>();
			var random = new Unity.Mathematics.Random(1); 
			var manager = FindObjectOfType<Server.EntityManager>();
			for (var i = 0; i < catStrings.Length; ++i) {
				var entity = manager.Create();
				var sprite = manager.AddComponent<RenderSprite>(entity);
				var position = manager.AddComponent<Position>(entity);
				var velocity = manager.AddComponent<Velocity>(entity);
				velocity.value.x = random.NextFloat();
				sprite.resource = catSprites[random.NextUInt() % catSprites.Length];
				position.value = new float3(random.NextInt() % 128, 0.5f, 0);
			}
		}
	}
}