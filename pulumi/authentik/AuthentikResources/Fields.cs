using System;
using System.Threading;
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
      UserType = "internal",
      UserCreationMode = "create_when_required",
    }, _parent);

  private StagePrompt CrateStagePrompt(string name, Action<StagePromptArgs> action)
  {
    var args = new StagePromptArgs();
    action(args);
    return new StagePrompt(name, args, _parent);
  }
}

public class Fields(ComponentResourceOptions? options = null) : SharedComponentResource(
  "custom:resource:AuthentikPrompts",
  "authentik-fields", options)
{
  public StagePromptField LoginUser => field ??= CreateField("login-user", new()
  {
    Label = "User",
    Type = "username",
    FieldKey = "user",
    Placeholder = "Username / Email",
    Required = true,
  });

  public StagePromptField Username => field ??= CreateField("username", new()
  {
    Label = "Username",
    Type = "username",
    FieldKey = "username",
    Placeholder = "Username",
  });

  public StagePromptField Email => field ??= CreateField("email", new()
  {
    Label = "Email",
    Type = "email",
    FieldKey = "email",
    Placeholder = "Email",
  });

  public StagePromptField Name => field ??= CreateField("name", new()
  {
    Label = "Name",
    Type = "text",
    FieldKey = "name",
    Placeholder = "Name (first is fine)",
    Required = true,
  });

  private int _order;

  private StagePromptField CreateField(string name, StagePromptFieldArgs args)
  {
    args.Order = Interlocked.Exchange(ref _order, _order + 10);
    return new StagePromptField(name, args, _parent);
  }
}
