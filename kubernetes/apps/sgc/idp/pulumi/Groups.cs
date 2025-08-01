// TODO: Pull from tailscale???
using System.Collections.Generic;
using Humanizer;
using Pulumi;
using Pulumi.Authentik;

static class Constants
{
  public class Roles
  {
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Family = "Family";
    public const string Friend = "Friend";
    public const string MediaManager = "Media Manager";
  }
}

class AuthentikGroups : Pulumi.ComponentResource
{
  private readonly IReadOnlyCollection<(string GroupName, string? ParentName)> initialGroups = [
    (Constants.Roles.User, null),
    (Constants.Roles.Admin, Constants.Roles.User),
    (Constants.Roles.Family, Constants.Roles.User),
    (Constants.Roles.Friend, Constants.Roles.User),
    (Constants.Roles.MediaManager, Constants.Roles.User)
  ];
  public AuthentikGroups(ComponentResourceOptions? options = null) : base("custom:resource:AuthentikGroups", "authentik-groups", options)
  {
    foreach (var group in initialGroups)
    {

      var roleResource = new RbacRole(group.GroupName.ToLowerInvariant().Dasherize(), new()
      {
        Name = group.GroupName,
      });
      roles[group.GroupName] = roleResource;
      var groupResource = new Group(group.GroupName.ToLowerInvariant().Dasherize(), new()
      {
        Name = group.GroupName,
        Roles = [roleResource.Id],
        IsSuperuser = group.GroupName == "Admin",
        Parent = groups.TryGetValue(group.ParentName ?? "", out var parentGroup) ? parentGroup.Id : Output.Create((string)null),
      });
      groups[group.GroupName] = groupResource;
    }
  }

  private Dictionary<string, Group> groups = new();
  public IReadOnlyDictionary<string, Group> Groups => groups;
  private Dictionary<string, RbacRole> roles = new();
  public IReadOnlyDictionary<string, RbacRole> Roles => roles;
}

// class AuthentikPrompts : Pulumi.ComponentResource
// {
//   public AuthentikPrompts(ComponentResourceOptions? options = null) : base("custom:resource:AuthentikPrompts", "authentik-prompts", options)
//   {

//     'default-password-change-field-password',
//     'default-password-change-field-password-repeat',
//     'default-source-enrollment-field-username',
//     'default-user-settings-field-email',
//     'default-user-settings-field-locale',
//     'default-user-settings-field-name',
//     'default-user-settings-field-username'
//   }

//   private static Output<StagePrompt> GetPromptLazy(string slug)
//   {
//     return Output.Create(slug).Apply(s => StagePrompt.Invoke(new() { Slug = s }));
//   }
// }

static class Stages
{
  public static Output<GetStageResult> DefaultAuthenticationIdentification { get => field ??= GetStageLazy("default-authentication-identification"); }
  public static Output<GetStageResult> DefaultAuthenticationLogin { get => field ??= GetStageLazy("default-authentication-login");  }
  public static Output<GetStageResult> DefaultAuthenticationMfaValidation { get => field ??= GetStageLazy("default-authentication-mfa-validation"); }
  public static Output<GetStageResult> DefaultAuthenticationPassword { get => field ??= GetStageLazy("default-authentication-password"); }
  public static Output<GetStageResult> DefaultAuthenticatorStaticSetup { get => field ??= GetStageLazy("default-authenticator-static-setup"); }
  public static Output<GetStageResult> DefaultAuthenticatorTotpSetup { get => field ??= GetStageLazy("default-authenticator-totp-setup"); }
  public static Output<GetStageResult> DefaultAuthenticatorWebauthnSetup { get => field ??= GetStageLazy("default-authenticator-webauthn-setup"); }
  public static Output<GetStageResult> DefaultInvalidationLogout { get => field ??= GetStageLazy("default-invalidation-logout"); }
  public static Output<GetStageResult> DefaultPasswordChangePrompt { get => field ??= GetStageLazy("default-password-change-prompt"); }
  public static Output<GetStageResult> DefaultPasswordChangeWrite { get => field ??= GetStageLazy("default-password-change-write"); }
  public static Output<GetStageResult> DefaultProviderAuthorizationConsent { get => field ??= GetStageLazy("default-provider-authorization-consent"); }
  public static Output<GetStageResult> DefaultSourceAuthenticationLogin { get => field ??= GetStageLazy("default-source-authentication-login"); }
  public static Output<GetStageResult> DefaultSourceEnrollmentLogin { get => field ??= GetStageLazy("default-source-enrollment-login"); }
  public static Output<GetStageResult> DefaultSourceEnrollmentPrompt { get => field ??= GetStageLazy("default-source-enrollment-prompt"); }
  public static Output<GetStageResult> DefaultSourceEnrollmentWrite { get => field ??= GetStageLazy("default-source-enrollment-write"); }
  public static Output<GetStageResult> DefaultUserSettings { get => field ??= GetStageLazy("default-user-settings"); }
  public static Output<GetStageResult> DefaultUserSettingsWrite { get => field ??= GetStageLazy("default-user-settings-write"); }

  private static Output<GetStageResult> GetStageLazy(string slug)
  {
    return Output.Create(slug).Apply(s => GetStage.Invoke(new() { Name = s }));
  }
}

static class Flows
{
  private static Output<GetFlowResult> GetFlowLazy(string slug)
  {
    return Output.Create(slug).Apply(s => GetFlow.Invoke(new() { Slug = s }));
  }

  public static Output<GetFlowResult> DefaultAuthenticationFlow { get => GetFlowLazy("default-authentication-flow"); }
  public static Output<GetFlowResult> DefaultSourceAuthentication { get => GetFlowLazy("default-source-authentication"); }
  public static Output<GetFlowResult> DefaultProviderAuthorizationExplicitConsent { get => GetFlowLazy("default-provider-authorization-explicit-consent"); }
  public static Output<GetFlowResult> DefaultProviderAuthorizationImplicitConsent { get => GetFlowLazy("default-provider-authorization-implicit-consent"); }
  public static Output<GetFlowResult> DefaultSourceEnrollment { get => GetFlowLazy("default-source-enrollment"); }
  public static Output<GetFlowResult> DefaultInvalidationFlow { get => GetFlowLazy("default-invalidation-flow"); }
  public static Output<GetFlowResult> DefaultProviderInvalidationFlow { get => GetFlowLazy("default-provider-invalidation-flow"); }
  public static Output<GetFlowResult> DefaultAuthenticatorStaticSetup { get => GetFlowLazy("default-authenticator-static-setup"); }
  public static Output<GetFlowResult> DefaultAuthenticatorTotpSetup { get => GetFlowLazy("default-authenticator-totp-setup"); }
  public static Output<GetFlowResult> DefaultAuthenticatorWebauthnSetup { get => GetFlowLazy("default-authenticator-webauthn-setup"); }
  public static Output<GetFlowResult> DefaultPasswordChange { get => GetFlowLazy("default-password-change"); }
  public static Output<GetFlowResult> DefaultSourcePreAuthentication { get => GetFlowLazy("default-source-pre-authentication"); }
  public static Output<GetFlowResult> DefaultUserSettingsFlow { get => GetFlowLazy("default-user-settings-flow"); }
}
