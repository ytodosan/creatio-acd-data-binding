define("BaseGridDetailV2", ["AcdDataBindingMixin"], function() {
	return {
		mixins: {
			AcdDataBindingMixin: "Terrasoft.AcdDataBindingMixin"
		},
		methods: {
			getColumnsSetupButtonVisible: function() {
				return true;
			},
			addGridOperationsMenuItems: function(toolsButtonMenu) {
				this.callParent(arguments);
				if (this.getGridSettingsMenuItem()) {
					this.addBindProfileDataMenuItem(toolsButtonMenu);
				}
			},
			init: function() {
				this.callParent(arguments);
				this.mixins.AcdDataBindingMixin.init.call(this);
			}
		}
	};
});
