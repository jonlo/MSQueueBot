using System;
using System.Collections.Generic;

namespace MSJLBot.ChatService.Model.ChatSubSystems.Queue {
	/// <summary>
	/// TODO CONVERTIR A JSON
	/// </summary>
	public static class ConversationWords {

		#region Actions
		public static List<string> requestWords = new List<string>{"pido","reservo","pillo","reservar","pedir","pillar"};
		public static List<string> freeWords = new List<string> {"libero", "suelto", "dejo", "agur"};
		public static List<string> askForServiceStateWords = new List<string> {"ver", "mostrar", "estado"};
		#endregion

		#region errorMessages 
		public static List<string> emptyServicePhrases = new List<string> {"Has escrito mal el nombre del recurso", "Tal vez deberías pedir algo..", "No puedes dejar el recurso vacío!"};
		public static List<string> authorizationErrors = new List<string> {"No puedes liberar lo que no posees!", "Chsss ese recurso no es tuyo actualmente", "Solo puedes liberar recursos que sean tuyos.."};
		public static string errorParsingMessage = "Error al procesar texto";
		#endregion

		#region actionMessages

		public static List<string> removeServicePhrases = new List<string> {"Recurso liberado"};


		#endregion
		public static string GetRandomValueFromList(List<string> phrases) {
			Random rnd = new Random();
			int r = rnd.Next(phrases.Count);
			return phrases[r];
		}
	}

}