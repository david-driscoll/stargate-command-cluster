// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/stageInvitation:StageInvitation")]
    public partial class StageInvitation : global::Pulumi.CustomResource
    {
        /// <summary>
        /// Defaults to `false`.
        /// </summary>
        [Output("continueFlowWithoutInvitation")]
        public Output<bool?> ContinueFlowWithoutInvitation { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        [Output("stageInvitationId")]
        public Output<string> StageInvitationId { get; private set; } = null!;


        /// <summary>
        /// Create a StageInvitation resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public StageInvitation(string name, StageInvitationArgs? args = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageInvitation:StageInvitation", name, args ?? new StageInvitationArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private StageInvitation(string name, Input<string> id, StageInvitationState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageInvitation:StageInvitation", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
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
        /// Get an existing StageInvitation resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static StageInvitation Get(string name, Input<string> id, StageInvitationState? state = null, CustomResourceOptions? options = null)
        {
            return new StageInvitation(name, id, state, options);
        }
    }

    public sealed class StageInvitationArgs : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Defaults to `false`.
        /// </summary>
        [Input("continueFlowWithoutInvitation")]
        public Input<bool>? ContinueFlowWithoutInvitation { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("stageInvitationId")]
        public Input<string>? StageInvitationId { get; set; }

        public StageInvitationArgs()
        {
        }
        public static new StageInvitationArgs Empty => new StageInvitationArgs();
    }

    public sealed class StageInvitationState : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Defaults to `false`.
        /// </summary>
        [Input("continueFlowWithoutInvitation")]
        public Input<bool>? ContinueFlowWithoutInvitation { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("stageInvitationId")]
        public Input<string>? StageInvitationId { get; set; }

        public StageInvitationState()
        {
        }
        public static new StageInvitationState Empty => new StageInvitationState();
    }
}
