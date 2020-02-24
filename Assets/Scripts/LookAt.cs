using UnityEngine;

[ExecuteInEditMode]
public class LookAt : MonoBehaviour
{
  public Transform Target;

  void Update()
  {
    transform.LookAt(Target, Vector3.up);
  }
}
