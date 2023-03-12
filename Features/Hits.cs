﻿using System.Collections.Generic;
using EFT.Trainer.Configuration;
using EFT.Trainer.Extensions;
using EFT.Trainer.Model;
using EFT.Trainer.UI;
using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace EFT.Trainer.Features
{
	[UsedImplicitly]
	internal class Hits : ToggleFeature
	{
		public override string Name => "hits";

		public override bool Enabled { get; set; } = false;

		[ConfigurationProperty(Order = 10)]
		public Color HitMarkerColor { get; set; } = new(225f / 255f, 66f / 255f, 33f / 255f, 1.0f);

		[ConfigurationProperty(Order = 11)]
		public Color ArmorDamageColor { get; set; } = new(0.0f, 126f / 255f, 1.0f, 1.0f);

		[ConfigurationProperty(Order = 12)]
		public Color HealthDamageColor { get; set; } = new(1.0f, 33f/255f, 33f/255f, 1.0f);

		[ConfigurationProperty(Order = 20)]
		public float DisplayTime { get; set; } = 2f;

		[ConfigurationProperty(Order = 21)]
		public float FadeOutTime { get; set; } = 1f;

		[ConfigurationProperty(Order = 30)]
		public bool ShowHitMarker { get; set; } = true;

		[ConfigurationProperty(Order = 31)]
		public bool ShowArmorDamage { get; set; } = true;

		[ConfigurationProperty(Order = 32)]
		public bool ShowHealthDamage { get; set; } = true;

		[ConfigurationProperty(Order = 33)]
		public bool ShowCrossTickMarker { get; set; } = true;

		[ConfigurationProperty(Order = 34)]
		public float ScaleX { get; set; } = 1.0f;

		[ConfigurationProperty(Order = 35)]
		public float ScaleY { get; set; } = 1.0f;

		[ConfigurationProperty(Order = 36)]
		public float TickDisplayTime { get; set; } = 0.3f;

		[ConfigurationProperty(Order = 37)]
		public float TickFadeTime { get; set; } = 1.0f;


		internal class HitMarker
		{
			public HitMarker(DamageInfoWrapper damageInfo)
			{
				DamageInfo = damageInfo;
			}

			public float ElapsedTime { get; set; } = 0.0f;
			public DamageInfoWrapper DamageInfo { get; set; }
			public bool IsTaggedForDeletion { get; set; } = false;
		}

		private static readonly HashSet<HitMarker> _hitMarkers = new();

#pragma warning disable IDE0060 
		[UsedImplicitly]
		protected static void ApplyDamagePostfix(EBodyPart bodyPart, float damage, object damageInfo, object? __instance)
		{
			var feature = FeatureFactory.GetFeature<Hits>();
			if (feature == null || !feature.Enabled)
				return;

			if (__instance == null)
				return;

			var healthControllerWrapper = new HealthControllerWrapper(__instance);

			var victim = healthControllerWrapper.Player;
			if (victim == null || victim.IsYourPlayer)
				return;

			var damageInfoWrapper = new DamageInfoWrapper(damageInfo);
			var shooter = damageInfoWrapper.Player;
			if (shooter == null || !shooter.IsYourPlayer)
				return;

			var marker = new HitMarker(damageInfoWrapper);
			_hitMarkers.Add(marker);
		}
#pragma warning restore IDE0060 

		protected override void OnGUIWhenEnabled()
		{
			var camera = GameState.Current?.Camera;
			if (camera == null)
				return;

			foreach (var marker in _hitMarkers)
			{
				var damageInfo = marker.DamageInfo;
				marker.ElapsedTime += Time.deltaTime;

				if (damageInfo.Weapon == null || marker.ElapsedTime >= DisplayTime + FadeOutTime)
				{
					marker.IsTaggedForDeletion = true;
					continue;
				}

				var alpha = marker.ElapsedTime > DisplayTime && FadeOutTime > 0f ? (FadeOutTime - marker.ElapsedTime + DisplayTime) / FadeOutTime : 1f;
				var armorDamage = Mathf.Round(damageInfo.ArmorDamage);
				var damage = Mathf.Round(damageInfo.Damage);
				var hitPoint = damageInfo.HitPoint;
				var screenHitPoint = camera.WorldPointToScreenPoint(hitPoint, ScaleX, ScaleY);

				if (ShowHitMarker)
				{
					var hitmarkerColor = HitMarkerColor.SetAlpha(alpha);
					var radius = 16f + marker.ElapsedTime * 2;

					Render.DrawCircle(screenHitPoint, radius, hitmarkerColor, 2.98f, 32);
				}

				if (ShowCrossTickMarker)
				{
					var tickAlpha = marker.ElapsedTime > TickDisplayTime && TickFadeTime > 0f ?
						(TickFadeTime - marker.ElapsedTime + TickDisplayTime) / TickFadeTime : 1.0f;

					var tickMarkerColor = HitMarkerColor.SetAlpha(tickAlpha);

					// Draw an X tick with a hollow center
					const float tickDistance = 32f;
					const float halfTickDistance = tickDistance / 2;
					const int thickness = 4;

					var centerx = Screen.width / 2;
					var centery = Screen.height / 2;
					// bottom left tick to center
					Render.DrawLine(new Vector2(centerx - tickDistance, centery - tickDistance), new Vector2(centerx - halfTickDistance, centery - halfTickDistance), thickness, tickMarkerColor);
					// bottom right tick to center
					Render.DrawLine(new Vector2(centerx + tickDistance, centery - tickDistance), new Vector2(centerx + halfTickDistance, centery - halfTickDistance), thickness, tickMarkerColor);
					// top right tick to center
					Render.DrawLine(new Vector2(centerx + tickDistance, centery + tickDistance), new Vector2(centerx + halfTickDistance, centery + halfTickDistance), thickness, tickMarkerColor);
					// top left tick to center
					Render.DrawLine(new Vector2(centerx - tickDistance, centery + tickDistance), new Vector2(centerx - halfTickDistance, centery + halfTickDistance), thickness, tickMarkerColor);
				}

				var offset = 0f;
				if (armorDamage > 0 && ShowArmorDamage)
				{
					offset = 10f;
					Render.DrawString(new Vector2(screenHitPoint.x, screenHitPoint.y - offset), $"{armorDamage}", ArmorDamageColor.SetAlpha(alpha));
				}

				if (damage > 0 && ShowHealthDamage)
					Render.DrawString(new Vector2(screenHitPoint.x, screenHitPoint.y + offset), $"{damage}", HealthDamageColor.SetAlpha(alpha));
			}

			_hitMarkers.RemoveWhere(m => m.IsTaggedForDeletion);
		}

		protected override void UpdateWhenEnabled()
		{
			var player = GameState.Current?.LocalPlayer;
			if (!player.IsValid())
				return;

			var healthController = player.ActiveHealthController;
			if (healthController == null)
				return;

			HarmonyPatchOnce(harmony =>
			{
				var original = HarmonyLib.AccessTools.Method(healthController.GetType(), nameof(healthController.ApplyDamage));
				if (original == null)
					return;

				var postfix = HarmonyLib.AccessTools.Method(GetType(), nameof(ApplyDamagePostfix));
				if (postfix == null)
					return;

				harmony.Patch(original, null, new HarmonyLib.HarmonyMethod(postfix));
			});
		}
	}
}
