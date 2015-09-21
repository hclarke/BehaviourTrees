using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public abstract class BehaviourTreeRoot : MonoBehaviour {

    static Dictionary<Type, CompiledBehaviourTree> compiledTrees = new Dictionary<Type, CompiledBehaviourTree>();
    public string current;
    CompiledBehaviourTree tree;
    BehaviourState state;
    void Start() {
        var type = GetType();
        if(!compiledTrees.TryGetValue(type, out tree)) {
            compiledTrees[type] = tree = new CompiledBehaviourTree(GetBehaviourTree());
        }
    }

    protected abstract StaticBehaviourTree GetBehaviourTree();

    void Update() {
        tree.Run(gameObject, ref state);
        current = tree.GetNode(state).ToString() ;
    }
}
