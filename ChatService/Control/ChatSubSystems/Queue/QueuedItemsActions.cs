using System.Collections.Generic;
using MSJLBot.ChatService.Model.ChatSubSystems.Queue;

namespace MSJLBot.ChatService.Control.ChatSubSystems.Queue {

	public enum ActionType {

		NoAction,
		RequestItem,
		AskForItemState,
		SetItemFree,
		

	}

	public struct ActionOnItem {

		public ActionType actionType;
		public string itemName;

	}

	public static class QueuedItemsActions {

		public static ActionOnItem GetActionService(List<string> texts) {
			ActionOnItem actionOnItem = new ActionOnItem();
			foreach (string t in texts) {
				if (ConversationWords.requestWords.Contains(t)) {
					actionOnItem.actionType = ActionType.RequestItem;
				}
				else if (ConversationWords.freeWords.Contains(t)) {
					actionOnItem.actionType = ActionType.SetItemFree;
				}
				else if (ConversationWords.askForServiceStateWords.Contains(t)) {
					actionOnItem.actionType = ActionType.AskForItemState;
				}
				int requestIndex = texts.IndexOf(t);
				actionOnItem.itemName = texts[requestIndex + 1];
				return actionOnItem;
			}
			return actionOnItem;
		}

	}

}