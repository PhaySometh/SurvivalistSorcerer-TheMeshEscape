using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

public static class AnimatorSetup
{
    [MenuItem("Tools/Animator/Add Player Animation Parameters")]
    public static void AddParamsFromSelection()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        var pac = Selection.activeGameObject.GetComponent<PlayerAnimatorController>();
        if (pac == null || pac.animator == null)
        {
            Debug.LogWarning("Selected GameObject must have a PlayerAnimatorController with an Animator assigned.");
            return;
        }

        AddParametersToController(pac.animator.runtimeAnimatorController as AnimatorController, pac);
    }

    public static void AddParametersToController(AnimatorController controller, PlayerAnimatorController pac)
    {
        if (controller == null)
        {
            Debug.LogError("AnimatorController is null or not editable.");
            return;
        }

        AddParamIfMissing(controller, pac.speedParam, AnimatorControllerParameterType.Float);
        AddParamIfMissing(controller, pac.groundedParam, AnimatorControllerParameterType.Bool);
        AddParamIfMissing(controller, pac.crouchParam, AnimatorControllerParameterType.Bool);
        AddParamIfMissing(controller, pac.sprintParam, AnimatorControllerParameterType.Bool);
        AddParamIfMissing(controller, pac.jumpParam, AnimatorControllerParameterType.Trigger);

        AssetDatabase.SaveAssets();
        Debug.Log("Animator parameters ensured on: " + controller.name);
    }

    public static void CreateBasicLocomotion(AnimatorController controller, PlayerAnimatorController pac)
    {
        if (controller == null)
        {
            Debug.LogError("AnimatorController is null or not editable.");
            return;
        }

        // Create a new BlendTree state called Locomotion in Base Layer
        var root = controller.layers[0].stateMachine;
        var locomotionState = root.AddState("Locomotion");

        // Create a BlendTree
        var bt = new BlendTree();
        bt.name = "LocomotionTree";
        bt.blendType = BlendTreeType.Simple1D;
        bt.useAutomaticThresholds = false;
        bt.blendParameter = pac.speedParam;

        // Try to find clips by common names
        AnimationClip idle = FindClip(controller, "Idle01") ?? FindClip(controller, "Idle");
        AnimationClip walk = FindClip(controller, "WalkForward") ?? FindClip(controller, "Walk");
        AnimationClip run = FindClip(controller, "BattleRunForward") ?? FindClip(controller, "Run");

        var childs = new System.Collections.Generic.List<ChildMotion>();
        if (idle != null) childs.Add(new ChildMotion() { motion = idle, threshold = 0f });
        if (walk != null) childs.Add(new ChildMotion() { motion = walk, threshold = 1f });
        if (run != null) childs.Add(new ChildMotion() { motion = run, threshold = 2f });

        // If no clips found, bail
        if (childs.Count == 0)
        {
            Debug.LogWarning("No suitable clips found for locomotion in controller.");
            return;
        }

        // Create the BlendTree object and attach it to the animator controller asset
        var newTree = new BlendTree();
        newTree.name = "LocomotionTree";
        newTree.blendType = BlendTreeType.Simple1D;
        newTree.blendParameter = pac.speedParam;
        newTree.useAutomaticThresholds = false;

        // Assign children
        newTree.children = childs.ToArray();

        // Add the BlendTree object to the AnimatorController asset so it serializes
        AssetDatabase.AddObjectToAsset(newTree, AssetDatabase.GetAssetPath(controller));

        // Attach the BlendTree to the state
        locomotionState.motion = newTree;

        // Set Locomotion as default state
        root.defaultState = locomotionState;

        AssetDatabase.SaveAssets();
        Debug.Log("Basic locomotion blend tree created (if clips were found).");
    }

    static AnimationClip FindClip(AnimatorController controller, string name)
    {
        if (controller == null || string.IsNullOrEmpty(name)) return null;
        var clips = controller.animationClips;
        foreach (var c in clips) if (c != null && c.name == name) return c;
        return null;
    }

    static void AddParamIfMissing(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(name)) return;
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return; // already present
        }

        var param = new AnimatorControllerParameter();
        param.name = name;
        param.type = type;
        controller.AddParameter(param);
        Debug.Log($"Added parameter '{name}' to {controller.name}");
    }
}

// Custom inspector button
[CustomEditor(typeof(PlayerAnimatorController))]
public class PlayerAnimatorControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerAnimatorController pac = (PlayerAnimatorController)target;
        if (GUILayout.Button("Add Missing Animator Parameters"))
        {
            var controller = pac.animator != null ? pac.animator.runtimeAnimatorController as AnimatorController : null;
            AnimatorSetup.AddParametersToController(controller, pac);
        }

        if (GUILayout.Button("Create Basic Locomotion Blend Tree"))
        {
            var controller = pac.animator != null ? pac.animator.runtimeAnimatorController as AnimatorController : null;
            AnimatorSetup.CreateBasicLocomotion(controller, pac);
        }
    }
}
