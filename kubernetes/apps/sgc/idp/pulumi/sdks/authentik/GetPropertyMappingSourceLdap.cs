// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik
{
    public static class GetPropertyMappingSourceLdap
    {
        public static Task<GetPropertyMappingSourceLdapResult> InvokeAsync(GetPropertyMappingSourceLdapArgs? args = null, InvokeOptions? options = null)
            => global::Pulumi.Deployment.Instance.InvokeAsync<GetPropertyMappingSourceLdapResult>("authentik:index/getPropertyMappingSourceLdap:getPropertyMappingSourceLdap", args ?? new GetPropertyMappingSourceLdapArgs(), options.WithDefaults(), Utilities.PackageParameterization());

        public static Output<GetPropertyMappingSourceLdapResult> Invoke(GetPropertyMappingSourceLdapInvokeArgs? args = null, InvokeOptions? options = null)
            => global::Pulumi.Deployment.Instance.Invoke<GetPropertyMappingSourceLdapResult>("authentik:index/getPropertyMappingSourceLdap:getPropertyMappingSourceLdap", args ?? new GetPropertyMappingSourceLdapInvokeArgs(), options.WithDefaults());

        public static Output<GetPropertyMappingSourceLdapResult> Invoke(GetPropertyMappingSourceLdapInvokeArgs args, InvokeOutputOptions options)
            => global::Pulumi.Deployment.Instance.Invoke<GetPropertyMappingSourceLdapResult>("authentik:index/getPropertyMappingSourceLdap:getPropertyMappingSourceLdap", args ?? new GetPropertyMappingSourceLdapInvokeArgs(), options.WithDefaults());
    }


    public sealed class GetPropertyMappingSourceLdapArgs : global::Pulumi.InvokeArgs
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

        public GetPropertyMappingSourceLdapArgs()
        {
        }
        public static new GetPropertyMappingSourceLdapArgs Empty => new GetPropertyMappingSourceLdapArgs();
    }

    public sealed class GetPropertyMappingSourceLdapInvokeArgs : global::Pulumi.InvokeArgs
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

        public GetPropertyMappingSourceLdapInvokeArgs()
        {
        }
        public static new GetPropertyMappingSourceLdapInvokeArgs Empty => new GetPropertyMappingSourceLdapInvokeArgs();
    }


    [OutputType]
    public sealed class GetPropertyMappingSourceLdapResult
    {
        public readonly string Expression;
        public readonly string Id;
        public readonly ImmutableArray<string> Ids;
        public readonly string? Managed;
        public readonly ImmutableArray<string> ManagedLists;
        public readonly string? Name;

        [OutputConstructor]
        private GetPropertyMappingSourceLdapResult(
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
