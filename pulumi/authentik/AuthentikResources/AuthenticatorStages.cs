using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class AuthenticatorStages(
  ComponentResourceOptions options) : SharedComponentResource(
  "custom:resource:AuthenticatorStages",
  "stages-authentication", options)
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
    FriendlyName = "Passkey",
    ConfigureFlow = DefaultFlows.AuthenticatorWebauthnSetup.Apply(z => z.Id),
    ResidentKeyRequirement = "preferred",
    UserVerification = "required",
  }, _parent);
}