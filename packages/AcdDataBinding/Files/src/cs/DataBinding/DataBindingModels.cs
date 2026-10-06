namespace AcdDataBinding.DataBinding {
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// User package a data binding can be saved to.
	/// </summary>
	public sealed class BindingPackage {

		public Guid Id { get; set; }

		public Guid UId { get; set; }

		public string Name { get; set; }

	}

	/// <summary>
	/// What to bind: the entity schema, the binding name and UId, and how to select the records.
	/// Records are selected by <see cref="RecordIds"/>, by <see cref="FilterColumnName"/> equal to
	/// <see cref="FilterValue"/>, or all records when <see cref="AllRecords"/> is set.
	/// </summary>
	public sealed class BindingDefinition {

		public string SchemaName { get; set; }

		public string Name { get; set; }

		/// <summary>
		/// UId of the data binding. When empty, the UId of the entity schema is used.
		/// </summary>
		public Guid UId { get; set; }

		public IList<Guid> RecordIds { get; set; }

		public bool AllRecords { get; set; }

		public string FilterColumnName { get; set; }

		public Guid FilterValue { get; set; }

		/// <summary>
		/// Related binding (for example, system setting values of a system setting). A related binding
		/// without records is skipped; a main binding without records is an error.
		/// </summary>
		public bool IsRelated { get; set; }

	}

	public enum BindingStatus {
		Saved,
		Skipped,
		Failed,
		NotProcessed
	}

	/// <summary>
	/// Result of saving one data binding.
	/// </summary>
	public sealed class BindingOutcome {

		public string SchemaName { get; set; }

		public string Name { get; set; }

		public Guid UId { get; set; }

		public int RecordsCount { get; set; }

		public BindingStatus Status { get; set; }

		public string Message { get; set; }

		public IList<string> Warnings { get; set; } = new List<string>();

		public static BindingOutcome For(BindingDefinition definition, BindingStatus status, string message = null) {
			return new BindingOutcome {
				SchemaName = definition?.SchemaName,
				Name = definition?.Name,
				UId = definition?.UId ?? Guid.Empty,
				Status = status,
				Message = message
			};
		}

	}
}
