using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class Item : VivaSessionAsset
    {


        private Coroutine waterCoroutine = null;
        private CountedSet<BoxCollider> waterSurfaces = new CountedSet<BoxCollider>();
        private CountedSet<WaterCurrent> waterCurrents = new CountedSet<WaterCurrent>();

        public void OnTriggerEnter(Collider collider)
        {

            if (collider.gameObject.layer != WorldUtil.waterLayer)
            {
                return;
            }
            //disable buoyancy if sphere radius is zero
            if (settings.buoyancySphereRadius == 0.0f)
            {
                return;
            }
            BoxCollider bc = collider as BoxCollider;
            if (bc == null)
            {
                return;
            }
            waterSurfaces.Add(bc);
            if (waterCoroutine == null)
            {
                waterCoroutine = GameDirector.instance.StartCoroutine(Buoyancy());
            }

            WaterCurrent wc = collider.gameObject.GetComponent<WaterCurrent>();
            if (wc != null)
            {
                waterCurrents.Add(wc);
            }
        }

        private void SplashWaterSurface(Vector3 surfacePos, Vector3 splashDir, float speed)
        {
            Quaternion rot = Quaternion.LookRotation(splashDir, Vector3.up);
            GameDirector.instance.SplashWaterFXAt(surfacePos, rot, speed * 0.1f, speed * 0.5f, 5);

            //find characters nearby to splash
            List<Character> characters = GameDirector.instance.FindCharactersInSphere(
                (int)Character.Type.LOLI,
                surfacePos + splashDir,
                0.5f
            );

            if (settings.splashSounds != null)
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(settings.splashSounds.GetRandomAudioClip());
            }
        }

        private void ApplyWaterForces(Rigidbody active, float maxSubmerged, Vector3 buoyancyForce)
        {
            //apply water friction
            Vector3 vel = active.velocity;
            vel.y *= 1.0f - maxSubmerged * 0.2f;
            vel.x *= 1.0f - maxSubmerged * 0.02f;
            vel.z *= 1.0f - maxSubmerged * 0.02f;
            active.velocity = vel;

            //apply water current forces
            if (waterCurrents.Count > 0)
            {
                Vector3 currentForce = Vector3.zero;
                for (var i = 0; i < waterCurrents.Count; i++)
                {
                    currentForce += waterCurrents[i].force;
                }
                currentForce *= maxSubmerged / waterCurrents.Count;

                active.AddForce(currentForce, ForceMode.Force);
            }

            //float on surface
            buoyancyForce *= -Physics.gravity.y * settings.buoyancyStrength * maxSubmerged;
            buoyancyForce /= waterSurfaces.Count;
            active.AddForce(buoyancyForce, ForceMode.Force);
        }

        private void ApplyRotationalWaterForces(Rigidbody active, float maxSubmerged)
        {
            //angle upwards
            active.angularVelocity *= 1.0f - maxSubmerged * 0.05f;
            if (settings.buoyancyEuler != Vector3.zero)
            {
                Vector3 forwardFlat;
                Quaternion targetFloatRotation;
                if (Math.Abs(settings.buoyancyEuler.z) == 1.0f)
                {
                    forwardFlat = transform.right;
                    forwardFlat.y = 0.001f;
                    targetFloatRotation = Quaternion.LookRotation(forwardFlat, Vector3.up * settings.buoyancyEuler.z);
                    targetFloatRotation *= Quaternion.Euler(90.0f, 0.0f, 90.0f);
                }
                else
                {
                    forwardFlat = transform.forward;
                    forwardFlat.y = 0.001f;
                    targetFloatRotation = Quaternion.LookRotation(forwardFlat, settings.buoyancyEuler);
                }
                Quaternion rotForce = targetFloatRotation * Quaternion.Inverse(transform.rotation);
                float rotForceScalar = rotForce.w * maxSubmerged;
                active.AddTorque(rotForce.x * rotForceScalar, rotForce.y * rotForceScalar, rotForce.z * rotForceScalar, ForceMode.VelocityChange);
            }
        }

        private IEnumerator Buoyancy()
        {

            bool itemBeingHeld = (settings.itemType != Type.CHARACTER && mainOwner != null);
            bool firstEntry = true;
            float lastFastSplashTime = UnityEngine.Random.value;
            while (true)
            {
                if (rigidBody == null)
                {
                    break;
                }
                if (waterSurfaces.Count == 0)
                {
                    break;
                }
                if (rigidBody.IsSleeping())
                {
                    break;
                }
                Plane waterPlane = new Plane();
                float maxSubmerged = 0.0f;
                Vector3 buoyancyForce = Vector3.zero;

                for (int i = 0; i < waterSurfaces.Count; i++)
                {
                    var bc = waterSurfaces[i];
                    Vector3 waterSurface = bc.transform.TransformPoint(bc.center + Vector3.up * bc.size.y * 0.5f);
                    waterPlane.SetNormalAndPosition(bc.transform.up, waterSurface);
                    //calculate how much buoyancy force based on how submerged it is
                    float submerged = settings.buoyancySphereRadius - waterPlane.GetDistanceToPoint(rigidBody.worldCenterOfMass);
                    submerged = Mathf.Clamp01(submerged / (settings.buoyancySphereRadius * 2.0f));
                    maxSubmerged = Math.Max(maxSubmerged, submerged);
                    buoyancyForce += waterPlane.normal;

                    //splash once
                    if (firstEntry)
                    {
                        firstEntry = false;
                        float sqSpeed = rigidBody.velocity.sqrMagnitude;
                        if (sqSpeed > 1.0f)
                        {
                            SplashWaterSurface(
                                waterPlane.ClosestPointOnPlane(rigidBody.worldCenterOfMass),
                                Vector3.Reflect(rigidBody.velocity, waterPlane.normal),
                                Mathf.Sqrt(sqSpeed)
                            );
                        }
                    }
                }

                ApplyWaterForces(rigidBody, maxSubmerged, buoyancyForce);

                //splash at high speeds
                if (maxSubmerged < 1.0f && Time.time - lastFastSplashTime > 0.1f && !itemBeingHeld)
                {
                    lastFastSplashTime = Time.time;
                    float sqSpeed = rigidBody.velocity.sqrMagnitude;
                    if (sqSpeed > 1.0f)
                    {
                        SplashWaterSurface(
                            rigidBody.worldCenterOfMass,
                            rigidBody.velocity,
                            Mathf.Sqrt(sqSpeed)
                        );
                    }
                }

                ApplyRotationalWaterForces(rigidBody, maxSubmerged);
                yield return new WaitForFixedUpdate();
            }
            waterCoroutine = null;
        }

        public void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.layer != WorldUtil.waterLayer)
            {
                return;
            }
            //disable buoyancy if sphere radius is zero
            if (settings.buoyancySphereRadius == 0.0f)
            {
                return;
            }
            BoxCollider bc = collider as BoxCollider;
            if (bc == null)
            {
                return;
            }
            waterSurfaces.Remove(bc);

            WaterCurrent wc = collider.gameObject.GetComponent<WaterCurrent>();
            if (wc != null)
            {
                waterCurrents.Remove(wc);
            }
        }
    }


}