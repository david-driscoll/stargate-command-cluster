using System.Linq;
using models.Applications;
using Pulumi;
using Pulumi.Authentik;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Provider = Rocket.Surgery.OnePasswordNativeUnofficial.Provider;

namespace authentik.AuthentikResources;

public record CustomFlows(
  Flow LogoutFlow,
  Flow ProviderLogoutFlow,
  Flow AuthenticatorBackupCodesFlow,
  Flow AuthenticatorTotpFlow,
  Flow AuthenticatorWebauthnFlow,
  Flow UserSettingsFlow,
  Flow ImplicitConsentFlow,
  Flow ExplicitConsentFlow,
  Flow AuthenticationFlow,
  Flow SourceAuthenticationFlow,
  Flow EnrollmentFlow);
public static class Flows2
{
  public static PropertyMappings PropertyMappings => field ??= new();
  public static Policies Policies => field ??= new();
  private static ComponentResource Stages => field ??= new("custom:resource:AuthentikStages", "authentik-stages");
  private static CustomResourceOptions StagesParent => field ??= new() { Parent = Stages };
  public static ConsentStages ConstentStages => field ??= new(new ComponentResourceOptions { Parent = Stages });
  public static Fields Fields => field ??= new(new ComponentResourceOptions { Parent = Stages });
  public static StagePrompts StagePrompts => field ??= new(Fields, new ComponentResourceOptions { Parent = Stages });

  public static InvalidationStages InvalidationStages =>
    field ??= new(new ComponentResourceOptions { Parent = Stages });

  public static AuthenticatorStages AuthenticatorStages =>
    field ??= new(new ComponentResourceOptions { Parent = Stages });

  public static AuthenticationStages AuthenticationStages =>
    field ??= new(AuthenticatorStages, new ComponentResourceOptions { Parent = Stages });

  private static CustomResourceOptions SourcesParent => field ??= new()
    { Parent = new ComponentResource("custom:resource:sources", "authentik-sources") };

  private static CustomResourceOptions FlowsParent => field ??= new()
    { Parent = new ComponentResource("custom:resource:flows", "authentik-flows") };

  public static CustomFlows CreateFlows(Provider provider)
  {
    var logoutFlow = CreateLogoutFlow();
    var providerLogoutFlow = CreateProviderLogoutFlow();
    var authenticatorBackupCodesFlow = CreateAuthenticatorBackupCodesFlow();
    var authenticatorTotpFlow = CreateAuthenticatorTotpFlow();
    var authenticatorWebauthnFlow = CreateAuthenticatorWebauthnFlow();
    var userSettingsFlow = CreateUserSettingsFlow();
    var implicitConsentFlow = CreateImplicitConsent();
    var explicitConsentFlow = CreateExplicitConsent();

    var enrollmentFlow = CreateEnrollmentFlow();

    var authenticationFlow = CreateAuthenticationFlow();
    var sourceAuthenticationFlow = CreateSourceAuthenticationFlow();
    var tailscaleSource = CreateTailscaleSource(enrollmentFlow, sourceAuthenticationFlow);
    var plexSource = CreatePlexSource(enrollmentFlow, sourceAuthenticationFlow, provider);

    var sourceIdentificationStage = new StageIdentification("source-identification", new()
    {
      Sources = [tailscaleSource.Uuid, plexSource.Uuid],
      ShowSourceLabels = true,
      EnableRememberMe = true,
      ShowMatchedUser = true,
      PasswordlessFlow = authenticationFlow.Uuid,
    }, StagesParent);

    var identificationStage = new StageIdentification("authentication-identification", new()
    {
      CaseInsensitiveMatching = true,
      PasswordStage = AuthenticationStages.Password.StagePasswordId,
      EnableRememberMe = true,
      ShowMatchedUser = true,
      UserFields = ["email", "username"],
      PretendUserExists = false,
      EnrollmentFlow = null,
      RecoveryFlow = null,
    }, StagesParent);
    authenticationFlow.AddFlowStageBinding(sourceIdentificationStage.StageIdentificationId);
    authenticationFlow.AddFlowStageBinding(identificationStage.StageIdentificationId);
    authenticationFlow.AddFlowStageBinding(AuthenticationStages.Mfa.StageAuthenticatorValidateId);
    authenticationFlow.AddFlowStageBinding(AuthenticationStages.Login.StageUserLoginId);

    return new (
      logoutFlow,
      providerLogoutFlow,
      authenticatorBackupCodesFlow,
      authenticatorTotpFlow,
      authenticatorWebauthnFlow,
      userSettingsFlow,
      implicitConsentFlow,
      explicitConsentFlow,

      authenticationFlow,
      sourceAuthenticationFlow,
      enrollmentFlow
    );
  }

