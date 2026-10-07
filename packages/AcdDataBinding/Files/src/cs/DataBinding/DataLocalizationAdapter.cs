namespace AcdDataBinding.DataBinding {
	using System;
	using System.Linq;
	using System.Reflection;
	using Terrasoft.Core.Packages;

	/// <summary>
	/// Saves data localizations with the binding, as the platform does when the IncludeDataLocalizationInPackage
	/// feature is on. The methods exist only on newer platform builds, so they are resolved at runtime: the package
	/// compiles against older SDKs and does nothing where the platform doesn't support it. The feature check also
	/// uses reflection: Creatio.FeatureToggling is strong-named and has no binding redirect, so a compile-time
	/// reference to one version would fail to load on Creatio versions that ship another.
	/// </summary>
	internal sealed class DataLocalizationAdapter {

		#region Constants: Private

		private const string FeatureCode = "IncludeDataLocalizationInPackage";

		private const string FeaturesTypeName = "Creatio.FeatureToggling.Features";

		#endregion

		#region Fields: Private

		private static readonly MethodInfo GetExistingRecordIdsMethod = typeof(PackageElementUtilities).GetMethod(
			"GetExistingPackageSchemaDataRecordIds", new[] { typeof(PackageSchemaDataDescriptor) });

		private static readonly MethodInfo SaveLocalizableValueMethod = typeof(PackageElementUtilities).GetMethod(
			"SaveDataLocalizableValue", new[] { typeof(Guid[]), typeof(PackageSchemaDataDescriptor), typeof(Guid[]) });

		private static readonly Lazy<MethodInfo> GetIsFeatureEnabledMethod = new Lazy<MethodInfo>(() =>
			AppDomain.CurrentDomain.GetAssemblies()
				.Select(assembly => assembly.GetType(FeaturesTypeName, false))
				.FirstOrDefault(type => type != null)?
				.GetMethod("GetIsEnabled", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) },
					null));

		private readonly PackageElementUtilities _utilities;

		#endregion

		#region Constructors: Public

		public DataLocalizationAdapter(PackageElementUtilities utilities) {
			_utilities = utilities;
		}

		#endregion

		#region Properties: Private

		private static bool IsEnabled => GetExistingRecordIdsMethod != null && SaveLocalizableValueMethod != null &&
			GetIsFeatureEnabledMethod.Value != null && (bool)GetIsFeatureEnabledMethod.Value.Invoke(null, new object[] { FeatureCode });

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
