using System.Collections.Generic;
using System.Linq;

namespace ChatService.Model.ChatSubSystems.Queuel {

	public class QueuedService {

		private readonly List<string> requesters = new List<string>();

		public string CurrentOwner { get; private set; }

		public string Id { get; private set; }

		public QueuedService(string id, string currentOwner) {
			Id = id;
			CurrentOwner = currentOwner;
		}

		public void SetNextOwner() {
			CurrentOwner = requesters.Any() ? requesters[0] : "";
		}

		public bool IsCurrentOwner(string userId) {
			return CurrentOwner == userId;
		}

		public void PushRequester(string userId) {
			requesters.Add(userId);
		}

		public void PopRequester() {
			if (requesters.Any()) {
				requesters.RemoveAt(0);
			}
		}

		public bool IsAnyUserWaiting() {
			return !string.IsNullOrEmpty(CurrentOwner) || requesters.Any();
		}

		public bool IsUserWaiting(string userId) {
			return requesters.Contains(userId);
		}

		public string RequestersToString() {
			return requesters.Aggregate("", (current, requester) => current + requester + " ");
		}

	}

}