  private static Flow CreateAuthenticationFlow()
  {
    var authenticationFlow = new Flow("authentication-flow", new()
    {
      Name = "Driscoll Home",
      Title = "Welcome to Driscoll Tech Net!",
      Slug = "authentication-flow",
      Layout = "sidebar_left",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
      // Background = "https://placeholder.jpeg",
    }, FlowsParent);
    return authenticationFlow;
  }

  private static SourcePlex CreatePlexSource(Flow enrollmentFlow, Flow authenticationFlow,
    Provider provider)
  {
    var plexDetails = GetAPICredential.Invoke(
      new() { Vault = "Eris", Title = "Authentik Plex Source", Id = "noivqwzursabsqwb3ujongo2ia" },
      new() { Provider = provider });

    var plexSource = new SourcePlex("plex", new()
    {
      Name = "Plex",
      Slug = "plex",

      Enabled = true,

      PolicyEngineMode = "any",
      UserPathTemplate = "driscoll.dev/plex/%(slug)s",

      UserMatchingMode = "email_link",
      GroupMatchingMode = "name_link",

      ClientId = plexDetails.Apply(z => z.Username),
      PlexToken = plexDetails.Apply(z => z.Credential),
      AllowedServers = plexDetails.Apply(z =>
        z.Sections.Values.Single(z => z.Label == "servers").Fields.Values.OrderBy(z => z.Value).Select(z => z.Value)),
      AllowFriends = true,

      AuthenticationFlow = authenticationFlow.Uuid,
      EnrollmentFlow = enrollmentFlow.Uuid,
    }, SourcesParent);
    return plexSource;
  }

  private static SourceOauth CreateTailscaleSource(Flow enrollmentFlow, Flow authenticationFlow)
  {
    var tailscaleSource = new SourceOauth("tailscale", new()
    {
      Name = "Tailscale",
      Slug = "tailscale",
      ProviderType = "openidconnect",

      Enabled = true,

      PolicyEngineMode = "any",
      UserPathTemplate = "driscoll.dev/tailscale/%(slug)s",

      OidcWellKnownUrl = "https://idp.opossum-yo.ts.net/.well-known/openid-configuration",
      ConsumerKey = "unused",
      ConsumerSecret = "unused",
      UserMatchingMode = "email_link",
      GroupMatchingMode = "name_link",

      AuthenticationFlow = authenticationFlow.Uuid,
      EnrollmentFlow = enrollmentFlow.Uuid,
    }, SourcesParent);
    return tailscaleSource;
  }

  private static Flow CreateSourceAuthenticationFlow()
  {
    var name = "source-authentication-flow";
    var flow = new Flow(name, new()
    {
      Name = "Welcome back!",
      Title = "Welcome back to Driscoll Tech Net!",
      Slug = name,
      Layout = "sidebar_left",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "continue",
      Authentication = "none",
      // Background = "https://placeholder.jpeg",
    }, FlowsParent);

    flow.AddPolicyBinding(Policies.SourceAuthenticationIfSingleSignOn);

    flow.AddFlowStageBinding(AuthenticationStages.SourceLogin.StageUserLoginId)
      .AddPolicyBinding(Policies.DefaultSourceGroups);
    flow.AddFlowStageBinding(StagePrompts.SourceAuthenticationUpdate.StageUserWriteId);
    flow.AddFlowStageBinding(StagePrompts.SourceAuthenticationUpdate.StageUserWriteId);

    return flow;
  }

