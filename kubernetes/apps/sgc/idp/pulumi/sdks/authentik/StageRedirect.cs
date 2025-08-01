// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/stageRedirect:StageRedirect")]
    public partial class StageRedirect : global::Pulumi.CustomResource
    {
        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Output("keepContext")]
        public Output<bool?> KeepContext { get; private set; } = null!;

        /// <summary>
        /// Allowed values: - `static` - `flow` Defaults to `flow`.
        /// </summary>
        [Output("mode")]
        public Output<string?> Mode { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        [Output("stageRedirectId")]
        public Output<string> StageRedirectId { get; private set; } = null!;

        [Output("targetFlow")]
        public Output<string?> TargetFlow { get; private set; } = null!;

        [Output("targetStatic")]
        public Output<string?> TargetStatic { get; private set; } = null!;


        /// <summary>
        /// Create a StageRedirect resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public StageRedirect(string name, StageRedirectArgs? args = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageRedirect:StageRedirect", name, args ?? new StageRedirectArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private StageRedirect(string name, Input<string> id, StageRedirectState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageRedirect:StageRedirect", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
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
        /// Get an existing StageRedirect resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static StageRedirect Get(string name, Input<string> id, StageRedirectState? state = null, CustomResourceOptions? options = null)
        {
            return new StageRedirect(name, id, state, options);
        }
    }

    public sealed class StageRedirectArgs : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("keepContext")]
        public Input<bool>? KeepContext { get; set; }

        /// <summary>
        /// Allowed values: - `static` - `flow` Defaults to `flow`.
        /// </summary>
        [Input("mode")]
        public Input<string>? Mode { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("stageRedirectId")]
        public Input<string>? StageRedirectId { get; set; }

        [Input("targetFlow")]
        public Input<string>? TargetFlow { get; set; }

        [Input("targetStatic")]
        public Input<string>? TargetStatic { get; set; }

        public StageRedirectArgs()
        {
        }
        public static new StageRedirectArgs Empty => new StageRedirectArgs();
    }

    public sealed class StageRedirectState : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("keepContext")]
        public Input<bool>? KeepContext { get; set; }

        /// <summary>
        /// Allowed values: - `static` - `flow` Defaults to `flow`.
        /// </summary>
        [Input("mode")]
        public Input<string>? Mode { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("stageRedirectId")]
        public Input<string>? StageRedirectId { get; set; }

        [Input("targetFlow")]
        public Input<string>? TargetFlow { get; set; }

        [Input("targetStatic")]
        public Input<string>? TargetStatic { get; set; }

        public StageRedirectState()
        {
        }
        public static new StageRedirectState Empty => new StageRedirectState();
    }
}
