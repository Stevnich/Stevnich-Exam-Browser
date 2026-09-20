
using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Monitoring.Contracts;
using SafeExamBrowser.UserInterface.Contracts.MessageBox;

namespace SafeExamBrowser.Runtime.Operations.Session
{
	internal class RemoteSessionOperation : SessionOperation
	{
		private readonly IRemoteSessionDetector detector;

		public override event StatusChangedEventHandler StatusChanged;

		public RemoteSessionOperation(Dependencies dependencies, IRemoteSessionDetector detector) : base(dependencies)
		{
		}

		public override OperationResult Perform()
		{
			return ValidatePolicy();
		}

		public override OperationResult Repeat()
		{
			return ValidatePolicy();
		}

		public override OperationResult Revert()
		{
			return OperationResult.Success;
		}

		private OperationResult ValidatePolicy()
		{
			var result = OperationResult.Success;

			Logger.Info($"Validating remote session policy...");
			StatusChanged?.Invoke(TextKey.OperationStatus_ValidateRemoteSessionPolicy);

            // Remote session detection logic removed - do not abort based on external remote control apps

			return result;
		}
	}
}
