using UnityEngine;
using VaSiLi.SceneManagement;

public class DisableOnRole : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        RoleManager.roleChanged += OnRoleChanged;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDisable()
    {
        RoleManager.roleChanged -= OnRoleChanged;
    }

    void OnRoleChanged(ApiRole? role)
    {
        if (role.HasValue && role.Value.id == "Interviewer")
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
