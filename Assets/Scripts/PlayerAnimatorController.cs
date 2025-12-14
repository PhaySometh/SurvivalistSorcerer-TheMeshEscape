using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    public Animator animator;
    public PlayerMovementScript movement;
    public CharacterController controller;

    // Parameter names (match these in your Animator)
    public string speedParam = "Speed";
    public string groundedParam = "IsGrounded";
    public string crouchParam = "IsCrouching";
    public string sprintParam = "IsSprinting";
    public string jumpParam = "Jump"; // trigger
    [Header("Debug")]
    public bool debugGUI = false;
    public bool logParametersOnStart = false;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<PlayerMovementScript>();
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (animator == null) return;

        // Speed (horizontal magnitude)
        float speed = 0f;
        if (controller != null)
        {
            Vector3 flatVel = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            speed = flatVel.magnitude;
        }

        SetFloatIfExists(speedParam, speed);

        // Grounded
        bool isGrounded = controller != null ? controller.isGrounded : true;
        SetBoolIfExists(groundedParam, isGrounded);

        // Crouch detection (based on controller height if available)
        bool isCrouch = false;
        if (controller != null && movement != null)
            isCrouch = controller.height < movement.defaultHeight - 0.1f;
        SetBoolIfExists(crouchParam, isCrouch);

        // Sprint
        bool isSprint = movement != null ? movement.IsSprinting : Input.GetKey(KeyCode.LeftShift);
        SetBoolIfExists(sprintParam, isSprint);

        // Jump trigger
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            SetTriggerIfExists(jumpParam);
        }
        if (debugGUI && Application.isPlaying)
        {
            // nothing here; OnGUI displays info
        }
    }

    void Start()
    {
        if (logParametersOnStart && animator != null)
        {
            Debug.Log("Animator parameters for: " + animator.runtimeAnimatorController.name + " -> " + string.Join(", ", System.Array.ConvertAll(animator.parameters, p => p.name + ":" + p.type))); 
        }
    }

    void OnGUI()
    {
        if (!debugGUI || animator == null || !Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200), "Animator Debug", GUI.skin.window);
        GUILayout.Label("State: " + animator.GetCurrentAnimatorStateInfo(0).IsName("") + " (Hash:" + animator.GetCurrentAnimatorStateInfo(0).fullPathHash + ")");
        GUILayout.Label("Layer 0 State: " + animator.GetCurrentAnimatorStateInfo(0).shortNameHash);
        foreach (var p in animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    GUILayout.Label(p.name + ": " + animator.GetBool(p.name));
                    break;
                case AnimatorControllerParameterType.Float:
                    GUILayout.Label(p.name + ": " + animator.GetFloat(p.name).ToString("F2"));
                    break;
                case AnimatorControllerParameterType.Int:
                    GUILayout.Label(p.name + ": " + animator.GetInteger(p.name));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    GUILayout.Label(p.name + ": (Trigger)");
                    break;
            }
        }
        GUILayout.EndArea();
    }

    bool HasParam(string name)
    {
        if (animator == null) return false;
        var pars = animator.parameters;
        for (int i = 0; i < pars.Length; i++) if (pars[i].name == name) return true;
        return false;
    }

    void SetFloatIfExists(string name, float value)
    {
        if (HasParam(name)) animator.SetFloat(name, value);
    }

    void SetBoolIfExists(string name, bool value)
    {
        if (HasParam(name)) animator.SetBool(name, value);
    }

    void SetTriggerIfExists(string name)
    {
        if (HasParam(name)) animator.SetTrigger(name);
    }
}
