using System;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Tls;
using Pulumi.Tls.Inputs;

namespace authentik.AuthentikResources;

public class ApplicationCertificate : SharedComponentResource
{
  public ApplicationCertificate(string name, ComponentResourceOptions? options = null) : base( "custom:resource:ApplicationCertificate",
    name, options)
  {
  }

  public PrivateKey SigningPrivateKey => field ??= new($"{GetResourceName()}-private-key", new()
  {
    Algorithm = "RSA",
    RsaBits = 4096,
  }, _parent);

  public SelfSignedCert SigningCertificate => field ??= new($"{GetResourceName()}-certificate", new()
  {
    PrivateKeyPem = SigningPrivateKey.PrivateKeyPem,
    AllowedUses = ["cert_signing"],
    ValidityPeriodHours = Convert.ToInt32(TimeSpan.FromDays(365).TotalHours),
    EarlyRenewalHours = Convert.ToInt32(TimeSpan.FromDays(1).TotalHours),
    Subject = new SelfSignedCertSubjectArgs()
    {
      CommonName = "Signing Key",
      OrganizationalUnit = "Authentik",
      Country = "Anywhere",
      Locality = "Everywhere",
    },
  }, _parent);

  public CertificateKeyPair SigningKeyPair => field ??= new($"{GetResourceName()}-key-pair", new()
  {
    CertificateData = SigningCertificate.CertPem,
    KeyData = SigningPrivateKey.PrivateKeyPem,
  }, _parent);
}
