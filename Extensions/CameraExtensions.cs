using UnityEngine;

#nullable enable

namespace EFT.Trainer.Extensions
{
	public static class CameraExtensions
	{
		public static Vector3 WorldPointToScreenPoint(this Camera camera, Vector3 worldPoint)
		{
			var screenPoint = camera.WorldToScreenPoint(worldPoint);
			screenPoint.y = Screen.height - screenPoint.y;
			return screenPoint;
		}

		public static Vector3 WorldPointToScreenPoint(this Camera camera, Vector3 worldPoint, float scaleX, float scaleY)
		{
			var screenPoint = camera.WorldToScreenPoint(worldPoint);
			screenPoint.x = screenPoint.x * scaleX;
			screenPoint.y = Screen.height - screenPoint.y * scaleY;
			return screenPoint;
		}

		private static readonly LayerMask _layerMask = 1 << 12 | 1 << 16 | 1 << 18 | 1 << 31 | 1 << 22;
		public static bool IsTransformVisible(this Camera camera, Transform transform)
		{
			var origin = camera.transform.position;
			var destination = transform.position;

			if (!Physics.Linecast(origin, destination, out var hitinfo, _layerMask))
				return false;

			return hitinfo.transform == transform;
		}

#pragma warning disable IDE0060 // Remove unused parameter
		public static bool IsScreenPointVisible(this Camera camera, Vector3 screenPoint)
#pragma warning restore IDE0060 // Remove unused parameter
		{
			return screenPoint.z > 0.01f && screenPoint.x > -5f && screenPoint.y > -5f && screenPoint.x < Screen.width + 5f && screenPoint.y < Screen.height + 5f;
		}
	}
}
