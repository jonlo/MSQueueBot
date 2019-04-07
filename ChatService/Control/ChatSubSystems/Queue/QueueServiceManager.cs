using System;
using System.Collections.Generic;
using System.Linq;
using ChatService.Model.ChatSubSystems.Queue;
using ChatService.Model.ChatSubSystems.Queuel;
using Microsoft.Bot.Builder;
using MSJLBot.ChatService.Control.ChatManagement;

namespace MSJLBot.ChatService.Control.ChatSubSystems.Queue {

	public class QueueServiceManager : IChatService {

		private readonly Dictionary<string, QueuedService> queuedServices = new Dictionary<string, QueuedService>();
		public event ChatServiceHandler OperationFinished;

		void IChatService.ProcessMessage(ITurnContext turnContext) {
		//	try {
				List<string> texts = turnContext.Activity.Text.Split(' ').ToList();
				ProcessAction(QueuePossibleActionServices.GetActionService(texts), turnContext);
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

		/// <summary>
		///     Solicitar un recurso
		/// </summary>
		/// <param name="turnContext"></param>
		/// <param name="userId"></param>
		/// <param name="serviceId"></param>
		public void RequestService(ActionService actionService, ITurnContext turnContext) {
			if (!IsValidServiceId(actionService.serviceName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = GetQueuedService(actionService.serviceName, turnContext);
			if (service != null && service.IsAnyUserWaiting()) {
				AddUserToServiceQueue(turnContext.Activity.From.Name, service, turnContext);
			}
			else {
				CreateQueuedService(turnContext.Activity.From.Name, actionService.serviceName);
				OperationFinished?.Invoke(turnContext, $"{actionService.serviceName} ha sido reservado por {turnContext.Activity.From.Name}");
			}
		}

		/// <summary>
		///     Liberar un recurso
		/// </summary>
		/// <param name="turnContext"></param>
		/// <param name="userId"></param>
		/// <param name="serviceId"></param>
		public void SetServiceFree(ActionService actionService, ITurnContext turnContext) {
			if (!IsValidServiceId(actionService.serviceName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = GetQueuedService(actionService.serviceName, turnContext);
			if (service != null) {
				if (service.IsCurrentOwner(turnContext.Activity.From.Name)) {
					RemoveOwnerFromService(service, turnContext);
					return;
				}
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.authorizationErrors));
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
			OperationFinished?.Invoke(turnContext, $"{queuedServices[serviceId].CurrentOwner} es tu turno en {serviceId}.");
		}

		private void ShowServiceState(string serviceId, ITurnContext turnContext) {
			QueuedService service = GetQueuedService(serviceId, turnContext);
			if (!service.IsAnyUserWaiting()) {
				OperationFinished?.Invoke(turnContext, $"Actualmente el recurso {serviceId} está libre.");
				return;
			}
			OperationFinished?.Invoke(turnContext, $"Actualmente el recurso {serviceId} está reservado por {queuedServices[serviceId].CurrentOwner}.");
			if (string.IsNullOrEmpty(service.RequestersToString())) {
				OperationFinished?.Invoke(turnContext, $"Y no hay lista de espera.");
			}
			else {
				OperationFinished?.Invoke(turnContext, $"Y están a la epera estas personas: {service.RequestersToString()}.");
			}
		}

		/// <summary>
		///     Ver el estado de un recurso
		/// </summary>
		/// <param name="turnContext"></param>
		/// <param name="userId"></param>
		/// <param name="serviceId"></param>
		public void AskForServiceState(ActionService actionService, ITurnContext turnContext) {
			if (!IsValidServiceId(actionService.serviceName)) {
				OperationFinished?.Invoke(turnContext, ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
			}
			QueuedService service = GetQueuedService(actionService.serviceName, turnContext);
			if (service != null) {
				ShowServiceState(service.Id, turnContext);
			}
		}
	
		private QueuedService GetQueuedService(string serviceId, ITurnContext turnContext) {
			QueuedService service;
			queuedServices.TryGetValue(serviceId, out service);
			return service ?? null;
		}

		private bool IsValidServiceId(string serviceId) {
			return !string.IsNullOrEmpty(serviceId);
		}

	}

}