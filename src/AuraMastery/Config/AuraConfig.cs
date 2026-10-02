using System;
using TaleWorlds.InputSystem;

namespace AuraMastery.Config
{
    public class AuraConfig
    {
        // Храним как строки — это надежнее для XML-сериализации
        public string ActivateKey { get; set; } = "V";
        public string ModifierKey { get; set; } = "Invalid";
        public string OpenMenuKey { get; set; } = "O";
        public string SwitchTechnique1 { get; set; } = "Numpad1";
        public string SwitchTechnique2 { get; set; } = "Numpad2";
        public string SwitchTechnique3 { get; set; } = "Numpad3";
        
        public bool UseModifierForActivation { get; set; } = false;
        public bool UseModifierForSwitching { get; set; } = false;
        
        public float ChargeIndicatorScale { get; set; } = 1.0f;
        public float DamageMultiplier { get; set; } = 1.0f;
        
        // Методы конвертации строки в InputKey
        private InputKey ParseKey(string keyName, InputKey fallback)
        {
            if (string.IsNullOrEmpty(keyName)) return fallback;
            try
            {
                return (InputKey)Enum.Parse(typeof(InputKey), keyName, true);
            }
            catch
            {
                return fallback;
            }
        }
        
        public InputKey GetActivateKey() => ParseKey(ActivateKey, InputKey.V);
        public InputKey GetModifierKey() => ParseKey(ModifierKey, InputKey.Invalid);
        public InputKey GetOpenMenuKey() => ParseKey(OpenMenuKey, InputKey.O);
        public InputKey GetSwitchTechnique1Key() => ParseKey(SwitchTechnique1, InputKey.Numpad1);
        public InputKey GetSwitchTechnique2Key() => ParseKey(SwitchTechnique2, InputKey.Numpad2);
        public InputKey GetSwitchTechnique3Key() => ParseKey(SwitchTechnique3, InputKey.Numpad3);
        
        public bool IsActivateKeyDown()
        {
            var mod = GetModifierKey();
            if (UseModifierForActivation && mod != InputKey.Invalid)
            {
                return Input.IsKeyDown(mod) && Input.IsKeyDown(GetActivateKey());
            }
            return Input.IsKeyDown(GetActivateKey());
        }
        
        public bool IsSwitchTechnique1Pressed()
        {
            var mod = GetModifierKey();
            if (UseModifierForSwitching && mod != InputKey.Invalid)
            {
                return Input.IsKeyDown(mod) && Input.IsKeyPressed(GetSwitchTechnique1Key());
            }
            return Input.IsKeyPressed(GetSwitchTechnique1Key());
        }
        
        public bool IsSwitchTechnique2Pressed()
        {
            var mod = GetModifierKey();
            if (UseModifierForSwitching && mod != InputKey.Invalid)
            {
                return Input.IsKeyDown(mod) && Input.IsKeyPressed(GetSwitchTechnique2Key());
            }
            return Input.IsKeyPressed(GetSwitchTechnique2Key());
        }
        
        public bool IsSwitchTechnique3Pressed()
        {
            var mod = GetModifierKey();
            if (UseModifierForSwitching && mod != InputKey.Invalid)
            {
                return Input.IsKeyDown(mod) && Input.IsKeyPressed(GetSwitchTechnique3Key());
            }
            return Input.IsKeyPressed(GetSwitchTechnique3Key());
        }
    }
}