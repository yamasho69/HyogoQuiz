﻿// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;


public class SampleSkipSpeedTagWithParam : MonoBehaviour
{
	public AdvEngine Engine { get { return this.GetComponentCacheFindIfMissing(ref engine); } }
	[SerializeField]
	protected AdvEngine engine;

	public string paramLabel = "SkipSpeedTag";


	void Update()
	{
		if (!Engine.IsStarted) return;
		if (!Engine.Param.IsInit) return;

		Engine.Page.EnableSkipSpeedTag = Engine.Param.GetParameterBoolean(paramLabel);
	}
}
