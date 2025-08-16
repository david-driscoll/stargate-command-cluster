using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class ConsentStages( ComponentResourceOptions options) : SharedComponentResource("custom:resource:ConsentStages", "stages-authorization", options)
{
  public StageConsent OneMonth => field ??= new("custom-authorization-consent-one-month",
    new StageConsentArgs()
    {
      ConsentExpireIn = "days=30",
      Mode = "expiring"
    }, _parent);

  public StageConsent OneWeek => field ??= new("custom-authorization-consent-one-week", new StageConsentArgs()
  {
    ConsentExpireIn = "days=7",
    Mode = "expiring"
  }, _parent);

  public StageConsent Require => field ??= new("custom-authorization-consent-require", new StageConsentArgs()
  {
    Mode = "always_require"
  }, _parent);

  public StageConsent Permanent => field ??= new("custom-authorization-consent-permanent",
    new StageConsentArgs()
    {
      Mode = "permanent"
    }, _parent);
}
