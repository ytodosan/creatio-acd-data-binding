namespace AcdDataBinding.Shared {

	/// <summary>
	/// Error-as-value result of an operation that returns a value.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	public sealed class Result<T> {

		#region Constructors: Private

		private Result(bool isSuccess, T value, string error) {
			IsSuccess = isSuccess;
			Value = value;
			Error = error;
		}

		#endregion

		#region Properties: Public

		public bool IsSuccess { get; }

		public bool IsError => !IsSuccess;

		public T Value { get; }

		public string Error { get; }

		#endregion

		#region Methods: Public

		public static Result<T> Success(T value) => new Result<T>(true, value, null);

		public static Result<T> Failure(string error) => new Result<T>(false, default(T), error);

		#endregion

	}
}