  private static Flow CreateEnrollmentFlow()
  {
    var name = "enrollment-flow";
    var flow = new Flow(name, new()
    {
      Name = "Driscoll Home",
      Title = "Welcome to Driscoll Tech Net! Please enter your user details.",
      Slug = name,
      Layout = "sidebar_left",
      Designation = "enrollment",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
    }, FlowsParent);

    flow.AddPolicyBinding(Policies.SourceEnrollmentIfSingleSignOn);
    flow.AddFlowStageBinding(StagePrompts.Enrollment)
      .AddPolicyBinding(Policies.DefaultGroups);
    flow.AddFlowStageBinding(StagePrompts.InternalEnrollmentWrite.StageUserWriteId);
    flow.AddFlowStageBinding(AuthenticationStages.SourceLogin.StageUserLoginId);

    return flow;
  }


  private static Flow CreateImplicitConsent()
  {
    var name = "implicit-consent-flow";
    return new Flow(name, new()
    {
      Name = "Redirect",
      Title = "Redirecting to %(app)s!",
      Slug = name,
      Layout = "content_right",
      Designation = "authorization",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "continue",
      Authentication = "require_authenticated",
      // Background = "https://placeholder.jpeg",
    }, FlowsParent);
  }

  private static Flow CreateExplicitConsent()
  {
    var name = "explicit-consent-flow";
    var flow = new Flow(name, new()
    {
      Name = "Redirect",
      Title = "Redirecting to %(app)s!",
      Slug = name,
      Layout = "content_right",
      Designation = "authorization",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
      // Background = "https://placeholder.jpeg",
    }, FlowsParent);
    flow.AddFlowStageBinding(ConstentStages.Permanent.StageConsentId);
    return flow;
  }


  private static Flow CreateLogoutFlow()
  {
    var name = "logout-flow";
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
    }, FlowsParent);

    flow.AddFlowStageBinding(InvalidationStages.Logout.StageUserLogoutId);
    return flow;
  }

  private static Flow CreateProviderLogoutFlow()
  {
    var name = "provider-logout-flow";
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
    }, FlowsParent);
    return flow;
  }

  private static Flow CreateUserSettingsFlow()
  {
    var name = "user-settings-flow";
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
    }, FlowsParent);
    flow.AddFlowStageBinding(StagePrompts.UserSettings.StagePromptId)
      .AddPolicyBinding(Policies.UserSettingsAuthorization);
    flow.AddFlowStageBinding(StagePrompts.InternalEnrollmentWrite.StageUserWriteId);

    return flow;
  }

  private static Flow CreateAuthenticatorBackupCodesFlow()
  {
    var name = "authenticator-backup-codes-flow";
    var flow = new Flow(name, new()
    {
      Name = "Backup Codes",
      Title = "Setup Backup Codes",
      Slug = name,
      Layout = "stacked",
      Designation = "stage_configuration",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, FlowsParent);

    flow.AddFlowStageBinding(AuthenticatorStages.BackupCodes.StageAuthenticatorStaticId);
    return flow;
  }

  private static Flow CreateAuthenticatorWebauthnFlow()
  {
    var name = "authenticator-webauthn-flow";
    var flow = new Flow(name, new()
    {
      Name = "Passkey",
      Title = "Setup Passkey",
      Slug = name,
      Layout = "stacked",
      Designation = "stage_configuration",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, FlowsParent);

    flow.AddFlowStageBinding(AuthenticatorStages.Passkey.StageAuthenticatorWebauthnId);
    return flow;
  }

  private static Flow CreateAuthenticatorTotpFlow()
  {
    var name = "authenticator-totp-flow";
    var flow = new Flow(name, new()
    {
      Name = "TOTP",
      Title = "Setup TOTP Code",
      Slug = name,
      Layout = "stacked",
      Designation = "stage_configuration",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "require_authenticated",
    }, FlowsParent);

    flow.AddFlowStageBinding(AuthenticatorStages.Totp.StageAuthenticatorTotpId);
    return flow;
  }
}
