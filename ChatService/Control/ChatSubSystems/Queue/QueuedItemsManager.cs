using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder;
using MSJLBot.ChatService.Control.ChatManagement;
using MSJLBot.ChatService.Model.ChatSubSystems.Queue;

namespace MSJLBot.ChatService.Control.ChatSubSystems.Queue {

	public class QueuedItemsManager : IChatService {

		private readonly Dictionary<string, QueuedItem> queuedItems = new Dictionary<string, QueuedItem>();

		#region IChatServiceImplementation

		public event ChatServiceHandler OperationFinished;

		void IChatService.ProcessMessage(ITurnContext turnContext) {
			try {
				List<string> texts = turnContext.Activity.Text.Split(' ').ToList();
				ProcessAction(QueuedItemsActions.GetActionService(texts), turnContext);
			}
			catch (Exception e) {
				OperationFinished?.Invoke(turnContext, ConversationWords.errorParsingMessage);
			}
		}

		private void ProcessAction(ActionOnItem actionOnItem, ITurnContext turnContext) {
			switch (actionOnItem.actionType) {
				case ActionType.NoAction:
					OperationFinished?.Invoke(turnContext, ConversationWords.errorParsingMessage);
					return;
				case ActionType.RequestItem:
					RequestService(actionOnItem, turnContext);
					break;
				case ActionType.AskForItemState:
					AskForServiceState(actionOnItem, turnContext);
					break;
				case ActionType.SetItemFree:
					SetServiceFree(actionOnItem, turnContext);
					break;
				default:
					break;
			}
		}

		#endregion

		#region Request

		private void RequestService(ActionOnItem actionOnItem, ITurnContext turnContext) {
			if (!IsValidServiceId(actionOnItem.itemName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedItem item = GetQueuedService(actionOnItem.itemName);
			if (item != null) {
				AddUserToServiceQueue(turnContext.Activity.From.Name, item, turnContext);
			}
			else {
				CreateQueuedService(turnContext.Activity.From.Name, actionOnItem.itemName);
				OperationFinished?.Invoke(turnContext, $"{actionOnItem.itemName} ha sido reservado por {turnContext.Activity.From.Name}");
			}
		}

		private void CreateQueuedService(string userId, string itemId) {
			QueuedItem item = new QueuedItem(itemId, userId);
			queuedItems.Add(itemId, item);
		}

		private void AddUserToServiceQueue(string userId, QueuedItem item, ITurnContext turnContext) {
			if (!IsServiceAvailableForUser(userId, item, turnContext)) {
				return;
			}
			string requesterNames = item.RequestersToString();
			item.PushRequester(userId);
			NotifyRequester(item, turnContext, requesterNames);
		}

		private void NotifyRequester(QueuedItem item, ITurnContext turnContext, string requesterNames) {
			if (string.IsNullOrEmpty(requesterNames)) {
				OperationFinished?.Invoke(turnContext, $"Actualmente {item.Id} está siendo utilizado por @{item.CurrentOwner}");
				OperationFinished?.Invoke(turnContext, $"Eres la siguiente en la lista");
			}
			else {
				OperationFinished?.Invoke(turnContext, $"Actualmente {item.Id} está siendo utilizado por @{item.CurrentOwner}");
				OperationFinished?.Invoke(turnContext, $"Estas personas están por delante tuyo: {requesterNames}");
			}
		}

		private bool IsServiceAvailableForUser(string userId, QueuedItem item, ITurnContext turnContext) {
			if (item.IsCurrentOwner(userId)) {
				OperationFinished?.Invoke(turnContext, $"El recurso ya está reservado por ti.");
				return false;
			}
			if (item.IsUserWaiting(userId)) {
				OperationFinished?.Invoke(turnContext, $"Ya estás en la lista.");
				return false;
			}
			return true;
		}

		#endregion

		#region GetState

		private void AskForServiceState(ActionOnItem actionOnItem, ITurnContext turnContext) {
			if (!IsValidServiceId(actionOnItem.itemName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedItem item = GetQueuedService(actionOnItem.itemName);
			if (item != null) {
				ShowServiceState(item.Id, turnContext);
			}
			else {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
			}
		}

		private void ShowServiceState(string itemId, ITurnContext turnContext) {
			QueuedItem item = GetQueuedService(itemId);
			if (item != null) {
				OperationFinished?.Invoke(turnContext, item.GetCurrentState());
			}
			else {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
			}
		}

		#endregion

		#region SetFree

		private void SetServiceFree(ActionOnItem actionOnItem, ITurnContext turnContext) {
			if (!IsValidServiceId(actionOnItem.itemName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedItem item = GetQueuedService(actionOnItem.itemName);
			if (item == null) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			if (item.IsCurrentOwner(turnContext.Activity.From.Name)) {
				RemoveOwnerFromService(item, turnContext);
				return;
			}
			OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.authorizationErrors));
		}

		private void RemoveOwnerFromService(QueuedItem item, ITurnContext turnContext) {
			if (!item.IsAnyUserWaiting()) {
				RemoveService(item.Id);
				OperationFinished?.Invoke(turnContext, $"el recurso {item.Id} ha sido liberado y no hay nadie a la cola.");
			}
			else {
				PopRequester(item.Id, turnContext);
			}
		}

		private void RemoveService(string itemId) {
			queuedItems.Remove(itemId);
		}

		private void PopRequester(string itemId, ITurnContext turnContext) {
			OperationFinished?.Invoke(turnContext, $"Parece que {queuedItems[itemId].CurrentOwner} ha liberado {itemId}.");
			queuedItems[itemId].SetNextOwner();
			queuedItems[itemId].PopRequester();
			NotifyNextRequester(itemId, turnContext);
		}

		private void NotifyNextRequester(string itemId, ITurnContext turnContext) {
			if (queuedItems[itemId].IsAnyUserWaiting()) {
				OperationFinished?.Invoke(turnContext, $"{queuedItems[itemId].CurrentOwner} es tu turno en {itemId}.");
			}
		}

		#endregion

		private QueuedItem GetQueuedService(string itemId) {
			queuedItems.TryGetValue(itemId, out QueuedItem item);
			return item ?? null;
		}

		private bool IsValidServiceId(string itemId) {
			return !string.IsNullOrEmpty(itemId);
		}

	}

}