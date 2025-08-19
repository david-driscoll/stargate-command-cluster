using System;
using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class StagePrompts(Fields Fields, ComponentResourceOptions? options = null) : SharedComponentResource(
  "custom:resource:AuthentikPrompts",
  "authentik-stage-prompts", options)
{
  public StagePrompt UserSettings => field ??= CrateStagePrompt("user-settings", a => a.AddFields(Fields.Name, Fields.Username, Fields.Email));

  public StagePrompt Enrollment => field ??=
    CrateStagePrompt("enrollment", a => a.AddFields(Fields.Email, Fields.Username, Fields.Name));

  public StageUserWrite InternalEnrollmentWrite => field ??=
    new("internal-enrollment-write", new StageUserWriteArgs()
    {
      CreateUsersAsInactive = false,
      UserType = "internal",
      UserCreationMode = "create_when_required",
    }, _parent);
  public StageUserWrite ExternalEnrollmentWrite => field ??=
    new("external-enrollment-write", new StageUserWriteArgs()
    {
      CreateUsersAsInactive = false,
      UserType = "external",
      UserCreationMode = "create_when_required",
    }, _parent);

  public StageUserWrite SourceAuthenticationUpdate => field ??=
    new("source-authentication-write", new StageUserWriteArgs()
    {
      CreateUsersAsInactive = false,
      UserType = "internal",
      UserCreationMode = "never_create",
    }, _parent);

  private StagePrompt CrateStagePrompt(string name, Action<StagePromptArgs> action)
  {
    var args = new StagePromptArgs();
    action(args);
    return new StagePrompt(name, args, _parent);
  }
}