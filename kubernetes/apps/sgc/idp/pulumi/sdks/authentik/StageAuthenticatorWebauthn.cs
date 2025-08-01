// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/stageAuthenticatorWebauthn:StageAuthenticatorWebauthn")]
    public partial class StageAuthenticatorWebauthn : global::Pulumi.CustomResource
    {
        /// <summary>
        /// Allowed values: - `platform` - `cross-platform`
        /// </summary>
        [Output("authenticatorAttachment")]
        public Output<string?> AuthenticatorAttachment { get; private set; } = null!;

        [Output("configureFlow")]
        public Output<string?> ConfigureFlow { get; private set; } = null!;

        [Output("deviceTypeRestrictions")]
        public Output<ImmutableArray<string>> DeviceTypeRestrictions { get; private set; } = null!;

        [Output("friendlyName")]
        public Output<string?> FriendlyName { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        /// <summary>
        /// Allowed values: - `discouraged` - `preferred` - `required` Defaults to `preferred`.
        /// </summary>
        [Output("residentKeyRequirement")]
        public Output<string?> ResidentKeyRequirement { get; private set; } = null!;

        [Output("stageAuthenticatorWebauthnId")]
        public Output<string> StageAuthenticatorWebauthnId { get; private set; } = null!;

        /// <summary>
        /// Allowed values: - `required` - `preferred` - `discouraged` Defaults to `preferred`.
        /// </summary>
        [Output("userVerification")]
        public Output<string?> UserVerification { get; private set; } = null!;


        /// <summary>
        /// Create a StageAuthenticatorWebauthn resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public StageAuthenticatorWebauthn(string name, StageAuthenticatorWebauthnArgs? args = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageAuthenticatorWebauthn:StageAuthenticatorWebauthn", name, args ?? new StageAuthenticatorWebauthnArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private StageAuthenticatorWebauthn(string name, Input<string> id, StageAuthenticatorWebauthnState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageAuthenticatorWebauthn:StageAuthenticatorWebauthn", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
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
        /// Get an existing StageAuthenticatorWebauthn resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static StageAuthenticatorWebauthn Get(string name, Input<string> id, StageAuthenticatorWebauthnState? state = null, CustomResourceOptions? options = null)
        {
            return new StageAuthenticatorWebauthn(name, id, state, options);
        }
    }

    public sealed class StageAuthenticatorWebauthnArgs : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Allowed values: - `platform` - `cross-platform`
        /// </summary>
        [Input("authenticatorAttachment")]
        public Input<string>? AuthenticatorAttachment { get; set; }

        [Input("configureFlow")]
        public Input<string>? ConfigureFlow { get; set; }

        [Input("deviceTypeRestrictions")]
        private InputList<string>? _deviceTypeRestrictions;
        public InputList<string> DeviceTypeRestrictions
        {
            get => _deviceTypeRestrictions ?? (_deviceTypeRestrictions = new InputList<string>());
            set => _deviceTypeRestrictions = value;
        }

        [Input("friendlyName")]
        public Input<string>? FriendlyName { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        /// <summary>
        /// Allowed values: - `discouraged` - `preferred` - `required` Defaults to `preferred`.
        /// </summary>
        [Input("residentKeyRequirement")]
        public Input<string>? ResidentKeyRequirement { get; set; }

        [Input("stageAuthenticatorWebauthnId")]
        public Input<string>? StageAuthenticatorWebauthnId { get; set; }

        /// <summary>
        /// Allowed values: - `required` - `preferred` - `discouraged` Defaults to `preferred`.
        /// </summary>
        [Input("userVerification")]
        public Input<string>? UserVerification { get; set; }

        public StageAuthenticatorWebauthnArgs()
        {
        }
        public static new StageAuthenticatorWebauthnArgs Empty => new StageAuthenticatorWebauthnArgs();
    }

    public sealed class StageAuthenticatorWebauthnState : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Allowed values: - `platform` - `cross-platform`
        /// </summary>
        [Input("authenticatorAttachment")]
        public Input<string>? AuthenticatorAttachment { get; set; }

        [Input("configureFlow")]
        public Input<string>? ConfigureFlow { get; set; }

        [Input("deviceTypeRestrictions")]
        private InputList<string>? _deviceTypeRestrictions;
        public InputList<string> DeviceTypeRestrictions
        {
            get => _deviceTypeRestrictions ?? (_deviceTypeRestrictions = new InputList<string>());
            set => _deviceTypeRestrictions = value;
        }

        [Input("friendlyName")]
        public Input<string>? FriendlyName { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        /// <summary>
        /// Allowed values: - `discouraged` - `preferred` - `required` Defaults to `preferred`.
        /// </summary>
        [Input("residentKeyRequirement")]
        public Input<string>? ResidentKeyRequirement { get; set; }

        [Input("stageAuthenticatorWebauthnId")]
        public Input<string>? StageAuthenticatorWebauthnId { get; set; }

        /// <summary>
        /// Allowed values: - `required` - `preferred` - `discouraged` Defaults to `preferred`.
        /// </summary>
        [Input("userVerification")]
        public Input<string>? UserVerification { get; set; }

        public StageAuthenticatorWebauthnState()
        {
        }
        public static new StageAuthenticatorWebauthnState Empty => new StageAuthenticatorWebauthnState();
    }
}
