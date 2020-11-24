using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Min(0f)]
    [SerializeField] private float speed = 1f;

    [Min(0f)]
    [SerializeField] private float shiftMultiplier = 10f;

    private void Update()
    {
        var calculatedShiftMultiplier = Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f;
        var input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        transform.position += input * (speed * Time.deltaTime * calculatedShiftMultiplier);
    }
}