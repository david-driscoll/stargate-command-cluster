using System;
using applications.Models.ApplicationDefinition;
using Pulumi;
using Pulumi.Authentik;

namespace applications.AuthentikResources;

public class Flows : SharedComponentResource
{
  private readonly Policies _policies;
  private readonly Stages _stages;
  private readonly ClusterDefinition _clusterDefinition;

  public Flows(
    Policies policies,
    Stages stages,
    ClusterDefinition clusterDefinition,
    ComponentResourceOptions? options = null) : base("custom:resource:AuthentikFlows",
    clusterDefinition.Metadata.Name, options)
  {
    _policies = policies;
    _stages = stages;
    _clusterDefinition = clusterDefinition;
    CreateImplicitConsent();
    CreateExplicitConsent();
    CreateSourceAuthenticationFlow();
    // tailscale enrollment flow
    CreateProviderLogoutFlow();
    CreateChangePasswordFlow();
    CreateAuthenticatorBackupCodesFlow();
    CreateAuthenticatorTotpFlow();
    CreateAuthenticatorWebauthnFlow();
// recovery
// unenrollmemnt
  }

  public Flow InvalidationFlow => field ??= CreateLogoutFlow();

  public Flow UserSettingsFlow => field ??= CreateUserSettingsFlow();

  public Flow AuthenticationFlow => field ??= CreateAuthenticationFlow();

  private Flow CreateImplicitConsent()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-implicit-consent-flow";
    return new Flow(name, new()
    {
      Name = "Redirect",
      Title = $"Redirecting to %(app)s!",
      Slug = name,
      Layout = "content_right",
      Designation = "authorization",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
      // Background = "https://placeholder.jpeg",
    }, _parent);
  }

  private Flow CreateExplicitConsent()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-explicit-consent-flow";
    var flow = new Flow(name, new()
    {
      Name = "Redirect",
      Title = $"Redirecting to %(app)s!",
      Slug = name,
      Layout = "content_right",
      Designation = "authorization",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
      // Background = "https://placeholder.jpeg",
    }, _parent);
    flow.AddFlowStageBinding(new FlowStageBindingArgs()
    {
      Stage = _stages.Consent.Permanent.StageConsentId,
    });
    return flow;
  }

  private Flow CreateSourceAuthenticationFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-source-authentication-flow";
    var flow = new Flow(name, new()
    {
      Name = _clusterDefinition.Spec.Name,
      Title = $"Welcome to {_clusterDefinition.Spec.Name}!",
      Slug = name,
      Layout = "sidebar_left",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
      // Background = "https://placeholder.jpeg",
    }, _parent);

    flow.AddFlowStageBinding(new FlowStageBindingArgs()
    {
      Stage = _stages.Authentication.SourceLogin.StageUserLoginId,
    });

    flow.AddPolicyBinding(_policies.SourceAuthenticationIfSingleSignOn.PolicyExpressionId);

    return flow;
  }

  private Flow CreateAuthenticationFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-authentication-flow";
    var flow = new Flow(name, new()
    {
      Name = _clusterDefinition.Spec.Name,
      Title = $"Welcome to {_clusterDefinition.Spec.Name}!",
      Slug = name,
      Layout = "sidebar_left",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
      // Background = "https://placeholder.jpeg"
    }, _parent);

    flow.AddFlowStageBinding(new FlowStageBindingArgs()
    {
      Stage = _stages.Authentication.Login.StageUserLoginId,
    });
    flow.AddFlowStageBinding(new FlowStageBindingArgs()
      {
        Stage = _stages.Authentication.Password.StagePasswordId,
      })
      .AddPolicyBinding(new PolicyBindingArgs()
      {
        Timeout = 30,
        FailureResult = true,
        Enabled = true,
        Policy = _policies.CheckIfUserHasPassword.PolicyExpressionId,
      });
    flow.AddFlowStageBinding(new FlowStageBindingArgs()
    {
      Stage = _stages.Authentication.Mfa.StageAuthenticatorValidateId,
    });
    flow.AddFlowStageBinding(new FlowStageBindingArgs()
    {
      Stage = _stages.Authentication.Login.StageUserLoginId,
    });
    return flow;
  }

  private Flow CreateLogoutFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-logout-flow";
    var flow = new Flow(name, new()
    {
      Name = "Logout",
      Title = "Logout",
      Slug = name,
      Layout = "stacked",
      Designation = "invalidation",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
    }, _parent);

    flow.AddFlowStageBinding(_stages.Invalidation.Logout.StageUserLogoutId);
    return flow;
  }

  private Flow CreateProviderLogoutFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-provider-logout-flow";
    var flow = new Flow(name, new()
    {
      Name = "Logout",
      Title = "You've logged out of %(app)s.",
      Slug = name,
      Layout = "stacked",
      Designation = "invalidation",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
    }, _parent);
    return flow;
  }

  private Flow CreateChangePasswordFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-change-password-flow";
    var flow = new Flow(name, new()
    {
      Name = "Change Password",
      Title = "Change Password",
      Slug = name,
      Layout = "content_left",
      Designation = "stage_configuration",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, _parent);


    return flow;
  }

  private Flow CreateUserSettingsFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-user-settings-flow";
    var flow = new Flow(name, new()
    {
      Name = "User Settings",
      Title = "User Settings",
      Slug = name,
      Layout = "content_left",
      Designation = "stage_configuration",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, _parent);


    return flow;
  }

  private Flow CreateAuthenticatorBackupCodesFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-authenticator-backup-codes-flow";
    var flow = new Flow(name, new()
    {
      Name = "Backup Codes",
      Title = "Setup Backup Codes",
      Slug = name,
      Layout = "stacked",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, _parent);

    flow.AddFlowStageBinding(_stages.Authenticator.BackupCodes.StageAuthenticatorStaticId);
    return flow;
  }

  private Flow CreateAuthenticatorWebauthnFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-authenticator-webauthn-flow";
    var flow = new Flow(name, new()
    {
      Name = "Passkey",
      Title = "Setup Passkey",
      Slug = name,
      Layout = "stacked",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, _parent);

    flow.AddFlowStageBinding(_stages.Authenticator.Passkey.StageAuthenticatorWebauthnId);
    return flow;
  }

  private Flow CreateAuthenticatorTotpFlow()
  {
    var name = $"{_clusterDefinition.Metadata.Name}-authenticator-totp-flow";
    var flow = new Flow(name, new()
    {
      Name = "TOTP",
      Title = "Setup TOTP Code",
      Slug = name,
      Layout = "stacked",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, _parent);

    flow.AddFlowStageBinding(_stages.Authenticator.Totp.StageAuthenticatorTotpId);
    return flow;
  }

  public static void CreateRecoveryFlow(ClusterDefinition clusterDefinition)
  {
    /*
    ## Consent stages
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

    // new FlowStageBinding($"{clusterDefinition.Metadata.Name}-recovery-flow-binding-00", new()
    // {
    //   Target = flow.Uuid,
    //   Stage = Defaults.Stages.AuthenticationIdentification.Apply(z => z.Id),
    //   Order = 0,
    // });

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
