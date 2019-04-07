using System;
using System.Collections.Generic;
using System.Linq;

namespace Queue.QueueApp.Model {

	public class QueuedService {

		private readonly List<string> requesters = new List<string>();

		public string CurrentOwner { get; private set; }

		public string Id { get; private set; }

		public QueuedService(string id, string currentOwner) {
			Id = id;
			CurrentOwner = currentOwner;
		}

		public void SetNextOwner() {
			try {
				CurrentOwner = requesters[0];
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}
		}

		public bool IsCurrentOwner(string userId) {
			return CurrentOwner == userId;
		}

		public void PushUser(string userId) {
			requesters.Add(userId);
		}

		public void PopUser() {
			requesters.RemoveAt(0);
		}

		public bool IsAnyUserWaiting() {
			return requesters.Any();
		}

		public bool IsUserWaiting(string userId) {
			return requesters.Contains(userId);
		}

		public string RequestersToString() {
			return requesters.Aggregate("", (current, requester) => current + requester + " ");
		}

	}

}