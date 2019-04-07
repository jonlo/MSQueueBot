using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder;
using MSJLBot.ChatService.Control.ChatManagement;
using MSJLBot.ChatService.Model.ChatSubSystems.Queue;

namespace MSJLBot.ChatService.Control.ChatSubSystems.Queue {

	public class QueueServiceManager : IChatService {

		private readonly Dictionary<string, QueuedService> queuedServices = new Dictionary<string, QueuedService>();

		#region IChatServiceImplementation

		public event ChatServiceHandler OperationFinished;

		void IChatService.ProcessMessage(ITurnContext turnContext) {
			//	try {
			List<string> texts = turnContext.Activity.Text.Split(' ').ToList();
			ProcessAction(QueuedServicesActions.GetActionService(texts), turnContext);
			//	}
			//	catch (Exception e) {
			//	OperationFinished?.Invoke(turnContext, ConversationWords.errorParsingMessage);
			//	}
		}

		private void ProcessAction(ActionService actionService, ITurnContext turnContext) {
			switch (actionService.actionType) {
				case ActionType.NoAction:
					OperationFinished?.Invoke(turnContext, ConversationWords.errorParsingMessage);
					return;
				case ActionType.RequestService:
					RequestService(actionService, turnContext);
					break;
				case ActionType.AskForServiceState:
					AskForServiceState(actionService, turnContext);
					break;
				case ActionType.FreeService:
					SetServiceFree(actionService, turnContext);
					break;
				default:
					break;
			}
		}

		#endregion

		#region Request

		private void RequestService(ActionService actionService, ITurnContext turnContext) {
			if (!IsValidServiceId(actionService.serviceName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = GetQueuedService(actionService.serviceName);
			if (service != null && service.IsAnyUserWaiting()) {
				AddUserToServiceQueue(turnContext.Activity.From.Name, service, turnContext);
			}
			else {
				CreateQueuedService(turnContext.Activity.From.Name, actionService.serviceName);
				OperationFinished?.Invoke(turnContext, $"{actionService.serviceName} ha sido reservado por {turnContext.Activity.From.Name}");
			}
		}

		private void CreateQueuedService(string userId, string serviceId) {
			QueuedService service = new QueuedService(serviceId, userId);
			queuedServices.Add(serviceId, service);
		}

		private void AddUserToServiceQueue(string userId, QueuedService service, ITurnContext turnContext) {
			if (!IsServiceAvailableForUser(userId, service, turnContext)) {
				return;
			}
			string requesterNames = service.RequestersToString();
			service.PushRequester(userId);
			NotifyRequester(service, turnContext, requesterNames);
		}

		private void NotifyRequester(QueuedService service, ITurnContext turnContext, string requesterNames) {
			if (string.IsNullOrEmpty(requesterNames)) {
				OperationFinished?.Invoke(turnContext, $"Actualmente {service.Id} está siendo utilizado por @{service.CurrentOwner}");
				OperationFinished?.Invoke(turnContext, $"Eres la siguiente en la lista");
			}
			else {
				OperationFinished?.Invoke(turnContext, $"Actualmente {service.Id} está siendo utilizado por @{service.CurrentOwner}");
				OperationFinished?.Invoke(turnContext, $"Estas personas están por delante tuyo: {requesterNames}");
			}
		}

		private bool IsServiceAvailableForUser(string userId, QueuedService service, ITurnContext turnContext) {
			if (service.IsCurrentOwner(userId)) {
				OperationFinished?.Invoke(turnContext, $"El recurso ya está reservado por ti.");
				return false;
			}
			if (service.IsUserWaiting(userId)) {
				OperationFinished?.Invoke(turnContext, $"Ya estás en la lista.");
				return false;
			}
			return true;
		}

		#endregion

		#region GetState

		private void AskForServiceState(ActionService actionService, ITurnContext turnContext) {
			if (!IsValidServiceId(actionService.serviceName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = GetQueuedService(actionService.serviceName);
			if (service != null) {
				ShowServiceState(service.Id, turnContext);
			}
		}

		private void ShowServiceState(string serviceId, ITurnContext turnContext) {
			QueuedService service = GetQueuedService(serviceId);
			if (service != null) {
				OperationFinished?.Invoke(turnContext, service.GetCurrentState());
			}
			else {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
			}
		}

		#endregion

		#region SetFree

		private void SetServiceFree(ActionService actionService, ITurnContext turnContext) {
			if (!IsValidServiceId(actionService.serviceName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = GetQueuedService(actionService.serviceName);
			if (service == null) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			if (service.IsCurrentOwner(turnContext.Activity.From.Name)) {
				RemoveOwnerFromService(service, turnContext);
				return;
			}
			OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.authorizationErrors));
		}

		private void RemoveOwnerFromService(QueuedService service, ITurnContext turnContext) {
			if (!service.IsAnyUserWaiting()) {
				RemoveService(service.Id);
				OperationFinished?.Invoke(turnContext, $"el recurso {service.Id} ha sido liberado y no hay nadie a la cola.");
			}
			else {
				PopRequester(service.Id, turnContext);
			}
		}

		private void RemoveService(string serviceId) {
			queuedServices.Remove(serviceId);
		}

		private void PopRequester(string serviceId, ITurnContext turnContext) {
			OperationFinished?.Invoke(turnContext, $"Parece que {queuedServices[serviceId].CurrentOwner} ha liberado {serviceId}.");
			queuedServices[serviceId].SetNextOwner();
			queuedServices[serviceId].PopRequester();
			NotifyNextRequester(serviceId, turnContext);
		}

		private void NotifyNextRequester(string serviceId, ITurnContext turnContext) {
			if (queuedServices[serviceId].IsAnyUserWaiting()) {
				OperationFinished?.Invoke(turnContext, $"{queuedServices[serviceId].CurrentOwner} es tu turno en {serviceId}.");
			}
		}

		#endregion

		private QueuedService GetQueuedService(string serviceId) {
			queuedServices.TryGetValue(serviceId, out QueuedService service);
			return service ?? null;
		}

		private bool IsValidServiceId(string serviceId) {
			return !string.IsNullOrEmpty(serviceId);
		}

	}

}