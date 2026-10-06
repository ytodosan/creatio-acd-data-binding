namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AcdDataBinding.Shared;
	using Terrasoft.Core;
	using Terrasoft.Core.Configuration;
	using Terrasoft.Core.DB;
	using Terrasoft.Core.Entities;
	using Terrasoft.Core.Packages;

	/// <summary>
	/// Returns packages of the current maintainer (the "Maintainer" system setting) that are editable
	/// (InstallType = 0) and have the package of the entity schema in their hierarchy. The data binding tool
	/// package itself is never a target. Ported from GlbDataBindingHelper.GetPackages.
	/// </summary>
	internal sealed class BindingPackageProvider : IBindingPackageProvider {

		#region Fields: Private

		private static readonly Guid ToolPackageUId = new Guid("2b598af8-93f5-4daf-8e78-83c5c8a88cae");

		#endregion

		#region Methods: Private

		private static Guid GetSchemaRootPackageId(UserConnection userConnection, EntitySchema schema) {
			var select = (Select)new Select(userConnection)
					.Column("SysPackageId")
				.From("SysSchema")
				.Where("UId").IsEqual(Column.Parameter(schema.UId));
			return select.ExecuteScalar<Guid>();
		}

		#endregion

		#region Methods: Public

		public Result<IReadOnlyList<BindingPackage>> GetAvailablePackages(UserConnection userConnection,
				string schemaName) {
			EntitySchema schema = userConnection.EntitySchemaManager.FindInstanceByName(schemaName);
			if (schema == null) {
				return Result<IReadOnlyList<BindingPackage>>.Failure($"Entity schema \"{schemaName}\" is not found.");
			}
			Guid rootPackageId = GetSchemaRootPackageId(userConnection, schema);
			if (rootPackageId == Guid.Empty) {
				return Result<IReadOnlyList<BindingPackage>>.Failure(
					$"Package of the \"{schemaName}\" entity schema is not found.");
			}
			string maintainer = SysSettings.GetValue(userConnection, "Maintainer", string.Empty);
			var esq = new EntitySchemaQuery(userConnection.EntitySchemaManager, "SysPackage");
			esq.PrimaryQueryColumn.IsAlwaysSelect = true;
			string nameColumn = esq.AddColumn("Name").Name;
			string uIdColumn = esq.AddColumn("UId").Name;
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Maintainer", maintainer));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "InstallType", 0));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "UId", ToolPackageUId));
			var packages = new List<BindingPackage>();
			foreach (Entity package in esq.GetEntityCollection(userConnection)) {
				IEnumerable<Guid> hierarchy = WorkspaceUtilities.GetPackageHierarchyIdsById(userConnection,
					package.PrimaryColumnValue, userConnection.Workspace.Id);
				if (hierarchy.Contains(rootPackageId)) {
					packages.Add(new BindingPackage {
						Id = package.PrimaryColumnValue,
						UId = package.GetTypedColumnValue<Guid>(uIdColumn),
						Name = package.GetTypedColumnValue<string>(nameColumn)
					});
				}
			}
			IReadOnlyList<BindingPackage> result = packages
				.OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
			return Result<IReadOnlyList<BindingPackage>>.Success(result);
		}

		#endregion

	}
}
