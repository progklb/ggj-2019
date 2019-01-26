﻿using UnityEngine;

using System.Collections;

namespace DerbyRoyale.Vehicles
{
    [RequireComponent(typeof(Rigidbody), typeof(MeshRenderer), typeof(MeshFilter))]
    [RequireComponent(typeof(VehicleController))]
    [AddComponentMenu("Derby Royale/Vehicles/Derby Car")]
    public sealed class DerbyCar : MonoBehaviour
    {
        #region CONSTANTS
        private const float DEFAULT_ACCELERATION = 1000f;
        private const float TURN_RATE = 2.5f;

        private const float MAXIMUM_CRASH_VELOCITY = 1000f;
        private const float MAXIMUM_CRASH_DAMAGE = 0.5f;

        private const float DEFAULT_HEALTH = 1f;
        private const float BOOST_MULTIPLIER = 1.5f;
        private const float ARMOR_MULTIPLIER = 0.5f;

        private const float TRASHED_DESTRUCTION_DELAY = 3f;
        private const float TRASHED_EXPLOSION_FORCE = 1000f;
        private const float TRASHED_EXPLOSION_RADIUS = 200f;
        #endregion


        #region PROPERTIES
        private Rigidbody rigidBody { get => m_RigidBody ?? (m_RigidBody = GetComponent<Rigidbody>()); }
        private Vector3 forwardAcceleration { get => transform.forward * DEFAULT_ACCELERATION * Time.deltaTime; }
        private Vector3 rightTurningTorque { get => transform.up * (vehicleController.turning * TURN_RATE * Time.deltaTime); }

        private VehicleController vehicleController { get => m_VehicleController ?? (m_VehicleController = GetComponent<VehicleController>()); }
        private FloorDetectionComponent[] floorDetectionComponents { get => m_FloorDetectionComponents; }

        public float currentHealth { get => Mathf.Clamp01(m_CurrentHealth); set { m_CurrentHealth = Mathf.Clamp01(value); } }
        public bool hasMaxHealth { get => currentHealth == 1f; }
        public bool isTrashed { get => currentHealth == 0f; }
        public bool isBoosting { get; set; }
        public bool isArmored { get; set; }
        public bool isGrounded { get => RefreshFloorDetection(); }
        #endregion


        #region EDITOR FIELDS
        [Space(3), Header("DERBY CAR SETUP"), Space(5)]
        [SerializeField]
        private FloorDetectionComponent[] m_FloorDetectionComponents;
        #endregion


        #region VARIABLES
        private Rigidbody m_RigidBody;
        private VehicleController m_VehicleController;
        private float m_CurrentHealth;
        #endregion


        #region UNITY EVENTS
        void Start()
        {
            RestartCar();
        }

        void FixedUpdate()
        {
            CarEngine();
        }

        void OnCollisionEnter(Collision col)
        {
            CrashCar(col);
        }
        #endregion


        #region PUBLIC API
        public void RestartCar()
        {
            currentHealth = DEFAULT_HEALTH;
            isBoosting = false;
            isArmored = false;
        }

        public void AccelerateCar()
        {
            if (isTrashed || !isGrounded)
            {
                return;
            }

            if (isBoosting)
            {
                rigidBody.AddForce(forwardAcceleration * BOOST_MULTIPLIER, ForceMode.Acceleration);
            }
            else
            {
                rigidBody.AddForce(forwardAcceleration, ForceMode.Acceleration);
            }

            if (isTrashed)
            {
                TrashCar();
            }
        }

        public void TurnCar()
        {
            if (isTrashed || !isGrounded)
            {
                return;
            }

            if (vehicleController.acceleration != 0f)
            {
                rigidBody.AddTorque(rightTurningTorque, ForceMode.Force);
            }
        }

        public void DamageCar(float damageAmount)
        {
            if (isTrashed)
            {
                return;
            }

            if (isArmored)
            {
                currentHealth -= damageAmount * ARMOR_MULTIPLIER;
            }
            else
            {
                currentHealth -= damageAmount;
            }

            if (isTrashed)
            {
                TrashCar();
            }
        }

        public void HealCar(float healingAmount)
        {
            if (isTrashed)
            {
                return;
            }

            currentHealth += healingAmount;
        }

        public void BoostCar(float boostDuration)
        {
            if (isTrashed)
            {
                return;
            }

            if (!isBoosting)
            {
                RunBoostTimer(boostDuration);
            }
        }

        public void ApplyCarArmor(float armorDuration)
        {
            if (isTrashed)
            {
                return;
            }

            if (!isArmored)
            {
                RunArmorTimer(armorDuration);
            }
        }

        public void TrashCar()
        {
            currentHealth = 0f;
            isBoosting = false;
            isArmored = false;

            rigidBody.AddExplosionForce(TRASHED_EXPLOSION_FORCE, transform.position, TRASHED_EXPLOSION_RADIUS, 1.5f, ForceMode.Impulse);
            RunDestructionTimer();
        }
        #endregion


        #region HELPER FUNCTIONS
        void CarEngine()
        {
            if (isTrashed)
            {
                return;
            }

            if (vehicleController.isAccelerating)
            {
                AccelerateCar();
            }

            if (vehicleController.isTurning)
            {
                TurnCar();
            }
        }

        bool RefreshFloorDetection()
        {
            bool anyAreGrounded = false;

            foreach(FloorDetectionComponent detection in floorDetectionComponents)
            {
                if (detection.isGrounded)
                {
                    anyAreGrounded = true;
                }
            }

            return anyAreGrounded;
        }

        void CrashCar(Collision collision)
        {
            float crashDamage = Mathf.Clamp01(collision.relativeVelocity.magnitude / MAXIMUM_CRASH_VELOCITY);
            DamageCar(Mathf.Lerp(0f, MAXIMUM_CRASH_DAMAGE, crashDamage));
        }

        IEnumerator RunBoostTimer(float boostDuration)
        {
            isBoosting = true;
            yield return new WaitForSeconds(boostDuration);
            isBoosting = false;
        }

        IEnumerator RunArmorTimer(float armorDuration)
        {
            isArmored = true;
            yield return new WaitForSeconds(armorDuration);
            isArmored = false;
        }

        IEnumerator RunDestructionTimer()
        {
            yield return new WaitForSeconds(TRASHED_DESTRUCTION_DELAY);
            Destroy(gameObject);
        }
        #endregion
    }
}