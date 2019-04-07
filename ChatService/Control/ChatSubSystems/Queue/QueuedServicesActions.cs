using System.Collections.Generic;
using MSJLBot.ChatService.Model.ChatSubSystems.Queue;

namespace MSJLBot.ChatService.Control.ChatSubSystems.Queue {

	public enum ActionType {

		RequestService,
		AskForServiceState,
		FreeService,
		NoAction

	}

	public struct ActionService {

		public ActionType actionType;
		public string serviceName;

	}

	public static class QueuedServicesActions {

		public static ActionService GetActionService(List<string> texts) {
			ActionService actionService = new ActionService();
			foreach (string t in texts) {
				if (ConversationWords.requestWords.Contains(t)) {
					actionService.actionType = ActionType.RequestService;
				}
				else if (ConversationWords.freeWords.Contains(t)) {
					actionService.actionType = ActionType.FreeService;
				}
				else if (ConversationWords.askForServiceStateWords.Contains(t)) {
					actionService.actionType = ActionType.AskForServiceState;
				}
				int requestIndex = texts.IndexOf(t);
				actionService.serviceName = texts[requestIndex + 1];
				return actionService;
			}
			actionService.actionType = ActionType.NoAction;
			return actionService;
		}

	}

}