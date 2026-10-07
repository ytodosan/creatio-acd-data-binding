namespace AcdDataBinding.DataBinding {
	using AcdDataBinding.Shared;
	using Terrasoft.Core;
	using Terrasoft.Core.DB;

	/// <summary>
	/// Deletes the current user's SysProfileData row for a key. Ported from GlbDataBindingHelper.ClearColumnsSetup.
	/// </summary>
	internal sealed class ColumnSetupRepository : IColumnSetupRepository {

		public Result<int> DeleteCurrentUserColumnSetup(UserConnection userConnection, string profileKey) {
			var delete = (Delete)new Delete(userConnection)
				.From("SysProfileData")
				.Where("ContactId").IsEqual(Column.Parameter(userConnection.CurrentUser.ContactId))
				.And("Key").IsEqual(Column.Parameter(profileKey));
			return Result<int>.Success(delete.Execute());
		}

	}
}
