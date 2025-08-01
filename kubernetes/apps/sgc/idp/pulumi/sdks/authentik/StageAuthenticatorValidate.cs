// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/stageAuthenticatorValidate:StageAuthenticatorValidate")]
    public partial class StageAuthenticatorValidate : global::Pulumi.CustomResource
    {
        [Output("configurationStages")]
        public Output<ImmutableArray<string>> ConfigurationStages { get; private set; } = null!;

        [Output("deviceClasses")]
        public Output<ImmutableArray<string>> DeviceClasses { get; private set; } = null!;

        /// <summary>
        /// Defaults to `seconds=0`.
        /// </summary>
        [Output("lastAuthThreshold")]
        public Output<string?> LastAuthThreshold { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        /// <summary>
        /// Allowed values: - `skip` - `deny` - `configure`
        /// </summary>
        [Output("notConfiguredAction")]
        public Output<string> NotConfiguredAction { get; private set; } = null!;

        [Output("stageAuthenticatorValidateId")]
        public Output<string> StageAuthenticatorValidateId { get; private set; } = null!;

        [Output("webauthnAllowedDeviceTypes")]
        public Output<ImmutableArray<string>> WebauthnAllowedDeviceTypes { get; private set; } = null!;

        /// <summary>
        /// Allowed values: - `required` - `preferred` - `discouraged` Defaults to `preferred`.
        /// </summary>
        [Output("webauthnUserVerification")]
        public Output<string?> WebauthnUserVerification { get; private set; } = null!;


        /// <summary>
        /// Create a StageAuthenticatorValidate resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public StageAuthenticatorValidate(string name, StageAuthenticatorValidateArgs args, CustomResourceOptions? options = null)
            : base("authentik:index/stageAuthenticatorValidate:StageAuthenticatorValidate", name, args ?? new StageAuthenticatorValidateArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private StageAuthenticatorValidate(string name, Input<string> id, StageAuthenticatorValidateState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageAuthenticatorValidate:StageAuthenticatorValidate", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
        {
        }

        private static CustomResourceOptions MakeResourceOptions(CustomResourceOptions? options, Input<string>? id)
        {
            var defaultOptions = new CustomResourceOptions
            {
                Version = Utilities.Version,
            };
            var merged = CustomResourceOptions.Merge(defaultOptions, options);
            // Override the ID if one was specified for consistency with other language SDKs.
            merged.Id = id ?? merged.Id;
            return merged;
        }
        /// <summary>
        /// Get an existing StageAuthenticatorValidate resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static StageAuthenticatorValidate Get(string name, Input<string> id, StageAuthenticatorValidateState? state = null, CustomResourceOptions? options = null)
        {
            return new StageAuthenticatorValidate(name, id, state, options);
        }
    }

    public sealed class StageAuthenticatorValidateArgs : global::Pulumi.ResourceArgs
    {
        [Input("configurationStages")]
        private InputList<string>? _configurationStages;
        public InputList<string> ConfigurationStages
        {
            get => _configurationStages ?? (_configurationStages = new InputList<string>());
            set => _configurationStages = value;
        }

        [Input("deviceClasses")]
        private InputList<string>? _deviceClasses;
        public InputList<string> DeviceClasses
        {
            get => _deviceClasses ?? (_deviceClasses = new InputList<string>());
            set => _deviceClasses = value;
        }

        /// <summary>
        /// Defaults to `seconds=0`.
        /// </summary>
        [Input("lastAuthThreshold")]
        public Input<string>? LastAuthThreshold { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        /// <summary>
        /// Allowed values: - `skip` - `deny` - `configure`
        /// </summary>
        [Input("notConfiguredAction", required: true)]
        public Input<string> NotConfiguredAction { get; set; } = null!;

        [Input("stageAuthenticatorValidateId")]
        public Input<string>? StageAuthenticatorValidateId { get; set; }

        [Input("webauthnAllowedDeviceTypes")]
        private InputList<string>? _webauthnAllowedDeviceTypes;
        public InputList<string> WebauthnAllowedDeviceTypes
        {
            get => _webauthnAllowedDeviceTypes ?? (_webauthnAllowedDeviceTypes = new InputList<string>());
            set => _webauthnAllowedDeviceTypes = value;
        }

        /// <summary>
        /// Allowed values: - `required` - `preferred` - `discouraged` Defaults to `preferred`.
        /// </summary>
        [Input("webauthnUserVerification")]
        public Input<string>? WebauthnUserVerification { get; set; }

        public StageAuthenticatorValidateArgs()
        {
        }
        public static new StageAuthenticatorValidateArgs Empty => new StageAuthenticatorValidateArgs();
    }

    public sealed class StageAuthenticatorValidateState : global::Pulumi.ResourceArgs
    {
        [Input("configurationStages")]
        private InputList<string>? _configurationStages;
        public InputList<string> ConfigurationStages
        {
            get => _configurationStages ?? (_configurationStages = new InputList<string>());
            set => _configurationStages = value;
        }

        [Input("deviceClasses")]
        private InputList<string>? _deviceClasses;
        public InputList<string> DeviceClasses
        {
            get => _deviceClasses ?? (_deviceClasses = new InputList<string>());
            set => _deviceClasses = value;
        }

        /// <summary>
        /// Defaults to `seconds=0`.
        /// </summary>
        [Input("lastAuthThreshold")]
        public Input<string>? LastAuthThreshold { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        /// <summary>
        /// Allowed values: - `skip` - `deny` - `configure`
        /// </summary>
        [Input("notConfiguredAction")]
        public Input<string>? NotConfiguredAction { get; set; }

        [Input("stageAuthenticatorValidateId")]
        public Input<string>? StageAuthenticatorValidateId { get; set; }

        [Input("webauthnAllowedDeviceTypes")]
        private InputList<string>? _webauthnAllowedDeviceTypes;
        public InputList<string> WebauthnAllowedDeviceTypes
        {
            get => _webauthnAllowedDeviceTypes ?? (_webauthnAllowedDeviceTypes = new InputList<string>());
            set => _webauthnAllowedDeviceTypes = value;
        }

        /// <summary>
        /// Allowed values: - `required` - `preferred` - `discouraged` Defaults to `preferred`.
        /// </summary>
        [Input("webauthnUserVerification")]
        public Input<string>? WebauthnUserVerification { get; set; }

        public StageAuthenticatorValidateState()
        {
        }
        public static new StageAuthenticatorValidateState Empty => new StageAuthenticatorValidateState();
    }
}
