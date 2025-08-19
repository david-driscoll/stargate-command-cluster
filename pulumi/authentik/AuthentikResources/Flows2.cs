using System.Linq;
using models.Applications;
using Pulumi;
using Pulumi.Authentik;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Provider = Rocket.Surgery.OnePasswordNativeUnofficial.Provider;

namespace authentik.AuthentikResources;

public static class Flows2
{
  public static PropertyMappings PropertyMappings => field ??= new();
  public static Policies Policies => field ??= new();
  private static ComponentResource Stages => field ??= new("custom:resource:AuthentikStages", "authentik-stages");
  private static CustomResourceOptions StagesParent => field ??= new() { Parent = Stages };
  public static ConsentStages ConstentStages => field ??= new(new ComponentResourceOptions() { Parent = Stages });
  public static Fields Fields => field ??= new(new ComponentResourceOptions() { Parent = Stages });
  public static StagePrompts StagePrompts => field ??= new(Fields, new ComponentResourceOptions() { Parent = Stages });

  public static InvalidationStages InvalidationStages =>
    field ??= new(new ComponentResourceOptions() { Parent = Stages });

  public static AuthenticatorStages AuthenticatorStages =>
    field ??= new(new ComponentResourceOptions() { Parent = Stages });

  public static AuthenticationStages AuthenticationStages =>
    field ??= new(AuthenticatorStages, new ComponentResourceOptions() { Parent = Stages });

  private static CustomResourceOptions SourcesParent => field ??= new()
    { Parent = new ComponentResource("custom:resource:sources", "authentik-sources") };

  private static CustomResourceOptions FlowsParent => field ??= new()
    { Parent = new ComponentResource("custom:resource:flows", "authentik-flows") };

  public static (Flow LogoutFlow,
    Flow ProviderLogoutFlow,
    Flow AuthenticatorBackupCodesFlow,
    Flow AuthenticatorTotpFlow,
    Flow AuthenticatorWebauthnFlow,
    Flow UserSettingsFlow,
    Flow ImplicitConsentFlow,
    Flow ExplicitConsentFlow) CreateFlows()
  {
    var logoutFlow = CreateLogoutFlow();
    var providerLogoutFlow = CreateProviderLogoutFlow();
    var authenticatorBackupCodesFlow = CreateAuthenticatorBackupCodesFlow();
    var authenticatorTotpFlow = CreateAuthenticatorTotpFlow();
    var authenticatorWebauthnFlow = CreateAuthenticatorWebauthnFlow();
    var userSettingsFlow = CreateUserSettingsFlow();
    var implicitConsentFlow = CreateImplicitConsent();
    var explicitConsentFlow = CreateExplicitConsent();

    return (
      logoutFlow,
      providerLogoutFlow,
      authenticatorBackupCodesFlow,
      authenticatorTotpFlow,
      authenticatorWebauthnFlow,
      userSettingsFlow,
      implicitConsentFlow,
      explicitConsentFlow
    );
  }

  public static (Flow SourceAuthenticationFlow, Flow AuthenticationFlow, Flow EnrollmentFlow) CreateClusterFlows(
    ClusterDefinition cluster, Provider provider)
  {
    var enrollmentFlow = CreateEnrollmentFlow(cluster);

    var authenticationFlow = new Flow($"{cluster.Metadata.Name}-authentication-flow", new()
    {
      Name = cluster.Spec.Name,
      Title = $"Welcome to {cluster.Spec.Name}!",
      Slug = $"{cluster.Metadata.Name}-authentication-flow",
      Layout = "sidebar_left",
      Designation = "authentication",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
      // Background = "https://placeholder.jpeg",
    }, FlowsParent);
    var sourceAuthenticationFlow = CreateSourceAuthenticationFlow(cluster);
    var tailscaleSource = CreateTailscaleSource(cluster, enrollmentFlow, sourceAuthenticationFlow);
    var plexSource = CreatePlexSource(cluster, enrollmentFlow, sourceAuthenticationFlow, provider);

    var identificationStage = new StageIdentification($"{cluster.Metadata.Name}-authentication-identification", new()
    {
      Sources = [tailscaleSource.Uuid, plexSource.Uuid],
      ShowSourceLabels = true,
      CaseInsensitiveMatching = true,
      PasswordStage = AuthenticationStages.Password.StagePasswordId,
      EnableRememberMe = true,
      ShowMatchedUser = true,
      UserFields = ["email", "username"],
      PretendUserExists = false,
      EnrollmentFlow = null,
      RecoveryFlow = null,
      PasswordlessFlow = authenticationFlow.Uuid,
    }, StagesParent);
    authenticationFlow.AddFlowStageBinding(identificationStage.StageIdentificationId);
    authenticationFlow.AddFlowStageBinding(AuthenticationStages.Mfa.StageAuthenticatorValidateId);
    authenticationFlow.AddFlowStageBinding(AuthenticationStages.Login.StageUserLoginId);

    return (
      sourceAuthenticationFlow,
      authenticationFlow,
      enrollmentFlow
    );
  }

