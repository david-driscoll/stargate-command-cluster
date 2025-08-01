// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    [AuthentikResourceType("authentik:index/stageCaptcha:StageCaptcha")]
    public partial class StageCaptcha : global::Pulumi.CustomResource
    {
        /// <summary>
        /// Defaults to `https://www.recaptcha.net/recaptcha/api/siteverify`.
        /// </summary>
        [Output("apiUrl")]
        public Output<string?> ApiUrl { get; private set; } = null!;

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Output("errorOnInvalidScore")]
        public Output<bool?> ErrorOnInvalidScore { get; private set; } = null!;

        /// <summary>
        /// Defaults to `false`.
        /// </summary>
        [Output("interactive")]
        public Output<bool?> Interactive { get; private set; } = null!;

        /// <summary>
        /// Defaults to `https://www.recaptcha.net/recaptcha/api.js`.
        /// </summary>
        [Output("jsUrl")]
        public Output<string?> JsUrl { get; private set; } = null!;

        [Output("name")]
        public Output<string> Name { get; private set; } = null!;

        [Output("privateKey")]
        public Output<string> PrivateKey { get; private set; } = null!;

        [Output("publicKey")]
        public Output<string> PublicKey { get; private set; } = null!;

        /// <summary>
        /// Defaults to `0.5`.
        /// </summary>
        [Output("scoreMaxThreshold")]
        public Output<double?> ScoreMaxThreshold { get; private set; } = null!;

        /// <summary>
        /// Defaults to `1`.
        /// </summary>
        [Output("scoreMinThreshold")]
        public Output<double?> ScoreMinThreshold { get; private set; } = null!;

        [Output("stageCaptchaId")]
        public Output<string> StageCaptchaId { get; private set; } = null!;


        /// <summary>
        /// Create a StageCaptcha resource with the given unique name, arguments, and options.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resource</param>
        /// <param name="args">The arguments used to populate this resource's properties</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public StageCaptcha(string name, StageCaptchaArgs args, CustomResourceOptions? options = null)
            : base("authentik:index/stageCaptcha:StageCaptcha", name, args ?? new StageCaptchaArgs(), MakeResourceOptions(options, ""), Utilities.PackageParameterization())
        {
        }

        private StageCaptcha(string name, Input<string> id, StageCaptchaState? state = null, CustomResourceOptions? options = null)
            : base("authentik:index/stageCaptcha:StageCaptcha", name, state, MakeResourceOptions(options, id), Utilities.PackageParameterization())
        {
        }

        private static CustomResourceOptions MakeResourceOptions(CustomResourceOptions? options, Input<string>? id)
        {
            var defaultOptions = new CustomResourceOptions
            {
                Version = Utilities.Version,
                AdditionalSecretOutputs =
                {
                    "privateKey",
                },
            };
            var merged = CustomResourceOptions.Merge(defaultOptions, options);
            // Override the ID if one was specified for consistency with other language SDKs.
            merged.Id = id ?? merged.Id;
            return merged;
        }
        /// <summary>
        /// Get an existing StageCaptcha resource's state with the given name, ID, and optional extra
        /// properties used to qualify the lookup.
        /// </summary>
        ///
        /// <param name="name">The unique name of the resulting resource.</param>
        /// <param name="id">The unique provider ID of the resource to lookup.</param>
        /// <param name="state">Any extra arguments used during the lookup.</param>
        /// <param name="options">A bag of options that control this resource's behavior</param>
        public static StageCaptcha Get(string name, Input<string> id, StageCaptchaState? state = null, CustomResourceOptions? options = null)
        {
            return new StageCaptcha(name, id, state, options);
        }
    }

    public sealed class StageCaptchaArgs : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Defaults to `https://www.recaptcha.net/recaptcha/api/siteverify`.
        /// </summary>
        [Input("apiUrl")]
        public Input<string>? ApiUrl { get; set; }

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("errorOnInvalidScore")]
        public Input<bool>? ErrorOnInvalidScore { get; set; }

        /// <summary>
        /// Defaults to `false`.
        /// </summary>
        [Input("interactive")]
        public Input<bool>? Interactive { get; set; }

        /// <summary>
        /// Defaults to `https://www.recaptcha.net/recaptcha/api.js`.
        /// </summary>
        [Input("jsUrl")]
        public Input<string>? JsUrl { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("privateKey", required: true)]
        private Input<string>? _privateKey;
        public Input<string>? PrivateKey
        {
            get => _privateKey;
            set
            {
                var emptySecret = Output.CreateSecret(0);
                _privateKey = Output.Tuple<Input<string>?, int>(value, emptySecret).Apply(t => t.Item1);
            }
        }

        [Input("publicKey", required: true)]
        public Input<string> PublicKey { get; set; } = null!;

        /// <summary>
        /// Defaults to `0.5`.
        /// </summary>
        [Input("scoreMaxThreshold")]
        public Input<double>? ScoreMaxThreshold { get; set; }

        /// <summary>
        /// Defaults to `1`.
        /// </summary>
        [Input("scoreMinThreshold")]
        public Input<double>? ScoreMinThreshold { get; set; }

        [Input("stageCaptchaId")]
        public Input<string>? StageCaptchaId { get; set; }

        public StageCaptchaArgs()
        {
        }
        public static new StageCaptchaArgs Empty => new StageCaptchaArgs();
    }

    public sealed class StageCaptchaState : global::Pulumi.ResourceArgs
    {
        /// <summary>
        /// Defaults to `https://www.recaptcha.net/recaptcha/api/siteverify`.
        /// </summary>
        [Input("apiUrl")]
        public Input<string>? ApiUrl { get; set; }

        /// <summary>
        /// Defaults to `true`.
        /// </summary>
        [Input("errorOnInvalidScore")]
        public Input<bool>? ErrorOnInvalidScore { get; set; }

        /// <summary>
        /// Defaults to `false`.
        /// </summary>
        [Input("interactive")]
        public Input<bool>? Interactive { get; set; }

        /// <summary>
        /// Defaults to `https://www.recaptcha.net/recaptcha/api.js`.
        /// </summary>
        [Input("jsUrl")]
        public Input<string>? JsUrl { get; set; }

        [Input("name")]
        public Input<string>? Name { get; set; }

        [Input("privateKey")]
        private Input<string>? _privateKey;
        public Input<string>? PrivateKey
        {
            get => _privateKey;
            set
            {
                var emptySecret = Output.CreateSecret(0);
                _privateKey = Output.Tuple<Input<string>?, int>(value, emptySecret).Apply(t => t.Item1);
            }
        }

        [Input("publicKey")]
        public Input<string>? PublicKey { get; set; }

        /// <summary>
        /// Defaults to `0.5`.
        /// </summary>
        [Input("scoreMaxThreshold")]
        public Input<double>? ScoreMaxThreshold { get; set; }

        /// <summary>
        /// Defaults to `1`.
        /// </summary>
        [Input("scoreMinThreshold")]
        public Input<double>? ScoreMinThreshold { get; set; }

        [Input("stageCaptchaId")]
        public Input<string>? StageCaptchaId { get; set; }

        public StageCaptchaState()
        {
        }
        public static new StageCaptchaState Empty => new StageCaptchaState();
    }
}
