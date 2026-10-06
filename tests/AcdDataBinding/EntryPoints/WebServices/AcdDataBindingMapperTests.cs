namespace AcdDataBinding.Tests.EntryPoints.WebServices {
	using System;
	using System.Collections.Generic;
	using AcdDataBinding.DataBinding;
	using AcdDataBinding.EntryPoints.WebServices;
	using AcdDataBinding.Shared;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture(Category = "UnitTests")]
	public class AcdDataBindingMapperTests {

		[Test]
		[Description("Binding DTOs map to definitions with every field")]
		public void ToDefinitions_MapsAllFields() {
			// Arrange
			var uId = Guid.NewGuid();
			var filterValue = Guid.NewGuid();
			var recordId = Guid.NewGuid();
			var dto = new AcdBindingDto {
				SchemaName = "SysSettingsValue",
				Name = "SysSettingsValue_Code",
				UId = uId,
				RecordIds = new List<Guid> { recordId },
				AllRecords = true,
				FilterColumnName = "SysSettings",
				FilterValue = filterValue,
				IsRelated = true
			};

			// Act
			IList<BindingDefinition> definitions = AcdDataBindingMapper.ToDefinitions(new[] { dto });

			// Assert
			definitions.Should().ContainSingle(because: "one DTO was given");
			definitions[0].Should().BeEquivalentTo(new BindingDefinition {
				SchemaName = "SysSettingsValue",
				Name = "SysSettingsValue_Code",
				UId = uId,
				RecordIds = new List<Guid> { recordId },
				AllRecords = true,
				FilterColumnName = "SysSettings",
				FilterValue = filterValue,
				IsRelated = true
			}, because: "the web service passes the request through unchanged");
		}

		[Test]
		[Description("A missing bindings list maps to an empty list")]
		public void ToDefinitions_Null_ReturnsEmpty() {
			// Act
			IList<BindingDefinition> definitions = AcdDataBindingMapper.ToDefinitions(null);

			// Assert
			definitions.Should().BeEmpty(because: "the manager reports the empty request");
		}

		[Test]
		[Description("A batch with a failed binding is unsuccessful and carries the failure message")]
		public void ToResponse_WithFailedOutcome_IsUnsuccessful() {
			// Arrange
			var outcomes = new List<BindingOutcome> {
				new BindingOutcome { Name = "SysSettings_Code", Status = BindingStatus.Saved, RecordsCount = 1 },
				new BindingOutcome { Name = "SysSettingsValue_Code", Status = BindingStatus.Failed, Message = "Locked" },
				new BindingOutcome { Name = "Other", Status = BindingStatus.NotProcessed }
			};

			// Act
			AcdSaveBindingsResponse response =
				AcdDataBindingMapper.ToResponse(Result<IReadOnlyList<BindingOutcome>>.Success(outcomes));

			// Assert
			response.Success.Should().BeFalse(because: "one binding failed");
			response.Message.Should().Be("Locked", because: "the first failure explains the result");
			response.Items.Should().HaveCount(3, because: "every binding is reported");
			response.Items[0].Status.Should().Be("saved", because: "statuses are serialized in camel case");
			response.Items[2].Status.Should().Be("notProcessed", because: "statuses are serialized in camel case");
		}

		[Test]
		[Description("A batch with saved and skipped bindings is successful")]
		public void ToResponse_SavedAndSkipped_IsSuccessful() {
			// Arrange
			var outcomes = new List<BindingOutcome> {
				new BindingOutcome { Status = BindingStatus.Saved, Warnings = new List<string> { "Column skipped" } },
				new BindingOutcome { Status = BindingStatus.Skipped }
			};

			// Act
			AcdSaveBindingsResponse response =
				AcdDataBindingMapper.ToResponse(Result<IReadOnlyList<BindingOutcome>>.Success(outcomes));

			// Assert
			response.Success.Should().BeTrue(because: "skipped related data is not an error");
			response.Items[0].Warnings.Should().Equal(new[] { "Column skipped" }, because: "warnings reach the client");
			response.Items[1].Status.Should().Be("skipped", because: "statuses are serialized in camel case");
		}

		[Test]
		[Description("A request error maps to an unsuccessful response with the message")]
		public void ToResponse_Error_ReturnsMessage() {
			// Act
			AcdSaveBindingsResponse response =
				AcdDataBindingMapper.ToResponse(Result<IReadOnlyList<BindingOutcome>>.Failure("Package is required."));

			// Assert
			response.Success.Should().BeFalse(because: "the request was rejected");
			response.Message.Should().Be("Package is required.", because: "the client shows the message");
			response.Items.Should().BeEmpty(because: "nothing was processed");
		}

		[Test]
		[Description("Packages map to the lookup shape the classic UI list expects")]
		public void ToResponse_Packages_MapsLookupFields() {
			// Arrange
			var package = new BindingPackage { Id = Guid.NewGuid(), UId = Guid.NewGuid(), Name = "UsrYacht" };

			// Act
			AcdGetPackagesResponse response = AcdDataBindingMapper.ToResponse(
				Result<IReadOnlyList<BindingPackage>>.Success(new List<BindingPackage> { package }));

			// Assert
			response.Success.Should().BeTrue(because: "the packages were found");
			response.Packages.Should().ContainSingle(because: "one package was found");
			response.Packages[0].Id.Should().Be(package.Id, because: "Id is the lookup value");
			response.Packages[0].UId.Should().Be(package.UId, because: "UId is sent back when saving");
			response.Packages[0].Name.Should().Be("UsrYacht", because: "Name is the display value");
		}

		[Test]
		[Description("Restored column setup maps the number of deleted rows")]
		public void ToResponse_RestoreColumnSetup_MapsRowsAffected() {
			// Act
			AcdRestoreColumnSetupResponse response = AcdDataBindingMapper.ToResponse(Result<int>.Success(2));

			// Assert
			response.Success.Should().BeTrue(because: "the delete succeeded");
			response.RowsAffected.Should().Be(2, because: "the client reloads the grid only when rows were deleted");
		}

	}
}
