using System.Diagnostics.CodeAnalysis;
using Pulumi;
using Pulumi.Authentik;

namespace applications.AuthentikResources;

public class Stages(ComponentResourceOptions? options = null) : SharedComponentResource("custom:resource:Stages",
  "authentik-stages", options)
{
  [field: MaybeNull]
  public AuthenticatorStages Authenticator => field ??= new(new ComponentResourceOptions() { Parent = this });

  [field: MaybeNull]
  public AuthenticationStages Authentication => field ??= new(Authenticator, new ComponentResourceOptions() { Parent = this });
  [field: MaybeNull] public ConsentStages Consent => field ??= new(new ComponentResourceOptions() { Parent = this });
  [field: MaybeNull] public InvalidationStages Invalidation => field ??= new(new ComponentResourceOptions() { Parent = this });
  // public EnrollmentStages Enrollment => field ??= new(new ComponentResourceOptions() { Parent = this });
  //
  // [field: MaybeNull]
  // public SettingsStages Settings => field ??= new(new ComponentResourceOptions() { Parent = this });


  public class AuthenticatorStages(ComponentResourceOptions options) : SharedComponentResource("custom:resource:AuthenticatorStages",
    "authentik-stages-authentication", options)
  {
    public StageAuthenticatorStatic BackupCodes => field ??= new("authenticator-static", new()
    {
      TokenCount = 8,
      TokenLength = 12,
      FriendlyName = "Backup Codes",
      ConfigureFlow = DefaultFlows.AuthenticatorStaticSetup.Apply(z => z.Id),
    }, _parent);

    public StageAuthenticatorTotp Totp => field ??= new("authenticator-totp", new()
    {
      FriendlyName = "TOTP Codes",
      ConfigureFlow = DefaultFlows.AuthenticatorTotpSetup.Apply(z => z.Id),
      Digits = "8",
    }, _parent);

    public StageAuthenticatorWebauthn Passkey => field ??= new("authenticator-passkey", new()
    {
      FriendlyName = "TOTP Codes",
      ConfigureFlow = DefaultFlows.AuthenticatorWebauthnSetup.Apply(z => z.Id),
      ResidentKeyRequirement = "preferred",
      UserVerification = "required",
    }, _parent);
  }

  public class AuthenticationStages(
    AuthenticatorStages authenticatorStages,
    ComponentResourceOptions options) : SharedComponentResource("custom:resource:AuthenticationStages",
    "authentik-stages-authentication", options)
  {

    public StageAuthenticatorValidate Mfa => field ??= new("authentication-mfa", new()
    {
      DeviceClasses = { "static", "totp", "webauthn" },
      NotConfiguredAction = "configure",
      ConfigurationStages =
      [
        authenticatorStages.Passkey.StageAuthenticatorWebauthnId,
        authenticatorStages.Totp.StageAuthenticatorTotpId,
        authenticatorStages.BackupCodes.StageAuthenticatorStaticId,
      ],
      LastAuthThreshold = "days=7",
      WebauthnUserVerification = "preferred",
    }, _parent);

    public StageUserLogin Login => field ??= new("authentication-login", new()
    {
      RememberMeOffset = "days=29",
      SessionDuration = "days=1",
    }, _parent);

    public StageUserLogin SourceLogin => field ??= new("authentication-source-login", new()
    {
      RememberMeOffset = "days=29",
      SessionDuration = "days=1",
    }, _parent);

    public StagePassword Password => field ??= new("authentication-password", new()
    {
      Backends =
      [
        "authentik.core.auth.InbuiltBackend",
        "authentik.core.auth.TokenBackend",
      ],
      AllowShowPassword = true,
      ConfigureFlow = DefaultFlows.PasswordChange.Apply(z => z.Id),
    }, _parent);

    public StageAuthenticatorValidate Passkey => field ??= new("authentication-passkey", new()
    {
      DeviceClasses = { "webauthn" },
      NotConfiguredAction = "skip",
      LastAuthThreshold = "days=7",
      WebauthnUserVerification = "preferred",
    }, _parent);
  }

  public class InvalidationStages(
    ComponentResourceOptions options) : SharedComponentResource("custom:resource:InvalidationStages",
    "authentik-stages-invalidation", options)
  {
    public StageUserLogout Logout => field ??= new("authentication-logout", new(), _parent);
    public StageUserLogout ProviderLogout => field ??= new("authentication-provider-logout", new(), _parent);
  }

  public class ConsentStages(
    ComponentResourceOptions options) : SharedComponentResource("custom:resource:ConsentStages",
    "authentik-stages-authorization", options)
  {
    public StageConsent OneMonth => field ??= new("authorization-consent-one-month", new StageConsentArgs()
    {
      ConsentExpireIn = "days=30",
      Mode = "expiring"
    }, _parent);
    public StageConsent OneWeek => field ??= new("authorization-consent-one-week", new StageConsentArgs()
    {
      ConsentExpireIn = "days=7",
      Mode = "expiring"
    }, _parent);
    public StageConsent Require => field ??= new("authorization-consent-require", new StageConsentArgs()
    {
      Mode = "always_require"
    }, _parent);
    public StageConsent Permanent => field ??= new("authorization-consent-permanent", new StageConsentArgs()
    {
      Mode = "permanent"
    }, _parent);
  }

}
