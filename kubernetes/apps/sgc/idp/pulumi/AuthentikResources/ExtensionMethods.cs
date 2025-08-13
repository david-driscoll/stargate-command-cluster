using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pulumi;
using Pulumi.Authentik;

namespace applications.AuthentikResources;

static class ExtensionMethods
{
  private static readonly Dictionary<string, int> BindingOrder = new();

  public static FlowStageBinding AddFlowStageBinding(this Flow flow, FlowStageBindingArgs args)
  {
    if (!BindingOrder.TryGetValue(flow.GetResourceName(), out var currentOrder))
    {
      currentOrder = BindingOrder[flow.GetResourceName()] = 0;
    }

    BindingOrder[flow.GetResourceName()] = currentOrder += 10;
    args.Order = currentOrder;
    args.Target = flow.Uuid;
    return new FlowStageBinding($"{flow.GetResourceName()}-binding-{currentOrder:00}", args, new () { Parent = flow });
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
    if (!BindingOrder.TryGetValue(flow.GetResourceName(), out var currentOrder))
    {
      currentOrder = BindingOrder[flow.GetResourceName()] = 0;
    }

    BindingOrder[flow.GetResourceName()] = currentOrder += 10;
    args.Order = currentOrder;
    args.Target = flow.Uuid;
    _ = new PolicyBinding($"{flow.GetResourceName()}-policy-{currentOrder}", args, new () { Parent = flow });
    return flow;
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
    if (!BindingOrder.TryGetValue(binding.GetResourceName(), out var currentOrder))
    {
      currentOrder = BindingOrder[binding.GetResourceName()] = 0;
    }

    BindingOrder[binding.GetResourceName()] = currentOrder += 10;
    args.Order = currentOrder;
    args.Target = binding.FlowStageBindingId;
    _ = new PolicyBinding($"{binding.GetResourceName()}-policy-{currentOrder}", args, new () { Parent = binding });
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
