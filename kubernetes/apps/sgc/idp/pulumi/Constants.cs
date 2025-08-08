// TODO: Pull from tailscale???

static class Constants
{
  public class Roles
  {
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Family = "Family";
    public const string Friend = "Friend";
    public const string MediaManager = "Media Manager";
  }
}

// class AuthentikPrompts : Pulumi.ComponentResource
// {
//   public AuthentikPrompts(ComponentResourceOptions? options = null) : base("custom:resource:AuthentikPrompts", "authentik-prompts", options)
//   {

//     'default-password-change-field-password',
//     'default-password-change-field-password-repeat',
//     'default-source-enrollment-field-username',
//     'default-user-settings-field-email',
//     'default-user-settings-field-locale',
//     'default-user-settings-field-name',
//     'default-user-settings-field-username'
//   }

//   private static Output<StagePrompt> GetPromptLazy(string slug)
//   {
//     return Output.Create(slug).Apply(s => StagePrompt.Invoke(new() { Slug = s }));
//   }
// }
