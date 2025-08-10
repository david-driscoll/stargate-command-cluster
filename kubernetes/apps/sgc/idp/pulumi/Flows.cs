using Pulumi;
using Pulumi.Authentik;

namespace applications;

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