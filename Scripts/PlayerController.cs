using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{

    public CharacterController controller;

    [Header("Movement Settings")]
    [SerializeField] private float ground_moving_speed = 5f;
    [SerializeField] private float air_moving_speed_mod = 2f;
    public float movementSpeed = 5f; // Швидкість руху персонажа
    public float gravity = -9.81f; // Прискорення вільного падіння
    public float jumpHeight = 1.5f; // Висота стрибка

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f; // Чутливість миші
    private Transform cameraTransform; // Посилання на камеру
    private float verticalRotation = 0f; // Поточна вертикальна орієнтація камери

    private Vector3 velocity; // Поточна швидкість персонажа
    private bool isGrounded; // Перевірка на контакт із землею

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [SerializeField] private float place_on_surface_delay = 0.1f;
    [SerializeField] private float ray_check_height = 50f;
    void Start() {

        // Блокуємо курсор у центрі екрана
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gravity = Physics.gravity.y;

        cameraTransform = Camera.main.transform;
        controller = GetComponent<CharacterController>();

        StartCoroutine(Delay(place_on_surface_delay, () => PlacePlayerOnTerrainSurface(ray_check_height)));
    }

    private bool isGrounded_prev_frame = true;
    void Update() {

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            //velocity.y = -2f; // Тримаємо персонажа притиснутим до землі
            if(isGrounded_prev_frame == false) {
                animator.SetTrigger("land");
            }
        }

        // Отримуємо вхід від WASD/стрілок
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move *= movementSpeed;

        animator.SetFloat("run_speed", move.magnitude);

        // Рух персонажа
        controller.Move(move * Time.deltaTime);

        // Обробка гравітації
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Обробка обертання камери
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Горизонтальне обертання
        transform.Rotate(Vector3.up * mouseX);

        // Вертикальне обертання камери
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -60f, 60f); // Обмежуємо кут
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Стрибок
        if (Input.GetButtonDown("Jump") && isGrounded) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // jump formula !!!
            animator.SetTrigger("jump");
        }

        isGrounded_prev_frame = isGrounded;
        
    }

    
    private IEnumerator Delay(float time, Action action) {
        yield return new WaitForSeconds(time);
        action?.Invoke();
    }

    public void PlacePlayerOnTerrainSurface(float ray_check_height) {
        Vector3 characterPosition = transform.position;
        
        Vector3 rayStartPos = new Vector3(characterPosition.x, ray_check_height, characterPosition.z);
        Ray ray = new Ray(rayStartPos, Vector3.down);
        RaycastHit hit;

        Debug.DrawLine(rayStartPos, rayStartPos+Vector3.down*ray_check_height*2, Color.red, 10f);
        
        if (Physics.Raycast(ray, out hit, ray_check_height*2)) {
            transform.position = new Vector3(characterPosition.x, hit.point.y, characterPosition.z);
        } else {
            Debug.LogWarning("No collision detected. The ray missed any colliders.");
        }
    }

}
