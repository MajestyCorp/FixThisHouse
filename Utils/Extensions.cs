using System.Collections.Generic;
using UnityEngine;

namespace FixThisHouse
{
	public static class Extensions
	{
		public static bool Contains(this LayerMask mask, int layer)
		{
			return mask == (mask | (1 << layer));
		}

		public static T Random<T>(this IList<T> list)
		{
			return list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : default;
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			var n = list.Count;

			while (n > 1)
			{
				n--;
				var k = UnityEngine.Random.Range(0, n);
				(list[n], list[k]) = (list[k], list[n]);
			}
		}

		public static void DestroyGameObject(this Component component, bool immediate = false)
		{
			if (immediate)
			{
				UnityEngine.Object.DestroyImmediate(component.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(component.gameObject);
			}
		}

		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			if (gameObject.TryGetComponent<T>(out var component))
				return component;
			else
				return gameObject.AddComponent<T>();
		}

		/// <summary>
		/// Sets a joint's targetRotation to match a given local rotation.
		/// The joint transform's local rotation must be cached on Start and passed into this method.
		/// </summary>
		public static void SetTargetRotationLocal(this ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
		{
			if (joint.configuredInWorldSpace)
			{
				Debug.LogError("SetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
			}
			SetTargetRotationInternal(joint, targetLocalRotation, startLocalRotation, Space.Self);
		}

		/// <summary>
		/// Sets a joint's targetRotation to match a given world rotation.
		/// The joint transform's world rotation must be cached on Start and passed into this method.
		/// </summary>
		public static void SetTargetRotation(this ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
		{
			if (!joint.configuredInWorldSpace)
			{
				Debug.LogError("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
			}
			SetTargetRotationInternal(joint, targetWorldRotation, startWorldRotation, Space.World);
		}

		static void SetTargetRotationInternal(ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
		{
			// Calculate the rotation expressed by the joint's axis and secondary axis
			var right = joint.axis;
			var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
			var up = Vector3.Cross(forward, right).normalized;
			Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);

			// Transform into world space
			Quaternion resultRotation = Quaternion.Inverse(worldToJointSpace);

			// Counter-rotate and apply the new local rotation.
			// Joint space is the inverse of world space, so we need to invert our value
			if (space == Space.World)
			{
				resultRotation *= startRotation * Quaternion.Inverse(targetRotation);
			}
			else
			{
				resultRotation *= Quaternion.Inverse(targetRotation) * startRotation;
			}

			// Transform back into joint space
			resultRotation *= worldToJointSpace;

			// Set target rotation to our newly calculated rotation
			joint.targetRotation = resultRotation;
		}

		public static void Release(this GameObject target)
		{
			target.GetComponent<PoolMember>().Release();
		}
	}
}
