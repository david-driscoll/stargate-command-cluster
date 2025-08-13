using Humanizer;
using Models.ApplicationDefinition;
using Pulumi;
using Pulumi.Authentik;

namespace applications;

public class LocalFlows
{
  public LocalFlows(ClusterDefinition clusterDefinition)
  {
    var mfa = new StageAuthenticatorValidate("authentication-mfa-validation", new()
    {
      DeviceClasses = { "static", "totp", "webauthn" },
      NotConfiguredAction = "skip",
      LastAuthThreshold = "days=1",
      WebauthnUserVerification = "preferred",
    });
  }

  public static void CreateRecoveryFlow(ClusterDefinition clusterDefinition)
  {
    /*
    ## Authorization stages
    resource "authentik_stage_identification" "authentication-identification" {
      name                      = "authentication-identification"
      user_fields               = ["username", "email"]
      case_insensitive_matching = false
      show_source_labels        = true
      show_matched_user         = false
      password_stage            = authentik_stage_password.authentication-password.id
      recovery_flow             = authentik_flow.recovery.uuid
      sources                   = [authentik_source_oauth.discord.uuid]
    }

    resource "authentik_stage_password" "authentication-password" {
      name                          = "authentication-password"
      backends                      = ["authentik.core.auth.InbuiltBackend"]
      failed_attempts_before_cancel = 3
    }

    resource "authentik_stage_authenticator_validate" "authentication-mfa-validation" {
      name                  = "authentication-mfa-validation"
      device_classes        = ["static", "totp", "webauthn"]
      not_configured_action = "skip"
    }

    resource "authentik_stage_user_login" "authentication-login" {
      name = "authentication-login"
    }

    ## Invalidation stages
    resource "authentik_stage_user_logout" "invalidation-logout" {
      name = "invalidation-logout"
    }

    ## Recovery stages
    resource "authentik_stage_identification" "recovery-identification" {
      name                      = "recovery-identification"
      user_fields               = ["username", "email"]
      case_insensitive_matching = false
      show_source_labels        = false
      show_matched_user         = false
    }

    resource "authentik_stage_email" "recovery-email" {
      name                     = "recovery-email"
      activate_user_on_success = true
      use_global_settings      = true
      template                 = "email/password_reset.html"
      subject                  = "Password recovery"
    }

    resource "authentik_stage_prompt" "password-change-prompt" {
      name = "password-change-prompt"
      fields = [
        resource.authentik_stage_prompt_field.password.id,
        resource.authentik_stage_prompt_field.password-repeat.id
      ]
      validation_policies = [
        resource.authentik_policy_password.password-complexity.id
      ]
    }

    resource "authentik_stage_user_write" "password-change-write" {
      name                     = "password-change-write"
      create_users_as_inactive = false
    }

    ## Invitation stages
    resource "authentik_stage_invitation" "enrollment-invitation" {
      name                             = "enrollment-invitation"
      continue_flow_without_invitation = false
    }

    resource "authentik_stage_prompt" "source-enrollment-prompt" {
      name = "source-enrollment-prompt"
      fields = [
        resource.authentik_stage_prompt_field.username.id,
        resource.authentik_stage_prompt_field.name.id,
        resource.authentik_stage_prompt_field.email.id,
        resource.authentik_stage_prompt_field.password.id,
        resource.authentik_stage_prompt_field.password-repeat.id
      ]
      validation_policies = [
        resource.authentik_policy_password.password-complexity.id
      ]
    }

    resource "authentik_stage_user_write" "enrollment-user-write" {
      name                     = "enrollment-user-write"
      create_users_as_inactive = false
      create_users_group       = authentik_group.default["users"].id
    }

    resource "authentik_stage_user_login" "source-enrollment-login" {
      name             = "source-enrollment-login"
      session_duration = "seconds=0"
    }

    ## User settings stages
    resource "authentik_stage_prompt" "user-settings" {
      name = "user-settings"
      fields = [
        resource.authentik_stage_prompt_field.username.id,
        resource.authentik_stage_prompt_field.name.id,
        resource.authentik_stage_prompt_field.email.id,
        resource.authentik_stage_prompt_field.locale.id
      ]

      validation_policies = [
        resource.authentik_policy_expression.user-settings-authorization.id
      ]

    }

    resource "authentik_stage_user_write" "user-settings-write" {
      name                     = "user-settings-write"
      create_users_as_inactive = false
    }
    */

    var flow = new Flow($"{clusterDefinition.Metadata.Name}-authentication-flow", new()
    {
      Name = clusterDefinition.Spec.Name,
      Title = $"Welcome to {clusterDefinition.Spec.Name}!",
      Slug = $"{clusterDefinition.Metadata.Name}-authentication-flow",
      Layout = "sidebar_left",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",

      // Background = "https://placeholder.jpeg"
    });


    new FlowStageBinding($"{clusterDefinition.Metadata.Name}-recovery-flow-binding-00", new()
    {
      Target = flow.Uuid,
      Stage = Defaults.Stages.AuthenticationIdentification.Apply(z => z.Id),
      Order = 0,
    });

    // resource "authentik_flow_stage_binding" "recovery-flow-binding-00" {
    //   target = authentik_flow.recovery.uuid
    //   stage  = authentik_stage_identification.recovery-identification.id
    //   order  = 0
    // }

    // resource "authentik_flow_stage_binding" "recovery-flow-binding-10" {
    //   target = authentik_flow.recovery.uuid
    //   stage  = authentik_stage_email.recovery-email.id
    //   order  = 10
    // }

    // resource "authentik_flow_stage_binding" "recovery-flow-binding-20" {
    //   target = authentik_flow.recovery.uuid
    //   stage  = authentik_stage_prompt.password-change-prompt.id
    //   order  = 20
    // }

    // resource "authentik_flow_stage_binding" "recovery-flow-binding-30" {
    //   target = authentik_flow.recovery.uuid
    //   stage  = authentik_stage_user_write.password-change-write.id
    //   order  = 30
    // }

  }
}

