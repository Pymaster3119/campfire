using Godot;
using System;
using System.Collections.Generic;

public partial class HankStats : Node
{
	// ── Stats (0–100) ──────────────────────────────────────────
	public float Hygiene      { get; private set; } = 100f;
	public float Entertainment{ get; private set; } = 100f;
	public float Education    { get; private set; } = 100f;
	public float Hunger       { get; private set; } = 100f;
	public float Health       { get; private set; } = 100f;

	// ── Base decay per real-world minute ──────────────────────
	private const float DecayHygiene       = 1.5f;
	private const float DecayEntertainment = 2.0f;
	private const float DecayEducation     = 1.0f;
	private const float DecayHunger        = 3.0f;
	private const float DecayHealth        = 0.5f;

		// ── Threshold tracking (prevent signal spam) ───────────────
		private Dictionary<string, bool> _isCritical = new();
		private Dictionary<string, bool> _isWarning  = new();

	// ── Signals ────────────────────────────────────────────────
	[Signal] public delegate void StatCriticalEventHandler(string statName);   // below 20
	[Signal] public delegate void StatWarningEventHandler(string statName);    // below 40
	[Signal] public delegate void StatRecoveredEventHandler(string statName);  // back above 50

	public override void _Ready()
	{
		foreach (string s in new[]{"hygiene","entertainment","education","hunger","health"})
		{
			_isCritical[s] = false;
			_isWarning[s]  = false;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta / 60f; // convert to per-minute rate

		Hygiene       = Mathf.Clamp(Hygiene       - GetModifiedDecay("hygiene",       DecayHygiene)       * dt, 0f, 100f);
		Entertainment = Mathf.Clamp(Entertainment - GetModifiedDecay("entertainment", DecayEntertainment) * dt, 0f, 100f);
		Education     = Mathf.Clamp(Education     - GetModifiedDecay("education",     DecayEducation)     * dt, 0f, 100f);
		Hunger        = Mathf.Clamp(Hunger        - GetModifiedDecay("hunger",        DecayHunger)        * dt, 0f, 100f);
		Health        = Mathf.Clamp(Health        - GetModifiedDecay("health",        DecayHealth)        * dt, 0f, 100f);

		CheckTriggers();
	}

	// ── Cross-stat multipliers ─────────────────────────────────
	private float GetModifiedDecay(string stat, float baseRate)
	{
		float rate = baseRate;

		// Global debuff if health is low
		if (Health < 30f) rate *= 1.5f;

		switch (stat)
		{
			case "health":
				if (Hunger   < 20f) rate *= 3.0f; // starving wrecks health
				if (Hygiene  < 20f) rate *= 2.0f; // dirty = sick
				break;
			case "education":
				if (Entertainment < 20f) rate *= 1.5f; // bored can't focus
				break;
			case "entertainment":
				if (Education > 80f) rate *= 0.75f; // engaged mind entertains itself
				break;
		}

		return rate;
	}

	// ── Signal firing ──────────────────────────────────────────
	private void CheckTriggers()
	{
		CheckStat("hygiene",       Hygiene);
		CheckStat("entertainment", Entertainment);
		CheckStat("education",     Education);
		CheckStat("hunger",        Hunger);
		CheckStat("health",        Health);
	}

	private void CheckStat(string name, float value)
	{
		// Critical: below 20
		if (value < 20f && !_isCritical[name])
		{
			_isCritical[name] = true;
			EmitSignal(SignalName.StatCritical, name);
		}
		// Warning: below 40
		else if (value < 40f && !_isWarning[name])
		{
			_isWarning[name] = true;
			EmitSignal(SignalName.StatWarning, name);
		}
		// Recovered: back above 50
		if (value > 50f && (_isCritical[name] || _isWarning[name]))
		{
			_isCritical[name] = false;
			_isWarning[name]  = false;
			EmitSignal(SignalName.StatRecovered, name);
		}
	}

	// ── Public method to modify stats (call from other scripts) ─
	public void ModifyStat(string stat, float amount)
	{
		switch (stat.ToLower())
		{
			case "hygiene":       Hygiene       = Mathf.Clamp(Hygiene       + amount, 0f, 100f); break;
			case "entertainment": Entertainment = Mathf.Clamp(Entertainment + amount, 0f, 100f); break;
			case "education":     Education     = Mathf.Clamp(Education     + amount, 0f, 100f); break;
			case "hunger":        Hunger        = Mathf.Clamp(Hunger        + amount, 0f, 100f); break;
			case "health":        Health        = Mathf.Clamp(Health        + amount, 0f, 100f); break;
		}
	}
}
