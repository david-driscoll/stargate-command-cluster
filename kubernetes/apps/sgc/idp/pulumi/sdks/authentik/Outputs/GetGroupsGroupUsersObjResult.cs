// *** WARNING: this file was generated by pulumi-language-dotnet. ***
// *** Do not edit by hand unless you're certain you know what you are doing! ***

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Serialization;

namespace Pulumi.Authentik.Outputs
{

    [OutputType]
    public sealed class GetGroupsGroupUsersObjResult
    {
        public readonly string Attributes;
        public readonly string Email;
        public readonly bool IsActive;
        public readonly string LastLogin;
        public readonly string Name;
        public readonly double Pk;
        public readonly string Uid;
        public readonly string Username;

        [OutputConstructor]
        private GetGroupsGroupUsersObjResult(
            string attributes,

            string email,

            bool isActive,

            string lastLogin,

            string name,

            double pk,

            string uid,

            string username)
        {
            Attributes = attributes;
            Email = email;
            IsActive = isActive;
            LastLogin = lastLogin;
            Name = name;
            Pk = pk;
            Uid = uid;
            Username = username;
        }
    }
}
