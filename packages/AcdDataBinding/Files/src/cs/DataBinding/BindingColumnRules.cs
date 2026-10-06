namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Key and force-update column rules of a data binding.
	/// </summary>
	public sealed class BindingColumnRule {

		#region Constructors: Public

		public BindingColumnRule(IEnumerable<string> keyColumns, IEnumerable<string> forceUpdateColumns) {
			KeyColumns = new HashSet<string>(keyColumns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
			ForceUpdateColumns = new HashSet<string>(forceUpdateColumns ?? Enumerable.Empty<string>(),
				StringComparer.OrdinalIgnoreCase);
		}

		#endregion

		#region Properties: Public

		public ISet<string> KeyColumns { get; }

		public ISet<string> ForceUpdateColumns { get; }

		#endregion

	}

	/// <summary>
	/// Column rules for the schemas the classic UI binds. Ported from GlbDataBindingMixin.getDataBindingColumnsConfig.
	/// </summary>
	public static class BindingColumnRules {

		#region Fields: Private

		private static readonly string[] DefaultKeyColumns = { "Id" };

		private static readonly Dictionary<string, BindingColumnRule> Rules =
			new Dictionary<string, BindingColumnRule>(StringComparer.OrdinalIgnoreCase) {
				["Lookup"] = new BindingColumnRule(DefaultKeyColumns,
					new[] { "Name", "Description", "SysEntitySchemaUId", "SysPageSchemaUId" }),
				["SysProfileData"] = new BindingColumnRule(new[] { "Key", "Contact", "SysCulture" },
					new[] { "ObjectData" }),
				["SysSettings"] = new BindingColumnRule(DefaultKeyColumns,
					new[] { "Name", "Position", "IsPersonal", "IsCacheable", "ReferenceSchemaUId", "IsSSPAvailable",
						"ValueTypeName" }),
				["SysSettingsValue"] = new BindingColumnRule(new[] { "SysSettings", "SysAdminUnit" },
					new[] { "IsDef", "Position", "TextValue", "IntegerValue", "FloatValue", "BooleanValue",
						"DateTimeValue", "GuidValue", "BinaryValue" }),
				["SysAdminOperation"] = new BindingColumnRule(DefaultKeyColumns, new[] { "Name", "Description" }),
				["SysAdminOperationGrantee"] = new BindingColumnRule(new[] { "SysAdminOperation", "SysAdminUnit" },
					new[] { "CanExecute", "Position" }),
				["SysWorkplace"] = new BindingColumnRule(DefaultKeyColumns, new[] { "Name", "Position", "HomePageUId" }),
				["SysModuleInWorkplace"] = new BindingColumnRule(new[] { "SysWorkplace", "SysModule" },
					new[] { "Position" }),
				["SysAdminUnitInWorkplace"] = new BindingColumnRule(new[] { "SysWorkplace", "SysAdminUnit" }, null)
			};

		#endregion

		#region Properties: Public

		/// <summary>
		/// Columns that are never bound: they reference users and runtime state of the source environment.
		/// </summary>
		public static IReadOnlyCollection<string> SkippedColumns { get; } =
			new[] { "CreatedBy", "ModifiedBy", "ProcessListeners" };

		#endregion

		#region Methods: Public

		/// <summary>
		/// Returns the rule for the schema, or the default rule (key is Id, nothing is force-updated).
		/// </summary>
		public static BindingColumnRule GetRule(string schemaName) {
			BindingColumnRule rule;
			return !string.IsNullOrEmpty(schemaName) && Rules.TryGetValue(schemaName, out rule)
				? rule
				: new BindingColumnRule(DefaultKeyColumns, null);
		}

		public static bool IsSkipped(string columnName) {
			return SkippedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase);
		}

		#endregion

	}
}
