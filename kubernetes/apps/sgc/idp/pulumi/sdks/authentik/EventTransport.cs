// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/eventTransport:EventTransport")]
    public partial class EventTransport : global::Pulumi.CustomResource
    {
        [Output("eventTransportId")]
        public Output<string> EventTransportId { get; private set; } = null!;

        /// <summary>
        /// Allowed values: - `local` - `webhook` - `webhook_slack` - `email`
        /// </summary>
        [Output("mode")]
        public Output<string> Mode { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Output("sendOnce")]
        public Output<bool?> SendOnce { get; private set; } = null!;

        [Output("webhookMappingBody")]
        public Output<string?> WebhookMappingBody { get; private set; } = null!;

        [Output("webhookMappingHeaders")]
        public Output<string?> WebhookMappingHeaders { get; private set; } = null!;

        [Output("webhookUrl")]
        public Output<string?> WebhookUrl { get; private set; } = null!;


        /// <summary>
        /// Create a EventTransport resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public EventTransport(string name, EventTransportArgs args, CustomResourceOptions? options = null)
            : base("authentik:index/eventTransport:EventTransport", name, args ?? new EventTransportArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private EventTransport(string name, Input<string> id, EventTransportState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/eventTransport:EventTransport", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
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
        /// Get an existing EventTransport resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static EventTransport Get(string name, Input<string> id, EventTransportState? state = null, CustomResourceOptions? options = null)
        {
            return new EventTransport(name, id, state, options);
        }
    }

    public sealed class EventTransportArgs : global::Pulumi.ResourceArgs
    {
        [Input("eventTransportId")]
        public Input<string>? EventTransportId { get; set; }

        /// <summary>
        /// Allowed values: - `local` - `webhook` - `webhook_slack` - `email`
        /// </summary>
        [Input("mode", required: true)]
        public Input<string> Mode { get; set; } = null!;

        [Input("name")]
        public Input<string>? Name { get; set; }

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("sendOnce")]
        public Input<bool>? SendOnce { get; set; }

        [Input("webhookMappingBody")]
        public Input<string>? WebhookMappingBody { get; set; }

        [Input("webhookMappingHeaders")]
        public Input<string>? WebhookMappingHeaders { get; set; }

        [Input("webhookUrl")]
        public Input<string>? WebhookUrl { get; set; }

        public EventTransportArgs()
        {
        }
        public static new EventTransportArgs Empty => new EventTransportArgs();
    }

    public sealed class EventTransportState : global::Pulumi.ResourceArgs
    {
        [Input("eventTransportId")]
        public Input<string>? EventTransportId { get; set; }

        /// <summary>
        /// Allowed values: - `local` - `webhook` - `webhook_slack` - `email`
        /// </summary>
        [Input("mode")]
        public Input<string>? Mode { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("sendOnce")]
        public Input<bool>? SendOnce { get; set; }

        [Input("webhookMappingBody")]
        public Input<string>? WebhookMappingBody { get; set; }

        [Input("webhookMappingHeaders")]
        public Input<string>? WebhookMappingHeaders { get; set; }

        [Input("webhookUrl")]
        public Input<string>? WebhookUrl { get; set; }

        public EventTransportState()
        {
        }
        public static new EventTransportState Empty => new EventTransportState();
    }
}
