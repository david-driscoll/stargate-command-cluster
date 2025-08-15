using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Pulumi;
using Pulumi.Authentik;

namespace applications.AuthentikResources;

static class ExtensionMethods
{
  private static readonly Dictionary<string, int> BindingOrder = new();

  public static FlowStageBinding AddFlowStageBinding(this Flow flow, FlowStageBindingArgs args)
  {
    var currentOrder = BindingOrder.GetValueOrDefault(flow.GetResourceName(), 0);
    BindingOrder[flow.GetResourceName()] = currentOrder += 10;
    args.Order = currentOrder;
    args.Target = flow.Uuid;
    return new FlowStageBinding($"{flow.GetResourceName()}-binding-{currentOrder:00}", args, new() { Parent = flow });
  }

  public static StagePromptArgs AddFields(this StagePromptArgs args, params IEnumerable<StagePromptField> fields)
  {
    args.Fields.AddRange(fields, field => field.StagePromptFieldId);
    return args;
  }

  public static InputList<TResult> AddRange<T, TResult>(this InputList<TResult> list, IEnumerable<T> fields, Func<T, Output<TResult>> selector)
  {
    list.AddRange(fields.ToOutputList(selector));
    return list;
  }

  public static Output<IEnumerable<TResult>> ToOutputList<T, TResult>(this IEnumerable<T> fields, Func<T, Output<TResult>> selector)
  {
    return Output.Create(fields)
      .Apply(z => z.Aggregate(Output.Create(Enumerable.Empty<TResult>()),
        (list, field) =>
        {
          return Output.Tuple(selector(field), list).Apply(x => (x.Item2.Append(x.Item1)));
        }));
  }

  public static StagePromptArgs AddValidationPolicy(this StagePromptArgs args, params IEnumerable<StageAuthenticatorValidate> validators)
  {
    args.ValidationPolicies.AddRange(validators, field => field.StageAuthenticatorValidateId);
    return args;
  }

  public static FlowStageBinding AddFlowStageBinding(this Flow flow, StagePrompt prompt)
  {
    return AddFlowStageBinding(flow, prompt.StagePromptId);
  }

  public static FlowStageBinding AddFlowStageBinding(this Flow flow, Output<string> stageUuid)
  {
    return AddFlowStageBinding(flow, new FlowStageBindingArgs()
    {
      Stage = stageUuid
    });
  }

  public static Flow AddPolicyBinding(this Flow flow, PolicyBindingArgs args)
  {
    var currentOrder = BindingOrder.GetValueOrDefault(flow.GetResourceName(), 0);
    BindingOrder[flow.GetResourceName()] = currentOrder += 10;
    args.Order = currentOrder;
    args.Target = flow.Uuid;
    _ = new PolicyBinding($"{flow.GetResourceName()}-policy-{currentOrder}", args, new() { Parent = flow });
    return flow;
  }

  public static Flow AddPolicyBinding(this Flow flow, PolicyExpression policy)
  {
    return AddPolicyBinding(flow, policy.PolicyExpressionId);
  }

  public static Flow AddPolicyBinding(this Flow flow, Output<string> policyUuid)
  {
    return AddPolicyBinding(flow, new PolicyBindingArgs()
    {
      Policy = policyUuid
    });
  }

  public static FlowStageBinding AddPolicyBinding(this FlowStageBinding binding, PolicyBindingArgs args)
  {
    var currentOrder = BindingOrder.GetValueOrDefault(binding.GetResourceName(), 0);
    BindingOrder[binding.GetResourceName()] = currentOrder += 10;
    args.Order = currentOrder;
    args.Target = binding.FlowStageBindingId;
    _ = new PolicyBinding($"{binding.GetResourceName()}-policy-{currentOrder}", args, new() { Parent = binding });
    return binding;
  }

  public static FlowStageBinding AddPolicyBinding(this FlowStageBinding flow, CustomResource policy)
  {
    return AddPolicyBinding(flow, new PolicyBindingArgs()
    {
      Policy = policy.Id
    });
  }
}
