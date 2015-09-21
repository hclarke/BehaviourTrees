using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;


public enum BehaviourStatus {
    Start,
    Pass,
    Fail,
    Continue,
    Call,
}
public abstract class StaticBehaviourTree {

    public string name;

    public Func<GameObject, BehaviourStatus, int, bool> Guard;
    public abstract void Run(GameObject obj, ref BehaviourStatus status, ref int state);
    public virtual StaticBehaviourTree[] Children {
        get {
            return new StaticBehaviourTree[0];
        }
    }

    public T Clone<T>() where T : StaticBehaviourTree {
        return MemberwiseClone() as T;
    }

    public static AndNode operator &(StaticBehaviourTree a, StaticBehaviourTree b) {
        return new AndNode(a, b);
    }
    public static AndNode operator &(AndNode a, StaticBehaviourTree b) {
        return new AndNode(a.Children.Concat(new[] { b }).ToArray());
    }

    public static OrNode operator |(StaticBehaviourTree a, StaticBehaviourTree b) {
        return new OrNode(a, b);
    }
    public static OrNode operator |(OrNode a, StaticBehaviourTree b) {
        return new OrNode(a.Children.Concat(new[] { b }).ToArray());
    }

    public static NotNode operator !(StaticBehaviourTree a) {
        return new NotNode(a);
    }

    public LoopNode Loop() {
        return new LoopNode(this);
    }
    public static ActionNode Create(Action<GameObject> action, string name = null) {
        var node = new ActionNode(action);

        node.name = name;
        return node;
    }

    public static ConditionNode Create(Func<GameObject, bool> condition, string name = null) {
        var node = new ConditionNode(condition);
        node.name = name;
        return node;
    }

    public ForceTrueNode ForceTrue() {
        return new ForceTrueNode(this);
    }

    public LoopNode While(Func<GameObject, bool> condition) {
        return (Create(condition) & this.ForceTrue()).Loop();
    }

    public static TimeNode Timer(float time, string name = null) {
        return new TimeNode(time).Name(name);
    }
    public static WaitNode Wait(int frames, string name = null) {
        return new WaitNode(frames).Name(name);
    }

    public override string ToString() {
        if (name != null) {
            return name;
        }
        return base.ToString();
    }
}


public struct BehaviourState {
    public int nodeIndex;
    public int state; //children completed
    public BehaviourStatus status;
}

public class CompiledBehaviourTree {
    struct GuardReturn {
        public Func<GameObject, BehaviourStatus, int, bool> guard;
        public int returnTo;
        public int state;
    }
    struct BehaviourNode {
        public StaticBehaviourTree node;
        public List<GuardReturn> guards;
        public int returnTo;
        public int childNumber;
        public int[] children;
    }
    class RootNode : StaticBehaviourTree {
        public override void Run(GameObject obj, ref BehaviourStatus status, ref int state) {
            if (status == BehaviourStatus.Fail) {
                Debug.LogError("root node failed!");
            }
            switch (state) {
                case 0: status = BehaviourStatus.Call; break;
                case 1: status = BehaviourStatus.Continue; state = -1; break; //loop
                default: throw new Exception("root node in invalid state");
            }
        }
    }
    BehaviourNode[] states;
    public CompiledBehaviourTree(StaticBehaviourTree root) {
        List<BehaviourNode> nodes = new List<BehaviourNode>();
        var rNode = new BehaviourNode() {
            children = new[] { 1 },
            returnTo = 0,
            node = new RootNode()
        };
        nodes.Add(rNode);
        AddToNodes(nodes, root, 0, 0, null);

        states = nodes.ToArray();
    }

    public StaticBehaviourTree GetNode(BehaviourState state) {
        return states[state.nodeIndex].node;
    }

    int AddToNodes(List<BehaviourNode> nodes, StaticBehaviourTree current, int returnTo, int childNumber, List<GuardReturn> guards) {
        List<GuardReturn> nGuards = guards;
        if (current.Guard != null) {
            new List<GuardReturn>(guards ?? new List<GuardReturn>());
            nGuards.Add(new GuardReturn() {
                guard = current.Guard,
                returnTo = returnTo,
                state = childNumber,
            });
        }
        var node = new BehaviourNode() {
            children = new int[current.Children.Length],
            guards = nGuards,
            returnTo = returnTo,
            childNumber = childNumber,
            node = current
        };
        var idx = nodes.Count;
        nodes.Add(node);
        for (int i = 0; i < node.children.Length; ++i) {
            node.children[i] = AddToNodes(nodes, current.Children[i], idx, i, nGuards);
        }
        return idx;        
    }

    public void Run(GameObject obj, ref BehaviourState stack) {
    StepStart:
        var idx = stack.nodeIndex;
        var node = states[idx];
        bool guardPassed = true;
        int guardReturn = -1;
        int guardState = -1;
        switch (stack.status) {
            case BehaviourStatus.Start:
                if (node.node.Guard != null) {
                    guardPassed = node.node.Guard(obj, stack.status, stack.state);
                    if (!guardPassed) {
                        guardReturn = node.returnTo;
                        guardState = node.childNumber;
                    }
                    
                }
                break;
            case BehaviourStatus.Continue:
                if (node.guards != null) {
                    foreach (var g in node.guards) {
                        guardPassed = g.guard(obj, stack.status, stack.state);
                        if (!guardPassed) {
                            guardReturn = g.returnTo;
                            guardState = g.state;
                            break;
                        }
                    }
                }
                break;
            default:
                break;
        }
        if (guardPassed) {
            node.node.Run(obj, ref stack.status, ref stack.state);
            switch (stack.status) {
                case BehaviourStatus.Pass:
                case BehaviourStatus.Fail:
                    stack.nodeIndex = node.returnTo;
                    stack.state = node.childNumber + 1;
                    goto StepStart; //continue processing
                case BehaviourStatus.Call:
                    if (stack.state >= node.children.Length) {
                        Debug.LogError("bad child index: " + stack.state);
                        Debug.LogError(node.node);
                        Debug.LogError(node.node.Children.Length);
                    }
                    stack.nodeIndex = node.children[stack.state];
                    stack.status = BehaviourStatus.Start;
                    stack.state = 0;
                    goto StepStart; //continue processing
                case BehaviourStatus.Continue:
                    stack.state++;
                    break; //halt until next frame
                default:
                    throw new NotImplementedException(
                        "unexpected status: " + stack.status);

            }
        }
        else {
            stack.status = BehaviourStatus.Fail;
            stack.state = guardState;
            stack.nodeIndex = guardReturn;
            goto StepStart;
        }
    }
}