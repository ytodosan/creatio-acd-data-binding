namespace AcdDataBinding.Tests.DataBinding {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AcdDataBinding.DataBinding;
	using AcdDataBinding.Shared;
	using FluentAssertions;
	using NSubstitute;
	using NUnit.Framework;
	using Terrasoft.Core;

	[TestFixture(Category = "UnitTests")]
	public class DataBindingManagerTests {

		private static readonly Guid PackageUId = new Guid("2b598af8-93f5-4daf-8e78-83c5c8a88cae");

		private IBindingPackageProvider _packageProvider;
		private IColumnSetupRepository _columnSetupRepository;
		private IDataBindingStore _store;
		private DataBindingManager _manager;

		private static BindingDefinition Binding(string schemaName, bool isRelated = false) {
			return new BindingDefinition {
				SchemaName = schemaName,
				Name = schemaName + "_Code",
				UId = Guid.NewGuid(),
				RecordIds = new List<Guid> { Guid.NewGuid() },
				IsRelated = isRelated
			};
		}

		private void StoreReturns(BindingStatus status) {
			_store.Save(Arg.Any<UserConnection>(), Arg.Any<Guid>(), Arg.Any<BindingDefinition>())
				.Returns(call => BindingOutcome.For(call.ArgAt<BindingDefinition>(2), status));
		}

		[SetUp]
		public void SetUp() {
			_packageProvider = Substitute.For<IBindingPackageProvider>();
			_columnSetupRepository = Substitute.For<IColumnSetupRepository>();
			_store = Substitute.For<IDataBindingStore>();
			_store.CanManageSolution(Arg.Any<UserConnection>()).Returns(true);
			_manager = new DataBindingManager(_packageProvider, _columnSetupRepository, _store);
		}

		[Test]
		[Description("SaveBindings saves every binding in request order and succeeds when all are saved")]
		public void SaveBindings_AllSaved_ReturnsOutcomesInOrder() {
			// Arrange
			StoreReturns(BindingStatus.Saved);
			var bindings = new List<BindingDefinition> { Binding("SysSettings"), Binding("SysSettingsValue", true) };

			// Act
			Result<IReadOnlyList<BindingOutcome>> result = _manager.SaveBindings(null, PackageUId, bindings);

			// Assert
			result.IsSuccess.Should().BeTrue(because: "a valid request with permission is processed");
			result.Value.Select(outcome => outcome.SchemaName).Should()
				.Equal(new[] { "SysSettings", "SysSettingsValue" }, because: "bindings are saved in request order");
			result.Value.Should().OnlyContain(outcome => outcome.Status == BindingStatus.Saved,
				because: "the store saved every binding");
			_store.Received(2).Save(null, PackageUId, Arg.Any<BindingDefinition>());
		}

		[Test]
		[Description("A failed binding stops the batch and later bindings are reported as not processed")]
		public void SaveBindings_FirstFails_StopsAndMarksRestNotProcessed() {
			// Arrange
			StoreReturns(BindingStatus.Failed);
			var bindings = new List<BindingDefinition> { Binding("SysWorkplace"), Binding("SysModuleInWorkplace", true) };

			// Act
			Result<IReadOnlyList<BindingOutcome>> result = _manager.SaveBindings(null, PackageUId, bindings);

			// Assert
			result.Value[0].Status.Should().Be(BindingStatus.Failed, because: "the store failed the first binding");
			result.Value[1].Status.Should().Be(BindingStatus.NotProcessed,
				because: "the original tool stops after a failed binding");
			_store.Received(1).Save(Arg.Any<UserConnection>(), Arg.Any<Guid>(), Arg.Any<BindingDefinition>());
		}

		[Test]
		[Description("A related binding without records is skipped and the batch goes on")]
		public void SaveBindings_RelatedWithoutRecords_IsSkipped() {
			// Arrange
			StoreReturns(BindingStatus.Skipped);
			var bindings = new List<BindingDefinition> {
				Binding("SysSettingsValue", true), Binding("SysAdminOperationGrantee", true)
			};

			// Act
			Result<IReadOnlyList<BindingOutcome>> result = _manager.SaveBindings(null, PackageUId, bindings);

			// Assert
			result.Value.Should().OnlyContain(outcome => outcome.Status == BindingStatus.Skipped,
				because: "related data is optional");
			_store.Received(2).Save(Arg.Any<UserConnection>(), Arg.Any<Guid>(), Arg.Any<BindingDefinition>());
		}

		[Test]
		[Description("A main binding without records fails with the no-data message")]
		public void SaveBindings_MainWithoutRecords_Fails() {
			// Arrange
			StoreReturns(BindingStatus.Skipped);
			var bindings = new List<BindingDefinition> { Binding("Lookup") };

			// Act
			Result<IReadOnlyList<BindingOutcome>> result = _manager.SaveBindings(null, PackageUId, bindings);

			// Assert
			result.Value[0].Status.Should().Be(BindingStatus.Failed, because: "the main binding must bind records");
			result.Value[0].Message.Should().Be(DataBindingManager.NoDataFoundMessage,
				because: "the user needs to know nothing was found");
		}

		[Test]
		[Description("SaveBindings returns an error and saves nothing without the CanManageSolution operation")]
		public void SaveBindings_NoPermission_ReturnsError() {
			// Arrange
			_store.CanManageSolution(Arg.Any<UserConnection>()).Returns(false);

			// Act
			Result<IReadOnlyList<BindingOutcome>> result =
				_manager.SaveBindings(null, PackageUId, new List<BindingDefinition> { Binding("SysSettings") });

			// Assert
			result.IsError.Should().BeTrue(because: "binding data changes packages");
			result.Error.Should().Be(DataBindingManager.NoPermissionMessage, because: "the reason is shown to the user");
			_store.DidNotReceive().Save(Arg.Any<UserConnection>(), Arg.Any<Guid>(), Arg.Any<BindingDefinition>());
		}

		[Test]
		[Description("SaveBindings rejects a request without a package")]
		public void SaveBindings_EmptyPackage_ReturnsError() {
			// Arrange
			var bindings = new List<BindingDefinition> { Binding("SysSettings") };

			// Act
			Result<IReadOnlyList<BindingOutcome>> result = _manager.SaveBindings(null, Guid.Empty, bindings);

			// Assert
			result.Error.Should().Be(DataBindingManager.PackageRequiredMessage, because: "a binding belongs to a package");
		}

		[Test]
		[Description("SaveBindings rejects a request without bindings")]
		public void SaveBindings_NoBindings_ReturnsError() {
			// Act
			Result<IReadOnlyList<BindingOutcome>> result =
				_manager.SaveBindings(null, PackageUId, new List<BindingDefinition>());

			// Assert
			result.Error.Should().Be(DataBindingManager.BindingsRequiredMessage, because: "there is nothing to save");
		}

		[Test]
		[Description("SaveBindings rejects a binding without a name")]
		public void SaveBindings_BindingWithoutName_ReturnsError() {
			// Arrange
			BindingDefinition binding = Binding("SysSettings");
			binding.Name = " ";

			// Act
			Result<IReadOnlyList<BindingOutcome>> result =
				_manager.SaveBindings(null, PackageUId, new List<BindingDefinition> { binding });

			// Assert
			result.Error.Should().Be(DataBindingManager.BindingNameRequiredMessage,
				because: "the data binding name is required by the platform");
			_store.DidNotReceive().Save(Arg.Any<UserConnection>(), Arg.Any<Guid>(), Arg.Any<BindingDefinition>());
		}

		[Test]
		[Description("SaveBindings rejects a binding that doesn't say which records to bind")]
		public void SaveBindings_BindingWithoutRecordSelection_ReturnsError() {
			// Arrange
			BindingDefinition binding = Binding("SysSettings");
			binding.RecordIds = new List<Guid> { Guid.Empty };

			// Act
			Result<IReadOnlyList<BindingOutcome>> result =
				_manager.SaveBindings(null, PackageUId, new List<BindingDefinition> { binding });

			// Assert
			result.Error.Should().Be(DataBindingManager.RecordsRequiredMessage,
				because: "record ids, a filter or all records must be given");
		}

		[Test]
		[Description("A filter selection without record ids is a valid request")]
		public void SaveBindings_FilterSelection_IsAccepted() {
			// Arrange
			StoreReturns(BindingStatus.Saved);
			var binding = new BindingDefinition {
				SchemaName = "SysSettingsValue",
				Name = "SysSettingsValue_Code",
				FilterColumnName = "SysSettings",
				FilterValue = Guid.NewGuid()
			};

			// Act
			Result<IReadOnlyList<BindingOutcome>> result =
				_manager.SaveBindings(null, PackageUId, new List<BindingDefinition> { binding });

			// Assert
			result.IsSuccess.Should().BeTrue(because: "the related records are selected by the filter");
		}

		[Test]
		[Description("GetPackages checks the permission before listing packages")]
		public void GetPackages_NoPermission_ReturnsError() {
			// Arrange
			_store.CanManageSolution(Arg.Any<UserConnection>()).Returns(false);

			// Act
			Result<IReadOnlyList<BindingPackage>> result = _manager.GetPackages(null, "SysSettings");

			// Assert
			result.Error.Should().Be(DataBindingManager.NoPermissionMessage, because: "binding data requires the operation");
			_packageProvider.DidNotReceive().GetAvailablePackages(Arg.Any<UserConnection>(), Arg.Any<string>());
		}

		[Test]
		[Description("GetPackages passes the schema name to the package provider")]
		public void GetPackages_ValidRequest_DelegatesToProvider() {
			// Arrange
			var packages = new List<BindingPackage> { new BindingPackage { Name = "UsrYacht" } };
			_packageProvider.GetAvailablePackages(null, "SysSettings")
				.Returns(Result<IReadOnlyList<BindingPackage>>.Success(packages));

			// Act
			Result<IReadOnlyList<BindingPackage>> result = _manager.GetPackages(null, "SysSettings");

			// Assert
			result.Value.Should().BeSameAs(packages, because: "the provider result is returned as is");
		}

		[Test]
		[Description("GetPackages rejects an empty schema name")]
		public void GetPackages_EmptySchemaName_ReturnsError() {
			// Act
			Result<IReadOnlyList<BindingPackage>> result = _manager.GetPackages(null, "");

			// Assert
			result.Error.Should().Be(DataBindingManager.SchemaNameRequiredMessage, because: "the schema is required");
		}

		[Test]
		[Description("RestoreColumnSetup rejects an empty profile key and doesn't touch the repository")]
		public void RestoreColumnSetup_EmptyKey_ReturnsError() {
			// Act
			Result<int> result = _manager.RestoreColumnSetup(null, null);

			// Assert
			result.Error.Should().Be(DataBindingManager.ProfileKeyRequiredMessage, because: "the key selects the setup");
			_columnSetupRepository.DidNotReceive()
				.DeleteCurrentUserColumnSetup(Arg.Any<UserConnection>(), Arg.Any<string>());
		}

		[Test]
		[Description("RestoreColumnSetup deletes the current user's setup for the key")]
		public void RestoreColumnSetup_ValidKey_DelegatesToRepository() {
			// Arrange
			_columnSetupRepository.DeleteCurrentUserColumnSetup(null, "SysSettingsSectionGridDataView")
				.Returns(Result<int>.Success(1));

			// Act
			Result<int> result = _manager.RestoreColumnSetup(null, "SysSettingsSectionGridDataView");

			// Assert
			result.Value.Should().Be(1, because: "the repository deleted one profile row");
		}

	}
}
