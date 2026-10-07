using System;
using AcdDataBinding.DataBinding;
using Common.Logging;

namespace AcdDataBindingApp {

	/// <summary>
	/// Composition root of the AcdDataBinding package: creates the application services once.
	/// Services are wired by hand instead of with Microsoft.Extensions.DependencyInjection, so the package
	/// references only assemblies that every supported Creatio version ships (see the minimum version in
	/// app-descriptor.json).
	/// </summary>
	public sealed class AcdDataBindingApp {

		#region Fields: Private

		private static Lazy<AcdDataBindingApp> _instance = new Lazy<AcdDataBindingApp>(() => new AcdDataBindingApp());

		private readonly Lazy<IDataBindingManager> _dataBindingManager;

		#endregion

		#region Fields: Internal

		/// <summary>
		/// Replaces the data binding manager (for tests). Set it before the first use or call <see cref="Reset"/>.
		/// </summary>
		internal static Func<IDataBindingManager> DataBindingManagerFactory;

		#endregion

		#region Constructors: Private

		private AcdDataBindingApp() {
			_dataBindingManager = new Lazy<IDataBindingManager>(() => DataBindingManagerFactory?.Invoke() ??
				CreateDataBindingManager());
		}

		#endregion

		#region Properties: Public

		public static AcdDataBindingApp Instance => _instance.Value;

		public IDataBindingManager DataBindingManager => _dataBindingManager.Value;

		#endregion

		#region Methods: Private

		private static IDataBindingManager CreateDataBindingManager() {
			ILog log = LogManager.GetLogger(Constants.LoggerName);
			return new DataBindingManager(new BindingPackageProvider(), new ColumnSetupRepository(),
				new DataBindingStore(log));
		}

		#endregion

		#region Methods: Internal

		/// <summary>
		/// Recreates the instance (for tests). Not thread-safe; don't use in production code.
		/// </summary>
		internal static AcdDataBindingApp Reset() {
			_instance = new Lazy<AcdDataBindingApp>(() => new AcdDataBindingApp());
			return _instance.Value;
		}

		#endregion

	}
}
