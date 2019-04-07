using Microsoft.Bot.Builder;

namespace MSJLBot.ChatService.Control.ChatManagement {

	public interface IChatService {

		void ProcessMessage(ITurnContext turnContext);
		event ChatServiceHandler OperationFinished;

	}

	public delegate void ChatServiceHandler(ITurnContext turnContext, string message);

}