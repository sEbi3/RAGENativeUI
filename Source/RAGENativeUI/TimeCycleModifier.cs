namespace RAGENativeUI
{
#if RPH1
    extern alias rph1;
    using IAddressable = rph1::Rage.IAddressable;
#else
    /** REDACTED **/
#endif

    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

    using Rage;

    using RAGENativeUI.Memory;

    /// <summary>
    /// Represents a time cycle modifier.
    /// <para>
    /// The default time cycle modifiers are defined in the "timecycle_mods_*.xml" from the game files.
    /// </para>
    /// </summary>
    public unsafe sealed class TimeCycleModifier : IAddressable
#if !RPH1
        /** REDACTED **/
#endif
    {
        internal readonly CTimeCycleModifier* Native;

        /// <summary>
        /// Gets the hash of the <see cref="TimeCycleModifier"/>.
        /// </summary>
        public uint Hash { get { return Native->Name; } }

        ///<summary>
        ///Gets the name of the <see cref= "TimeCycleModifier"/>.
        ///</summary>
        ///<returns>
        ///The name of the <see cref="TimeCycleModifier"/>, or if it's unknown, the hash in hexadecimal.
        ///</returns>
        public string Name
        {
            get
            {
                if (KnownNames.TimeCycleModifiers.Dictionary.TryGetValue(Native->Name, out string n))
                {
                    return n;
                }

                return $"0x{Native->Name:X8}";
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="TimeCycleModifier"/> is currently active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return GameMemory.TimeCycle->CurrentModifierIndex == Index ||
                       GameMemory.TimeCycle->TransitionModifierIndex == Index;
            }
            set
            {
                if (value)
                {
                    CurrentModifier = this;
                }
                else if (IsActive)
                {
                    CurrentModifier = null;
                }
            }
        }

        /// <summary>
        /// Gets whether the <see cref="TimeCycleModifier"> is currently transitioning.
        /// </summary>
        public bool IsInTransition
        {
            get { return GameMemory.TimeCycle->TransitionModifierIndex == Index; }
        }

        /// <summary>
        /// Gets the memory address of the <see cref="TimeCycleModifier"/>.
        /// </summary>
        public IntPtr MemoryAddress { get { return (IntPtr)Native; } }

        /// <summary>
        /// Gets the index of the <see cref="TimeCycleModifier"/>.
        /// </summary>
        public int Index { get; } = -1;

        /// <summary>
        /// Gets the flags of the <see cref="TimeCycleModifier"/>.
        /// </summary>
        public uint Flags { get { return Native->Flags; } }

        /// <summary>
        /// Gets a dictionary representing the mods of the <see cref="TimeCycleModifier"/>.
        /// </summary>
        public TimeCycleModifierMods Mods { get; }

        /// <summary>
        /// Checks whether the <see cref="TimeCycleModifier"/> is valid.
        /// </summary>
        /// <returns><c>true</c> if this instance is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid => Native != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeCycleModifier"/> class from existing timecycle modifier in memory.
        /// </summary>
        private TimeCycleModifier(CTimeCycleModifier* native, int idx)
        {
            Native = native;
            Index = idx;
            Mods = new TimeCycleModifierMods(this);

            Cache.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeCycleModifier"/> class, creating a new timecycle modifier in memory.
        /// </summary>
        private TimeCycleModifier(string name, uint flags, CTimeCycleModifier.Mod[] mods)
        {
            Throw.IfNull(name, nameof(name));
            Throw.IfNull(mods, nameof(mods));

            uint hash = RPH.Game.GetHashKey(name);

            Throw.InvalidOperationIf(GameMemory.TimeCycle->IsNameUsed(hash), $"The name '{name}' is already in use.");

            KnownNames.TimeCycleModifiers.Dictionary[hash] = name;

            Native = GameMemory.TimeCycle->NewTimeCycleModifier(hash, mods, flags);
            Index = GameMemory.TimeCycle->Modifiers.Count - 1;
            Mods = new TimeCycleModifierMods(this);

            Cache.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the<see cref="TimeCycleModifier"/> class using other <see cref="TimeCycleModifier"/> as template.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="template">The template from which the flags and mods will be copied to the new instance.</param>
        /// <exception cref="InvalidOperationException"><paramref name="name"/> is already used by other <see cref="TimeCycleModifier"/>.</exception>
        public TimeCycleModifier(string name, TimeCycleModifier template)
            : this(name, template.Flags, template.Mods.Select(m => new CTimeCycleModifier.Mod { ModType = (int)m.Key, Value1 = m.Value.Value1, Value2 = m.Value.Value2 }).ToArray())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeCycleModifier"/> class with the specified flags and mods.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="mods">The mods.</param>
        /// <exception cref="InvalidOperationException"><paramref name="name"/> is already used by other <see cref="TimeCycleModifier"/>.</exception>
        public TimeCycleModifier(string name, uint flags, params (TimeCycleModifierModType Type, float Value1, float Value2)[] mods)
            : this(name, flags, mods?.Select(m => new CTimeCycleModifier.Mod { ModType = (int)m.Type, Value1 = m.Value1, Value2 = m.Value2 }).ToArray())
        {
        }

        public void SetActiveWithTransition(float time) => SetActiveWithTransition(time, Strength);
        public void SetActiveWithTransition(float time, float targetStrength)
        {
            CTimeCycle* timecycle = GameMemory.TimeCycle;
            timecycle->CurrentModifierIndex = -1;
            timecycle->CurrentModifierStrength = targetStrength;
            timecycle->TransitionCurrentStrength = 0.0f;
            timecycle->TransitionModifierIndex = Index;
            timecycle->TransitionSpeed = targetStrength / time;
        }


        /// <summary>
        /// Gets a <see cref="TimeCycleModifier"/> instance by its name.
        /// </summary>
        /// <param name="name">The case insensitive name of the instance to get.</param>
        /// <returns>If an instance of <see cref="TimeCycleModifier"/> matches the specified name, returns that instance; otherwise, returns <c>null</c>.</returns>
        public static TimeCycleModifier GetByName(string name)
        {
            Throw.IfNull(name, nameof(name));

            uint hash = RPH.Game.GetHashKey(name);
            KnownNames.TimeCycleModifiers.Dictionary[hash] = name;
            return GetByHash(hash);
        }

        /// <summary>
        /// Gets a <see cref="TimeCycleModifier"/> instance by its hash.
        /// </summary>
        /// <param name="hash">The hash of the instance to get.</param>
        /// <returns>If an instance of <see cref="TimeCycleModifier"/> matches the specified hash, returns that instance; otherwise, returns <c>null</c>.</returns>
        public static TimeCycleModifier GetByHash(uint hash)
        {
            if (Cache.Get(hash, out TimeCycleModifier p))
            {
                return p;
            }
            else
            {
                int index = GameFunctions.GetTimeCycleModifierIndex(GameMemory.TimeCycle, &hash);

                if (index != -1)
                {
                    return GetByIndex(index);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a <see cref="TimeCycleModifier"/> instance by its index.
        /// <para>
        /// Indices are between 0, inclusive, and <see cref="NumberOfTimeCycleModifiers"/>, exclusive.
        /// </para>
        /// </summary>
        /// <param name="index">The index of the instance to get.</param>
        /// <returns>If an instance of <see cref="TimeCycleModifier"/> matches the specified hash, returns that instance; otherwise, returns <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is equal to or greater than <see cref="NumberOfTimeCycleModifiers"/>.</exception>
        public static TimeCycleModifier GetByIndex(int index)
        {
            Throw.IfOutOfRange(index, 0, NumberOfTimeCycleModifiers - 1, nameof(index));

            ushort i = (ushort)index;
            CTimeCycleModifier* native = GameMemory.TimeCycle->Modifiers.Items[i];

            if (Cache.Get(native->Name, out TimeCycleModifier p))
            {
                return p;
            }
            else
            {
                return new TimeCycleModifier(native, index);
            }
        }

        /// <summary>
        /// Gets all <see cref="TimeCycleModifier"/> instances currently in memory.
        /// </summary>
        /// <returns>An array that contains all <see cref="TimeCycleModifier"/> instances currently in memory.</returns>
        public static TimeCycleModifier[] GetAll()
        {
            TimeCycleModifier[] mods = new TimeCycleModifier[GameMemory.TimeCycle->Modifiers.Count];
            for (short i = 0; i < GameMemory.TimeCycle->Modifiers.Count; i++)
            {
                mods[i] = GetByIndex(i);
            }

            return mods;
        }

        /// <summary>
        /// Gets the number of <see cref="TimeCycleModifier"/> instances currently in memory.
        /// </summary>
        /// <returns>The number of <see cref="TimeCycleModifier"/> instances currently in memory.</returns>
        public static int NumberOfTimeCycleModifiers
        {
            get
            {
                return GameMemory.TimeCycle->Modifiers.Count;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="TimeCycleModifier"/> instance that is currently active.
        /// </summary>
        /// <returns>The <see cref="TimeCycleModifier"/> instance that is currently active.</returns>
        public static TimeCycleModifier CurrentModifier
        {
            get
            {
                CTimeCycle* timecycle = GameMemory.TimeCycle;

                int index = timecycle->CurrentModifierIndex;
                if (index == -1)
                {
                    index = timecycle->TransitionModifierIndex;
                }
                return index == -1 ? null : GetByIndex(index);
            }
            set
            {
                CTimeCycle* timecycle = GameMemory.TimeCycle;

                if (value == null || !value.IsValid)
                {
                    timecycle->CurrentModifierIndex = -1;

                    timecycle->TransitionModifierIndex = -1;
                    timecycle->TransitionCurrentStrength = 0.0f;
                    timecycle->TransitionSpeed = 0.0f;
                }
                else
                {
                    timecycle->CurrentModifierIndex = value.Index;
                }
            }
        }
        
        public static float Strength
        {
            get
            {
                return GameMemory.TimeCycle->CurrentModifierStrength;
            }
            set
            {
                GameMemory.TimeCycle->CurrentModifierStrength = value;
            }
        }

    }

    /// <summary>
    /// Represents the mods dictionary of a <see cref="TimeCycleModifier"/>.
    /// </summary>
    public sealed unsafe class TimeCycleModifierMods : IDictionary<TimeCycleModifierModType, (float Value1, float Value2)>
    {
        private readonly TimeCycleModifier modifier;
        private KeyCollection keys;
        private ValueCollection values;
        
        public int Count => modifier.Native->Mods.Count;
        public ICollection<TimeCycleModifierModType> Keys => keys ?? (keys = new KeyCollection(this));
        public ICollection<(float Value1, float Value2)> Values => values ?? (values = new ValueCollection(this));
        public bool IsReadOnly => false;

        public (float Value1, float Value2) this[int index]
        {
            get
            {
                CTimeCycleModifier* native = modifier.Native;

                Throw.IfOutOfRange(index, 0, native->Mods.Count - 1, nameof(index));

                CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];
                return (nativeMod->Value1, nativeMod->Value2);
            }
            set
            {
                CTimeCycleModifier* native = modifier.Native;

                Throw.IfOutOfRange(index, 0, native->Mods.Count - 1, nameof(index));

                CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];
                nativeMod->Value1 = value.Value1;
                nativeMod->Value2 = value.Value2;
            }
        }
        
        public (float Value1, float Value2) this[TimeCycleModifierModType type]
        {
            get
            {
                CTimeCycleModifier* native = modifier.Native;
                ushort index = FindIndex(type);

                Throw.InvalidOperationIf(index == 0xFFFF, $"This {nameof(TimeCycleModifierMods)} doesn't contain a mod of type '{type}'.");

                CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];
                return (nativeMod->Value1, nativeMod->Value2);
            }
            set
            {
                CTimeCycleModifier* native = modifier.Native;
                ushort index = FindIndex(type);

                if(index == 0xFFFF)
                {
                    Add(type, value.Value1, value.Value2);
                }
                else
                {
                    CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];
                    nativeMod->Value1 = value.Value1;
                    nativeMod->Value2 = value.Value2;
                }
            }
        }

        internal TimeCycleModifierMods(TimeCycleModifier modifier)
        {
            this.modifier = modifier;
        }

        private ushort FindIndex(TimeCycleModifierModType type)
        {
            CTimeCycleModifier* native = modifier.Native;

            int keyType = (int)type;

            int leftIndex = 0;
            int rightIndex = native->Mods.Count - 1;

            while (leftIndex <= rightIndex)
            {
                int mid = (rightIndex + leftIndex) >> 1;

                int modType = native->Mods.Items[(ushort)mid].ModType;

                if (keyType == modType)
                {
                    return (ushort)mid;
                }

                if (keyType > modType)
                {
                    leftIndex = mid + 1;
                }
                else
                {
                    rightIndex = mid - 1;
                }
            }

            return 0xFFFF;
        }
        
        public bool ContainsKey(TimeCycleModifierModType type)
        {
            return FindIndex(type) != 0xFFFF;
        }

        public void Add(TimeCycleModifierModType type, (float Value1, float Value2) values) => Add(type, values.Value1, values.Value2);
        public void Add(TimeCycleModifierModType type, float value1, float value2)
        {
            Throw.ArgumentExceptionIf(ContainsKey(type), nameof(type), $"This {nameof(TimeCycleModifierMods)} already contains a mod of type '{type}'.");

            CTimeCycleModifier* native = modifier.Native;

            CTimeCycleModifier.Mod* newMod = native->GetUnusedModEntry();
            newMod->ModType = (int)type;
            newMod->Value1 = value1;
            newMod->Value2 = value2;

            native->SortMods();
        }
        
        public bool Remove(TimeCycleModifierModType type)
        {
            ushort index = FindIndex(type);
            if(index == 0xFFFF)
            {
                return false;
            }

            CTimeCycleModifier* native = modifier.Native;
            native->RemoveModEntry(index);
            return true;
        }

        public bool TryGetValue(TimeCycleModifierModType type, out (float Value1, float Value2) values)
        {
            CTimeCycleModifier* native = modifier.Native;
            ushort index = FindIndex(type);

            if (index != 0xFFFF)
            {
                CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];
                values = (nativeMod->Value1, nativeMod->Value2);
                return true;
            }

            values = default;
            return false;
        }

        void ICollection<KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)>>.Add(KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)>>.Contains(KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)> keyValuePair)
        {
            ushort index = FindIndex(keyValuePair.Key);
            if (index != 0xFFFF)
            {
                CTimeCycleModifier* native = modifier.Native;
                CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];

                return nativeMod->Value1 == keyValuePair.Value.Value1 && nativeMod->Value2 == keyValuePair.Value.Value2;
            }

            return false;
        }

        bool ICollection<KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)>>.Remove(KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)> keyValuePair)
        {
            ushort index = FindIndex(keyValuePair.Key);
            if (index != 0xFFFF)
            {
                CTimeCycleModifier* native = modifier.Native;
                CTimeCycleModifier.Mod* nativeMod = &native->Mods.Items[index];

                if (nativeMod->Value1 == keyValuePair.Value.Value1 && nativeMod->Value2 == keyValuePair.Value.Value2)
                {
                    native->RemoveModEntry(index);
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            CTimeCycleModifier* native = modifier.Native;
            if (native->Mods.Count > 0)
            {
                native->RemoveAllMods();
            }
        }

        public void CopyTo(KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)>[] array, int arrayIndex)
        {
            Throw.IfNull(array, nameof(array));

            CTimeCycleModifier* native = modifier.Native;
            ushort sourceCount = native->Mods.Count;

            Throw.IfOutOfRange(arrayIndex, 0, sourceCount - 1, nameof(arrayIndex));
            Throw.ArgumentExceptionIf(sourceCount > (array.Length - arrayIndex));

            for (short i = 0; i < sourceCount; i++)
            {
                CTimeCycleModifier.Mod* m = &native->Mods.Items[i];
                array[arrayIndex++] = new KeyValuePair<TimeCycleModifierModType, (float, float)>((TimeCycleModifierModType)m->ModType, (m->Value1, m->Value2));
            }
        }

        public IEnumerator<KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)>> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        private sealed class Enumerator : IEnumerator<KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)>>
        {
            private readonly TimeCycleModifierMods mods;
            private short index;
            private KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)> current;

            public KeyValuePair<TimeCycleModifierModType, (float Value1, float Value2)> Current => current;
            object IEnumerator.Current => Current;

            internal Enumerator(TimeCycleModifierMods mods)
            {
                Throw.IfNull(mods, nameof(mods));

                this.mods = mods;
                index = -1;
                current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                CTimeCycleModifier* native = mods.modifier.Native;

                if(index < native->Mods.Count)
                {
                    index++;
                    if(index < native->Mods.Count)
                    {
                        CTimeCycleModifier.Mod* m = &native->Mods.Items[index];
                        current = new KeyValuePair<TimeCycleModifierModType, (float, float)>((TimeCycleModifierModType)m->ModType, (m->Value1, m->Value2));
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
                current = default;
            }
        }

        private sealed class KeyCollection : ICollection<TimeCycleModifierModType>, IReadOnlyCollection<TimeCycleModifierModType>
        {
            private TimeCycleModifierMods mods;
            
            public int Count
            {
                get { return mods.Count; }
            }

            public bool IsReadOnly => true;

            internal KeyCollection(TimeCycleModifierMods mods)
            {
                Throw.IfNull(mods, nameof(mods));

                this.mods = mods;
            }

            public void CopyTo(TimeCycleModifierModType[] array, int arrayIndex)
            {
                Throw.IfNull(array, nameof(array));

                CTimeCycleModifier* native = mods.modifier.Native;
                ushort sourceCount = native->Mods.Count;

                Throw.IfOutOfRange(arrayIndex, 0, sourceCount - 1, nameof(arrayIndex));
                Throw.ArgumentExceptionIf(sourceCount > (array.Length - arrayIndex));

                for (short i = 0; i < sourceCount; i++)
                {
                    CTimeCycleModifier.Mod* m = &native->Mods.Items[i];
                    array[arrayIndex++] = (TimeCycleModifierModType)m->ModType;
                }
            }

            void ICollection<TimeCycleModifierModType>.Add(TimeCycleModifierModType item) => Throw.NotSupportedException();
            void ICollection<TimeCycleModifierModType>.Clear() => Throw.NotSupportedException();

            public bool Contains(TimeCycleModifierModType item) => mods.ContainsKey(item);

            bool ICollection<TimeCycleModifierModType>.Remove(TimeCycleModifierModType item)
            {
                Throw.NotSupportedException();
                return false;
            }

            public IEnumerator<TimeCycleModifierModType> GetEnumerator()
            {
                foreach (KeyValuePair<TimeCycleModifierModType, (float, float)> pair in mods)
                {
                    yield return pair.Key;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ValueCollection : ICollection<(float Value1, float Value2)>, IReadOnlyCollection<(float Value1, float Value2)>
        {
            private TimeCycleModifierMods mods;

            public int Count
            {
                get { return mods.Count; }
            }

            public bool IsReadOnly => true;

            internal ValueCollection(TimeCycleModifierMods mods)
            {
                Throw.IfNull(mods, nameof(mods));

                this.mods = mods;
            }

            public void CopyTo((float Value1, float Value2)[] array, int arrayIndex)
            {
                Throw.IfNull(array, nameof(array));

                CTimeCycleModifier* native = mods.modifier.Native;
                ushort sourceCount = native->Mods.Count;

                Throw.IfOutOfRange(arrayIndex, 0, sourceCount - 1, nameof(arrayIndex));
                Throw.ArgumentExceptionIf(sourceCount > (array.Length - arrayIndex));

                for (short i = 0; i < sourceCount; i++)
                {
                    CTimeCycleModifier.Mod* m = &native->Mods.Items[i];
                    array[arrayIndex++] = (m->Value1, m->Value2);
                }
            }

            void ICollection<(float Value1, float Value2)>.Add((float Value1, float Value2) item) => Throw.NotSupportedException();
            void ICollection<(float Value1, float Value2)>.Clear() => Throw.NotSupportedException();

            public bool Contains((float Value1, float Value2) item)
            {
                CTimeCycleModifier* native = mods.modifier.Native;

                for (short i = 0; i < native->Mods.Count; i++)
                {
                    CTimeCycleModifier.Mod* m = &native->Mods.Items[i];
                    if (m->Value1 == item.Value1 && m->Value2 == item.Value1)
                    {
                        return true;
                    }
                }

                return false;
            }

            bool ICollection<(float Value1, float Value2)>.Remove((float Value1, float Value2) item)
            {
                Throw.NotSupportedException();
                return false;
            }

            public IEnumerator<(float Value1, float Value2)> GetEnumerator()
            {
                foreach (KeyValuePair<TimeCycleModifierModType, (float, float)> pair in mods)
                {
                    yield return pair.Value;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    /// <summary>
    /// Represents the various <see cref="TimeCycleModifier"/> mod types.
    /// </summary>
    public enum TimeCycleModifierModType
    {
        light_dir_col_r = 0,
        light_dir_col_g = 1,
        light_dir_col_b = 2,
        light_dir_mult = 3,
        light_directional_amb_col_r = 4,
        light_directional_amb_col_g = 5,
        light_directional_amb_col_b = 6,
        light_directional_amb_intensity = 7,
        light_directional_amb_intensity_mult = 8,
        light_directional_amb_bounce_enabled = 9,
        light_amb_down_wrap = 10,
        light_natural_amb_down_col_r = 11,
        light_natural_amb_down_col_g = 12,
        light_natural_amb_down_col_b = 13,
        light_natural_amb_down_intensity = 14,
        light_natural_amb_up_col_r = 15,
        light_natural_amb_up_col_g = 16,
        light_natural_amb_up_col_b = 17,
        light_natural_amb_up_intensity = 18,
        light_natural_amb_up_intensity_mult = 19,
        light_natural_push = 20,
        light_ambient_bake_ramp = 21,
        light_artificial_int_down_col_r = 22,
        light_artificial_int_down_col_g = 23,
        light_artificial_int_down_col_b = 24,
        light_artificial_int_down_intensity = 25,
        light_artificial_int_up_col_r = 26,
        light_artificial_int_up_col_g = 27,
        light_artificial_int_up_col_b = 28,
        light_artificial_int_up_intensity = 29,
        light_artificial_ext_down_col_r = 30,
        light_artificial_ext_down_col_g = 31,
        light_artificial_ext_down_col_b = 32,
        light_artificial_ext_down_intensity = 33,
        light_artificial_ext_up_col_r = 34,
        light_artificial_ext_up_col_g = 35,
        light_artificial_ext_up_col_b = 36,
        light_artificial_ext_up_intensity = 37,
        ped_light_col_r = 38,
        ped_light_col_g = 39,
        ped_light_col_b = 40,
        ped_light_mult = 41,
        ped_light_direction_x = 42,
        ped_light_direction_y = 43,
        ped_light_direction_z = 44,
        light_amb_occ_mult = 45,
        light_amb_occ_mult_ped = 46,
        light_amb_occ_mult_veh = 47,
        light_amb_occ_mult_prop = 48,
        light_amb_volumes_in_diffuse = 49,
        ssao_inten = 50,
        ssao_type = 51,
        ssao_cp_strength = 52,
        ssao_qs_strength = 53,
        light_ped_rim_mult = 54,
        light_dynamic_bake_tweak = 55,
        light_vehicle_second_spec_override = 56,
        light_vehicle_intenity_scale = 57,
        light_direction_override = 58,
        light_direction_override_overrides_sun = 59,
        sun_direction_x = 60,
        sun_direction_y = 61,
        sun_direction_z = 62,
        moon_direction_x = 63,
        moon_direction_y = 64,
        moon_direction_z = 65,
        light_ray_col_r = 66,
        light_ray_col_g = 67,
        light_ray_col_b = 68,
        light_ray_mult = 69,
        light_ray_underwater_mult = 70,
        light_ray_dist = 71,
        light_ray_heightfalloff = 72,
        light_ray_height_falloff_start = 73,
        light_ray_add_reducer = 74,
        light_ray_blit_size = 75,
        light_ray_length = 76,
        postfx_exposure = 77,
        postfx_exposure_min = 78,
        postfx_exposure_max = 79,
        postfx_bright_pass_thresh_width = 80,
        postfx_bright_pass_thresh = 81,
        postfx_intensity_bloom = 82,
        postfx_correct_col_r = 83,
        postfx_correct_col_g = 84,
        postfx_correct_col_b = 85,
        postfx_correct_cutoff = 86,
        postfx_shift_col_r = 87,
        postfx_shift_col_g = 88,
        postfx_shift_col_b = 89,
        postfx_shift_cutoff = 90,
        postfx_desaturation = 91,
        postfx_noise = 92,
        postfx_noise_size = 93,
        postfx_tonemap_filmic_override_dark = 94,
        postfx_tonemap_filmic_exposure_dark = 95,
        postfx_tonemap_filmic_a = 96,
        postfx_tonemap_filmic_b = 97,
        postfx_tonemap_filmic_c = 98,
        postfx_tonemap_filmic_d = 99,
        postfx_tonemap_filmic_e = 100,
        postfx_tonemap_filmic_f = 101,
        postfx_tonemap_filmic_w = 102,
        postfx_tonemap_filmic_override_bright = 103,
        postfx_tonemap_filmic_exposure_bright = 104,
        postfx_tonemap_filmic_a_bright = 105,
        postfx_tonemap_filmic_b_bright = 106,
        postfx_tonemap_filmic_c_bright = 107,
        postfx_tonemap_filmic_d_bright = 108,
        postfx_tonemap_filmic_e_bright = 109,
        postfx_tonemap_filmic_f_bright = 110,
        postfx_tonemap_filmic_w_bright = 111,
        postfx_vignetting_intensity = 112,
        postfx_vignetting_radius = 113,
        postfx_vignetting_contrast = 114,
        postfx_vignetting_col_r = 115,
        postfx_vignetting_col_g = 116,
        postfx_vignetting_col_b = 117,
        postfx_grad_top_col_r = 118,
        postfx_grad_top_col_g = 119,
        postfx_grad_top_col_b = 120,
        postfx_grad_middle_col_r = 121,
        postfx_grad_middle_col_g = 122,
        postfx_grad_middle_col_b = 123,
        postfx_grad_bottom_col_r = 124,
        postfx_grad_bottom_col_g = 125,
        postfx_grad_bottom_col_b = 126,
        postfx_grad_midpoint = 127,
        postfx_grad_top_middle_midpoint = 128,
        postfx_grad_middle_bottom_midpoint = 129,
        postfx_scanlineintensity = 130,
        postfx_scanline_frequency_0 = 131,
        postfx_scanline_frequency_1 = 132,
        postfx_scanline_speed = 133,
        postfx_motionblurlength = 134,
        dof_far = 135,
        dof_blur_mid = 136,
        dof_blur_far = 137,
        dof_enable_hq = 138,
        dof_hq_smallblur = 139,
        dof_hq_shallowdof = 140,
        dof_hq_nearplane_out = 141,
        dof_hq_nearplane_in = 142,
        dof_hq_farplane_out = 143,
        dof_hq_farplane_in = 144,
        environmental_blur_in = 145,
        environmental_blur_out = 146,
        environmental_blur_size = 147,
        bokeh_brightness_min = 148,
        bokeh_brightness_max = 149,
        bokeh_fade_min = 150,
        bokeh_fade_max = 151,
        nv_light_dir_mult = 152,
        nv_light_amb_down_mult = 153,
        nv_light_amb_up_mult = 154,
        nv_lowLum = 155,
        nv_highLum = 156,
        nv_topLum = 157,
        nv_scalerLum = 158,
        nv_offsetLum = 159,
        nv_offsetLowLum = 160,
        nv_offsetHighLum = 161,
        nv_noiseLum = 162,
        nv_noiseLowLum = 163,
        nv_noiseHighLum = 164,
        nv_bloomLum = 165,
        nv_colorLum_r = 166,
        nv_colorLum_g = 167,
        nv_colorLum_b = 168,
        nv_colorLowLum_r = 169,
        nv_colorLowLum_g = 170,
        nv_colorLowLum_b = 171,
        nv_colorHighLum_r = 172,
        nv_colorHighLum_g = 173,
        nv_colorHighLum_b = 174,
        hh_startRange = 175,
        hh_farRange = 176,
        hh_minIntensity = 177,
        hh_maxIntensity = 178,
        hh_displacementU = 179,
        hh_displacementV = 180,
        hh_tex1UScale = 181,
        hh_tex1VScale = 182,
        hh_tex1UOffset = 183,
        hh_tex1VOffset = 184,
        hh_tex2UScale = 185,
        hh_tex2VScale = 186,
        hh_tex2UOffset = 187,
        hh_tex2VOffset = 188,
        hh_tex1UFrequencyOffset = 189,
        hh_tex1UFrequency = 190,
        hh_tex1UAmplitude = 191,
        hh_tex1VScrollingSpeed = 192,
        hh_tex2UFrequencyOffset = 193,
        hh_tex2UFrequency = 194,
        hh_tex2UAmplitude = 195,
        hh_tex2VScrollingSpeed = 196,
        lens_dist_coeff = 197,
        lens_dist_cube_coeff = 198,
        chrom_aberration_coeff = 199,
        chrom_aberration_coeff2 = 200,
        lens_artefacts_intensity = 201,
        lens_artefacts_min_exp_intensity = 202,
        lens_artefacts_max_exp_intensity = 203,
        blur_vignetting_radius = 204,
        blur_vignetting_intensity = 205,
        screen_blur_intensity = 206,
        sky_zenith_transition_position = 207,
        sky_zenith_transition_east_blend = 208,
        sky_zenith_transition_west_blend = 209,
        sky_zenith_blend_start = 210,
        sky_zenith_col_r = 211,
        sky_zenith_col_g = 212,
        sky_zenith_col_b = 213,
        sky_zenith_col_inten = 214,
        sky_zenith_transition_col_r = 215,
        sky_zenith_transition_col_g = 216,
        sky_zenith_transition_col_b = 217,
        sky_zenith_transition_col_inten = 218,
        sky_azimuth_transition_position = 219,
        sky_azimuth_east_col_r = 220,
        sky_azimuth_east_col_g = 221,
        sky_azimuth_east_col_b = 222,
        sky_azimuth_east_col_inten = 223,
        sky_azimuth_transition_col_r = 224,
        sky_azimuth_transition_col_g = 225,
        sky_azimuth_transition_col_b = 226,
        sky_azimuth_transition_col_inten = 227,
        sky_azimuth_west_col_r = 228,
        sky_azimuth_west_col_g = 229,
        sky_azimuth_west_col_b = 230,
        sky_azimuth_west_col_inten = 231,
        sky_hdr = 232,
        sky_plane_r = 233,
        sky_plane_g = 234,
        sky_plane_b = 235,
        sky_plane_inten = 236,
        sky_sun_col_r = 237,
        sky_sun_col_g = 238,
        sky_sun_col_b = 239,
        sky_sun_disc_col_r = 240,
        sky_sun_disc_col_g = 241,
        sky_sun_disc_col_b = 242,
        sky_sun_disc_size = 243,
        sky_sun_hdr = 244,
        sky_sun_miephase = 245,
        sky_sun_miescatter = 246,
        sky_sun_mie_intensity_mult = 247,
        sky_sun_influence_radius = 248,
        sky_sun_scatter_inten = 249,
        sky_moon_col_r = 250,
        sky_moon_col_g = 251,
        sky_moon_col_b = 252,
        sky_moon_disc_size = 253,
        sky_moon_iten = 254,
        sky_stars_iten = 255,
        sky_moon_influence_radius = 256,
        sky_moon_scatter_inten = 257,
        sky_cloud_gen_frequency = 258,
        sky_cloud_gen_scale = 259,
        sky_cloud_gen_threshold = 260,
        sky_cloud_gen_softness = 261,
        sky_cloud_density_mult = 262,
        sky_cloud_density_bias = 263,
        sky_cloud_mid_col_r = 264,
        sky_cloud_mid_col_g = 265,
        sky_cloud_mid_col_b = 266,
        sky_cloud_base_col_r = 267,
        sky_cloud_base_col_g = 268,
        sky_cloud_base_col_b = 269,
        sky_cloud_base_strength = 270,
        sky_cloud_shadow_col_r = 271,
        sky_cloud_shadow_col_g = 272,
        sky_cloud_shadow_col_b = 273,
        sky_cloud_shadow_strength = 274,
        sky_cloud_gen_density_offset = 275,
        sky_cloud_offset = 276,
        sky_cloud_overall_strength = 277,
        sky_cloud_overall_color = 278,
        sky_cloud_edge_strength = 279,
        sky_cloud_fadeout = 280,
        sky_cloud_hdr = 281,
        sky_cloud_dither_strength = 282,
        sky_small_cloud_col_r = 283,
        sky_small_cloud_col_g = 284,
        sky_small_cloud_col_b = 285,
        sky_small_cloud_detail_strength = 286,
        sky_small_cloud_detail_scale = 287,
        sky_small_cloud_density_mult = 288,
        sky_small_cloud_density_bias = 289,
        cloud_shadow_density = 290,
        cloud_shadow_softness = 291,
        cloud_shadow_opacity = 292,
        dir_shadow_num_cascades = 293,
        dir_shadow_distance_multiplier = 294,
        dir_shadow_softness = 295,
        dir_shadow_cascade0_scale = 296,
        sprite_brightness = 297,
        sprite_size = 298,
        sprite_corona_screenspace_expansion = 299,
        Lensflare_visibility = 300,
        sprite_distant_light_twinkle = 301,
        water_reflection = 302,
        water_reflection_far_clip = 303,
        water_reflection_lod = 304,
        water_reflection_sky_flod_range = 305,
        water_reflection_lod_range_enabled = 306,
        water_reflection_lod_range_hd_start = 307,
        water_reflection_lod_range_hd_end = 308,
        water_reflection_lod_range_orphanhd_start = 309,
        water_reflection_lod_range_orphanhd_end = 310,
        water_reflection_lod_range_lod_start = 311,
        water_reflection_lod_range_lod_end = 312,
        water_reflection_lod_range_slod1_start = 313,
        water_reflection_lod_range_slod1_end = 314,
        water_reflection_lod_range_slod2_start = 315,
        water_reflection_lod_range_slod2_end = 316,
        water_reflection_lod_range_slod3_start = 317,
        water_reflection_lod_range_slod3_end = 318,
        water_reflection_lod_range_slod4_start = 319,
        water_reflection_lod_range_slod4_end = 320,
        water_reflection_height_offset = 321,
        water_reflection_height_override = 322,
        water_reflection_height_override_amount = 323,
        water_reflection_distant_light_intensity = 324,
        water_reflection_corona_intensity = 325,
        water_foglight = 326,
        water_interior = 327,
        water_fogstreaming = 328,
        water_foam_intensity_mult = 329,
        water_drying_speed_mult = 330,
        water_specular_intensity = 331,
        mirror_reflection_local_light_intensity = 332,
        fog_start = 333,
        fog_near_col_r = 334,
        fog_near_col_g = 335,
        fog_near_col_b = 336,
        fog_near_col_a = 337,
        fog_col_r = 338,
        fog_col_g = 339,
        fog_col_b = 340,
        fog_col_a = 341,
        fog_sun_lighting_calc_pow = 342,
        fog_moon_col_r = 343,
        fog_moon_col_g = 344,
        fog_moon_col_b = 345,
        fog_moon_col_a = 346,
        fog_moon_lighting_calc_pow = 347,
        fog_east_col_r = 348,
        fog_east_col_g = 349,
        fog_east_col_b = 350,
        fog_east_col_a = 351,
        fog_density = 352,
        fog_falloff = 353,
        fog_base_height = 354,
        fog_alpha = 355,
        fog_horizon_tint_scale = 356,
        fog_hdr = 357,
        fog_haze_col_r = 358,
        fog_haze_col_g = 359,
        fog_haze_col_b = 360,
        fog_haze_density = 361,
        fog_haze_alpha = 362,
        fog_haze_hdr = 363,
        fog_haze_start = 364,
        fog_shape_bottom = 365,
        fog_shape_top = 366,
        fog_shape_log_10_of_visibility = 367,
        fog_shape_weight_0 = 368,
        fog_shape_weight_1 = 369,
        fog_shape_weight_2 = 370,
        fog_shape_weight_3 = 371,
        fog_shadow_amount = 372,
        fog_shadow_falloff = 373,
        fog_shadow_base_height = 374,
        fog_volume_light_range = 375,
        fog_volume_light_fade = 376,
        fog_volume_light_intensity = 377,
        fog_volume_light_size = 378,
        fogray_contrast = 379,
        fogray_intensity = 380,
        fogray_density = 381,
        fogray_nearfade = 382,
        fogray_farfade = 383,
        reflection_lod_range_start = 384,
        reflection_lod_range_end = 385,
        reflection_slod_range_start = 386,
        reflection_slod_range_end = 387,
        reflection_interior_range = 388,
        reflection_tweak_interior_amb = 389,
        reflection_tweak_exterior_amb = 390,
        reflection_tweak_emissive = 391,
        reflection_tweak_directional = 392,
        reflection_hdr_mult = 393,
        far_clip = 394,
        temperature = 395,
        particle_emissive_intensity_mult = 396,
        vfxlightning_intensity_mult = 397,
        vfxlightning_visibility = 398,
        particle_light_intensity_mult = 399,
        natural_ambient_multiplier = 400,
        artificial_int_ambient_multiplier = 401,
        fog_cut_off = 402,
        no_weather_fx = 403,
        no_gpu_fx = 404,
        no_rain = 405,
        no_rain_ripples = 406,
        fogvolume_density_scalar = 407,
        fogvolume_density_scalar_interior = 408,
        fogvolume_fog_scaler = 409,
        time_offset = 410,
        vehicle_dirt_mod = 411,
        wind_speed_mult = 412,
        entity_reject = 413,
        lod_mult = 414,
        enable_occlusion = 415,
        enable_shadow_occlusion = 416,
        render_exterior = 417,
        portal_weight = 418,
        light_falloff_mult = 419,
        lodlight_range_mult = 420,
        shadow_distance_mult = 421,
        lod_mult_hd = 422,
        lod_mult_orphanhd = 423,
        lod_mult_lod = 424,
        lod_mult_slod1 = 425,
        lod_mult_slod2 = 426,
        lod_mult_slod3 = 427,
        lod_mult_slod4 = 428,
    }
}

