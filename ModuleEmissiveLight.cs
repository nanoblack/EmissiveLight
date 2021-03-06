﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EmissiveLight
{
	public class ModuleEmissiveLight : ModuleLight
	{
		Color lightColor;
		Color emissiveColor;

		// used for emissive mat HACK
		Light referenceLight;
		float lightIntensity = 0f;

		[KSPField(isPersistant = false)]
		public string emissiveName;
		List<Renderer> emissives = new List<Renderer>();

		public override void OnStart(PartModule.StartState state)
		{
			// ModuleLight's OnStart is probably somewhat important
			base.OnStart(state);

			Renderer[] componentArray = part.FindModelComponents<Renderer>(emissiveName);
			if(componentArray != null && componentArray.Length != 0)
				emissives = componentArray.ToList();
			else
				Debug.Log("ModuleEmissiveLight: emissiveName " + emissiveName + " found nothing");

			// expose base tweakable fields in-flight
			this.Fields["lightR"].guiActive = true;
			this.Fields["lightG"].guiActive = true;
			this.Fields["lightB"].guiActive = true;

			// HACK for layer culling mask
			int mask = (1 << 0) | (1 << 3) | (1 << 4) | (1 << 6) | (1 << 7) | (1 << 9) | (1 << 10) | (1 << 15)
				| (0 << 16) | (1 << 18) | (1 << 19) | (1 << 23) | (1 << 24) | (1 << 28);
			if(lights != null) // unsure if base ever allows for this to be null, doesn't hurt to check anyway
			{
				foreach(Light l in lights)
				{
					// some layers might not be used ingame, and some may be used but unnecessary for light
					// DAS WHY IS A HACK
					l.cullingMask = mask;
				}
			}

			// for emissive mat HACK
			referenceLight = lights.FirstOrDefault();
		}

		public void LateUpdate()
		{
			if (HighLogic.LoadedSceneIsEditor | HighLogic.LoadedSceneIsFlight)
			{
				// create color from color tweakable values
				lightColor = new Color(lightR, lightG, lightB);

				// for future reference, light.color seems to be _always_ null
				// why it exists I'll never know, it's useless and probably leftover from old code or something
				// and UpdateLightColors() is a public method but also useless for our purposes
				// because it ref's a private Color field that tweakables only update in editor scene

				// iterate lights list and set colors
				if(lights != null) // unsure if base ever allows for this to be null, doesn't hurt to check anyway
				{
					foreach(Light l in lights)
					{
						l.color = lightColor;
					}
				}

				// HACK pull intensity from a light to use when making emissive color
				// otherwise setting it same way as lights results in emissive mat being always on
				if(referenceLight != null) // you never know
				{
					lightIntensity = referenceLight.intensity;
				}
				// make color for emissive mat
				emissiveColor = new Color(lightColor.r * lightIntensity, lightColor.g * lightIntensity, lightColor.b * lightIntensity);

				// iterate emissives list and set colors
				foreach(Renderer em in emissives)
				{
					// finally set the mat color
					em.material.SetColor("_EmissiveColor", emissiveColor);
				}
			}
		}
	}
}

