using UnityEngine;
using System.Collections;
using System;

public class PingPongBehaviour : BehaviourTreeRoot {

    public Transform pingTarget;
    public Transform pongTarget;

    protected override StaticBehaviourTree GetBehaviourTree() {
        var moveToPing = MoveToward(obj => obj.GetComponent<PingPongBehaviour>().pingTarget.position).Loop();
        var moveToPong = MoveToward(obj => obj.GetComponent<PingPongBehaviour>().pongTarget.position).Loop();

        return (moveToPing & moveToPong).Loop();
    }

    StaticBehaviourTree MoveToward(Func<BehaviourTreeRoot, Vector3> getTarget) {
        return StaticBehaviourTree.Create(obj => {
            var target = getTarget(obj);
            var pos = obj.transform.position;
            var delta = target - pos;
            if (delta.magnitude < Time.deltaTime) {
                transform.position = target;
                return false;
            }
            else {
                transform.position += delta.normalized * Time.deltaTime;
                return true;
            }
        },
        "Move Toward");
    }
}
