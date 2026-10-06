namespace AcdDataBinding.Tests.DataBinding {
	using AcdDataBinding.DataBinding;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture(Category = "UnitTests")]
	public class BindingColumnRulesTests {

		[Test]
		[Description("System setting values are keyed by setting and admin unit and force-update their values")]
		public void GetRule_SysSettingsValue_ReturnsCompositeKey() {
			// Act
			BindingColumnRule rule = BindingColumnRules.GetRule("SysSettingsValue");

			// Assert
			rule.KeyColumns.Should().BeEquivalentTo(new[] { "SysSettings", "SysAdminUnit" },
				because: "a value is unique per setting and admin unit");
			rule.ForceUpdateColumns.Should().Contain(new[] { "TextValue", "GuidValue", "IsDef" },
				because: "installing the package must overwrite the values");
		}

		[Test]
		[Description("Column setup is keyed by profile key, contact and culture")]
		public void GetRule_SysProfileData_ReturnsProfileKey() {
			// Act
			BindingColumnRule rule = BindingColumnRules.GetRule("SysProfileData");

			// Assert
			rule.KeyColumns.Should().BeEquivalentTo(new[] { "Key", "Contact", "SysCulture" },
				because: "profile rows have no stable Id across environments");
			rule.ForceUpdateColumns.Should().BeEquivalentTo(new[] { "ObjectData" },
				because: "the column setup itself must be overwritten");
		}

		[Test]
		[Description("Schema names are matched case-insensitively")]
		public void GetRule_DifferentCase_ReturnsSameRule() {
			// Act
			BindingColumnRule rule = BindingColumnRules.GetRule("syssettings");

			// Assert
			rule.ForceUpdateColumns.Should().Contain("ValueTypeName", because: "schema names are case-insensitive");
		}

		[Test]
		[Description("An unknown schema (a regular lookup) uses Id as key and force-updates Name")]
		public void GetRule_UnknownSchema_ReturnsDefault() {
			// Act
			BindingColumnRule rule = BindingColumnRules.GetRule("OtpYachtStatus");

			// Assert
			rule.KeyColumns.Should().BeEquivalentTo(new[] { "Id" }, because: "lookup values are matched by Id");
			rule.ForceUpdateColumns.Should().BeEquivalentTo(new[] { "Name" },
				because: "a renamed lookup value must reach every environment where the package is installed");
		}

		[Test]
		[Description("Roles use the default rule, so their Name is force-updated as well")]
		public void GetRule_SysAdminUnit_ForcesName() {
			// Act
			BindingColumnRule rule = BindingColumnRules.GetRule("SysAdminUnit");

			// Assert
			rule.ForceUpdateColumns.Should().Contain("name",
				because: "column names are matched case-insensitively and roles have no dedicated rule");
		}

		[Test]
		[Description("A dedicated rule without Name (column setup) doesn't get the default Name forced update")]
		public void GetRule_SysProfileData_DoesNotForceName() {
			// Act
			BindingColumnRule rule = BindingColumnRules.GetRule("SysProfileData");

			// Assert
			rule.ForceUpdateColumns.Should().NotContain("Name", because: "dedicated rules are kept as they are");
		}

		[TestCase("CreatedBy")]
		[TestCase("modifiedby")]
		[TestCase("ProcessListeners")]
		[Description("User and runtime columns are never bound")]
		public void IsSkipped_SystemColumns_ReturnsTrue(string columnName) {
			// Act
			bool isSkipped = BindingColumnRules.IsSkipped(columnName);

			// Assert
			isSkipped.Should().BeTrue(because: "these columns reference users and state of the source environment");
		}

		[Test]
		[Description("Regular columns are bound")]
		public void IsSkipped_RegularColumn_ReturnsFalse() {
			// Act
			bool isSkipped = BindingColumnRules.IsSkipped("Name");

			// Assert
			isSkipped.Should().BeFalse(because: "only the listed system columns are skipped");
		}

	}
}
