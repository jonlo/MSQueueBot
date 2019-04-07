// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Samples.HelloWorld.Web.Controllers.QueueApp;
using Queue.QueueApp;

namespace Queue {

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

	/// <summary>
	///     Represents a bot that processes incoming activities.
	///     For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
	///     This is a Transient lifetime service.  Transient lifetime services are created
	///     each time they're requested. For each Activity received, a new instance of this
	///     class is created. Objects that are expensive to construct, or have a lifetime
	///     beyond the single turn, should be carefully managed.
	///     For example, the <see cref="MemoryStorage" /> object and associated
	///     <see cref="IStatePropertyAccessor{T}" /> object are created with a singleton lifetime.
	/// </summary>
	/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1" />
	public class QueueBot : IBot {

		private readonly QueueAccessors _accessors;
		private readonly ILogger _logger;

		/// <summary>
		///     Initializes a new instance of the class.
		/// </summary>
		/// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}" /> used to manage state.</param>
		/// <param name="loggerFactory">A <see cref="ILoggerFactory" /> that is hooked to the Azure App Service provider.</param>
		/// <seealso
		///     cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider" />
		public QueueBot(QueueAccessors accessors, ILoggerFactory loggerFactory) {
			if (loggerFactory == null) {
				throw new ArgumentNullException(nameof(loggerFactory));
			}
			_logger = loggerFactory.CreateLogger<QueueBot>();
			_logger.LogTrace("Turn start.");
			_accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
		}

		/// <summary>
		///     Every conversation turn for our Echo Bot will call this method.
		///     There are no dialogs used, since it's "single turn" processing, meaning a single
		///     request and response.
		/// </summary>
		/// <param name="turnContext">
		///     A <see cref="ITurnContext" /> containing all the data needed
		///     for processing this conversation turn.
		/// </param>
		/// <param name="cancellationToken">
		///     (Optional) A <see cref="CancellationToken" /> that can be used by other objects
		///     or threads to receive notice of cancellation.
		/// </param>
		/// <returns>A <see cref="Task" /> that represents the work queued to execute.</returns>
		/// <seealso cref="BotStateSet" />
		/// <seealso cref="ConversationState" />
		/// <seealso cref="IMiddleware" />
		public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken)) {
			// Handle Message activity type, which is the main activity type for shown within a conversational interface
			// Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
			// see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
			if (turnContext.Activity.Type == ActivityTypes.Message) {
				// Get the conversation state from the turn context.
				CounterState state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());
				ProcessText(turnContext);
			}
		}

		private async void ProcessText(ITurnContext turnContext) {
			try {
				List<string> texts = turnContext.Activity.Text.Split(' ').ToList();
				ActionService actionService = GetActionService(texts);
				ProcessAction(actionService, turnContext);
			}
			catch (Exception e) {
				await turnContext.SendActivityAsync($"Error al procesar texto");
			}
		}

		private ActionService GetActionService(List<string> texts) {
			ActionService actionService = new ActionService();
			foreach (string t in texts) {
				if (ConversationWords.requestWords.Contains(t)) {
					actionService.actionType = ActionType.RequestService;
				}else if (ConversationWords.freeWords.Contains(t)) {
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
		
		
		private async void ProcessAction(ActionService actionService, ITurnContext turnContext) {
			switch (actionService.actionType) {
				case ActionType.NoAction:
					await turnContext.SendActivityAsync($"Error al procesar texto");
					return;
				case ActionType.RequestService:
					RequestService(actionService, turnContext);
					break;
				case ActionType.AskForServiceState:
					AskForServiceState(actionService, turnContext);
					break;
				case ActionType.FreeService:
					FreeService(actionService, turnContext);
					break;
				default:
					break;
			}
		}

		private void RequestService(ActionService actionService, ITurnContext turnContext) {
			QueueManager.RequestService(turnContext, turnContext.Activity.From.Name, actionService.serviceName);
		}

		private void FreeService(ActionService actionService, ITurnContext turnContext) {
			QueueManager.SetServiceFree(turnContext, turnContext.Activity.From.Name, actionService.serviceName);
		}

		private void AskForServiceState(ActionService actionService, ITurnContext turnContext) {
			QueueManager.AskForServiceState(turnContext, turnContext.Activity.From.Name, actionService.serviceName);
		}
	}

}