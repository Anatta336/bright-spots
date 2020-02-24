using UnityEngine;

public class OrbitHorizontal : MonoBehaviour
{
  public Transform Target;
  public float RadiansPerSecond;

  float distance;
  float y;
  float angle;

  void OnEnable()
  {
    Vector3 displacement = transform.position - Target.position;
    distance = displacement.magnitude;
    angle = Mathf.Atan2(displacement.z, displacement.x);
    
    y = transform.position.y;
  }

  void Update()
  {
    angle = (angle + RadiansPerSecond * Time.deltaTime) % (2f * Mathf.PI);

    Vector3 displacement = new Vector3(
      Mathf.Cos(angle) * distance,
      y,
      Mathf.Sin(angle) * distance
    );
    transform.position = Target.TransformPoint(displacement);
  }
}
