using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class AuthenticationStages(
  AuthenticatorStages authenticatorStages,
  ComponentResourceOptions options) : SharedComponentResource("custom:resource:AuthenticationStages", "stages-authentication", options)
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