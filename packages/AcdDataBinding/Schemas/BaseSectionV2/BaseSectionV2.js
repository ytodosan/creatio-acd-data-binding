define("BaseSectionV2", ["AcdDataBindingMixin"], function() {
	return {
		mixins: {
			AcdDataBindingMixin: "Terrasoft.AcdDataBindingMixin"
		},
		messages: {
			"GetPageRecordInfo": {
				mode: this.Terrasoft.MessageMode.PTP,
				direction: this.Terrasoft.MessageDirectionType.PUBLISH
			}
		},
		methods: {
			getColumnsSetupButtonVisible: function() {
				return this.get("IsSectionVisible");
			},
			getViewOptions: function() {
				var viewOptions = this.callParent(arguments);
				viewOptions.addItem(this.getButtonMenuSeparator());
				this.addBindProfileDataMenuItem(viewOptions);
				return viewOptions;
			},
			init: function() {
				this.callParent(arguments);
				this.mixins.AcdDataBindingMixin.init.call(this);
			}
		}
	};
});