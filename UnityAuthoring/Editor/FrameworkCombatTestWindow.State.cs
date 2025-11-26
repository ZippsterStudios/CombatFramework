#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Framework.Damage.Components;
using Unity.Entities;
using UnityEngine;

namespace Framework.UnityAuthoring.Editor
{
    public sealed partial class FrameworkCombatTestWindow : UnityEditor.EditorWindow
    {
        // Target + actions state
        private GameObject _anchorObject;
        private Entity _targetEntity = Entity.Null;
        private Entity _casterEntity = Entity.Null;
        private bool _casterIsTarget = true;
        private float _manualAmount = 25f;
        private DamageSchool _manualSchool = DamageSchool.Fire;
        private Vector2 _targetScroll;

        // Entities list state
        private Vector2 _entityScroll;
        private bool _filterByHealth = true;
        private int _maxEntities = 256;

        // Spells state
        private bool _includeSampleSpells = true;
        private int _selectedSampleSpell;
        private string _customSpellId = "fireball";
        private Vector2 _spellsScroll;
        private Vector2 _libraryScroll;

        // Spawn state
        private int _spawnCount = 1;
        private float _spawnHealth = 150f;
        private float _spawnHealthRegen = 0f;
        private bool _spawnGiveMana = true;
        private int _spawnMana = 100;
        private float _spawnManaRegen = 0f;
        private float _spawnArmor = 5f;
        private float _spawnResist = 0.1f;
        private float _spawnOffset = 4f;
        private float _spawnRadius = 6f;
        private byte _spawnTeam = 2;
        private bool _spawnAsTarget = true;
        private bool _spawnAsCaster = false;
        private int _spawnIdCounter;

        // Pets state
        private bool _petsUseCaster = true;
        private int _petSelectedSample;
        private string _petCustomId = "wolf";
        private int _petSummonCount = 1;
        private float _petSummonRadius = 2.5f;
        private string _petCategoryId = "summons";
        private int _petCategoryLevel = 1;
        private Vector2 _petListScroll;

        // Skirmish state
        private List<SkirmishTeamConfig> _skirmishTeams;
        private bool _skirmishShowGizmos = true;
        private bool _skirmishAutoClear = true;
        private string _skirmishDefaultSpell = "fireball";

        // UI housekeeping
        private const double RepaintInterval = 0.1;
        private double _nextRepaint;
        private int _tabIndex;

        [Serializable]
        private sealed class SkirmishTeamConfig
        {
            public string Label = "Team";
            public byte TeamId = 1;
            public int Count = 4;
            public Vector2 Center = new Vector2(-10f, 0f);
            public float Radius = 4f;
            public int Health = 150;
            public int Mana = 50;
            public float Armor = 4f;
            public float Resist = 0.1f;
            public float VisionRange = 20f;
            public float AttackRange = 10f;
            public float MoveSpeed = 4f;
            public float Cooldown = 1.5f;
            public string SpellId = "fireball";
            public Color Color = Color.red;
        }
    }
}
#endif
