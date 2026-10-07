define("AcdDataBindingMixin", ["AcdDataBindingMixinResources", "RightUtilities"],
	function(resources, RightUtilities) {

		/**
		 * Adds "Bind data" actions to classic UI system pages and sections. The browser selects the records and
		 * names the bindings; the AcdDataBindingService web service builds and saves the data bindings, so the
		 * mixin works on both .NET Framework and .NET Creatio. Ported from GlbDataBindingMixin.
		 */
		Ext.define("Terrasoft.configuration.mixins.AcdDataBindingMixin", {
			alternateClassName: "Terrasoft.AcdDataBindingMixin",

			_bindSchemaName: null,

			_currentPackage: null,

			_availablePackages: null,

			init: function() {
				RightUtilities.checkCanExecuteOperation({operation: "CanManageSolution"}, function(result) {
					this.set("IsBindDataEnabled", result);
				}, this);
			},

			calculateUId: function(str) {
				str = str || "";
				if (typeof(str) === "object") {
					str = JSON.stringify(str);
				}
				var hash = 0;
				for (var i = 0; i < str.length; i++) {
					hash = ~~(((hash << 5) - hash) + str.charCodeAt(i));
				}
				hash = hash >>> 0;
				return Terrasoft.GUID_EMPTY.replace(/[0]/g, function(c, p) {
					var value = ((p + 1) * hash).toString(16);
					return value[p % value.length];
				});
			},

			generateCodeForDataBinding: function(value, id) {
				value = value || "";
				var result = value
					.replace(new RegExp(/[-_]+/, "g"), " ")
					.replace(new RegExp(/[^\w\s]/, "g"), "")
					.replace(new RegExp(/\s+(.)(\w*)/, "g"), function($1, $2, $3) {
						return $2.toUpperCase() + $3.toLowerCase();
					})
					.replace(new RegExp(/\w/), function(s) {
						return s.toUpperCase();
					})
					.replace(new RegExp(/\s/, "g"), "");
				return result || (id && id.replace(new RegExp(/[^\w\d]/, "g"), ""));
			},

			getBindRecordInfo: function() {
				if (this.get("IsCardVisible")) {
					return this.sandbox.publish("GetPageRecordInfo", this, [this.getCardModuleSandboxId()]);
				} else {
					return {
						id: this.get("Id"),
						name: this.get("Name"),
						code: this.get("Code")
					};
				}
			},

			/**
			 * Creates a binding request item. Record selection: config.records (ids), config.allRecords, or
			 * config.filterColumn equal to config.recordId. Empty values are left out: the service expects Guids.
			 */
			createBinding: function(config) {
				var binding = {
					schemaName: config.schemaName,
					name: (config.name || "").replaceAll("-", "")
				};
				if (config.uId) {
					binding.uId = config.uId;
				}
				if (config.records) {
					binding.recordIds = config.records;
				}
				if (config.allRecords) {
					binding.allRecords = true;
				}
				if (config.filterColumn) {
					binding.filterColumnName = config.filterColumn;
					binding.filterValue = config.recordId;
				}
				if (config.isRelated) {
					binding.isRelated = true;
				}
				return binding;
			},

			/**
			 * Calls AcdDataBindingService. An HTTP error is logged to the browser console (method, URL, status) and
			 * returned as an unsuccessful response with a message, the same shape as an error reported by the
			 * service itself. A 404 means the service isn't available: the app isn't installed, Creatio wasn't
			 * restarted after the installation, or the app's server code failed to load.
			 */
			callDataBindingService: function(methodName, data, callback) {
				this.callService({
					serviceName: "AcdDataBindingService",
					methodName: methodName,
					data: data
				}, function(response, request) {
					var status = request && request.status;
					if (status && status !== 200) {
						response = this.createServiceErrorResponse(methodName, status);
					}
					callback.call(this, response || {});
				}, this);
			},

			createServiceErrorResponse: function(methodName, status) {
				var message = status === 404
					? resources.localizableStrings.ServiceUnavailableMessage
					: Ext.String.format(resources.localizableStrings.ServiceRequestFailedMessage, status);
				var url = Terrasoft.combinePath(Terrasoft.utils.uri.getConfigurationWebServiceBaseUrl(), "rest",
					"AcdDataBindingService", methodName);
				this.log("[Data Binding kit] " + methodName + " failed: HTTP " + status + " " + url + ". " + message,
					Terrasoft.LogMessageType.ERROR);
				return {
					success: false,
					message: message
				};
			},

			selectPackage: function(callback, scope) {
				this.showBodyMask({showHidden: true});
				this.callDataBindingService("GetPackages", {
					schemaName: this._bindSchemaName
				}, function(response) {
					this.hideBodyMask();
					this.onLoadPackages(response, callback, scope);
				});
			},

			onLoadPackages: function(response, callback, scope) {
				response = response || {};
				if (!response.success) {
					this.showInformationDialog(response.message || resources.localizableStrings.SendingRequestError);
					return;
				}
				var packages = response.packages || [];
				if (packages.length === 1) {
					this._currentPackage = packages[0];
					callback.call(scope || this);
				} else if (packages.length > 1) {
					this._availablePackages = packages;
					this.openPackagePopup(callback, scope);
				} else {
					var message = Ext.String.format(resources.localizableStrings.NoAvailablePackagesError,
						this._bindSchemaName);
					this.showInformationDialog(message);
				}
			},

			openPackagePopup: function(callback, scope) {
				this.set("PackagesList", new Terrasoft.Collection());
				var buttons = [
					Terrasoft.MessageBoxButtons.SAVE,
					Terrasoft.MessageBoxButtons.CANCEL
				];
				Terrasoft.utils.inputBox(resources.localizableStrings.SelectPackageMessage, function(result, arg) {
					var selectedPackage = arg.Package.value;
					if (result === Terrasoft.MessageBoxButtons.SAVE.returnCode && !Ext.isEmpty(selectedPackage)) {
						this._currentPackage = selectedPackage;
						callback.call(scope || this);
					}
				}, buttons, this, {
					Package: {
						"dataValueType": Terrasoft.DataValueType.ENUM,
						"value": { "bindTo": "Package" },
						"customConfig": {
							"list": { "bindTo": "PackagesList" },
							"prepareList": { "bindTo": "preparePackagesList" }
						}
					}
				});
				Terrasoft.each(Terrasoft.MessageBox.controlArray, function(item) {
					item.control.bind(this);
				}, this);
			},

			preparePackagesList: function(searchValue, list) {
				if (list && this._availablePackages) {
					list.clear();
					var objects = {};
					Terrasoft.each(this._availablePackages, function(item) {
						objects[item.value] = {
							value: item.value,
							displayValue: item.displayValue,
							uId: item.uId
						};
					}, this);
					list.loadAll(objects);
				}
			},

			getBindDataButtonVisible: function() {
				return this.get("IsBindDataEnabled") &&
					this.get("Operation") !== Terrasoft.ConfigurationEnums.CardOperation.ADD &&
					this.get("Operation") !== Terrasoft.ConfigurationEnums.CardOperation.COPY;
			},

			getProfileDataButtonVisible: function() {
				return this.get("IsBindDataEnabled") && this.getColumnsSetupButtonVisible();
			},

			addBindProfileDataMenuItem: function(menuItems) {
				menuItems.addItem(this.getButtonMenuItem({
					"Caption": resources.localizableStrings.BindProfileCaption,
					"Click": { "bindTo": "onBindProfileDataClick" },
					"Visible": { "bindTo": "getProfileDataButtonVisible" }
				}));
				menuItems.addItem(this.getButtonMenuItem({
					"Caption": resources.localizableStrings.ClearProfileCaption,
					"Click": { "bindTo": "onClearProfileDataClick" },
					"Visible": { "bindTo": "getProfileDataButtonVisible" }
				}));
			},

			getBindDataButtonCaption: function() {
				return resources.localizableStrings.BindDataButtonCaption;
			},

			getBindAllDataButtonCaption: function() {
				return resources.localizableStrings.BindAllDataButtonCaption;
			},

			getBindLookupButtonCaption: function() {
				return resources.localizableStrings.BindLookupButtonCaption;
			},

			getBindOperationButtonCaption: function() {
				return resources.localizableStrings.BindOperationButtonCaption;
			},

			getBindSettingButtonCaption: function() {
				return resources.localizableStrings.BindSettingButtonCaption;
			},

			getBindRoleButtonCaption: function() {
				return resources.localizableStrings.BindRoleButtonCaption;
			},

			getBindWorkplaceButtonCaption: function() {
				return resources.localizableStrings.BindWorkplaceButtonCaption;
			},

			getBindNewWorkplaceCaption: function() {
				return resources.localizableStrings.BindNewWorkplaceCaption;
			},

			getBindWorkplaceSectionsCaption: function() {
				return resources.localizableStrings.BindWorkplaceSectionsCaption;
			},

			onClearProfileDataClick: function() {
				var caption = this.get("Caption") || this.get("SeparateModeActionsButtonHeaderMenuItemCaption") || "";
				var objectType = this.get("Caption") ? resources.localizableStrings.DetailCaption :
					resources.localizableStrings.SectionCaption;
				var messageTemplate = resources.localizableStrings.ColumnsSetupRestoredConfirm;
				this.showConfirmationDialog(Ext.String.format(messageTemplate, objectType, caption), function(returnCode) {
					if (returnCode === Terrasoft.MessageBoxButtons.YES.returnCode) {
						this.clearProfileData();
					}
				}, [Terrasoft.MessageBoxButtons.YES, Terrasoft.MessageBoxButtons.NO]);
			},

			clearProfileData: function() {
				this.showBodyMask({showHidden: true});
				this.callDataBindingService("RestoreColumnSetup", {
					profileKey: this.getProfileKey()
				}, function(response) {
					if (response.success) {
						if (response.rowsAffected) {
							this.applyDefaultColumnSetup();
							this.showInformationDialog(resources.localizableStrings.ColumnsSetupRestoredMessage, function() {
								this.reloadGridColumnsConfig(true);
							});
						} else {
							this.showInformationDialog(resources.localizableStrings.ColumnsSetupRestoredMessage);
						}
					} else {
						var message = response.message || resources.localizableStrings.SendingRequestError;
						this.showInformationDialog(message, Terrasoft.emptyFn, {
							style: Terrasoft.controls.MessageBoxEnums.Styles.RED
						});
					}
					this.hideBodyMask();
				});
			},

			applyDefaultColumnSetup: function() {
				this.requireProfile(function(profile) {
					profile = Terrasoft.ColumnUtilities.updateProfileColumnCaptions({
						profile: profile,
						entityColumns: this.columns
					});
					this.set("Profile", profile);
					this.set("IsClearGridData", true);
					this.loadGridData();
				}, this);
			},

			onBindProfileDataClick: function() {
				this._bindSchemaName = "SysProfileData";
				var profileKey = this.getProfileKey();
				var records;
				var esq = Ext.create("Terrasoft.EntitySchemaQuery", {
					rootSchemaName: this._bindSchemaName
				});
				esq.addColumn("Id");
				esq.filters.addItem(esq.createColumnFilterWithParameter(
					Terrasoft.ComparisonType.EQUAL, "Key", profileKey));
				esq.filters.addItem(esq.createColumnIsNullFilter("Contact"));
				Terrasoft.chain(
					function(next) {
						esq.getEntityCollection(function(result) {
							if (result.success) {
								records = result.collection.getKeys();
								if (records.length) {
									next();
								} else {
									this.showInformationDialog(
										resources.localizableStrings.ColumnsSetupNotFoundMessage);
								}
							}
						}, this);
					},
					this.selectPackage,
					function() {
						this.saveBindings([this.createBinding({
							schemaName: this._bindSchemaName,
							records: records,
							name: this._bindSchemaName + "_" + profileKey,
							uId: this.calculateUId(profileKey)
						})]);
					},
					this);
			},

			onBindLookupButtonClick: function() {
				this._bindSchemaName = "Lookup";
				var code, uId, records;
				var lookupConfig = {
					entitySchemaName: this._bindSchemaName,
					columns: ["Id", "Name"],
					isQuickAdd: false,
					multiSelect: true
				};
				Terrasoft.chain(
					function(next) {
						if (this.get("IsCardVisible") || this.get("IsCardOpened")) {
							var recordInfo = this.getBindRecordInfo();
							records = [recordInfo.id];
							uId = recordInfo.id;
							code = this.generateCodeForDataBinding(recordInfo.name, recordInfo.id);
							next();
						} else {
							this.openLookup(lookupConfig, function(result) {
								records = result.selectedRows.getKeys();
								if (records.length) {
									if (records.length === 1) {
										var selectedRow = result.selectedRows.first();
										uId = selectedRow.Id;
										code = this.generateCodeForDataBinding(selectedRow.Name, selectedRow.Id);
									} else {
										uId = this.calculateUId(records.sort());
										code = uId.replaceAll("-", "");
									}
									next();
								}
							}, this);
						}
					},
					this.selectPackage,
					function() {
						this.saveBindings([this.createBinding({
							schemaName: this._bindSchemaName,
							records: records,
							name: this._bindSchemaName + "_" + code,
							uId: uId
						})]);
					},
					this);
			},

			onBindLookupDataButtonClick: function() {
				this._bindSchemaName = this.entitySchemaName;
				var code, uId, records;
				var lookupConfig = {
					entitySchemaName: this._bindSchemaName,
					isQuickAdd: false,
					multiSelect: true
				};
				Terrasoft.chain(
					function(next) {
						this.openLookup(lookupConfig, function(result) {
							records = result.selectedRows.getKeys();
							if (records.length) {
								if (records.length === 1) {
									var selectedRow = result.selectedRows.first();
									uId = selectedRow.Id;
									code = this.generateCodeForDataBinding(selectedRow.Name, selectedRow.Id);
								} else {
									uId = this.calculateUId(records.sort());
									code = uId.replaceAll("-", "");
								}
								next();
							}
						}, this);
					},
					this.selectPackage,
					function() {
						this.saveBindings([this.createBinding({
							schemaName: this._bindSchemaName,
							records: records,
							name: this._bindSchemaName + "_" + code,
							uId: uId
						})]);
					},
					this);
			},

			onBindAllLookupDataButtonClick: function() {
				this._bindSchemaName = this.entitySchemaName;
				Terrasoft.chain(
					this.selectPackage,
					function() {
						this.saveBindings([this.createBinding({
							schemaName: this._bindSchemaName,
							allRecords: true,
							name: this._bindSchemaName + "_Values"
						})]);
					},
					this);
			},

			onBindOperationButtonClick: function() {
				this._bindSchemaName = "SysAdminOperation";
				var recordInfo = this.getBindRecordInfo();
				var code = this.generateCodeForDataBinding(recordInfo.code);
				Terrasoft.chain(
					this.selectPackage,
					function() {
						this.saveBindings([
							this.createBinding({
								schemaName: this._bindSchemaName,
								records: [recordInfo.id],
								name: this._bindSchemaName + "_" + code,
								uId: recordInfo.id
							}),
							this.createBinding({
								schemaName: "SysAdminOperationGrantee",
								filterColumn: this._bindSchemaName,
								recordId: recordInfo.id,
								name: "SysAdminOperationGrantee_" + code,
								uId: this.calculateUId("SysAdminOperationGrantee" + recordInfo.id),
								isRelated: true
							})
						]);
					},
					this);
			},

			onBindSettingButtonClick: function() {
				this._bindSchemaName = "SysSettings";
				var recordInfo = this.getBindRecordInfo();
				var code = this.generateCodeForDataBinding(recordInfo.code);
				Terrasoft.chain(
					this.selectPackage,
					function() {
						this.saveBindings([
							this.createBinding({
								schemaName: this._bindSchemaName,
								records: [recordInfo.id],
								name: this._bindSchemaName + "_" + code,
								uId: recordInfo.id
							}),
							this.createBinding({
								schemaName: "SysSettingsValue",
								filterColumn: this._bindSchemaName,
								recordId: recordInfo.id,
								name: "SysSettingsValue_" + code,
								uId: this.calculateUId("SysSettingsValue" + recordInfo.id),
								isRelated: true
							})
						]);
					},
					this);
			},

			onBindRoleButtonClick: function() {
				this._bindSchemaName = "SysAdminUnit";
				var recordInfo = this.getBindRecordInfo();
				var code = this.generateCodeForDataBinding(recordInfo.name, recordInfo.id);
				Terrasoft.chain(
					this.selectPackage,
					function() {
						this.saveBindings([this.createBinding({
							schemaName: this._bindSchemaName,
							records: [recordInfo.id],
							name: this._bindSchemaName + "_" + code,
							uId: recordInfo.id
						})]);
					},
					this);
			},

			/**
			 * Binds a workplace created in this environment: its header, its sections and the roles that can
			 * use it.
			 */
			onBindNewWorkplaceClick: function() {
				this._bindSchemaName = "SysWorkplace";
				var recordInfo = this.getBindRecordInfo();
				var code = this.generateCodeForDataBinding(recordInfo.name, recordInfo.id);
				Terrasoft.chain(
					this.selectPackage,
					function() {
						this.saveBindings([
							this.createBinding({
								schemaName: this._bindSchemaName,
								records: [recordInfo.id],
								name: this._bindSchemaName + "_" + code,
								uId: recordInfo.id
							}),
							this.createWorkplaceSectionsBinding(recordInfo, code, true),
							this.createBinding({
								schemaName: "SysAdminUnitInWorkplace",
								filterColumn: "SysWorkplace",
								recordId: recordInfo.id,
								name: "SysAdminUnitInWorkplace_" + code,
								uId: this.calculateUId("SysAdminUnitInWorkplace" + recordInfo.id),
								isRelated: true
							})
						]);
					},
					this);
			},

			/**
			 * Binds only the sections of a workplace, for workplaces that already exist in the target
			 * environment (for example, workplaces installed with Creatio applications). The workplace header
			 * and its roles are not bound.
			 */
			onBindWorkplaceSectionsClick: function() {
				this._bindSchemaName = "SysModuleInWorkplace";
				var recordInfo = this.getBindRecordInfo();
				var code = this.generateCodeForDataBinding(recordInfo.name, recordInfo.id);
				Terrasoft.chain(
					this.selectPackage,
					function() {
						this.saveBindings([this.createWorkplaceSectionsBinding(recordInfo, code, false)]);
					},
					this);
			},

			/**
			 * Same name and UId in both workplace actions, so binding sections again updates one data binding.
			 */
			createWorkplaceSectionsBinding: function(recordInfo, code, isRelated) {
				return this.createBinding({
					schemaName: "SysModuleInWorkplace",
					filterColumn: "SysWorkplace",
					recordId: recordInfo.id,
					name: "SysModuleInWorkplace_" + code,
					uId: this.calculateUId("SysModuleInWorkplace" + recordInfo.id),
					isRelated: isRelated
				});
			},

			saveBindings: function(bindings) {
				this.showBodyMask({showHidden: true});
				this.callDataBindingService("SaveBindings", {
					packageUId: this._currentPackage.uId,
					bindings: bindings
				}, function(response) {
					this.hideBodyMask();
					this.showDataBindingDetailsWindow(response);
				});
			},

			showDataBindingDetailsWindow: function(response) {
				var items = response.items || [];
				var savedUIds = [];
				var warnings = [];
				Terrasoft.each(items, function(item) {
					if (item.status === "saved") {
						savedUIds.push(item.uId);
					}
					Terrasoft.each(item.warnings || [], function(warning) {
						warnings.push(item.name + ": " + warning);
					}, this);
				}, this);
				var buttons = [Terrasoft.MessageBoxButtons.CLOSE];
				if (savedUIds.length) {
					buttons.push({
						className: "Terrasoft.Button",
						returnCode: "ViewDetails",
						style: Terrasoft.controls.ButtonEnums.style.DEFAULT,
						caption: resources.localizableStrings.ViewDetailsCaption
					});
				}
				var message;
				var msgStyle;
				if (response.success) {
					message = resources.localizableStrings.DataBoundMessage;
					msgStyle = Terrasoft.controls.MessageBoxEnums.Styles.BLUE;
					if (warnings.length) {
						Terrasoft.each(warnings, function(warning) {
							this.log(warning, Terrasoft.LogMessageType.WARNING);
						}, this);
						message += "\n\n" + resources.localizableStrings.DataBoundWithWarningsMessage + "\n" +
							warnings.join("\n");
					}
				} else {
					message = response.message || resources.localizableStrings.SendingRequestError;
					msgStyle = Terrasoft.controls.MessageBoxEnums.Styles.RED;
				}
				this.showInformationDialog(message, function(returnCode) {
					if (returnCode === "ViewDetails") {
						this.openDataBinding(savedUIds);
					}
				}, {
					style: msgStyle,
					buttons: buttons
				});
			},

			openDataBinding: function(uIds) {
				Terrasoft.each(uIds, function(uId) {
					if (uId) {
						var url = Terrasoft.workspaceBaseUrl + "/ClientApp/#/SchemaDataDesigner/" + uId;
						window.open(url, "_blank");
					}
				}, this);
			}
		});

		return Terrasoft.AcdDataBindingMixin;
	});
