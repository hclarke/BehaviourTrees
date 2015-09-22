using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;


public static class BehaviourTreeEx {
    public static T Name<T>(this T node, string name) where T : StaticBehaviourTree {
        var clone = node.Clone<T>();
        clone.name = name;

        return clone;
    }

    public static T AddGuard<T>(this T node, Func<BehaviourTreeRoot, BehaviourStatus, int, bool> guard) where T:StaticBehaviourTree {
        var clone = node.Clone<T>();
        var g1 = clone.Guard;
        var g2 = guard;
        if (g1 != null) {
            g2 = (obj, status, state) => g1(obj, status, state) && guard(obj, status, state);
        }

        clone.Guard = g2;
        return clone;
    }

    public static T AddAction<T>(this T node, Action<BehaviourTreeRoot, BehaviourStatus, int> action) where T : StaticBehaviourTree {
        return node.AddGuard(
            (obj, status, state) => {
                action(obj, status, state);
                return true;
            });
    }

    public static OrNode AddPanic(this StaticBehaviourTree node, StaticBehaviourTree handler, Func<BehaviourTreeRoot, BehaviourStatus, int,bool> condition) {
        return
            node.AddGuard((a, b, c) => !condition(a, b, c)) | handler;

    }
}
public class ConditionNode : StaticBehaviourTree {
    Func<BehaviourTreeRoot, bool> condition;

    public ConditionNode(Func<BehaviourTreeRoot, bool> condition) {
        this.condition = condition;
    }
    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        if (condition(obj)) {
            status = BehaviourStatus.Pass;
        }
        else {
            status = BehaviourStatus.Fail;
        }
    }
}

public class ActionNode : StaticBehaviourTree {
    Action<BehaviourTreeRoot> action;

    public ActionNode(Action<BehaviourTreeRoot> action) {
        this.action = action;
    }
    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        action(obj);
        status = BehaviourStatus.Pass;
    }
}

public class WaitNode : StaticBehaviourTree {
    int frames;
    public WaitNode(int frames) {
        this.frames = frames;
    }
    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        if (state < frames) status = BehaviourStatus.Continue;
        else status = BehaviourStatus.Pass;
    }
}
public class AndNode : StaticBehaviourTree {
    StaticBehaviourTree[] children;
    public override StaticBehaviourTree[] Children {
        get {
            return children;
        }
    }

    public AndNode(params StaticBehaviourTree[] children) {
        this.children = children;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (status) {
            case BehaviourStatus.Start:
            case BehaviourStatus.Pass:
                if (state >= children.Length) {
                    status = BehaviourStatus.Pass;
                }
                else {
                    status = BehaviourStatus.Call;
                }
                break;
            case BehaviourStatus.Fail:
                status = BehaviourStatus.Fail;
                break;
            default:
                throw new NotImplementedException(
                    "unexpected status: " + status);
        }
    }
}

public class OrNode : StaticBehaviourTree {
    StaticBehaviourTree[] children;
    public override StaticBehaviourTree[] Children {
        get {
            return children;
        }
    }

    public OrNode(params StaticBehaviourTree[] children) {
        this.children = children;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (status) {
            case BehaviourStatus.Start:
            case BehaviourStatus.Fail:
                if (state >= children.Length) {
                    status = BehaviourStatus.Fail;
                }
                else {
                    status = BehaviourStatus.Call;
                }
                break;
            case BehaviourStatus.Pass:
                status = BehaviourStatus.Pass;
                break;
            default:
                throw new NotImplementedException(
                    "unexpected status: " + status);
        }
    }
}

public class ForceTrueNode : StaticBehaviourTree {
    StaticBehaviourTree child;
    public override StaticBehaviourTree[] Children {
        get {
            return new[] {child};
        }
    }

