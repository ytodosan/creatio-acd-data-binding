namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using Common.Logging;
	using Terrasoft.Common;
	using Terrasoft.Core;
	using Terrasoft.Core.DB;
	using Terrasoft.Core.Entities;
	using Terrasoft.Core.Packages;

	/// <summary>
	/// Saves data bindings through the public <see cref="PackageElementUtilities"/> API. This is the part of the
	/// platform SchemaDataDesignerService.SaveSchema (SaveSchemaDataCommand) that configuration code can reach on
	/// both .NET Framework and .NET: save the binding info, its columns, and the serialized records.
	/// </summary>
	internal sealed class DataBindingStore : IDataBindingStore {

		#region Constants: Private

		private const string CanManageSolutionOperation = "CanManageSolution";

		#endregion

		#region Fields: Private

		private readonly ILog _log;

		#endregion

		#region Constructors: Public

		public DataBindingStore(ILog log) {
			_log = log;
		}

		#endregion

		#region Methods: Private

		private static Guid GetPackageIdByUId(UserConnection userConnection, Guid packageUId) {
			var select = (Select)new Select(userConnection)
					.Column("Id")
				.From("SysPackage")
				.Where("UId").IsEqual(Column.Parameter(packageUId));
			return select.ExecuteScalar<Guid>();
		}

		private static Dictionary<Guid, Guid> GetSchemaPackageIds(UserConnection userConnection,
				IEnumerable<Guid> schemaUIds) {
			var result = new Dictionary<Guid, Guid>();
			object[] uIds = schemaUIds.Where(uId => uId != Guid.Empty).Distinct().Cast<object>().ToArray();
			if (uIds.Length == 0) {
				return result;
			}
			var select = (Select)new Select(userConnection)
					.Column("UId")
					.Column("SysPackageId")
				.From("SysSchema")
				.Where("UId").In(Column.Parameters(uIds));
			using (DBExecutor executor = userConnection.EnsureDBConnection())
			using (IDataReader reader = select.ExecuteReader(executor)) {
				while (reader.Read()) {
					Guid uId = reader.GetGuid(0);
					if (!result.ContainsKey(uId)) {
						result[uId] = reader.GetGuid(1);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Keeps the columns the target package can see: columns created by schemas of packages in its hierarchy.
		/// Columns added by other packages would fail to install where those packages are absent.
		/// </summary>
		private static List<EntitySchemaColumn> GetBindableColumns(UserConnection userConnection, EntitySchema schema,
				Guid packageId, ICollection<string> excludedColumns) {
			var hierarchy = new HashSet<Guid>(
				WorkspaceUtilities.GetPackageHierarchyIdsById(userConnection, packageId, userConnection.Workspace.Id));
			List<EntitySchemaColumn> candidates = schema.Columns
				.Where(column => !BindingColumnRules.IsSkipped(column.Name))
				.ToList();
			Dictionary<Guid, Guid> columnPackages = GetSchemaPackageIds(userConnection,
				candidates.Select(column => column.CreatedInSchemaUId));
			var result = new List<EntitySchemaColumn>();
			foreach (EntitySchemaColumn column in candidates) {
				Guid columnPackageId;
				bool isKnown = columnPackages.TryGetValue(column.CreatedInSchemaUId, out columnPackageId);
				if (isKnown && !hierarchy.Contains(columnPackageId)) {
					excludedColumns.Add(column.Name);
					continue;
				}
				result.Add(column);
			}
			return result;
		}

		private static EntitySchemaQuery CreateQuery(EntitySchema schema, IEnumerable<EntitySchemaColumn> columns) {
			var esq = new EntitySchemaQuery(schema);
			esq.PrimaryQueryColumn.IsAlwaysSelect = true;
			foreach (EntitySchemaColumn column in columns) {
				bool isAdded = esq.Columns.Any(esqColumn => esqColumn.Name == column.Name);
				if (!isAdded && !(column.DataValueType is SecureTextDataValueType)) {
					esq.AddColumn(column.Name);
				}
			}
			return esq;
		}

		private static EntitySchemaQuery CreateRecordsQuery(EntitySchema schema, IEnumerable<EntitySchemaColumn> columns,
				IEnumerable<Guid> recordIds) {
			EntitySchemaQuery esq = CreateQuery(schema, columns);
			object[] ids = recordIds.Cast<object>().ToArray();
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, schema.PrimaryColumn.Name, ids));
			return esq;
		}

		private static string ApplyRecordsSelection(EntitySchemaQuery esq, EntitySchema schema,
				BindingDefinition definition) {
			List<Guid> recordIds = (definition.RecordIds ?? new List<Guid>()).Where(id => id != Guid.Empty)
				.Distinct().ToList();
			if (recordIds.Count > 0) {
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, schema.PrimaryColumn.Name,
					recordIds.Cast<object>().ToArray()));
			}
			if (!string.IsNullOrWhiteSpace(definition.FilterColumnName)) {
				EntitySchemaColumn filterColumn = schema.Columns.FindByName(definition.FilterColumnName);
				if (filterColumn == null) {
					return $"Column \"{definition.FilterColumnName}\" is not found in \"{schema.Name}\".";
				}
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, filterColumn.Name,
					definition.FilterValue));
			}
			return null;
		}

		private static PackageSchemaDataDescriptor CreateDescriptor(BindingDefinition definition, EntitySchema schema,
				Guid packageUId, Guid bindingUId, IEnumerable<EntitySchemaColumn> columns, BindingColumnRule rule) {
			var descriptor = new PackageSchemaDataDescriptor {
				UId = bindingUId,
				Name = definition.Name,
				InstallType = SysPackageSchemaDataInstallType.Install,
				Schema = new SchemaDescriptor {
					UId = schema.UId
				},
				Package = new Package {
					UId = packageUId
				}
			};
			foreach (EntitySchemaColumn column in columns) {
				descriptor.AddColumn(new PackageSchemaDataColumnDescriptor {
					ColumnUId = column.UId,
					ColumnName = column.Name,
					IsKey = rule.KeyColumns.Contains(column.Name),
					IsForceUpdate = rule.ForceUpdateColumns.Contains(column.Name),
					DataValueTypeUId = column.DataValueTypeUId
				});
			}
			return descriptor;
		}

		private BindingOutcome SaveInternal(UserConnection userConnection, Guid packageUId,
				BindingDefinition definition) {
			EntitySchema schema = userConnection.EntitySchemaManager.FindInstanceByName(definition.SchemaName);
			if (schema == null) {
				return BindingOutcome.For(definition, BindingStatus.Failed,
					$"Entity schema \"{definition.SchemaName}\" is not found.");
			}
			Guid packageId = GetPackageIdByUId(userConnection, packageUId);
			if (packageId == Guid.Empty) {
				return BindingOutcome.For(definition, BindingStatus.Failed, $"Package \"{packageUId}\" is not found.");
			}
			var excludedColumns = new List<string>();
			List<EntitySchemaColumn> columns = GetBindableColumns(userConnection, schema, packageId, excludedColumns);
			EntitySchemaQuery esq = CreateQuery(schema, columns);
			string selectionError = ApplyRecordsSelection(esq, schema, definition);
			if (selectionError != null) {
				return BindingOutcome.For(definition, BindingStatus.Failed, selectionError);
			}
			EntityCollection entities = esq.GetEntityCollection(userConnection);
			Guid bindingUId = definition.UId == Guid.Empty ? schema.UId : definition.UId;
			var outcome = BindingOutcome.For(definition, BindingStatus.Skipped);
			outcome.UId = bindingUId;
			outcome.RecordsCount = entities.Count;
			if (entities.Count == 0) {
				return outcome;
			}
			foreach (string columnName in excludedColumns) {
				outcome.Warnings.Add($"Column \"{columnName}\" was not bound: it belongs to a package " +
					"outside the hierarchy of the selected package.");
			}
			Guid[] recordIds = entities.Select(entity => entity.PrimaryColumnValue).ToArray();
			BindingColumnRule rule = BindingColumnRules.GetRule(schema.Name);
			PackageSchemaDataDescriptor descriptor = CreateDescriptor(definition, schema, packageUId, bindingUId,
				columns, rule);
			var utilities = new PackageElementUtilities(userConnection);
			var localization = new DataLocalizationAdapter(utilities);
			Guid[] existingLocalizedRecordIds = localization.GetExistingRecordIds(descriptor);
			Guid schemaDataId = utilities.SavePackageSchemaDataInfo(descriptor);
			if (schemaDataId == Guid.Empty) {
				outcome.Status = BindingStatus.Failed;
				outcome.Message = $"Data binding \"{definition.Name}\" was not saved: it is locked, or another data " +
					"binding in this package already uses this name.";
				return outcome;
			}
			utilities.SavePackageSchemaDataColumns(schemaDataId,
				descriptor.Columns.Select(column => new PackageElementUtilities.SchemaDataColumn(column.ColumnUId,
					column.IsForceUpdate, column.IsKey)),
				schema);
			utilities.LoadData(schemaDataId, CreateRecordsQuery(schema, columns, recordIds),
				descriptor.Columns.Select(column => column.ColumnUId).ToArray(), string.Empty);
			localization.SaveLocalizableValues(recordIds, descriptor, existingLocalizedRecordIds);
			outcome.Status = BindingStatus.Saved;
			return outcome;
		}

		#endregion

		#region Methods: Public

		public bool CanManageSolution(UserConnection userConnection) {
			return userConnection.DBSecurityEngine.GetCanExecuteOperation(CanManageSolutionOperation);
		}

		public BindingOutcome Save(UserConnection userConnection, Guid packageUId, BindingDefinition definition) {
			try {
				return SaveInternal(userConnection, packageUId, definition);
			} catch (Exception ex) {
				// Platform API failures (security, foreign package, DB errors) become a failed outcome.
				_log.Error($"Data binding \"{definition.Name}\" was not saved", ex);
				return BindingOutcome.For(definition, BindingStatus.Failed, ex.Message);
			}
		}

		#endregion

	}
}
