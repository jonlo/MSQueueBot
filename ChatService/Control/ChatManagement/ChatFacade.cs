using System.Collections.Generic;
using Microsoft.Bot.Builder;

namespace MSJLBot.ChatService.Control.ChatManagement {

	public static class ChatFacade {

		private static List<IChatService> chatServices = new List<IChatService>();

		public static void AddChatService(IChatService chatService) {
			if (!chatServices.Contains(chatService)) {
				chatServices.Add(chatService);
				chatService.OperationFinished += OnChatOperationFinished;
			}
		}

		public static void RemoveChatService(IChatService chatService) {
			if (chatServices.Contains(chatService)) {
				chatServices.Remove(chatService);
				chatService.OperationFinished -= OnChatOperationFinished;
			}
		}

		public static async void ProcessText(ITurnContext turnContext) {
			foreach (IChatService chatService in chatServices) {
				chatService.ProcessMessage(turnContext);
			}
		}

		private static async void OnChatOperationFinished(ITurnContext turnContext, string message) {
			await turnContext.SendActivityAsync(message);
		}

	}

}