    public ForceTrueNode( StaticBehaviourTree child) {
        this.child = child;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (status) {
            case BehaviourStatus.Start:
                status = BehaviourStatus.Call;
                break;
            case BehaviourStatus.Pass:
            case BehaviourStatus.Fail:
                status = BehaviourStatus.Pass;
                break;
            default:
                throw new NotImplementedException("unexpected status: " + status);
        }
    }
}

public class TimeNode : StaticBehaviourTree {
    int time;
    public TimeNode(float time) {
        this.time = (int)(time * 10000);
    }
    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        var dt = (int)(Time.deltaTime * 10000);
        state += dt;
        if (state >= time) {
            status = BehaviourStatus.Pass;
        }
        else {
            status = BehaviourStatus.Continue;
            state--;
        }
    }
}


public class AllNode : StaticBehaviourTree {
    StaticBehaviourTree[] children;
    public override StaticBehaviourTree[] Children {
        get {
            return children;
        }
    }

    public AllNode(params StaticBehaviourTree[] children) {
        this.children = children;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (status) {
            case BehaviourStatus.Start:
            case BehaviourStatus.Pass:
            case BehaviourStatus.Fail:
                if (state >= children.Length) {
                    status = BehaviourStatus.Pass;
                }
                else {
                    status = BehaviourStatus.Call;
                }
                break;
            default:
                throw new NotImplementedException(
                    "unexpected status: " + status);
        }
    }
}

public class NotNode : StaticBehaviourTree {
    StaticBehaviourTree child;
    public override StaticBehaviourTree[] Children {
        get {
            return new[] {child};
        }
    }

    public NotNode( StaticBehaviourTree child) {
        this.child = child;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (status) {
            case BehaviourStatus.Start:
                status = BehaviourStatus.Call;
                break;
            case BehaviourStatus.Pass:
                status = BehaviourStatus.Fail;
                break;
            case BehaviourStatus.Fail:
                status = BehaviourStatus.Pass;
                break;
            default:
                throw new NotImplementedException("unexpected status: " + status);
        }
    }
}

public class LoopNode : StaticBehaviourTree {
    StaticBehaviourTree child;
    public override StaticBehaviourTree[] Children {
        get {
            return new[] {child};
        }
    }

    public LoopNode( StaticBehaviourTree child) {
        this.child = child;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (state) {
            case 0:
                status = BehaviourStatus.Call;
                break;
            case 1:
                switch (status) {
                    case BehaviourStatus.Pass:
                        status = BehaviourStatus.Continue;
                        state = -1;
                        break;
                    case BehaviourStatus.Fail:
                        status = BehaviourStatus.Pass;
                        break;
                    default: throw new NotImplementedException("unexpected status: " + status);
                }
                break;
            default: throw new NotImplementedException("unexpected state: " + state);
        }
    }
}

public class RandomNode : StaticBehaviourTree {
    StaticBehaviourTree[] children = new StaticBehaviourTree[0];
    float[] weights = new float[0];
    float total = 0;
    public override StaticBehaviourTree[] Children {
        get {
            return children;
        }
    }


    public RandomNode() {

    }


    public RandomNode Add(StaticBehaviourTree child, float weight) {
        var clone = MemberwiseClone() as RandomNode;

        clone.total += weight;

        clone.children = new StaticBehaviourTree[children.Length + 1];
        children.CopyTo(clone.children, 0);
        clone.children[children.Length] = child;

        clone.weights = new float[weights.Length + 1];
        weights.CopyTo(clone.weights, 0);
        clone.weights[weights.Length] = total+weight;

        return clone;
    }

    public override void Run(BehaviourTreeRoot obj, ref BehaviourStatus status, ref int state) {
        switch (status) {
            case BehaviourStatus.Start:
                status = BehaviourStatus.Call;
                var r = UnityEngine.Random.Range(0, total);
                for (int i = 0; i < weights.Length; ++i) {
                    state = i;
                    if (weights[i] > r) break;
                }
                break;
            case BehaviourStatus.Pass:
            case BehaviourStatus.Fail:
                break;
            default:
                throw new NotImplementedException("unexpected status: " + status);
        }
    }
}