static partial class Defaults
{
  public static class Flows
  {
    private static Output<GetFlowResult> GetFlowLazy(string slug)
    {
      return Output.Create(slug).Apply(s => GetFlow.Invoke(new() { Slug = s }));
    }

    public static Output<GetFlowResult> AuthenticationFlow =>
      field ??= GetFlowLazy("default-authentication-flow");

    public static Output<GetFlowResult> SourceAuthentication =>
      field ??= GetFlowLazy("default-source-authentication");

    public static Output<GetFlowResult> ProviderAuthorizationExplicitConsent =>
      field ??= GetFlowLazy("default-provider-authorization-explicit-consent");

    public static Output<GetFlowResult> ProviderAuthorizationImplicitConsent =>
      field ??= GetFlowLazy("default-provider-authorization-implicit-consent");

    public static Output<GetFlowResult> SourceEnrollment => field ??= GetFlowLazy("default-source-enrollment");

    public static Output<GetFlowResult> InvalidationFlow => field ??= GetFlowLazy("default-invalidation-flow");

    public static Output<GetFlowResult> ProviderInvalidationFlow =>
      field ??= GetFlowLazy("default-provider-invalidation-flow");

    public static Output<GetFlowResult> AuthenticatorStaticSetup =>
      field ??= GetFlowLazy("default-authenticator-static-setup");

    public static Output<GetFlowResult> AuthenticatorTotpSetup =>
      field ??= GetFlowLazy("default-authenticator-totp-setup");

    public static Output<GetFlowResult> AuthenticatorWebauthnSetup =>
      field ??= GetFlowLazy("default-authenticator-webauthn-setup");

    public static Output<GetFlowResult> PasswordChange => field ??= GetFlowLazy("default-password-change");

    public static Output<GetFlowResult> SourcePreAuthentication =>
      field ??= GetFlowLazy("default-source-pre-authentication");

    public static Output<GetFlowResult> UserSettingsFlow =>
      field ??= GetFlowLazy("default-user-settings-flow");
  }
}
