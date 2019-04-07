using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Queue.QueueApp;
using Queue.QueueApp.Model;

namespace Microsoft.Teams.Samples.HelloWorld.Web.Controllers.QueueApp {

	public static class QueueManager {

		private static readonly Dictionary<string, QueuedService> queuedServices = new Dictionary<string, QueuedService>();

		/// <summary>
		///     Solicitar un recurso
		/// </summary>
		/// <param name="turnContext"></param>
		/// <param name="userId"></param>
		/// <param name="serviceId"></param>
		public static async void RequestService(ITurnContext turnContext, string userId, string serviceId) {
			if (!IsValidServiceId(serviceId)) {
				await turnContext.SendActivityAsync(ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = await GetQueuedService(serviceId, turnContext);
			if (service != null) {
				if (IsServiceFree(service.Id)) {
					CreateQueuedService(userId, service.Id);
					await turnContext.SendActivityAsync($"{service} ha sido reservado por {userId}");
					return;
				}
				AddUserToServiceQueue(userId, service, turnContext);
			}
		}

		/// <summary>
		///     Liberar un recurso
		/// </summary>
		/// <param name="turnContext"></param>
		/// <param name="userId"></param>
		/// <param name="serviceId"></param>
		public static async void SetServiceFree(ITurnContext turnContext, string userId, string serviceId) {
			if (!IsValidServiceId(serviceId)) {
				await turnContext.SendActivityAsync(ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
				return;
			}
			QueuedService service = await GetQueuedService(serviceId, turnContext);
			if (service != null) {
				if (IsServiceOwner(service.Id, userId)) {
					RemoveOwnerFromService(service, turnContext);
					return;
				}
				await turnContext.SendActivityAsync(ConversationWords.GetRandomValueFromList(ConversationWords.authorizationErrors));
			}
		}

		/// <summary>
		///     Ver el estado de un recurso
		/// </summary>
		/// <param name="turnContext"></param>
		/// <param name="userId"></param>
		/// <param name="serviceId"></param>
		public static async void AskForServiceState(ITurnContext turnContext, string userId, string serviceId) {
			if (!IsValidServiceId(serviceId)) {
				await turnContext.SendActivityAsync(ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));
			}
			QueuedService service = await GetQueuedService(serviceId, turnContext);
			if (service != null) {
				ShowServiceState(service.Id, turnContext);
			}
		}

		private static async Task<QueuedService> GetQueuedService(string serviceId, ITurnContext turnContext) {
			QueuedService service;
			queuedServices.TryGetValue(serviceId, out service);
			if (service != null) {
				return service;
			}
			await turnContext.SendActivityAsync(ConversationWords.GetRandomValueFromList(ConversationWords.emptyServicePhrases));

			return null;
		}

		private static bool IsServiceFree(string serviceId) {
			try {
				QueuedService selectedService = queuedServices[serviceId];
				if (selectedService == null) {
					return true;
				}
				return selectedService.CurrentOwner == "";
			}
			catch (Exception e) {
				return true;
			}
		}

		private static void CreateQueuedService(string userId, string serviceId) {
			QueuedService service = new QueuedService(serviceId, userId);
			queuedServices.Add(serviceId, service);
		}

		private static async void AddUserToServiceQueue(string userId, QueuedService service, ITurnContext turnContext) {
			if (!await IsServiceAvailableForUser(userId, service, turnContext)) {
				return;
			}
			QueuedService selectedService = queuedServices[service.Id];
			string requesterNames = GetServiceRequesters(service.Id);
			selectedService.PushUser(userId);
			if (string.IsNullOrEmpty(requesterNames)) {
				await turnContext.SendActivityAsync($"Actualmente {service.Id} está siendo utilizado por @{GetRequestedServiceCurrentOwner(service.Id)}");
				await turnContext.SendActivityAsync($"Eres la siguiente en la lista");
			}
			else {
				await turnContext.SendActivityAsync($"Actualmente {service.Id} está siendo utilizado por @{GetRequestedServiceCurrentOwner(service.Id)}");
				await turnContext.SendActivityAsync($"Estas personas están por delante tuyo: {requesterNames}");
			}
		}

		private static async Task<bool> IsServiceAvailableForUser(string userId, QueuedService service, ITurnContext turnContext) {
			if (service.IsCurrentOwner(userId)) {
				await turnContext.SendActivityAsync($"El recurso ya está reservado por ti.");
				return false;
			}
			if (service.IsUserWaiting(userId)) {
				await turnContext.SendActivityAsync($"Ya estás en la lista.");
				return false;
			}
			return true;
		}

		private static async void RemoveOwnerFromService(QueuedService service, ITurnContext turnContext) {
			if (!service.IsAnyUserWaiting()) {
				RemoveService(service.Id);
				await turnContext.SendActivityAsync($"el recurso {service.Id} ha sido liberado y no hay nadie a la cola.");
			}
			else {
				PopRequester(service.Id, turnContext);
			}
		}

		private static void RemoveService(string serviceId) {
			queuedServices.Remove(serviceId);
		}

		private static async void PopRequester(string serviceId, ITurnContext turnContext) {
			await turnContext.SendActivityAsync($"Parece que {queuedServices[serviceId].CurrentOwner} ha liberado {serviceId}.");
			queuedServices[serviceId].SetNextOwner();
			queuedServices[serviceId].PopUser();
			await turnContext.SendActivityAsync($"{queuedServices[serviceId].CurrentOwner} es tu turno en {serviceId}.");
		}

		private static async void ShowServiceState(string serviceId, ITurnContext turnContext) {
			if (IsServiceFree(serviceId)) {
				await turnContext.SendActivityAsync($"Actualmente el recurso {serviceId} está libre.");
				return;
			}
			await turnContext.SendActivityAsync($"Actualmente el recurso {serviceId} está reservado por {queuedServices[serviceId].CurrentOwner}.");
			if (string.IsNullOrEmpty(GetServiceRequesters(serviceId))) {
				await turnContext.SendActivityAsync($"Y no hay lista de espera.");
			}
			else {
				await turnContext.SendActivityAsync($"Y están a la epera estas personas: {GetServiceRequesters(serviceId)}.");
			}
		}

		private static bool IsValidServiceId(string serviceId) {
			return !string.IsNullOrEmpty(serviceId);
		}

		private static bool IsServiceOwner(string serviceId, string userId) {
			return queuedServices[serviceId].CurrentOwner == userId;
		}

		private static string GetRequestedServiceCurrentOwner(string serviceId) {
			return queuedServices[serviceId].CurrentOwner;
		}

		private static string GetServiceRequesters(string serviceId) {
			return queuedServices[serviceId].RequestersToString();
		}

	}

}