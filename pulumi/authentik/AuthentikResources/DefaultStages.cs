using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public static class DefaultStages
{
  public  static Output<GetStageResult> AuthenticationIdentification =>
    field ??= GetStageLazy("default-authentication-identification");

  public  static Output<GetStageResult> AuthenticationLogin => field ??= GetStageLazy("default-authentication-login");

  public  static Output<GetStageResult> AuthenticationMfaValidation =>
    field ??= GetStageLazy("default-authentication-mfa-validation");

  public  static Output<GetStageResult> AuthenticationPassword =>
    field ??= GetStageLazy("default-authentication-password");

  public  static Output<GetStageResult> AuthenticatorStaticSetup =>
    field ??= GetStageLazy("default-authenticator-static-setup");

  public  static Output<GetStageResult> AuthenticatorTotpSetup =>
    field ??= GetStageLazy("default-authenticator-totp-setup");

  public  static Output<GetStageResult> AuthenticatorWebauthnSetup =>
    field ??= GetStageLazy("default-authenticator-webauthn-setup");

  public  static Output<GetStageResult> InvalidationLogout => field ??= GetStageLazy("default-invalidation-logout");

  public  static Output<GetStageResult> PasswordChangePrompt =>
    field ??= GetStageLazy("default-password-change-prompt");

  public  static Output<GetStageResult> PasswordChangeWrite => field ??= GetStageLazy("default-password-change-write");

  public  static Output<GetStageResult> ProviderAuthorizationConsent =>
    field ??= GetStageLazy("default-provider-authorization-consent");

  public  static Output<GetStageResult> SourceAuthenticationLogin =>
    field ??= GetStageLazy("default-source-authentication-login");

  public  static Output<GetStageResult> SourceEnrollmentLogin =>
    field ??= GetStageLazy("default-source-enrollment-login");

  public  static Output<GetStageResult> SourceEnrollmentPrompt =>
    field ??= GetStageLazy("default-source-enrollment-prompt");

  public  static Output<GetStageResult> SourceEnrollmentWrite =>
    field ??= GetStageLazy("default-source-enrollment-write");

  public  static Output<GetStageResult> UserSettings => field ??= GetStageLazy("default-user-settings");

  public  static Output<GetStageResult> UserSettingsWrite => field ??= GetStageLazy("default-user-settings-write");

  private static Output<GetStageResult> GetStageLazy(string slug)
  {
    return Output.Create(slug).Apply(s => GetStage.Invoke(new() { Name = s }, new InvokeOptions() { Parent = PulumiGroups.DefaultStages }));
  }
}
