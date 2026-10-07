namespace AcdDataBinding.EntryPoints.WebServices {
	using System.Collections.Generic;
	using System.Linq;
	using AcdDataBinding.Shared;
	using AcdDataBinding.DataBinding;

	/// <summary>
	/// Maps web service DTOs to application models and results back to response DTOs.
	/// </summary>
	internal static class AcdDataBindingMapper {

		#region Methods: Private

		private static string ToStatus(BindingStatus status) {
			switch (status) {
				case BindingStatus.Saved:
					return "saved";
				case BindingStatus.Skipped:
					return "skipped";
				case BindingStatus.NotProcessed:
					return "notProcessed";
				default:
					return "failed";
			}
		}

		#endregion

		#region Methods: Public

		public static IList<BindingDefinition> ToDefinitions(IEnumerable<AcdBindingDto> bindings) {
			return (bindings ?? Enumerable.Empty<AcdBindingDto>())
				.Select(binding => binding == null ? null : new BindingDefinition {
					SchemaName = binding.SchemaName,
					Name = binding.Name,
					UId = binding.UId,
					RecordIds = binding.RecordIds,
					AllRecords = binding.AllRecords,
					FilterColumnName = binding.FilterColumnName,
					FilterValue = binding.FilterValue,
					IsRelated = binding.IsRelated
				})
				.ToList();
		}

		public static AcdGetPackagesResponse ToResponse(Result<IReadOnlyList<BindingPackage>> result) {
			if (result.IsError) {
				return new AcdGetPackagesResponse { Success = false, Message = result.Error };
			}
			return new AcdGetPackagesResponse {
				Success = true,
				Packages = result.Value.Select(package => new AcdPackageDto {
					Id = package.Id,
					UId = package.UId,
					Name = package.Name
				}).ToList()
			};
		}

		public static AcdRestoreColumnSetupResponse ToResponse(Result<int> result) {
			return result.IsError
				? new AcdRestoreColumnSetupResponse { Success = false, Message = result.Error }
				: new AcdRestoreColumnSetupResponse { Success = true, RowsAffected = result.Value };
		}

		public static AcdSaveBindingsResponse ToResponse(Result<IReadOnlyList<BindingOutcome>> result) {
			if (result.IsError) {
				return new AcdSaveBindingsResponse { Success = false, Message = result.Error };
			}
			List<AcdBindingResultDto> items = result.Value.Select(outcome => new AcdBindingResultDto {
				SchemaName = outcome.SchemaName,
				Name = outcome.Name,
				UId = outcome.UId,
				RecordsCount = outcome.RecordsCount,
				Status = ToStatus(outcome.Status),
				Message = outcome.Message,
				Warnings = (outcome.Warnings ?? new List<string>()).ToList()
			}).ToList();
			BindingOutcome failed = result.Value.FirstOrDefault(outcome => outcome.Status == BindingStatus.Failed);
			return new AcdSaveBindingsResponse {
				Success = failed == null,
				Message = failed?.Message,
				Items = items
			};
		}

		#endregion

	}
}