  private static SourcePlex CreatePlexSource(ClusterDefinition cluster, Flow enrollmentFlow, Flow authenticationFlow,
    Provider provider)
  {
    var plexDetails = GetAPICredential.Invoke(
      new() { Vault = "Eris", Title = "Authentik Plex Source", Id = "noivqwzursabsqwb3ujongo2ia" },
      new() { Provider = provider });

    var plexSource = new SourcePlex($"{cluster.Metadata.Name}-plex", new()
    {
      Name = "Plex",
      Slug = $"{cluster.Metadata.Name}-plex",

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

  private static SourceOauth CreateTailscaleSource(ClusterDefinition cluster, Flow enrollmentFlow, Flow authenticationFlow)
  {
    var tailscaleSource = new SourceOauth($"{cluster.Metadata.Name}-tailscale", new()
    {
      Name = "Tailscale",
      Slug = $"{cluster.Metadata.Name}-tailscale",
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

  private static Flow CreateSourceAuthenticationFlow(ClusterDefinition definition)
  {
    var name = $"{definition.Metadata.Name}-source-authentication-flow";
    var flow = new Flow(name, new()
    {
      Name = definition.Spec.Name,
      Title = $"Welcome to {definition.Spec.Name}!",
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

    flow.AddFlowStageBinding(AuthenticationStages.SourceLogin.StageUserLoginId);

    return flow;
  }

  private static Flow CreateEnrollmentFlow(ClusterDefinition definition)
  {
    var name = $"{definition.Metadata.Name}-enrollment-flow";
    var flow = new Flow(name, new()
    {
      Name = definition.Spec.Name,
      Title = $"Welcome to {definition.Spec.Name}! Please enter your user details.",
      Slug = name,
      Layout = "sidebar_left",
      Designation = "enrollment",
      CompatibilityMode = true,
      PolicyEngineMode = "any",
      DeniedAction = "message_continue",
      Authentication = "none",
    }, FlowsParent);

    flow.AddPolicyBinding(Policies.SourceEnrollmentIfSingleSignOn);
    flow.AddFlowStageBinding(StagePrompts.Enrollment);
    flow.AddFlowStageBinding(StagePrompts.InternalEnrollmentWrite.StageUserWriteId);
    flow.AddFlowStageBinding(AuthenticationStages.SourceLogin.StageUserLoginId);

    return flow;
  }


  private static Flow CreateImplicitConsent()
  {
    var name = $"implicit-consent-flow";
    return new Flow(name, new()
    {
      Name = "Redirect",
      Title = $"Redirecting to %(app)s!",
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
    var name = $"explicit-consent-flow";
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
    }, FlowsParent);
    flow.AddFlowStageBinding(ConstentStages.Permanent.StageConsentId);
    return flow;
  }


  private static Flow CreateLogoutFlow()
  {
    var name = $"logout-flow";
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
    var name = $"provider-logout-flow";
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
    var name = $"user-settings-flow";
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
    flow.AddFlowStageBinding(StagePrompts.UserSettings.StagePromptId);
    flow.AddFlowStageBinding(StagePrompts.InternalEnrollmentWrite.StageUserWriteId);

    return flow;
  }

  private static Flow CreateAuthenticatorBackupCodesFlow()
  {
    var name = $"authenticator-backup-codes-flow";
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
    var name = $"authenticator-webauthn-flow";
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
    var name = $"authenticator-totp-flow";
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
