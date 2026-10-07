namespace AcdDataBinding.EntryPoints.WebServices {
	using System.ServiceModel;
	using System.ServiceModel.Activation;
	using System.ServiceModel.Web;
	using System.Web.SessionState;
	using AcdDataBinding.DataBinding;
	using Terrasoft.Web.Common;

	/// <summary>
	/// Backend of the classic UI "Bind data" actions. Replaces GlbDataBindingService and the browser-side call to
	/// the platform SchemaDataDesignerService.SaveSchema, so the same package works on .NET Framework and .NET.
	/// Routes: .NET Framework - /0/rest/AcdDataBindingService/{method}; .NET - /rest/AcdDataBindingService/{method}.
	/// Business failures are returned with HTTP 200 and success = false: classic UI callService handlers read the
	/// response body and show the message.
	/// </summary>
	[ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public class AcdDataBindingService : BaseService, IReadOnlySessionState {

		#region Properties: Private

		private static IDataBindingManager Manager =>
			AcdDataBindingApp.AcdDataBindingApp.Instance.DataBindingManager;

		#endregion

		#region Methods: Public

		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
			BodyStyle = WebMessageBodyStyle.Bare)]
		public AcdGetPackagesResponse GetPackages(AcdGetPackagesRequest request) {
			return AcdDataBindingMapper.ToResponse(Manager.GetPackages(UserConnection, request?.SchemaName));
		}

		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
			BodyStyle = WebMessageBodyStyle.Bare)]
		public AcdRestoreColumnSetupResponse RestoreColumnSetup(AcdRestoreColumnSetupRequest request) {
			return AcdDataBindingMapper.ToResponse(Manager.RestoreColumnSetup(UserConnection, request?.ProfileKey));
		}

		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
			BodyStyle = WebMessageBodyStyle.Bare)]
		public AcdSaveBindingsResponse SaveBindings(AcdSaveBindingsRequest request) {
			AcdSaveBindingsRequest model = request ?? new AcdSaveBindingsRequest();
			return AcdDataBindingMapper.ToResponse(Manager.SaveBindings(UserConnection, model.PackageUId,
				AcdDataBindingMapper.ToDefinitions(model.Bindings)));
		}

		#endregion

	}
}
