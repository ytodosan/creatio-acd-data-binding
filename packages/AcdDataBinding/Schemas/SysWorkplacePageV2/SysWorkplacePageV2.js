define("SysWorkplacePageV2", [], function() {
	return {
		diff: /**SCHEMA_DIFF*/[
			{
				"operation": "insert",
				"name": "AcdBindWorkplaceButton",
				"parentName": "ActionButtonsContainer",
				"propertyName": "items",
				"index": 5,
				"values": {
					"itemType": Terrasoft.ViewItemType.BUTTON,
					"style": Terrasoft.controls.ButtonEnums.style.BLUE,
					"caption": {"bindTo": "getBindWorkplaceButtonCaption"},
					"menu": {
						"items": [
							{
								"caption": {"bindTo": "getBindNewWorkplaceCaption"},
								"click": {"bindTo": "onBindNewWorkplaceClick"}
							},
							{
								"caption": {"bindTo": "getBindWorkplaceSectionsCaption"},
								"click": {"bindTo": "onBindWorkplaceSectionsClick"}
							}
						]
					},
					"classes": {
						"textClass": ["actions-button-margin-right"],
						"wrapperClass": ["actions-button-margin-right"]
					},
					"visible": {"bindTo": "getBindDataButtonVisible"}
				}
			}
		]/**SCHEMA_DIFF*/
	};
});