namespace AcdDataBinding.DataBinding {
	using System;
	using System.Reflection;
	using Creatio.FeatureToggling;
	using Terrasoft.Core.Packages;

	/// <summary>
	/// Saves data localizations with the binding, as the platform does when the IncludeDataLocalizationInPackage
	/// feature is on. The methods exist only on newer platform builds, so they are resolved at runtime: the package
	/// compiles against older SDKs and does nothing where the platform doesn't support it.
	/// </summary>
	internal sealed class DataLocalizationAdapter {

		#region Constants: Private

		private const string FeatureCode = "IncludeDataLocalizationInPackage";

		#endregion

		#region Fields: Private

		private static readonly MethodInfo GetExistingRecordIdsMethod = typeof(PackageElementUtilities).GetMethod(
			"GetExistingPackageSchemaDataRecordIds", new[] { typeof(PackageSchemaDataDescriptor) });

		private static readonly MethodInfo SaveLocalizableValueMethod = typeof(PackageElementUtilities).GetMethod(
			"SaveDataLocalizableValue", new[] { typeof(Guid[]), typeof(PackageSchemaDataDescriptor), typeof(Guid[]) });

		private readonly PackageElementUtilities _utilities;

		#endregion

		#region Constructors: Public

		public DataLocalizationAdapter(PackageElementUtilities utilities) {
			_utilities = utilities;
		}

		#endregion

		#region Properties: Private

		private static bool IsEnabled => GetExistingRecordIdsMethod != null && SaveLocalizableValueMethod != null &&
			Features.GetIsEnabled(FeatureCode);

		#endregion

		#region Methods: Private

		private static object Invoke(MethodInfo method, object target, params object[] arguments) {
			try {
				return method.Invoke(target, arguments);
			} catch (TargetInvocationException ex) when (ex.InnerException != null) {
				throw ex.InnerException;
			}
		}

		#endregion

		#region Methods: Public

		public Guid[] GetExistingRecordIds(PackageSchemaDataDescriptor descriptor) {
			return IsEnabled
				? (Guid[])Invoke(GetExistingRecordIdsMethod, _utilities, descriptor) ?? Array.Empty<Guid>()
				: Array.Empty<Guid>();
		}

		public void SaveLocalizableValues(Guid[] recordIds, PackageSchemaDataDescriptor descriptor,
				Guid[] existingRecordIds) {
			if (IsEnabled) {
				Invoke(SaveLocalizableValueMethod, _utilities, recordIds, descriptor, existingRecordIds);
			}
		}

		#endregion

	}
}
