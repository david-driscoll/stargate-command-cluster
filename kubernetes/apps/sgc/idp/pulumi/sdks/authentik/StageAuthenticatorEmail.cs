// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/stageAuthenticatorEmail:StageAuthenticatorEmail")]
    public partial class StageAuthenticatorEmail : global::Pulumi.CustomResource
    {
        [Output("configureFlow")]
        public Output<string?> ConfigureFlow { get; private set; } = null!;

        [Output("friendlyName")]
        public Output<string?> FriendlyName { get; private set; } = null!;

        /// <summary>
        /// Defaults to `system@authentik.local`.
        /// </summary>
        [Output("fromAddress")]
        public Output<string?> FromAddress { get; private set; } = null!;

        /// <summary>
        /// Defaults to `localhost`.
        /// </summary>
        [Output("host")]
        public Output<string?> Host { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        [Output("password")]
        public Output<string?> Password { get; private set; } = null!;

        /// <summary>
        /// Defaults to `25`.
        /// </summary>
        [Output("port")]
        public Output<double?> Port { get; private set; } = null!;

        [Output("stageAuthenticatorEmailId")]
        public Output<string> StageAuthenticatorEmailId { get; private set; } = null!;

        /// <summary>
        /// Defaults to `authentik`.
        /// </summary>
        [Output("subject")]
        public Output<string?> Subject { get; private set; } = null!;

        /// <summary>
        /// Defaults to `email/password_reset.html`.
        /// </summary>
        [Output("template")]
        public Output<string?> Template { get; private set; } = null!;

        /// <summary>
        /// Defaults to `30`.
        /// </summary>
        [Output("timeout")]
        public Output<double?> Timeout { get; private set; } = null!;

        /// <summary>
        /// Defaults to `minutes=30`.
        /// </summary>
        [Output("tokenExpiry")]
        public Output<string?> TokenExpiry { get; private set; } = null!;

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Output("useGlobalSettings")]
        public Output<bool?> UseGlobalSettings { get; private set; } = null!;

        [Output("useSsl")]
        public Output<bool?> UseSsl { get; private set; } = null!;

        [Output("useTls")]
        public Output<bool?> UseTls { get; private set; } = null!;

        [Output("username")]
        public Output<string?> Username { get; private set; } = null!;


        /// <summary>
        /// Create a StageAuthenticatorEmail resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public StageAuthenticatorEmail(string name, StageAuthenticatorEmailArgs? args = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageAuthenticatorEmail:StageAuthenticatorEmail", name, args ?? new StageAuthenticatorEmailArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private StageAuthenticatorEmail(string name, Input<string> id, StageAuthenticatorEmailState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageAuthenticatorEmail:StageAuthenticatorEmail", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
        {
        }

        private static CustomResourceOptions MakeResourceOptions(CustomResourceOptions? options, Input<string>? id)
        {
            var defaultOptions = new CustomResourceOptions
            {
                Version = Utilities.Version,
                AdditionalSecretOutputs =
                {
                    "password",
                },
            };
            var merged = CustomResourceOptions.Merge(defaultOptions, options);
            // Override the ID if one was specified for consistency with other language SDKs.
            merged.Id = id ?? merged.Id;
            return merged;
        }
        /// <summary>
        /// Get an existing StageAuthenticatorEmail resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static StageAuthenticatorEmail Get(string name, Input<string> id, StageAuthenticatorEmailState? state = null, CustomResourceOptions? options = null)
        {
            return new StageAuthenticatorEmail(name, id, state, options);
        }
    }

    public sealed class StageAuthenticatorEmailArgs : global::Pulumi.ResourceArgs
    {
        [Input("configureFlow")]
        public Input<string>? ConfigureFlow { get; set; }

        [Input("friendlyName")]
        public Input<string>? FriendlyName { get; set; }

        /// <summary>
        /// Defaults to `system@authentik.local`.
        /// </summary>
        [Input("fromAddress")]
        public Input<string>? FromAddress { get; set; }

        /// <summary>
        /// Defaults to `localhost`.
        /// </summary>
        [Input("host")]
        public Input<string>? Host { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("password")]
        private Input<string>? _password;
        public Input<string>? Password
        {
            get => _password;
            set
            {
                var emptySecret = Output.CreateSecret(0);
                _password = Output.Tuple<Input<string>?, int>(value, emptySecret).Apply(t => t.Item1);
            }
        }

        /// <summary>
        /// Defaults to `25`.
        /// </summary>
        [Input("port")]
        public Input<double>? Port { get; set; }

        [Input("stageAuthenticatorEmailId")]
        public Input<string>? StageAuthenticatorEmailId { get; set; }

        /// <summary>
        /// Defaults to `authentik`.
        /// </summary>
        [Input("subject")]
        public Input<string>? Subject { get; set; }

        /// <summary>
        /// Defaults to `email/password_reset.html`.
        /// </summary>
        [Input("template")]
        public Input<string>? Template { get; set; }

        /// <summary>
        /// Defaults to `30`.
        /// </summary>
        [Input("timeout")]
        public Input<double>? Timeout { get; set; }

        /// <summary>
        /// Defaults to `minutes=30`.
        /// </summary>
        [Input("tokenExpiry")]
        public Input<string>? TokenExpiry { get; set; }

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("useGlobalSettings")]
        public Input<bool>? UseGlobalSettings { get; set; }

        [Input("useSsl")]
        public Input<bool>? UseSsl { get; set; }

        [Input("useTls")]
        public Input<bool>? UseTls { get; set; }

        [Input("username")]
        public Input<string>? Username { get; set; }

        public StageAuthenticatorEmailArgs()
        {
        }
        public static new StageAuthenticatorEmailArgs Empty => new StageAuthenticatorEmailArgs();
    }

    public sealed class StageAuthenticatorEmailState : global::Pulumi.ResourceArgs
    {
        [Input("configureFlow")]
        public Input<string>? ConfigureFlow { get; set; }

        [Input("friendlyName")]
        public Input<string>? FriendlyName { get; set; }

        /// <summary>
        /// Defaults to `system@authentik.local`.
        /// </summary>
        [Input("fromAddress")]
        public Input<string>? FromAddress { get; set; }

        /// <summary>
        /// Defaults to `localhost`.
        /// </summary>
        [Input("host")]
        public Input<string>? Host { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("password")]
        private Input<string>? _password;
        public Input<string>? Password
        {
            get => _password;
            set
            {
                var emptySecret = Output.CreateSecret(0);
                _password = Output.Tuple<Input<string>?, int>(value, emptySecret).Apply(t => t.Item1);
            }
        }

        /// <summary>
        /// Defaults to `25`.
        /// </summary>
        [Input("port")]
        public Input<double>? Port { get; set; }

        [Input("stageAuthenticatorEmailId")]
        public Input<string>? StageAuthenticatorEmailId { get; set; }

        /// <summary>
        /// Defaults to `authentik`.
        /// </summary>
        [Input("subject")]
        public Input<string>? Subject { get; set; }

        /// <summary>
        /// Defaults to `email/password_reset.html`.
        /// </summary>
        [Input("template")]
        public Input<string>? Template { get; set; }

        /// <summary>
        /// Defaults to `30`.
        /// </summary>
        [Input("timeout")]
        public Input<double>? Timeout { get; set; }

        /// <summary>
        /// Defaults to `minutes=30`.
        /// </summary>
        [Input("tokenExpiry")]
        public Input<string>? TokenExpiry { get; set; }

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("useGlobalSettings")]
        public Input<bool>? UseGlobalSettings { get; set; }

        [Input("useSsl")]
        public Input<bool>? UseSsl { get; set; }

        [Input("useTls")]
        public Input<bool>? UseTls { get; set; }

        [Input("username")]
        public Input<string>? Username { get; set; }

        public StageAuthenticatorEmailState()
        {
        }
        public static new StageAuthenticatorEmailState Empty => new StageAuthenticatorEmailState();
    }
}
