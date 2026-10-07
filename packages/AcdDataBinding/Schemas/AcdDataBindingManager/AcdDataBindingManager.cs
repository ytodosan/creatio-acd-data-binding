namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
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

		internal const int MaxBindingsPerRequest = 10;

		internal const int MaxRecordIdsPerBinding = 10000;

		internal const int MaxBindingNameLength = 250;

		internal static readonly string TooManyBindingsMessage =
			$"A request can contain at most {MaxBindingsPerRequest} data bindings.";

		internal static readonly string TooManyRecordsMessage =
			$"A data binding can list at most {MaxRecordIdsPerBinding} record ids. Use all records instead.";

		internal static readonly string InvalidBindingNameMessage =
			$"Data binding name can contain only Latin letters, digits and underscores, up to {MaxBindingNameLength} characters.";

		internal const string InvalidSchemaNameMessage = "Entity schema name is not a valid schema name.";

		internal const string InvalidFilterColumnMessage = "Filter column name is not a valid column name.";

		#endregion

		#region Fields: Private

		private static readonly Regex IdentifierRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]{0,127}\z", RegexOptions.Compiled);

		private static readonly Regex BindingNameRegex = new Regex(@"^[A-Za-z0-9_]+\z", RegexOptions.Compiled);

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
			if (!IdentifierRegex.IsMatch(definition.SchemaName)) {
				return InvalidSchemaNameMessage;
			}
			if (string.IsNullOrWhiteSpace(definition.Name)) {
				return BindingNameRequiredMessage;
			}
			if (definition.Name.Length > MaxBindingNameLength || !BindingNameRegex.IsMatch(definition.Name)) {
				return InvalidBindingNameMessage;
			}
			if (definition.RecordIds != null && definition.RecordIds.Count > MaxRecordIdsPerBinding) {
				return TooManyRecordsMessage;
			}
			bool hasRecordIds = definition.RecordIds != null && definition.RecordIds.Any(id => id != Guid.Empty);
			bool hasFilter = !string.IsNullOrWhiteSpace(definition.FilterColumnName);
			if (hasFilter && !IdentifierRegex.IsMatch(definition.FilterColumnName)) {
				return InvalidFilterColumnMessage;
			}
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
			if (!IdentifierRegex.IsMatch(schemaName)) {
				return Result<IReadOnlyList<BindingPackage>>.Failure(InvalidSchemaNameMessage);
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
			if (definitions.Count > MaxBindingsPerRequest) {
				return Result<IReadOnlyList<BindingOutcome>>.Failure(TooManyBindingsMessage);
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
