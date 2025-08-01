// *** WARNING: this file was generated by crd2pulumi. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Kubernetes.Types.Outputs.Meta.V1
{

    /// <summary>
    /// OwnerReference contains enough information to let you identify an owning object. An owning object must be in the same namespace as the dependent, or be cluster-scoped, so there is no namespace field.
    /// </summary>
    [OutputType]
    public sealed class OwnerReference
    {
        /// <summary>
        /// API version of the referent.
        /// </summary>
        public readonly string ApiVersion;
        /// <summary>
        /// If true, AND if the owner has the "foregroundDeletion" finalizer, then the owner cannot be deleted from the key-value store until this reference is removed. See https://kubernetes.io/docs/concepts/architecture/garbage-collection/#foreground-deletion for how the garbage collector interacts with this field and enforces the foreground deletion. Defaults to false. To set this field, a user needs "delete" permission of the owner, otherwise 422 (Unprocessable Entity) will be returned.
        /// </summary>
        public readonly bool BlockOwnerDeletion;
        /// <summary>
        /// If true, this reference points to the managing controller.
        /// </summary>
        public readonly bool Controller;
        /// <summary>
        /// Kind of the referent. More info: https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#types-kinds
        /// </summary>
        public readonly string Kind;
        /// <summary>
        /// Name of the referent. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/names#names
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// UID of the referent. More info: https://kubernetes.io/docs/concepts/overview/working-with-objects/names#uids
        /// </summary>
        public readonly string Uid;

        [OutputConstructor]
        private OwnerReference(
            string apiVersion,

            bool blockOwnerDeletion,

            bool controller,

            string kind,

            string name,

            string uid)
        {
            ApiVersion = apiVersion;
            BlockOwnerDeletion = blockOwnerDeletion;
            Controller = controller;
            Kind = kind;
            Name = name;
            Uid = uid;
        }
    }
}
