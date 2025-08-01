// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    public static class GetPropertyMappingProviderScim
    {
        public static Task<GetPropertyMappingProviderScimResult> InvokeAsync(GetPropertyMappingProviderScimArgs? args = null, InvokeOptions? options = null)
            => global::Pulumi.Deployment.Instance.InvokeAsync<GetPropertyMappingProviderScimResult>("authentik:index/getPropertyMappingProviderScim:getPropertyMappingProviderScim", args ?? new GetPropertyMappingProviderScimArgs(), options.WithDefaults(), Utilities.PackageParameterization());

        public static Output<GetPropertyMappingProviderScimResult> Invoke(GetPropertyMappingProviderScimInvokeArgs? args = null, InvokeOptions? options = null)
            => global::Pulumi.Deployment.Instance.Invoke<GetPropertyMappingProviderScimResult>("authentik:index/getPropertyMappingProviderScim:getPropertyMappingProviderScim", args ?? new GetPropertyMappingProviderScimInvokeArgs(), options.WithDefaults());

        public static Output<GetPropertyMappingProviderScimResult> Invoke(GetPropertyMappingProviderScimInvokeArgs args, InvokeOutputOptions options)
            => global::Pulumi.Deployment.Instance.Invoke<GetPropertyMappingProviderScimResult>("authentik:index/getPropertyMappingProviderScim:getPropertyMappingProviderScim", args ?? new GetPropertyMappingProviderScimInvokeArgs(), options.WithDefaults());
    }


    public sealed class GetPropertyMappingProviderScimArgs : global::Pulumi.InvokeArgs
    {
        [Input("id")]
        public string? Id { get; set; }

        [Input("ids")]
        private List<string>? _ids;
        public List<string> Ids
        {
            get => _ids ?? (_ids = new List<string>());
            set => _ids = value;
        }

        [Input("managed")]
        public string? Managed { get; set; }

        [Input("managedLists")]
        private List<string>? _managedLists;
        public List<string> ManagedLists
        {
            get => _managedLists ?? (_managedLists = new List<string>());
            set => _managedLists = value;
        }

        [Input("name")]
        public string? Name { get; set; }

        public GetPropertyMappingProviderScimArgs()
        {
        }
        public static new GetPropertyMappingProviderScimArgs Empty => new GetPropertyMappingProviderScimArgs();
    }

    public sealed class GetPropertyMappingProviderScimInvokeArgs : global::Pulumi.InvokeArgs
    {
        [Input("id")]
        public Input<string>? Id { get; set; }

        [Input("ids")]
        private InputList<string>? _ids;
        public InputList<string> Ids
        {
            get => _ids ?? (_ids = new InputList<string>());
            set => _ids = value;
        }

        [Input("managed")]
        public Input<string>? Managed { get; set; }

        [Input("managedLists")]
        private InputList<string>? _managedLists;
        public InputList<string> ManagedLists
        {
            get => _managedLists ?? (_managedLists = new InputList<string>());
            set => _managedLists = value;
        }

        [Input("name")]
        public Input<string>? Name { get; set; }

        public GetPropertyMappingProviderScimInvokeArgs()
        {
        }
        public static new GetPropertyMappingProviderScimInvokeArgs Empty => new GetPropertyMappingProviderScimInvokeArgs();
    }


    [OutputType]
    public sealed class GetPropertyMappingProviderScimResult
    {
        public readonly string Expression;
        public readonly string Id;
        public readonly ImmutableArray<string> Ids;
        public readonly string? Managed;
        public readonly ImmutableArray<string> ManagedLists;
        public readonly string? Name;

        [OutputConstructor]
        private GetPropertyMappingProviderScimResult(
            string expression,

            string id,

            ImmutableArray<string> ids,

            string? managed,

            ImmutableArray<string> managedLists,

            string? name)
        {
            Expression = expression;
            Id = id;
            Ids = ids;
            Managed = managed;
            ManagedLists = managedLists;
            Name = name;
        }
    }
}
