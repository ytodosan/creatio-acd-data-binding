namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;
	using AcdDataBinding.Shared;
	using Terrasoft.Core;

	/// <summary>
	/// Finds user packages that can hold a data binding of an entity schema.
	/// </summary>
	public interface IBindingPackageProvider {

		Result<IReadOnlyList<BindingPackage>> GetAvailablePackages(UserConnection userConnection, string schemaName);

	}

	/// <summary>
	/// Removes the current user's column setup so the default ("for all users") setup applies again.
	/// </summary>
	public interface IColumnSetupRepository {

		Result<int> DeleteCurrentUserColumnSetup(UserConnection userConnection, string profileKey);

	}

	/// <summary>
	/// Saves one data binding (package schema data) with its columns and bound records.
	/// </summary>
	public interface IDataBindingStore {

		bool CanManageSolution(UserConnection userConnection);

		BindingOutcome Save(UserConnection userConnection, Guid packageUId, BindingDefinition definition);

	}

	/// <summary>
	/// Application service behind the data binding web service.
	/// </summary>
	public interface IDataBindingManager {

		Result<IReadOnlyList<BindingPackage>> GetPackages(UserConnection userConnection, string schemaName);

		Result<int> RestoreColumnSetup(UserConnection userConnection, string profileKey);

		Result<IReadOnlyList<BindingOutcome>> SaveBindings(UserConnection userConnection, Guid packageUId,
			IList<BindingDefinition> definitions);

	}
}
