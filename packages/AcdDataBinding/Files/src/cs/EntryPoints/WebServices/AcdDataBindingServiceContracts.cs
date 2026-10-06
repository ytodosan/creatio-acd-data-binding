namespace AcdDataBinding.EntryPoints.WebServices {
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	[DataContract]
	public class AcdBaseResponse {

		[DataMember(Name = "success")]
		public bool Success { get; set; }

		[DataMember(Name = "message")]
		public string Message { get; set; }

	}

	[DataContract]
	public class AcdGetPackagesRequest {

		[DataMember(Name = "schemaName")]
		public string SchemaName { get; set; }

	}

	[DataContract]
	public class AcdPackageDto {

		[DataMember(Name = "value")]
		public Guid Id { get; set; }

		[DataMember(Name = "uId")]
		public Guid UId { get; set; }

		[DataMember(Name = "displayValue")]
		public string Name { get; set; }

	}

	[DataContract]
	public class AcdGetPackagesResponse : AcdBaseResponse {

		[DataMember(Name = "packages")]
		public List<AcdPackageDto> Packages { get; set; } = new List<AcdPackageDto>();

	}

	[DataContract]
	public class AcdRestoreColumnSetupRequest {

		[DataMember(Name = "profileKey")]
		public string ProfileKey { get; set; }

	}

	[DataContract]
	public class AcdRestoreColumnSetupResponse : AcdBaseResponse {

		[DataMember(Name = "rowsAffected")]
		public int RowsAffected { get; set; }

	}

	[DataContract]
	public class AcdBindingDto {

		[DataMember(Name = "schemaName")]
		public string SchemaName { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "uId")]
		public Guid UId { get; set; }

		[DataMember(Name = "recordIds")]
		public List<Guid> RecordIds { get; set; }

		[DataMember(Name = "allRecords")]
		public bool AllRecords { get; set; }

		[DataMember(Name = "filterColumnName")]
		public string FilterColumnName { get; set; }

		[DataMember(Name = "filterValue")]
		public Guid FilterValue { get; set; }

		[DataMember(Name = "isRelated")]
		public bool IsRelated { get; set; }

	}

	[DataContract]
	public class AcdSaveBindingsRequest {

		[DataMember(Name = "packageUId")]
		public Guid PackageUId { get; set; }

		[DataMember(Name = "bindings")]
		public List<AcdBindingDto> Bindings { get; set; }

	}

	[DataContract]
	public class AcdBindingResultDto {

		[DataMember(Name = "schemaName")]
		public string SchemaName { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "uId")]
		public Guid UId { get; set; }

		[DataMember(Name = "recordsCount")]
		public int RecordsCount { get; set; }

		/// <summary>
		/// saved, skipped, failed or notProcessed.
		/// </summary>
		[DataMember(Name = "status")]
		public string Status { get; set; }

		[DataMember(Name = "message")]
		public string Message { get; set; }

		[DataMember(Name = "warnings")]
		public List<string> Warnings { get; set; } = new List<string>();

	}

	[DataContract]
	public class AcdSaveBindingsResponse : AcdBaseResponse {

		[DataMember(Name = "items")]
		public List<AcdBindingResultDto> Items { get; set; } = new List<AcdBindingResultDto>();

	}
}
