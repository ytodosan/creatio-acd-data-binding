namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AcdDataBinding.Shared;
	using Terrasoft.Core;

	/// <summary>
	/// Validates data binding requests and saves the bindings in order. The first failed binding stops the
	/// batch (later bindings are reported as not processed); related bindings without records are skipped.
	/// </summary>
	internal sealed class DataBindingManager : IDataBindingManager {

		#region Constants: Internal

		internal const string NoPermissionMessage =
			"You don't have permission to bind data. The \"CanManageSolution\" system operation is required.";

		internal const string SchemaNameRequiredMessage = "Entity schema name is required.";

		internal const string ProfileKeyRequiredMessage = "Column setup key is required.";

		internal const string PackageRequiredMessage = "Package is required.";

		internal const string BindingsRequiredMessage = "At least one data binding is required.";

		internal const string BindingNameRequiredMessage = "Data binding name is required.";

		internal const string RecordsRequiredMessage =
			"Specify record ids, a filter column with a value, or all records.";

		internal const string NoDataFoundMessage = "No records found to bind.";

		internal const string NotProcessedMessage = "Not processed because a previous data binding failed.";

		#endregion

		#region Fields: Private

		private readonly IBindingPackageProvider _packageProvider;

		private readonly IColumnSetupRepository _columnSetupRepository;

		private readonly IDataBindingStore _store;

		#endregion

		#region Constructors: Public

		public DataBindingManager(IBindingPackageProvider packageProvider, IColumnSetupRepository columnSetupRepository,
				IDataBindingStore store) {
			_packageProvider = packageProvider;
			_columnSetupRepository = columnSetupRepository;
			_store = store;
		}

		#endregion

		#region Methods: Private

		private static string Validate(BindingDefinition definition) {
			if (definition == null || string.IsNullOrWhiteSpace(definition.SchemaName)) {
				return SchemaNameRequiredMessage;
			}
			if (string.IsNullOrWhiteSpace(definition.Name)) {
				return BindingNameRequiredMessage;
			}
			bool hasRecordIds = definition.RecordIds != null && definition.RecordIds.Any(id => id != Guid.Empty);
			bool hasFilter = !string.IsNullOrWhiteSpace(definition.FilterColumnName);
			if (!hasRecordIds && !hasFilter && !definition.AllRecords) {
				return RecordsRequiredMessage;
			}
			return null;
		}

		#endregion

		#region Methods: Public

		public Result<IReadOnlyList<BindingPackage>> GetPackages(UserConnection userConnection, string schemaName) {
			if (string.IsNullOrWhiteSpace(schemaName)) {
				return Result<IReadOnlyList<BindingPackage>>.Failure(SchemaNameRequiredMessage);
			}
			if (!_store.CanManageSolution(userConnection)) {
				return Result<IReadOnlyList<BindingPackage>>.Failure(NoPermissionMessage);
			}
			return _packageProvider.GetAvailablePackages(userConnection, schemaName);
		}

		public Result<int> RestoreColumnSetup(UserConnection userConnection, string profileKey) {
			if (string.IsNullOrWhiteSpace(profileKey)) {
				return Result<int>.Failure(ProfileKeyRequiredMessage);
			}
			return _columnSetupRepository.DeleteCurrentUserColumnSetup(userConnection, profileKey);
		}

		public Result<IReadOnlyList<BindingOutcome>> SaveBindings(UserConnection userConnection, Guid packageUId,
				IList<BindingDefinition> definitions) {
			if (packageUId == Guid.Empty) {
				return Result<IReadOnlyList<BindingOutcome>>.Failure(PackageRequiredMessage);
			}
			if (definitions == null || definitions.Count == 0) {
				return Result<IReadOnlyList<BindingOutcome>>.Failure(BindingsRequiredMessage);
			}
			string validationError = definitions.Select(Validate).FirstOrDefault(error => error != null);
			if (validationError != null) {
				return Result<IReadOnlyList<BindingOutcome>>.Failure(validationError);
			}
			if (!_store.CanManageSolution(userConnection)) {
				return Result<IReadOnlyList<BindingOutcome>>.Failure(NoPermissionMessage);
			}
			var outcomes = new List<BindingOutcome>();
			bool isStopped = false;
			foreach (BindingDefinition definition in definitions) {
				if (isStopped) {
					outcomes.Add(BindingOutcome.For(definition, BindingStatus.NotProcessed, NotProcessedMessage));
					continue;
				}
				BindingOutcome outcome = _store.Save(userConnection, packageUId, definition)
					?? BindingOutcome.For(definition, BindingStatus.Failed);
				if (outcome.Status == BindingStatus.Skipped && !definition.IsRelated) {
					outcome.Status = BindingStatus.Failed;
					outcome.Message = outcome.Message ?? NoDataFoundMessage;
				}
				outcomes.Add(outcome);
				isStopped = outcome.Status == BindingStatus.Failed;
			}
			return Result<IReadOnlyList<BindingOutcome>>.Success(outcomes);
		}

		#endregion

	}
}
